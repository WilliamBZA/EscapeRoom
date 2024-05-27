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
    class MqttDispatcher(string server, int port) : IMessageDispatcher
    {
        static SemaphoreSlim connectionSemaphore = new SemaphoreSlim(1, 1);

        public string Server { get; } = server;

        public int Port { get; } = port;

        public async Task Dispatch(TransportOperations outgoingMessages, TransportTransaction transaction, CancellationToken cancellationToken = default)
        {
            foreach (var msg in outgoingMessages.UnicastTransportOperations)
            {
                await PublishMessage(msg, cancellationToken);
            }

            foreach (var msg in outgoingMessages.MulticastTransportOperations.Select(ConvertToUnicastMessage))
            {
                if (msg != null)
                {
                    await PublishMessage(msg, cancellationToken);
                }
            }
        }

        public async Task ConnectToMqttBroker()
        {
            await connectionSemaphore.WaitAsync();

            var mqttFactory = new MqttFactory();
            client = mqttFactory.CreateMqttClient();

            var clientOptions = new MqttClientOptionsBuilder()
                .WithTcpServer(Server, Port)
                .WithCleanStart(true)
                .Build();

            if (!client.IsConnected)
            {
                await client.ConnectAsync(clientOptions, CancellationToken.None);
            }

            connectionSemaphore.Release();
        }

        private UnicastTransportOperation? ConvertToUnicastMessage(MulticastTransportOperation message)
        {
            return new UnicastTransportOperation(message.Message, $"events/{message.MessageType.Name}", message.Properties, message.RequiredDispatchConsistency);
        }

        private async Task PublishMessage(UnicastTransportOperation message, CancellationToken cancellationToken)
        {
            var wrapper = new MessageWrapper { Body = message.Message.Body.ToArray(), Headers = message.Message.Headers, Id = message.Message.MessageId };

            if (!wrapper.Headers.ContainsKey(Headers.MessageId) || string.IsNullOrEmpty(wrapper.Headers[Headers.MessageId]))
            {
                wrapper.Headers[Headers.MessageId] = message.Message.MessageId;
            }

            var outgoingMessage = new MqttApplicationMessageBuilder()
                        .WithTopic(message.Destination.Replace("_", "/"))
                        .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                        .WithPayload(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(wrapper)))
            .Build();

            if (client != null)
            {
                await client.PublishAsync(outgoingMessage, cancellationToken);
            }
        }

        IMqttClient? client;
    }
}
