using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Server;
using NServiceBus;
using NServiceBus.Extensibility;
using NServiceBus.Logging;
using NServiceBus.Transport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace NserviceBus.Mqtt
{
    public class MqttMessagePump(string id, string receiveAddress, ISubscriptionManager? subscriptionManager, string server, int port) : IMessageReceiver, IDisposable
    {
        public string Server { get; } = server;

        public int Port { get; } = port;

        public ISubscriptionManager? Subscriptions { get; } = subscriptionManager;

        public string Id { get; } = id;

        public string ReceiveAddress { get; } = receiveAddress;

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

            client.ApplicationMessageReceivedAsync += async e =>
            {
                var context = new ContextBag();

                var message = UnwrapMessage(e.ApplicationMessage.PayloadSegment, context);
                if (onMessage != null)
                {
                    try
                    {
                        await onMessage(message);
                    }
                    catch (Exception ex) when (!(ex is OperationCanceledException || (messagePumpCancellationTokenSource != null && messagePumpCancellationTokenSource.IsCancellationRequested)))
                    {
                        Logger.Error("Message processing failed.", ex);
                        var errorContext = new ErrorContext(ex, message.Headers, message.NativeMessageId, message.Body, message.TransportTransaction, 1, ReceiveAddress, context);

                        try
                        {
                            var result = await onError(errorContext, messageProcessingCancellationTokenSource.Token).ConfigureAwait(false);

                            if (result == ErrorHandleResult.RetryRequired)
                            {
                                e.ProcessingFailed = true;
                                return;
                            }
                        }
                        catch (Exception onErrorEx)
                        {
                            e.ProcessingFailed = true;

                            return;
                        }
                    }
                }
            };

            await client.ConnectAsync(clientOptions, CancellationToken.None);
            await client.SubscribeAsync(ReceiveAddress, MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce, CancellationToken.None);

            // Todo: subscribe to other that the endpoint is subscribed to
        }

        private MessageContext UnwrapMessage(ArraySegment<byte> payload, ContextBag contextBag)
        {
            var wrappedMessage = JsonSerializer.Deserialize<MessageWrapper>(payload);
            if (wrappedMessage == null)
            {
                throw new ArgumentException("Unable to deserialize the payload");
            }

            wrappedMessage.Id = wrappedMessage.Headers.ContainsKey(NServiceBus.Headers.MessageId) ? wrappedMessage.Headers[NServiceBus.Headers.MessageId] : Guid.Empty.ToString();

            return new MessageContext(wrappedMessage.Id, wrappedMessage.Headers, wrappedMessage.Body, new TransportTransaction(), ReceiveAddress, contextBag);
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

        OnMessage? onMessage;
        OnError? onError;
        CancellationTokenSource? messagePumpCancellationTokenSource;
        CancellationTokenSource? messageProcessingCancellationTokenSource;

        public static IMqttClient? client;

        bool disposed = false;
        static readonly ILog Logger = LogManager.GetLogger(typeof(MqttMessagePump));
    }
}