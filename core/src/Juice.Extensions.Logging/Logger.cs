﻿using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Juice.Extensions.Logging
{
    internal class Logger : ILogger, IDisposable
    {
        /* properties */
        /// <summary>
        /// The logger provider who created this instance
        /// </summary>
        public LoggerProvider Provider { get; private set; }
        /// <summary>
        /// The category this instance serves.
        /// <para>The category is usually the fully qualified class name of a class asking for a logger, e.g. MyNamespace.MyClass </para>
        /// </summary>
        public string Category { get; private set; }
        public Logger(LoggerProvider provider, string category)
        {
            Provider = provider;
            Category = category;
        }

        private IExternalScopeProvider? _scopeProvider;
        private bool _disposedValue;

        public IExternalScopeProvider ScopeProvider
        {
            get
            {
                if (_scopeProvider == null)
                {
                    _scopeProvider = new LoggerExternalScopeProvider();
                }
                return _scopeProvider;
            }
        }

        public IDisposable? BeginScope<TState>(TState state) where TState: notnull
        {
            var scope = BeginScopeInternal(state);
            Provider.ScopeStarted(Category, state, _scopeProvider);
            return new ScopeWrapper<TState>(scope, state, this);
        }

        private IDisposable BeginScopeInternal<TState>(TState state)
        {
            if (Provider is ExternalScopeLoggerProvider supportExternalScope)
            {
                return supportExternalScope.ScopeProvider.Push(state);
            }
            return ScopeProvider.Push(state);
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var logEntry = new LogEntry<TState>(logLevel, Category, eventId, state, exception, formatter);
            Provider.WriteLog(logEntry, formatter(state, exception), _scopeProvider);
        }

        public void ScopeDisposed<TState>(TState state)
        {
            Provider.ScopeDisposed(Category, state, _scopeProvider);
        }

        #region IDisposable Support
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }
                _scopeProvider = null;
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

    internal class ScopeWrapper<TState> : IDisposable
    {
        private readonly IDisposable _scope;
        private readonly TState _state;
        private readonly Logger _logger;
        private bool _disposedValue;

        public ScopeWrapper(IDisposable scope, TState state, Logger logger)
        {
            _scope = scope;
            _state = state;
            _logger = logger;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    _scope.Dispose();
                    _logger.ScopeDisposed(_state);
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
