using NserviceBus.Mqtt;
using NServiceBus.Logging;
using NServiceBus.Mqtt.Tests;
using NServiceBus.Transport;
using NServiceBus.TransportTests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class ConfigureMqttTransportInfrastructure : IConfigureTransportInfrastructure
{
    public Task Cleanup(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public async Task<TransportInfrastructure> Configure(TransportDefinition transportDefinition, HostSettings hostSettings, QueueAddress inputQueueName, string errorQueueName, CancellationToken cancellationToken = default)
    {
        var transportInfrastructure = await transportDefinition.Initialize(
        hostSettings,
        new[]
        {
            new ReceiveSettings(inputQueueName.ToString(), inputQueueName, true, false, errorQueueName),
        },
        Array.Empty<string>(),
        cancellationToken);

        return transportInfrastructure;
    }

    public TransportDefinition CreateTransportDefinition()
    {
        LogManager.UseFactory(new ConsoleLoggerFactory());

        var transport = new MqttTransport("localhost", 1883);

        return transport;
    }
}