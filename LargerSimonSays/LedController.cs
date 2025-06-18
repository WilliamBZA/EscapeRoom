namespace LargerSimonSays;

using Iot.Device.Mcp23xxx;
using System;
using System.Threading;

public class LedController
{
    private readonly Mcp23017 _mcp;

    public LedController(Mcp23017 mcp)
    {
        _mcp = mcp;

        _mcp.WriteByte(Register.IODIR, 0, Port.PortA);
        _mcp.WriteByte(Register.IODIR, 0, Port.PortB);
    }

    public void SetLed(int ledNumber, bool on)
    {
        if (ledNumber >= 1 && ledNumber <= 6)
        {
            // LEDs 1-6 are in inverse order on PortA pins 5-0
            int pin = 6 - ledNumber;
            byte current = _mcp.ReadByte(Register.GPIO, Port.PortA);
            if (on)
            {
                current |= (byte)(1 << pin);
            }
            else
            {
                current &= (byte)~(1 << pin);
            }
            _mcp.WriteByte(Register.GPIO, current, Port.PortA);
        }
        else if (ledNumber == 7 || ledNumber == 8)
        {
            // LEDs 7 and 8 are on PortA pins 6 and 7
            int pin = ledNumber - 1;
            byte current = _mcp.ReadByte(Register.GPIO, Port.PortA);
            if (on)
            {
                current |= (byte)(1 << pin);
            }
            else
            {
                current &= (byte)~(1 << pin);
            }
            _mcp.WriteByte(Register.GPIO, current, Port.PortA);
        }
        else if (ledNumber >= 9 && ledNumber <= 15)
        {
            // LEDs 9-15 are on PortB pins 0-6
            int pin = ledNumber - 9;
            byte current = _mcp.ReadByte(Register.GPIO, Port.PortB);
            if (on)
            {
                current |= (byte)(1 << pin);
            }
            else
            {
                current &= (byte)~(1 << pin);
            }
            _mcp.WriteByte(Register.GPIO, current, Port.PortB);
        }
    }

    public void TurnAllLedsOff()
    {
        _mcp.WriteByte(Register.GPIO, 0, Port.PortA);
        _mcp.WriteByte(Register.GPIO, 0, Port.PortB);
    }

    public void FlashAllLeds(int times, int duration, int extraLED)
    {
        for (int t = 0; t < times; t++)
        {
            _mcp.WriteByte(Register.GPIO, (byte)0b1111_1111, Port.PortA);
            _mcp.WriteByte(Register.GPIO, (byte)0b0000_1111, Port.PortB);
            SetLed(extraLED, true);
            Thread.Sleep(duration);

            _mcp.WriteByte(Register.GPIO, (byte)0, Port.PortA);
            _mcp.WriteByte(Register.GPIO, (byte)0, Port.PortB);
            SetLed(extraLED, false);
            Thread.Sleep(duration);
        }
    }
}