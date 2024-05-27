using MQTTnet.Client;
using MQTTnet;
using NServiceBus.Transport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MQTTnet.Server;
using System.Text.Json;
using MQTTnet.Protocol;
using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;

namespace NserviceBus.Mqtt
{
    class MqttDispatcher(string server, int port, MqttSubscriptionManager subscriptionStore) : IMessageDispatcher
    {
        public string Server { get; } = server;

        public int Port { get; } = port;

        public async Task Dispatch(TransportOperations outgoingMessages, TransportTransaction transaction, CancellationToken cancellationToken = default)
        {
            foreach (var msg in outgoingMessages.UnicastTransportOperations)
            {
                await PublishMessage(msg, cancellationToken);
            }

            foreach (var msg in outgoingMessages.MulticastTransportOperations.SelectMany(ConvertToUnicastMessage))
            {
                if (msg != null)
                {
                    await PublishMessage(msg, cancellationToken);
                }
            }
        }

        private IEnumerable<UnicastTransportOperation?> ConvertToUnicastMessage(MulticastTransportOperation message)
        {
            var subscribers = subscriptionStore.GetSubscribers(message.MessageType);

            return subscribers.Select(subscription => new UnicastTransportOperation(message.Message, subscription, message.Properties, message.RequiredDispatchConsistency));
        }

        private async Task PublishMessage(UnicastTransportOperation message, CancellationToken cancellationToken)
        {
            var wrapper = new MessageWrapper { Body = message.Message.Body.ToArray(), Headers = message.Message.Headers, Id = message.Message.MessageId };

            if (!wrapper.Headers.ContainsKey(Headers.MessageId) || string.IsNullOrEmpty(wrapper.Headers[Headers.MessageId]))
            {
                wrapper.Headers[Headers.MessageId] = message.Message.MessageId;
            }

            var outgoingMessage = new MqttApplicationMessageBuilder()
                        .WithTopic(message.Destination)
                        .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                        .WithPayload(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(wrapper)))
            .Build();

            if (MqttMessagePump.client != null)
            {
                await MqttMessagePump.client.PublishAsync(outgoingMessage, cancellationToken);
            }
        }

        MqttSubscriptionManager subscriptionStore = subscriptionStore;
    }
}
