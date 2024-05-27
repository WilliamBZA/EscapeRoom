using NServiceBus.Transport;

namespace NserviceBus.Mqtt
{
    public class MqttTransport : TransportDefinition
    {
        public MqttTransport(string server, int port = 1883)
            : base(TransportTransactionMode.ReceiveOnly,
                supportsDelayedDelivery: false,
                supportsPublishSubscribe: true,
                supportsTTBR: false)
        {
            Server = server;
            Port = port;
            subscriptions = new List<string>();
        }

        public string Server { get; }

        public int Port { get; }

        public override IReadOnlyCollection<TransportTransactionMode> GetSupportedTransactionModes() => new[] { TransportTransactionMode.ReceiveOnly };

        public override async Task<TransportInfrastructure> Initialize(HostSettings hostSettings, ReceiveSettings[] receivers, string[] sendingAddresses, CancellationToken cancellationToken = default)
        {
            var infrastructure = new MqttTransportInfrastructure(hostSettings, this, receivers, Server, Port);

            infrastructure.ConfigureReceiveInfrastructure(subscriptions);
            await infrastructure.ConfigureSendInfrastructure();

            return infrastructure;
        }

        [Obsolete]
        public override string ToTransportAddress(QueueAddress address)
        {
            return address.BaseAddress;
        }

        public void SubscribeTo(string topic)
        {
            subscriptions.Add(topic);
        }

        List<string> subscriptions;
    }
}