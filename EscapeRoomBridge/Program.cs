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
                var escapeRoom = new BridgeTransport(new MqttTransport("localhost"));

                var iotEndpoint = new BridgeEndpoint("escaperoom/puzzles/tonelock");
                iotEndpoint.RegisterPublisher<RunStarted>("EscapeRoomManager");

                escapeRoom.HasEndpoint(iotEndpoint);
                escapeRoom.AutoCreateQueues = true;

                var escapeRoomManagerEndpoint = new BridgeEndpoint("EscapeRoomManager");
                escapeRoomManagerEndpoint.RegisterPublisher<PuzzleSolved>("escaperoom/puzzles/tonelock");

                var rabbitBridge = new BridgeTransport(new RabbitMQTransport(RoutingTopology.Conventional(QueueType.Quorum), "host=localhost"));
                rabbitBridge.HasEndpoint(escapeRoomManagerEndpoint);
                rabbitBridge.AutoCreateQueues = true;

                bridgeConfiguration.AddTransport(escapeRoom);
                bridgeConfiguration.AddTransport(rabbitBridge);
            })
            .Build()
            .RunAsync().ConfigureAwait(false);
        }
    }
}
