﻿using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace Juice.EventBus.RabbitMQ
{
    public class RabbitMQEventBus : EventBusBase, IDisposable
    {
        public string BROKER_NAME = "juice_event_bus";

        private IRabbitMQPersistentConnection _persistentConnection;

        private IModel? _consumerChannel;
        private string _queueName;
        private string _type;
        private readonly int _retryCount;

        private IServiceScopeFactory _scopeFactory;


        public RabbitMQEventBus(IEventBusSubscriptionsManager subscriptionsManager,
            IServiceScopeFactory scopeFactory,
            ILogger<RabbitMQEventBus> logger,
            IRabbitMQPersistentConnection mQPersistentConnection,
            IOptions<RabbitMQOptions> options
            )
            : base(subscriptionsManager, logger)
        {
            _persistentConnection = mQPersistentConnection;
            _queueName = options.Value.SubscriptionClientName ?? string.Empty;
            _type = options.Value.ExchangeType ?? "direct";
            if (!string.IsNullOrEmpty(options.Value.BrokerName))
            {
                BROKER_NAME = options.Value.BrokerName;
            }
            _consumerChannel = CreateConsumerChannel();
            _scopeFactory = scopeFactory;
            _retryCount = options.Value.RetryCount;
            SubsManager.OnEventRemoved += SubsManager_OnEventRemoved;
        }

        #region Init consume channel and processing incoming event

        private void SubsManager_OnEventRemoved(object? sender, string eventName)
        {
            if (!_persistentConnection.IsConnected && !_persistentConnection.TryConnect())
            {
                return;
            }
            using (var channel = _persistentConnection.CreateModel())
            {
                channel.QueueUnbind(queue: _queueName,
                    exchange: BROKER_NAME,
                    routingKey: eventName);

                Logger.LogInformation("Queue unbind {queueName}", _queueName);

                if (SubsManager.IsEmpty)
                {
                    _consumerChannel?.Close();
                }
            }

        }

        private async Task Consumer_ReceivedAsync(object sender, BasicDeliverEventArgs eventArgs)
        {
            var eventName = eventArgs.RoutingKey;
            var message = Encoding.UTF8.GetString(eventArgs.Body.Span);

            try
            {
                if (message.ToLowerInvariant().Contains("throw-fake-exception"))
                {
                    throw new InvalidOperationException($"Fake exception requested: \"{message}\"");
                }

                var processed = await ProcessingEventAsync(eventName, message);
                if (processed)
                {

                    // Even on exception we take the message off the queue.
                    // in a REAL WORLD app this should be handled with a Dead Letter Exchange (DLX). 
                    // For more information see: https://www.rabbitmq.com/dlx.html
                    _consumerChannel?.BasicAck(eventArgs.DeliveryTag, multiple: false);
                }
                else
                {
                    _consumerChannel?.BasicNack(eventArgs.DeliveryTag, multiple: true, requeue: true);
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "----- ERROR Processing message \"{0}\". {1}", message, ex.Message);
                Logger.LogTrace(ex.StackTrace);
            }

        }

        private void StartBasicConsume()
        {
            Logger.LogInformation("Starting RabbitMQ basic consume queue {queueName}.", _queueName);

            if (_consumerChannel != null)
            {
                var consumer = new AsyncEventingBasicConsumer(_consumerChannel);

                consumer.Received += Consumer_ReceivedAsync;

                _consumerChannel.BasicConsume(
                    queue: _queueName,
                    autoAck: false,
                    consumer: consumer);

                _consumerChannel.BasicQos(0, 1, false);
            }
            else
            {
                Logger.LogError("StartBasicConsume can't call on _consumerChannel == null");
            }
        }

        private IModel? CreateConsumerChannel()
        {
            if (!_persistentConnection.IsConnected && !_persistentConnection.TryConnect())
            {
                return null;
            }

            Logger.LogInformation("Creating RabbitMQ consumer channel. Broker: {Broker}.", BROKER_NAME);

            var channel = _persistentConnection.CreateModel();
            if (channel == null) { return null; }

            channel.ExchangeDeclare(exchange: BROKER_NAME,
                                    type: _type);

            var queuDeclareOk = channel.QueueDeclare(queue: _queueName,
                                 durable: true,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);

            if (_queueName == string.Empty && queuDeclareOk != null)
            {
                _queueName = queuDeclareOk.QueueName;
            }

            channel.CallbackException += (sender, ea) =>
            {
                Logger.LogWarning(ea.Exception, "Recreating RabbitMQ consumer channel");

                _consumerChannel?.Dispose();
                _consumerChannel = CreateConsumerChannel();
                if (_consumerChannel != null)
                {
                    StartBasicConsume();
                }
            };

            return channel;
        }

        private async Task<bool> ProcessingEventAsync(string eventName, string message)
        {
            using (Logger.BeginScope($"Processing integration event: {eventName}"))
            {
                if (SubsManager.HasSubscriptionsForEvent(eventName))
                {
                    using var scope = _scopeFactory.CreateScope();
                    var subscriptions = SubsManager.GetHandlersForEvent(eventName);
                    if (Logger.IsEnabled(LogLevel.Trace))
                    {
                        Logger.LogTrace("Found {count} handlers for event: {EventName}", subscriptions.Count(), eventName);
                    }

                    var eventType = SubsManager.GetEventTypeByName(eventName);
                    if (eventType == null)
                    {
                        Logger.LogWarning("Failed to get event type for event: {EventName}", eventName);
                        return false;
                    }
                    var concreteType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);

                    var integrationEvent = JsonConvert.DeserializeObject(message, eventType);
                    if (integrationEvent == null)
                    {
                        Logger.LogWarning("Failed to deserialize message to {eventType}", eventType.Name);
                        return false;
                    }

                    var ok = false;
                    foreach (var subscription in subscriptions)
                    {
                        if(!subscription.HandlerType.IsAssignableTo(concreteType))
                        {
                            Logger.LogWarning("Type {typeName} not assignable to {concreteType}", subscription.HandlerType.Name, concreteType.Name);

                            continue;
                        }
                        var handler = scope.ServiceProvider.GetService(subscription.HandlerType);
                        if (handler == null)
                        {
                            Logger.LogWarning("Type {typeName} not registered as a service", subscription.HandlerType.Name);

                            continue;
                        }
                        
                        try
                        {
                            await (Task)concreteType.GetMethod(nameof(IIntegrationEventHandler<IntegrationEvent>.HandleAsync))!.Invoke(handler, new object[] { integrationEvent! })!;
                            ok = true;
                        }
                        catch (Exception ex)
                        {
                            var eventId = integrationEvent != null ? ((IntegrationEvent)integrationEvent).Id : Guid.Empty;
                            Logger.LogError(ex, "{handler} failed to handle event: {EventName}, eventId: {eventId}", handler.GetGenericTypeName(), eventName, eventId);
                        }
                    }
                    return ok;
                }
                else
                {
                    Logger.LogDebug("No subscription for RabbitMQ event: {EventName}", eventName);
                    return false;
                }
            }
        }

        #endregion

        #region Subscribe/UnSubscribe
        public override void Subscribe<T, TH>(string? key = default)
        {
            var eventName = key ?? SubsManager.GetDefaultEventKey<T>();

            DoInternalSubscription(eventName);

            Logger.LogInformation("Subscribing to event {EventName} with {EventHandler}", eventName, typeof(TH).GetGenericTypeName());

            SubsManager.AddSubscription<T, TH>(key);

            StartBasicConsume();

        }

        private void DoInternalSubscription(string eventName)
        {
            var containsKey = SubsManager.HasSubscriptionsForEvent(eventName);
            if (!containsKey)
            {
                if (!_persistentConnection.IsConnected && !_persistentConnection.TryConnect())
                {
                    return;
                }

                _consumerChannel?.QueueBind(queue: _queueName,
                                exchange: BROKER_NAME,
                                routingKey: eventName);
            }

        }
        #endregion

        #region Publish outgoing event
        public override async Task PublishAsync(IntegrationEvent @event)
        {
            await Task.Yield();
            if (@event == null)
            {
                throw new ArgumentNullException("@event");
            }
            if (!_persistentConnection.IsConnected && !_persistentConnection.TryConnect())
            {
                throw new InvalidOperationException("RabbitMQ broker is not connected");
            }

            var policy = RetryPolicy.Handle<BrokerUnreachableException>()
                .Or<SocketException>()
                .WaitAndRetry(_retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                {
                    Logger.LogWarning(ex, "Could not publish event: {EventId} after {Timeout}s ({ExceptionMessage})", @event.Id, $"{time.TotalSeconds:n1}", ex.Message);
                });

            var eventName = @event.GetEventKey();

            if (Logger.IsEnabled(LogLevel.Trace))
            {
                Logger.LogTrace("Creating RabbitMQ channel to publish event: {EventId} ({EventName})", @event.Id, eventName);
            }

            using (var channel = _persistentConnection.CreateModel())
            {
                if (Logger.IsEnabled(LogLevel.Trace))
                {
                    Logger.LogTrace("Declaring RabbitMQ exchange to publish event: {EventId}", @event.Id);
                }

                if (channel == null) { return; }

                channel.ExchangeDeclare(exchange: BROKER_NAME, type: _type);

                var body = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(@event, @event.GetType(), new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                policy.Execute(() =>
                {
                    var properties = channel.CreateBasicProperties();
                    properties.DeliveryMode = 2; // persistent

                    if (Logger.IsEnabled(LogLevel.Debug))
                    {
                        Logger.LogDebug("Publishing event to RabbitMQ: {EventId} {EventName}", @event.Id, eventName);
                    }

                    channel.BasicPublish(
                        exchange: BROKER_NAME,
                        routingKey: eventName,
                        mandatory: true,
                        basicProperties: properties,
                        body: body);
                });
            }
        }

        #endregion

        #region Dispose
        private bool _disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // dispose managed state (managed objects)
                    _consumerChannel?.Dispose();
                    _consumerChannel = null!;
                    _persistentConnection?.Dispose();
                    _persistentConnection = null!;
                    SubsManager?.Clear();
                    _scopeFactory = null!;
                }

                // free unmanaged resources (unmanaged objects) and override finalizer
                // set large fields to null

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
