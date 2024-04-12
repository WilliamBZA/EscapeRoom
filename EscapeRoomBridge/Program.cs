using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NserviceBus.Mqtt;
using NServiceBus;

namespace EscapeRoomBridge
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            await Host.CreateDefaultBuilder()
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddSimpleConsole(options =>
                {
                    options.IncludeScopes = false;
                    options.SingleLine = true;
                    options.TimestampFormat = "hh:mm:ss ";
                });
            })
            .UseNServiceBusBridge((ctx, bridgeConfiguration) =>
            {
                var escapeRoom = new BridgeTransport(new MqttTransport("192.168.88.114"));
                escapeRoom.HasEndpoint("escaperoom");


                var rabbitBridge = new BridgeTransport(new RabbitMQTransport(RoutingTopology.Conventional(QueueType.Quorum), "host=localhost"));
                rabbitBridge.HasEndpoint("EscapeRoomManager");
                rabbitBridge.AutoCreateQueues = true;

                bridgeConfiguration.AddTransport(escapeRoom);
                bridgeConfiguration.AddTransport(rabbitBridge);
            })
            .Build()
            .RunAsync().ConfigureAwait(false);
        }
    }
}
