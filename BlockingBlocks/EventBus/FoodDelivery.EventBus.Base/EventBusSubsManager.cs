using FoodDelivery.EventBus.Base.Infrastructure;
using FoodDelivery.EventBus.Base.Infrastructure.Event;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoodDelivery.EventBus.Base
{
    public class EventBusSubsManager : IEventBusSubsManager
    {

        Dictionary<string, List<Type>> _eventHandler;

        public EventBusSubsManager()
        {
            _eventHandler = new Dictionary<string, List<Type>>();
        }

        public void AddSubs<T, TH>()
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>
        {
            var eventName = GetEventName<T>();

            var eventHandlerType = GetEventHandler<TH>();


            var eventHandlerTypeHasRegistered = _eventHandler[eventName].Any(s => s == eventHandlerType);

            if (eventHandlerTypeHasRegistered)
            {
                throw new ArgumentException($"{eventHandlerType.Name} has already registered in {eventName}");
            }

            if (!HasSubsForEvent<T>())
            {
                _eventHandler.Add(eventName, new List<Type>());
            }

            _eventHandler[eventName].Add(eventHandlerType);
        }

        public void Clear()
        {
            _eventHandler.Clear();
        }

        public bool HasSubsForEvent<T>() where T : IntegrationEvent
        {
            var eventName = GetEventName<T>();

            return _eventHandler.ContainsKey(eventName);
        }

        public bool HasSubsForEvent(string eventName)
        {
            return _eventHandler.ContainsKey(eventName);
        }

        public void RemoveSubs<T, TH>()
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>
        {
            var eventName = GetEventName<T>();

            var eventHandlerType = GetEventHandler<TH>();

            if (HasSubsForEvent<T>())
            {
                var removedHandlerType = _eventHandler[eventName].Single(r => r == eventHandlerType);

                _eventHandler[eventName].Remove(removedHandlerType);

                RemoveEvent(eventName);
            }
        }


        public string GetEventName<T>()
        {
            return typeof(T).Name;
        }


        private Type GetEventHandler<TH>()
        {
            return typeof(TH);
        }

        private void RemoveEvent(string eventName)
        {
            if (!_eventHandler[eventName].Any())
            {
                _eventHandler.Remove(eventName);
            }
        }

        public IEnumerable<Type> GetHandlersForEvent(string eventName)
        {
            return _eventHandler[eventName];
        }

        public Type GetEventTypeByName(string eventName)
        {
            return Type.GetType(eventName);
        } 

    }
}

