// See https://aka.ms/new-console-template for more information
using MQTTnet.Client;
using MQTTnet;
using System.Text.Json;
using System.Text;

public class Program
{
    static async Task SubscribeToEvents(IMqttClient mqttClient)
    {
        var mqttClientOptions = new MqttClientOptionsBuilder()
            .WithTcpServer("broker", 1883)
            .WithCleanStart(false)
            .Build();

        mqttClient.ApplicationMessageReceivedAsync += e =>
        {
            Console.WriteLine($"Received application message on topic {e.ApplicationMessage.Topic}:");
            Console.WriteLine($"\t{Encoding.Default.GetString(e.ApplicationMessage.PayloadSegment)}");

            return Task.CompletedTask;
        };

        await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);
        await mqttClient.SubscribeAsync("escaperoom/puzzles/#", MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce, CancellationToken.None);

        Console.WriteLine("MQTT client subscribed to topic.");
    }

    static async Task Main(string[] args)
    {
        var mqttFactory = new MqttFactory();
        var difficulty = 5;

        using (var mqttClient = mqttFactory.CreateMqttClient())
        {
            await SubscribeToEvents(mqttClient);

            Console.WriteLine("Select an option:");
            Console.WriteLine("\tPress H to increase difficulty");
            Console.WriteLine("\tPress E to decrease difficulty");
            Console.WriteLine("\tPress R to restart");
            Console.WriteLine("\tPress U to unlock the easy mag box");
            Console.WriteLine("\tPress ENTER to exit");

            while (true)
            {
                var input = Console.ReadLine();
                switch (input?.ToLowerInvariant())
                {
                    case "":
                        return;

                    case "h":
                        difficulty++;

                        var harderMessage = new MqttApplicationMessageBuilder()
                            .WithTopic("escaperoom/puzzles/changedifficulty")
                            .WithPayload($"{difficulty}")
                            .Build();

                        await mqttClient.PublishAsync(harderMessage);
                        Console.WriteLine($"Changed difficulty to {difficulty}");
                        break;

                    case "e":
                        difficulty--;

                        var easierMessage = new MqttApplicationMessageBuilder()
                            .WithTopic("escaperoom/puzzles/changedifficulty")
                            .WithPayload($"{difficulty}")
                            .Build();

                        await mqttClient.PublishAsync(easierMessage);
                        Console.WriteLine($"Changed difficulty to {difficulty}");
                        break;

                    case "r":
                        var restartMessage = new MqttApplicationMessageBuilder()
                            .WithTopic("escaperoom/puzzles/startroom")
                            .WithPayload("")
                            .Build();

                        await mqttClient.PublishAsync(restartMessage);
                        Console.WriteLine("Restarted room");
                        break;

                    case "u":
                        var unlockMessage = new MqttApplicationMessageBuilder()
                        .WithTopic("escaperoom/puzzles/easymagunlock/unlock")
                        .WithPayload("")
                        .Build();

                        await mqttClient.PublishAsync(unlockMessage);
                        Console.WriteLine("Unlocked easy mag box");
                        break;
                }
            }
        }
    }
}