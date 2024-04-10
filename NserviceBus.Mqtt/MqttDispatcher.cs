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

namespace NserviceBus.Mqtt
{
    class MqttDispatcher(string server, int port) : IMessageDispatcher
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

            yield return null;
        }

        private async Task PublishMessage(UnicastTransportOperation message, CancellationToken cancellationToken)
        {
            var wrapper = new MessageWrapper { Body = message.Message.Body.ToArray(), Headers = message.Message.Headers, Id = message.Message.MessageId };

            var outgointMessage = new MqttApplicationMessageBuilder()
                        .WithTopic(message.Destination)
                        .WithPayload(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(wrapper)))
            .Build();

            if (MqttMessagePump.client != null)
            {
                await MqttMessagePump.client.PublishAsync(outgointMessage, cancellationToken);
            }
        }

        IMqttClient? cliednt;
        bool connected = false;
    }
}
