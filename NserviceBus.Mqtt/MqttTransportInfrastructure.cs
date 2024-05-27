using NServiceBus.Transport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.Tasks;

namespace NserviceBus.Mqtt
{
    class MqttTransportInfrastructure : TransportInfrastructure
    {
        public MqttTransportInfrastructure(HostSettings settings, MqttTransport transport, ReceiveSettings[] receiverSettings, string server, int port)
        {
            this.settings = settings;
            this.transport = transport;
            this.receiverSettings = receiverSettings;

            Server = server;
            Port = port;
        }

        public string Server { get; }

        public int Port { get; }

        public void ConfigureReceiveInfrastructure(List<string> subscribers)
        {
            var receivers = new Dictionary<string, IMessageReceiver>();

            foreach (var receiverSetting in receiverSettings)
            {
                receivers.Add(receiverSetting.Id, CreateReceiver(receiverSetting, subscribers));
            }

            Receivers = receivers;
        }

        public IMessageReceiver CreateReceiver(ReceiveSettings receiveSettings, List<string> subscribers)
        {
            var errorQueueAddress = receiveSettings.ErrorQueue;
            var queueAddress = ToTransportAddress(receiveSettings.ReceiveAddress);

            var pump = new MqttMessagePump(receiveSettings.Id, queueAddress, subscribers, Server, Port, settings.CriticalErrorAction);
            return pump;
        }

        public async Task ConfigureSendInfrastructure()
        {
            Dispatcher = new MqttDispatcher(Server, Port);

            await ((MqttDispatcher)Dispatcher).ConnectToMqttBroker();
        }

        public override async Task Shutdown(CancellationToken cancellationToken = default)
        {
            await Task.WhenAll(Receivers.Values.Select(r => r.StopReceive(cancellationToken)));
        }

        public override string ToTransportAddress(QueueAddress address)
        {
            return address.BaseAddress.Replace("_", "/");
        }

        readonly HostSettings settings;
        readonly ReceiveSettings[] receiverSettings;
        readonly MqttTransport transport;
    }
}