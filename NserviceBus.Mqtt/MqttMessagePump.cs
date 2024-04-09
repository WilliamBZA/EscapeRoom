using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Server;
using NServiceBus.Transport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace NserviceBus.Mqtt
{
    class MqttMessagePump : IMessageReceiver, IDisposable
    {
        public MqttMessagePump(string id, string receiveAddress, ISubscriptionManager subscriptionManager, string server, int port)
        {
            Id = id;
            ReceiveAddress = receiveAddress;
            Subscriptions = subscriptionManager;
            Server = server;
            Port = port;
        }

        public string Server { get; }

        public int Port { get; }

        public ISubscriptionManager Subscriptions { get; }

        public string Id { get; }

        public string ReceiveAddress { get; }

        public Task ChangeConcurrency(PushRuntimeSettings limitations, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task Initialize(PushRuntimeSettings limitations, OnMessage onMessage, OnError onError, CancellationToken cancellationToken = default)
        {
            this.onMessage = onMessage;
            this.onError = onError;

            return Task.CompletedTask;
        }

        public async Task StartReceive(CancellationToken cancellationToken = default)
        {
            messagePumpCancellationTokenSource = new CancellationTokenSource();
            messageProcessingCancellationTokenSource = new CancellationTokenSource();

            await ConnectToBroker();
        }

        async Task ConnectToBroker()
        {
            var mqttFactory = new MqttFactory();
            client = mqttFactory.CreateMqttClient();

            var clientOptions = new MqttClientOptionsBuilder()
                .WithTcpServer(Server, Port)
                .WithCleanStart(false)
            .Build();

            client.ApplicationMessageReceivedAsync += e =>
            {
                Console.WriteLine($"Received application message on topic '{e.ApplicationMessage.Topic}':");
                Console.WriteLine($"\t'{Encoding.Default.GetString(e.ApplicationMessage.PayloadSegment)}'");

                e.

                return Task.CompletedTask;
            };

            await client.ConnectAsync(clientOptions, CancellationToken.None);
            await client.SubscribeAsync(ReceiveAddress, MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce, CancellationToken.None);

            // Todo: subscribe to other that the endpoint is subscribed to
        }

        public Task StopReceive(CancellationToken cancellationToken = default)
        {
            if (messagePumpCancellationTokenSource is not null)
            {
                messagePumpCancellationTokenSource?.Cancel();
                messagePumpCancellationTokenSource?.Dispose();
                messagePumpCancellationTokenSource = null;

                messageProcessingCancellationTokenSource?.Dispose();
                client?.Dispose();
            }

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    client?.Dispose();
                    client = null;

                    messagePumpCancellationTokenSource?.Cancel();
                    messagePumpCancellationTokenSource?.Dispose();
                    messagePumpCancellationTokenSource = null;

                    messageProcessingCancellationTokenSource?.Dispose();
                }

                disposed = true;
            }
        }

        OnMessage onMessage;
        OnError onError;
        CancellationTokenSource messagePumpCancellationTokenSource;
        CancellationTokenSource messageProcessingCancellationTokenSource;

        IMqttClient client;

        bool disposed = false;
    }
}