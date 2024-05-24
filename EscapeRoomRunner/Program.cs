using NserviceBus.Mqtt;
using NServiceBus;

namespace EscapeRoomManager
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var mqtt = new MqttTransport("localhost");
            mqtt.SubscribeTo("escaperoom/puzzles/tonelock/puzzlesolved");

            var endpointConfiguration = new EndpointConfiguration("escaperoom");
            var routing = endpointConfiguration.UseTransport(mqtt);

            routing.RouteToEndpoint(typeof(RunStarted), "escaperoom/puzzles/startroom");
            routing.DoNotEnforceBestPractices();

            endpointConfiguration.SendFailedMessagesTo("error");
            endpointConfiguration.EnableInstallers();
            endpointConfiguration.UsePersistence<LearningPersistence>();
            endpointConfiguration.UseSerialization<SystemJsonSerializer>();
            endpointConfiguration.Recoverability().Delayed(d => d.NumberOfRetries(0));
            endpointConfiguration.Recoverability().Immediate(d => d.NumberOfRetries(3));

            endpointConfiguration.Conventions().DefiningEventsAs(type => type.Assembly.GetName().Name == "Messages");

            var endpointInstance = await Endpoint.Start(endpointConfiguration);

            Console.WriteLine("Press any key to exit");
            Console.ReadKey();

            await endpointInstance.Stop();
        }
    }
}