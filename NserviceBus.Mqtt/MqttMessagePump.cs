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
    public class MqttMessagePump(string id, string receiveAddress, List<string> topicsToSubscribeTo, string server, int port, Action<string, Exception, CancellationToken> onCritical) : IMessageReceiver, IDisposable
    {
        static SemaphoreSlim connectionSemaphore = new SemaphoreSlim(1, 1);

        public string Server { get; } = server;

        public int Port { get; } = port;

        public List<string> TopicsToSubscribeTo { get; } = topicsToSubscribeTo;

        public string Id { get; } = id;

        public string ReceiveAddress { get; } = receiveAddress;

        public ISubscriptionManager? Subscriptions => new MqttSubscriptionManager(this);

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

            client.ApplicationMessageReceivedAsync += async e =>
            {
                Logger.Info($"Message receieved on topic '{e.ApplicationMessage.Topic}'");
                var context = new ContextBag();

                if (onMessage != null)
                {
                    for (int processingAttempt = 1; true; processingAttempt++)
                    {
                        try
                        {
                            var message = UnwrapMessage(e.ApplicationMessage.PayloadSegment, context);
                            try
                            {
                                await onMessage(message, messageProcessingCancellationTokenSource.Token);
                                await e.AcknowledgeAsync(messageProcessingCancellationTokenSource.Token);
                                break;
                            }
                            catch (Exception ex) when (!ex.IsCausedBy(messageProcessingCancellationTokenSource.Token))
                            {
                                Logger.Error("Message processing failed.", ex);
                                var unwrappedAgain = UnwrapMessage(e.ApplicationMessage.PayloadSegment, context);

                                var errorContext = new ErrorContext(ex, unwrappedAgain.Headers, message.NativeMessageId, message.Body, message.TransportTransaction, processingAttempt, ReceiveAddress, context);

                                try
                                {
                                    var result = await onError(errorContext, messageProcessingCancellationTokenSource.Token).ConfigureAwait(false);

                                    e.IsHandled = result != ErrorHandleResult.RetryRequired;
                                    e.ProcessingFailed = result == ErrorHandleResult.RetryRequired;

                                    if (e.IsHandled)
                                    {
                                        break;
                                    }
                                }
                                catch (Exception onErrorEx) when (!ex.IsCausedBy(messageProcessingCancellationTokenSource.Token))
                                {
                                    if (onCritical != null)
                                    {
                                        onCritical($"Failed to execute recoverability policy for message with native ID: `{message.NativeMessageId}`", onErrorEx, messageProcessingCancellationTokenSource.Token);
                                    }
                                    e.ProcessingFailed = true;
                                }
                            }
                        }
                        catch (Exception ex) when (!ex.IsCausedBy(messageProcessingCancellationTokenSource.Token))
                        {
                            Logger.Error("Could not deserialize message", ex);
                            // poison message, discard.
                            break;
                        }
                    }
                }
            };

            var subscribeOptions = new MqttTopicFilterBuilder()
                .WithTopic(ReceiveAddress)
                .WithAtLeastOnceQoS()
                .Build();
            await client.SubscribeAsync(subscribeOptions, messagePumpCancellationTokenSource.Token);

            foreach (var topic in topicsToSubscribeTo)
            {
                await SubscribeToTopic(topic);
            }

            connectionSemaphore.Release();
        }

        public async Task SubscribeToTopic(string topic)
        {
            if (client is not null)
            {
                var result = await client.SubscribeAsync(new MqttTopicFilterBuilder()
                        .WithTopic(topic)
                        .WithAtLeastOnceQoS()
                        .Build(), messagePumpCancellationTokenSource.Token);
            }
            else
            {
                topicsToSubscribeTo.Add(topic);
            }
        }

        private MessageContext UnwrapMessage(ArraySegment<byte> payload, ContextBag contextBag)
        {
            var wrappedMessage = JsonSerializer.Deserialize<MessageWrapper>(payload);
            if (wrappedMessage == null)
            {
                throw new ArgumentException("Unable to deserialize the payload");
            }

            wrappedMessage.Id = wrappedMessage.Headers.ContainsKey(NServiceBus.Headers.MessageId) ? wrappedMessage.Headers[NServiceBus.Headers.MessageId] : Guid.NewGuid().ToString();

            if (!wrappedMessage.Headers.ContainsKey(NServiceBus.Headers.MessageId))
            {
                wrappedMessage.Headers[NServiceBus.Headers.MessageId] = wrappedMessage.Id;
            }

            return new MessageContext(wrappedMessage.Id, wrappedMessage.Headers, wrappedMessage.Body, new TransportTransaction(), ReceiveAddress, contextBag);
        }

        public Task StopReceive(CancellationToken cancellationToken = default)
        {
            using (cancellationToken.Register(() => messageProcessingCancellationTokenSource?.Cancel()))
            {
            }

            if (messagePumpCancellationTokenSource is not null && !messagePumpCancellationTokenSource.IsCancellationRequested)
            {
                messagePumpCancellationTokenSource?.Cancel();
                messagePumpCancellationTokenSource?.Dispose();
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
        Action<string, Exception, CancellationToken>? onCritical = onCritical;
        CancellationTokenSource? messagePumpCancellationTokenSource;
        CancellationTokenSource? messageProcessingCancellationTokenSource;

        public IMqttClient? client;

        bool disposed = false;
        static readonly ILog Logger = LogManager.GetLogger(typeof(MqttMessagePump));
    }
}