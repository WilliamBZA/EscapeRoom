using Amqp;
using nanoFramework.Networking;
using System;
using System.Device.Gpio;
using System.Diagnostics;
using System.Threading;

namespace Relay2._0
{
    public class Program
    {
        private static GpioPin relayPin;

        public static void Main()
        {
            var gpioController = new GpioController();
            relayPin = gpioController.OpenPin(8, PinMode.Output);
            relayPin.Write(PinValue.High);

            ConnectToWifi();

            var connection = new Connection(new Address("amqps://RootManageSharedAccessKey:RJyx55XhgjDzcfikOIVqgt5Whn9zZAO6Q%2BASbA5nud0%3D@introtomessagingworkshop.servicebus.windows.net:5671/?verify=verify_none"));
            var session = new Session(connection);
            var receiver = new ReceiverLink(session, "Listen for puzzle solved", "tonelocksolved");


            var message = (object)null;
            do
            {
                message = receiver.Receive();
            } while (message is null);

            relayPin.Write(PinValue.Low);

            Thread.Sleep(Timeout.Infinite);
        }

        private static void ConnectToWifi()
        {
            var success = WifiNetworkHelper.ConnectDhcp("introtomessaging", "IsY2TPxx0TI9");
            if (!success)
            {
                Debug.WriteLine("Couldn't connect to WiFi");
            }
        }
    }
}
