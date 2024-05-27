using NserviceBus.Mqtt;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class ConfigureEndpointMqttTransport : IConfigureEndpointTestExecution
{
    public Task Cleanup()
    {
        return Task.CompletedTask;
    }

    public Task Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings, PublisherMetadata publisherMetadata)
    {
        var transport = new MqttTransport("localhost");

        configuration.UseTransport(transport);

        return Task.CompletedTask;
    }
}