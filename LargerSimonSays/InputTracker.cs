namespace LargerSimonSays;

using Iot.Device.Mcp23xxx;
using System;
using System.Threading;

internal class InputTracker
{
    private readonly Mcp23017 _mcp;
    private byte _lastButtonState;
    private readonly Thread _pollThread;
    private bool _running;

    public event EventHandler ButtonPressed;

    public InputTracker(Mcp23017 mcp)
    {
        _mcp = mcp;

        // Set PortA as input (buttons)
        var portAIODirRegister = _mcp.ReadByte(Register.IODIR, Port.PortA);
        portAIODirRegister |= 0b1111_1111; // All bits input
        _mcp.WriteByte(Register.IODIR, portAIODirRegister, Port.PortA);

        // Set PortB as output (LEDs)
        var portBIODirRegister = _mcp.ReadByte(Register.IODIR, Port.PortB);
        portBIODirRegister &= 0b0000_0000; // All bits output
        _mcp.WriteByte(Register.IODIR, portBIODirRegister, Port.PortB);

        _lastButtonState = ReadButtons();

        _running = true;
        _pollThread = new Thread(PollButtons);
        _pollThread.Start();
    }

    public byte ReadButtons()
    {
        // Each bit represents a button state (1 = pressed, 0 = not pressed)
        return _mcp.ReadByte(Register.GPIO, Port.PortA);
    }

    public void SetLeds(byte ledStates)
    {
        // Each bit represents an LED state (1 = on, 0 = off)
        _mcp.WriteByte(Register.GPIO, ledStates, Port.PortB);
    }

    private void PollButtons()
    {
        while (_running)
        {
            var currentState = ReadButtons();
            var changed = (byte)(currentState & ~_lastButtonState); // Only new presses

            Console.WriteLine($"Last state:\t\t{_lastButtonState}");
            Console.WriteLine($"Current state:\t\t{currentState}");

            for (int i = 0; i < 8; i++)
            {
                if (((changed >> i) & 0x1) == 1)
                {
                    ButtonPressed?.Invoke(this, new ButtonPressedEventArgs(i));
                }
            }

            _lastButtonState = currentState;
            Thread.Sleep(10); // Polling interval (10ms)
        }
    }

    public void Stop()
    {
        _running = false;
        _pollThread.Join();
    }
}
