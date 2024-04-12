using NServiceBus;

namespace EscapeRoomManager
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var endpointConfiguration = new EndpointConfiguration("EscapeRoomManager");
            endpointConfiguration.UseTransport(new RabbitMQTransport(RoutingTopology.Conventional(QueueType.Quorum), "host=localhost"));

            endpointConfiguration.SendFailedMessagesTo("error");
            endpointConfiguration.EnableInstallers();
            endpointConfiguration.UsePersistence<LearningPersistence>();
            endpointConfiguration.UseSerialization<SystemJsonSerializer>();

            var endpointInstance = await Endpoint.Start(endpointConfiguration);


            Console.WriteLine("Press any key to exit");
            Console.ReadKey();

            await endpointInstance.SendLocal(new RunStarted { RunId = "123", TeamName = "Testing" });

            Console.WriteLine("Press any key to exit");
            Console.ReadKey();

            await endpointInstance.Stop();
        }
    }
}