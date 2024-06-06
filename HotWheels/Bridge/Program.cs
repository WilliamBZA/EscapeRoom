using Messages;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NserviceBus.Mqtt;
using NServiceBus;

namespace Bridge
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

                var iotEndpoint = new BridgeEndpoint("escaperoom_puzzles_hotwheels");
                iotEndpoint.RegisterPublisher<CarReleased>("HotWheels");
                iotEndpoint.RegisterPublisher<DrawbridgeLowered>("HotWheels");

                escapeRoom.HasEndpoint(iotEndpoint);
                escapeRoom.AutoCreateQueues = true;

                var escapeRoomManagerEndpoint = new BridgeEndpoint("HotWheels");

                var rabbitBridge = new BridgeTransport(new RabbitMQTransport(RoutingTopology.Conventional(QueueType.Quorum), "host=192.168.88.114; username=escaperoom; password=escaperoom;"));
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
