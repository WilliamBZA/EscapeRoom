using NServiceBus;

namespace HotWheels
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var endpointConfiguration = new EndpointConfiguration("HotWheels");
            var routing = endpointConfiguration.UseTransport(new RabbitMQTransport(RoutingTopology.Conventional(QueueType.Quorum), "host=192.168.88.114; username=escaperoom; password=escaperoom;"));
            routing.RouteToEndpoint(typeof(LowerDrawbridge), "HotWheels");

            endpointConfiguration.SendFailedMessagesTo("error");
            endpointConfiguration.EnableInstallers();
            endpointConfiguration.UsePersistence<NonDurablePersistence>();
            endpointConfiguration.UseSerialization<SystemJsonSerializer>();
            endpointConfiguration.PurgeOnStartup(true);

            var endpointInstance = await Endpoint.Start(endpointConfiguration);

            await endpointInstance.SendLocal(new StartHotWheelsTrack { Event = "NDC Oslo" });

            Console.WriteLine("Press any key to exit");
            Console.ReadKey();

            await endpointInstance.Stop();
        }
    }
}