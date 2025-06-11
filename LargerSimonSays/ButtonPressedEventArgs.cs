namespace LargerSimonSays;

using System;

public class ButtonPressedEventArgs : EventArgs
{
    public int ButtonIndex { get; }

    public ButtonPressedEventArgs(int buttonIndex)
    {
        ButtonIndex = buttonIndex;
    }
}