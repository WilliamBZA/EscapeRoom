using System;
using System.Threading;
using System.Diagnostics;
using nanoFramework.Hardware.Esp32;
using Iot.Device.Pn532;
using System.Device.I2c;
using Iot.Device.Pn532.ListPassive;
using System.Text;
using System.Reflection;
using Iot.Device.Ws28xx.Esp32;
using System.Device.Gpio;

namespace RfidReader
{
    public class Program
    {
        public static void Main()
        {
            Configuration.SetPinFunction(Gpio.IO10, DeviceFunction.I2C1_DATA);
            Configuration.SetPinFunction(Gpio.IO09, DeviceFunction.I2C1_CLOCK);

            var rfidReader = new RfidReader(new Pn532(I2cDevice.Create(new I2cConnectionSettings(1, Pn532.I2cDefaultAddress))));
            rfidReader.OnCardRead += (s, e) =>
            {
                Console.WriteLine($"Card Read ID: {e.NfcId}");
            };

            rfidReader.Start();

            Thread.Sleep(Timeout.Infinite);
        }
    }
}