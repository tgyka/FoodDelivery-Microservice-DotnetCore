using FoodDelivery.EventBus.Base.Infrastructure.Event;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoodDelivery.EventBus.Base.Infrastructure
{
    public interface IEventBus : IDisposable
    {
        void Subscribe<T, TH>()
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>;

        void Unsubscribe<T, TH>()
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>;

        void Publish(IntegrationEvent @event);

    }
}
