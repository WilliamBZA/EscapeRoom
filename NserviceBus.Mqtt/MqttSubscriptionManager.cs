using NServiceBus;
using NServiceBus.Extensibility;
using NServiceBus.Transport;
using NServiceBus.Unicast.Messages;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NserviceBus.Mqtt
{
    class MqttSubscriptionManager : ISubscriptionManager
    {
        public MqttSubscriptionManager(MqttMessagePump messagePump)
        {
            this.messagePump = messagePump;
        }

        public async Task SubscribeAll(MessageMetadata[] eventTypes, ContextBag context, CancellationToken cancellationToken = default)
        {
            foreach (var @event in eventTypes)
            {
                await messagePump.SubscribeToTopic($"events/{@event.MessageType.Name}");
            }
        }

        public Task Unsubscribe(MessageMetadata eventType, ContextBag context, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        MqttMessagePump messagePump;
    }
}
