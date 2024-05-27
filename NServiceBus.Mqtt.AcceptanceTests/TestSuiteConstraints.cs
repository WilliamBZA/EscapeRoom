namespace NServiceBus.AcceptanceTests
{
    using AcceptanceTesting.Support;

    public partial class TestSuiteConstraints
    {
        public bool SupportsDtc => false;
        public bool SupportsCrossQueueTransactions => false;
        public bool SupportsNativePubSub => true;
        public bool SupportsDelayedDelivery => false;
        public bool SupportsOutbox => false;
        public bool SupportsPurgeOnStartup => false;
        public IConfigureEndpointTestExecution CreateTransportConfiguration() => new ConfigureEndpointMqttTransport();
        public IConfigureEndpointTestExecution CreatePersistenceConfiguration() => new ConfigureEndpointAcceptanceTestingPersistence();
    }
}
