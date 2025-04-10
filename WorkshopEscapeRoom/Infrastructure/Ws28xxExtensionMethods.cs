using Iot.Device.Ws28xx.Esp32;
using System;
using System.Collections.Generic;
using System.Text;

namespace Deployment
{
    public static class Ws28xxExtensionMethods
    {
        public static void TurnAllLightsOff(this Ws28xx strip)
        {
            var length = strip.Image.Data.Length / 3;
            for (var x = 0; x < length; x++)
            {
                strip.Image.SetPixel(x, 0, 0, 0, 0);
            }

            strip.Update();
        }
    }
}