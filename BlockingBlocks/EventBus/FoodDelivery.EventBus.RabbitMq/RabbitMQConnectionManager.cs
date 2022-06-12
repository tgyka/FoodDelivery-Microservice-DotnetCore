using FoodDelivery.EventBus.RabbitMQ.Infrastructure;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace FoodDelivery.EventBus.RabbitMQ
{
    public class RabbitMQConnectionManager : IRabbitMQConnectionManager
    {
        private readonly IConnectionFactory _connectionFactory;
        private readonly int _retryCount;
        private readonly ILogger<RabbitMQConnectionManager> _logger;
        private IConnection _connection;
        private bool _disposed;


        public RabbitMQConnectionManager(IConnectionFactory connectionFactory , 
            ILogger<RabbitMQConnectionManager> logger,
            int retryCount = 3)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException($"{nameof(connectionFactory)} is not loaded");
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _retryCount = retryCount;
        }

        public bool isConnected
        {
            get
            {
                return _connection != null && _connection.IsOpen && !_disposed;
            }
        }

        public IModel CreateModel()
        {
            if(isConnected)
            {
                return _connection.CreateModel();
            }
            else
            {
                throw new InvalidOperationException("Rabbitmq connection is not connected");
            }
        }

        public bool TryConnect()
        {

            _logger.LogInformation("Rabbitmq trying to connected");

            lock (new object())
            {
                var policy = RetryPolicy
                    .Handle<SocketException>()
                    .Or<Exception>()
                    .WaitAndRetry(_retryCount, retryAttempt => TimeSpan.FromSeconds(5 * retryAttempt), (exception, time) =>
                    {
                        _logger.LogWarning($"RabbitMQ is not connected and retry after {time.TotalSeconds} and {exception.Message}");
                    });

                policy.Execute(() =>
                {
                    _connection = _connectionFactory.CreateConnection();
                });

                if(isConnected)
                {
                    _connection.ConnectionShutdown += OnConnectionShutdown;
                    _connection.CallbackException += OnCallbackException;
                    _connection.ConnectionBlocked += OnConnectionBlocked;

                    _logger.LogInformation("Rabbitmq is connected and all events are loaded");

                    return true;
                }
                else
                {
                    _logger.LogError("Rabbitmq is not connected and connection is not created");

                    return false;
                }

            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;

            try
            {
                _connection.ConnectionShutdown -= OnConnectionShutdown;
                _connection.CallbackException -= OnCallbackException;
                _connection.ConnectionBlocked -= OnConnectionBlocked;
                _connection.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex.ToString());

                throw;
            }
        }


        private void OnConnectionBlocked(object sender, ConnectionBlockedEventArgs e)
        {
            if (_disposed) return;

            _logger.LogWarning("A RabbitMQ connection is blocked. Trying to re-connect...");

            TryConnect();
        }

        private void OnCallbackException(object sender, CallbackExceptionEventArgs e)
        {
            if (_disposed) return;

            _logger.LogWarning("A RabbitMQ connection has exception. Trying to re-connect...");

            TryConnect();
        }

        private void OnConnectionShutdown(object sender, ShutdownEventArgs e)
        {
            if (_disposed) return;

            _logger.LogWarning("A RabbitMQ connection is shutdown. Trying to re-connect...");

            TryConnect();
        }


    }
}
