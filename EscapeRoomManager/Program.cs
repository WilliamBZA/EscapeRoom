using NserviceBus.Mqtt;
using NServiceBus;

namespace EscapeRoomManager
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var endpointConfiguration = new EndpointConfiguration("EscapeRoomHelper");
            endpointConfiguration.UseTransport(new MqttTransport("broker"));

            endpointConfiguration.SendFailedMessagesTo("error");
            endpointConfiguration.EnableInstallers();
            endpointConfiguration.UsePersistence<LearningPersistence>();
            endpointConfiguration.UseSerialization<SystemJsonSerializer>();
            endpointConfiguration.Recoverability().Delayed(d => d.NumberOfRetries(0));
            endpointConfiguration.Recoverability().Immediate(d => d.NumberOfRetries(3));

            var endpointInstance = await Endpoint.Start(endpointConfiguration);

            await endpointInstance.SendLocal(new RunStarted { RunId = "123" });

            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
            await endpointInstance.Stop();
        }
    }
}