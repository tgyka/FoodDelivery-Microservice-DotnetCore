using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoodDelivery.EventBus.RabbitMQ.Infrastructure
{

    public interface IRabbitMQConnectionManager : IDisposable
    {
        bool isConnected { get; }

        bool TryConnect();

        IModel CreateModel();
    }
}
