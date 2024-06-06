using NserviceBus.Mqtt;
using NServiceBus;

namespace IoTDevices
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var mqtt = new MqttTransport("192.168.88.114");

            var endpointConfiguration = new EndpointConfiguration("escaperoom_puzzles_hotwheels");
            var routing = endpointConfiguration.UseTransport(mqtt);

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