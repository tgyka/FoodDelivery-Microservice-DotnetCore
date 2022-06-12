using FoodDelivery.EventBus.Base.Infrastructure;
using FoodDelivery.EventBus.Base.Infrastructure.Event;
using FoodDelivery.EventBus.RabbitMQ.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FoodDelivery.EventBus.RabbitMQ
{
    public class RabbitMQEventBus : IEventBus
    {

        private const string EXCHANGE_NAME = "fooddelivery-exchange";
        private readonly IRabbitMQConnectionManager _connectionManager;
        private readonly IEventBusSubsManager _subsManager;
        private readonly ILogger<RabbitMQEventBus> _logger;
        private IModel _consumerModel;
        private readonly string _queueName;
        private readonly IServiceProvider _serviceProvider;
        private readonly int _retryCount;

        public RabbitMQEventBus(IRabbitMQConnectionManager connectionManager,
            IEventBusSubsManager subsManager,
            ILogger<RabbitMQEventBus> logger,
            string queueName,
            IServiceProvider serviceProvider,
            int retryCount  = 3)
        {
            _connectionManager = connectionManager;
            _subsManager = subsManager;
            _logger = logger;
            _consumerModel = CreateConsumerModel();
            _queueName = queueName;
            _serviceProvider = serviceProvider;
            _retryCount = retryCount;
        }

        public void Subscribe<T, TH>()
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>
        {
            var eventName = _subsManager.GetEventName<T>();

            var hasSubsForEvent = _subsManager.HasSubsForEvent<T>();

            if (!hasSubsForEvent)
            {
                IsNotConnectedTryConnect();

                _consumerModel.QueueBind(
                    queue: _queueName,
                    exchange: EXCHANGE_NAME,
                    routingKey: eventName
                );
            }

            _subsManager.AddSubs<T, TH>();

            Consume();

        }


        public void Unsubscribe<T, TH>()
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>
        {
            
            var eventName = _subsManager.GetEventName<T>();

            _logger.LogInformation($"Unsubbing {eventName} ...");

            _subsManager.RemoveSubs<T, TH>();
        }

        public void Publish(IntegrationEvent @event)
        {
            IsNotConnectedTryConnect();

            var policy = RetryPolicy
                .Handle<BrokerUnreachableException>()
                .Or<SocketException>()
                .WaitAndRetry(_retryCount, retryAttempt => TimeSpan.FromSeconds(retryAttempt * 5), (ex, time) =>
                {
                    _logger.LogWarning($"Message didnt publish try again {time.TotalSeconds} error message: {ex.Message}");
                });

            var eventName = @event.GetType().Name;

            using var model = _connectionManager.CreateModel();

            model.ExchangeDeclare(
                exchange: EXCHANGE_NAME, 
                type: "direct");

            var body = JsonSerializer.SerializeToUtf8Bytes(@event, @event.GetType(), new JsonSerializerOptions
            {
                WriteIndented = true
            });

            policy.Execute(() =>
            {
                var properties = model.CreateBasicProperties();
                properties.DeliveryMode = 2; // persistent

                _logger.LogInformation($"Publishing event  {eventName}");

                model.BasicPublish(
                    exchange: EXCHANGE_NAME,
                    routingKey: eventName,
                    mandatory: true,
                    basicProperties: properties,
                    body: body);
            });
        }

        public void Dispose()
        {
            if(_consumerModel != null)
            {
                _consumerModel.Dispose();
            }

            _subsManager.Clear();
        }

        private IModel CreateConsumerModel()
        {
            IsNotConnectedTryConnect();

            _logger.LogInformation("Creating RabbitMQ consumer");

            var consumerModel = _connectionManager.CreateModel();

            consumerModel.ExchangeDeclare(
                exchange: EXCHANGE_NAME,
                type: "direct");

            consumerModel.QueueDeclare(
                queue: _queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null
            );

            consumerModel.CallbackException += Consumer_CallbackException;

            return consumerModel;
        }

        private void Consume()
        {
            _logger.LogInformation("Start consume process");

            if(_consumerModel != null)
            {
                var basicConsumer = new AsyncEventingBasicConsumer(_consumerModel);

                basicConsumer.Received += Consumer_Received;

                _consumerModel.BasicConsume(
                    queue: _queueName,
                    autoAck: false,
                    consumer: basicConsumer);
            }
            else
            {
                _logger.LogError("Consumer is null");
            }
        }

        private async Task Consumer_Received(object sender, BasicDeliverEventArgs @event)
        {
            var eventName = @event.RoutingKey;

            var data = Encoding.UTF8.GetString(@event.Body.Span);

            try
            {
                _logger.LogInformation($"{eventName} consume processing");

                if(_subsManager.HasSubsForEvent(eventName))
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var subs = _subsManager.GetHandlersForEvent(eventName);

                        foreach (var sub in subs)
                        {
                            var handler = _serviceProvider.GetService(sub);

                            if (handler == null) continue;

                            var eventType = _subsManager.GetEventTypeByName(eventName);

                            var integrationEvent = JsonSerializer.Deserialize(data, eventType, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                            var concreteType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);


                            await (Task)concreteType.GetMethod("Handle").Invoke(handler, new object[] { integrationEvent });
                        }
                    }
 
                }

            }
            catch(Exception e)
            {
                _logger.LogError($"Error while consumer received ,  message: {e.Message}");
            }
        }

        private void Consumer_CallbackException(object sender, global::RabbitMQ.Client.Events.CallbackExceptionEventArgs e)
        {
            _logger.LogWarning($"RabbitMQ consumer has exception. {e.Exception.Message} Recreating consumer...");

            _consumerModel.Dispose();

            _consumerModel = CreateConsumerModel();

            Consume();
        }

        private void IsNotConnectedTryConnect()
        {
            if (!_connectionManager.isConnected)
            {
                _connectionManager.TryConnect();
            }
        }
    }
}
