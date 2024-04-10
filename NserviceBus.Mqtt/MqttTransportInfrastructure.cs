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

        public void ConfigureReceiveInfrastructure()
        {
            var receivers = new Dictionary<string, IMessageReceiver>();

            foreach (var receiverSetting in receiverSettings)
            {
                receivers.Add(receiverSetting.Id, CreateReceiver(receiverSetting));
            }

            Receivers = receivers;
        }

        public IMessageReceiver CreateReceiver(ReceiveSettings receiveSettings)
        {
            var errorQueueAddress = receiveSettings.ErrorQueue;
            var queueAddress = ToTransportAddress(receiveSettings.ReceiveAddress);

            ISubscriptionManager? subscriptionManager = null;
            if (receiveSettings.UsePublishSubscribe)
            {
                subscriptionManager = new MqttSubscriptionManager();
            }

            var pump = new MqttMessagePump(receiveSettings.Id, queueAddress, subscriptionManager, Server, Port);
            return pump;
        }

        public void ConfigureSendInfrastructure()
        {
            Dispatcher = new MqttDispatcher(Server, Port);
            
        }

        public override async Task Shutdown(CancellationToken cancellationToken = default)
        {
            await Task.WhenAll(Receivers.Values.Select(r => r.StopReceive(cancellationToken)));
        }

        public override string ToTransportAddress(QueueAddress address)
        {
            return address.BaseAddress;
        }

        readonly HostSettings settings;
        readonly ReceiveSettings[] receiverSettings;
        readonly MqttTransport transport;
    }
}