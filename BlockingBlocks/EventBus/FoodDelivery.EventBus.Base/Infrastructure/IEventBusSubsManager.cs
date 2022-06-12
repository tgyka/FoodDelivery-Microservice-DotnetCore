using FoodDelivery.EventBus.Base.Infrastructure.Event;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoodDelivery.EventBus.Base.Infrastructure
{
    public interface IEventBusSubsManager
    {
        void AddSubs<T, TH>()
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>;

        void RemoveSubs<T, TH>()
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>;

        void Clear();

        bool HasSubsForEvent<T>() where T : IntegrationEvent;

        bool HasSubsForEvent(string eventName);

        string GetEventName<T>();

        IEnumerable<Type> GetHandlersForEvent(string eventName);

        Type GetEventTypeByName(string eventName);



    }
}
