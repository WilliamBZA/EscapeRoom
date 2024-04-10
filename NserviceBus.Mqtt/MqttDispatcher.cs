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

        public async Task ConnectToBroker()
        {
            var mqttFactory = new MqttFactory();
            client = mqttFactory.CreateMqttClient();

            var clientOptions = new MqttClientOptionsBuilder()
                .WithTcpServer(Server, Port)
                .WithCleanStart(false)
                .Build();

            connected = true;

            await client.ConnectAsync(clientOptions);
        }

        public async Task Dispatch(TransportOperations outgoingMessages, TransportTransaction transaction, CancellationToken cancellationToken = default)
        {
            if (!connected)
            {
                await ConnectToBroker();
            }

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

            if (client != null)
            {
                await client.PublishAsync(outgointMessage, cancellationToken);
            }
        }

        IMqttClient? client;
        bool connected = false;
    }
}
