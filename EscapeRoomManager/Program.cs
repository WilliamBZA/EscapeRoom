using NServiceBus;

namespace EscapeRoomManager
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // {"Id":"9222e65a-dbae-4e4e-a654-b1780074522b","Headers":{"NServiceBus.EnclosedMessageTypes":"ToneLockSolved"},"Body":"eyJSdW5JZCI6IjEyMyJ9"}
            var endpointConfiguration = new EndpointConfiguration("EscapeRoomManager");
            var routing = endpointConfiguration.UseTransport(new RabbitMQTransport(RoutingTopology.Conventional(QueueType.Quorum), "host=localhost"));

            routing.RouteToEndpoint(typeof(RunStarted), "EscapeRoomManager");
            routing.RouteToEndpoint(typeof(RunStarted), "escaperoom");

            endpointConfiguration.SendFailedMessagesTo("error");
            endpointConfiguration.EnableInstallers();
            endpointConfiguration.UsePersistence<LearningPersistence>();
            endpointConfiguration.UseSerialization<SystemJsonSerializer>();

            var endpointInstance = await Endpoint.Start(endpointConfiguration);


            Console.WriteLine("Press any key to start saga");
            Console.ReadKey();

            await endpointInstance.Publish(new RunStarted { RunId = "123", TeamName = "Testing" });

            Console.WriteLine("Press any key to exit");
            Console.ReadKey();

            await endpointInstance.Stop();
        }
    }
}