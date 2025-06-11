using Iot.Device.Mcp23xxx;
using nanoFramework.Hardware.Esp32;
using System;
using System.Device.I2c;
using System.Diagnostics;
using System.Threading;

namespace LargerSimonSays
{
    public class Program
    {
        public static void Main()
        {
            Configuration.SetPinFunction(Gpio.IO10, DeviceFunction.I2C1_DATA);
            Configuration.SetPinFunction(Gpio.IO09, DeviceFunction.I2C1_CLOCK);

            var connectionSettings = new I2cConnectionSettings(1, 0x20, I2cBusSpeed.StandardMode);
            var i2cDevice = I2cDevice.Create(connectionSettings);
            var mcp23017 = new Mcp23017(i2cDevice);

            var inputTracker = new InputTracker(mcp23017);
            inputTracker.ButtonPressed += (sender, e) =>
            {
                var buttonEventArgs = e as ButtonPressedEventArgs;
                Debug.WriteLine($"Button {buttonEventArgs.ButtonIndex} pressed");
                inputTracker.SetLeds((byte)(1 << buttonEventArgs.ButtonIndex)); // Light up the corresponding LED
            };

            Thread.Sleep(Timeout.Infinite);
        }
    }
}
