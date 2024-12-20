﻿using RabbitMQ.Client;

namespace Juice.EventBus.RabbitMQ
{
    public interface IRabbitMQPersistentConnection
        : IDisposable
    {
        bool IsConnected { get; }

        bool TryConnect();

        IModel? CreateModel();
    }
}
