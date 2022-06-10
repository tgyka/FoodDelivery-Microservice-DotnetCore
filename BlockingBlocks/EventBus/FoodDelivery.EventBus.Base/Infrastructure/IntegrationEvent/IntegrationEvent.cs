using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FoodDelivery.EventBus.Base.Infrastructure.Event
{
    public class IntegrationEvent
    {
        public IntegrationEvent()
        {
            Id = new Guid();
            CreationDate = DateTime.UtcNow;
        }

        [JsonInclude]
        public Guid Id { get; set; }

        [JsonInclude]
        public DateTime CreationDate { get; set; }
    }
}
