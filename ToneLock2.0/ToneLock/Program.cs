using Amqp;
using Iot.Device.Buzzer;
using Iot.Device.Buzzer.Samples;
using Iot.Device.KeyMatrix;
using nanoFramework.Hardware.Esp32;
using nanoFramework.Networking;
using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Diagnostics;
using System.Threading;

namespace ToneLock
{
    public class Program
    {
        private static Buzzer buzzer;
        private static MelodyPlayer player;
        private static string password = "4466117";
        private static int currentPasswordIndex = 0;
        private static GpioPin greenLed;
        private static GpioPin redLed;

        public static void Main()
        {
            ConnectToWiFi();

            Configuration.SetPinFunction(18, DeviceFunction.PWM1);
            buzzer = new Buzzer(18);
            player = new MelodyPlayer(buzzer);

            var gpioController = new GpioController();

            greenLed = gpioController.OpenPin(22, PinMode.Output);
            greenLed.Write(PinValue.Low);

            redLed = gpioController.OpenPin(23, PinMode.Output);
            redLed.Write(PinValue.Low);

            var topButton = gpioController.OpenPin(13, PinMode.InputPullDown);
            topButton.DebounceTimeout = TimeSpan.FromMilliseconds(100);
            topButton.ValueChanged += (s, e) =>
            {
                if (e.ChangeType == PinEventTypes.Rising)
                {
                    PlayPasswordTone();
                }
            };

            ConfigureKeyMatrix(gpioController);

            Thread.Sleep(Timeout.Infinite);
        }

        private static void SuccessfullySolved()
        {
            player.Play(GetVictorySequence(), 100);

            var connection = new Connection(new Address("connection string"));
            var session = new Session(connection);
            var sender = new SenderLink(session, "Tone lock 2.0", "tonelocksolved");

            var message = new Message("");
            sender.Send(message);

            sender.Close();
            session.Close();
            connection.Close();
        }

        private static void ConnectToWiFi()
        {
            var success = WifiNetworkHelper.ConnectDhcp("introtomessaging", "IsY2TPxx0TI9");
            if (!success)
            {
                Debug.WriteLine("Couldn't connect to WiFi");
            }
        }

        private static void ConfigureKeyMatrix(GpioController gpioController)
        {
            int[] inputs = new int[] { 12, 14, 27, 26 };
            int[] outputs = new int[] { 25, 33, 32 };

            var keymap = new char[][] { new char [] { '1', '2', '3' },
                                        new char [] { '4', '5', '6' },
                                        new char [] { '7', '8', '9' },
                                        new char [] { '*', '0', '#' } };

            var keyMatrix = new KeyMatrix(outputs, inputs, TimeSpan.FromMilliseconds(15), shouldDispose: false);
            var debounceUntil = DateTime.MinValue;
            keyMatrix.KeyEvent += (s, e) =>
            {
                if (DateTime.UtcNow >= debounceUntil)
                {
                    if (e.EventType == PinEventTypes.Rising)
                    {
                        debounceUntil = DateTime.UtcNow + TimeSpan.FromMilliseconds(50);

                        if (password[currentPasswordIndex] == keymap[e.Input][e.Output])
                        {
                            if (currentPasswordIndex +1 < password.Length)
                            {
                                currentPasswordIndex++;

                                new Thread(FlashCorrect).Start(); 
                            }
                            else
                            {
                                redLed.Write(PinValue.High);
                                greenLed.Write(PinValue.High);

                                SuccessfullySolved();
                                
                                currentPasswordIndex = 0;
                                return;
                            }
                        }
                        else
                        {
                            currentPasswordIndex = 0;
                            new Thread(FlashIncorrect).Start();
                        }

                        PlayTone(keymap[e.Input][e.Output]);
                    }
                }
            };

            keyMatrix.StartListeningKeyEvent();
        }

        private static void FlashIncorrect()
        {
            for (var i = 0; i < 5; i++)
            {
                redLed.Write(PinValue.High);
                Thread.Sleep(125);
                redLed.Write(PinValue.Low);
                Thread.Sleep(125);
            }
        }

        private static void FlashCorrect()
        {
            greenLed.Write(PinValue.High);
            Thread.Sleep(500);
            greenLed.Write(PinValue.Low);
        }

        private static void PlayTone(char button)
        {
            switch (button)
            {
                case '1':
                    PlayTone(new NoteElement(Note.A, Octave.Fourth, Duration.Quarter));
                    return;

                case '2':
                    PlayTone(new NoteElement(Note.B, Octave.Fourth, Duration.Quarter));
                    return;

                case '3':
                    PlayTone(new NoteElement(Note.C, Octave.Fourth, Duration.Quarter));
                    return;

                case '4':
                    PlayTone(new NoteElement(Note.D, Octave.Fourth, Duration.Quarter));
                    return;

                case '5':
                    PlayTone(new NoteElement(Note.E, Octave.Fourth, Duration.Quarter));
                    return;

                case '6':
                    PlayTone(new NoteElement(Note.F, Octave.Fourth, Duration.Quarter));
                    return;

                case '7':
                    PlayTone(new NoteElement(Note.G, Octave.Fourth, Duration.Quarter));
                    return;

                case '8':
                    PlayTone(new NoteElement(Note.A, Octave.Fifth, Duration.Quarter));
                    return;

                case '9':
                    PlayTone(new NoteElement(Note.B, Octave.Fifth, Duration.Quarter));
                    return;

                case '0':
                    PlayTone(new NoteElement(Note.C, Octave.Fifth, Duration.Quarter));
                    return;

                default:
                    break;
            }
        }

        static void ShowKeyMatrixEvent(KeyMatrix sender, KeyMatrixEvent pinValueChangedEventArgs)
        {
            Debug.WriteLine($"{DateTime.UtcNow} ({pinValueChangedEventArgs.Output}, {pinValueChangedEventArgs.Input})");
        }

        static void PlayTone(NoteElement noteElement)
        {
            player.Play(noteElement, 100);
        }

        static void PlayPasswordTone()
        {
            ListMelodyElement sequence = GetHelloDarknessNotes();
            player.Play(sequence, 100);
        }

        private static ListMelodyElement GetTakeOneMe()
        {
            return new ListMelodyElement()
            {
                new NoteElement(Note.E, Octave.Fourth, Duration.Sixteenth),
                new NoteElement(Note.E, Octave.Fourth, Duration.Sixteenth),
                new NoteElement(Note.C, Octave.Fourth, Duration.Eighth),
                new NoteElement(Note.A, Octave.Third, Duration.Sixteenth),
                new PauseElement(Duration.Sixteenth),
                new NoteElement(Note.A, Octave.Third, Duration.Sixteenth),
                new NoteElement(Note.D, Octave.Fourth, Duration.Sixteenth),
                new PauseElement(Duration.Sixteenth),
                new NoteElement(Note.D, Octave.Fourth, Duration.Sixteenth),
                new PauseElement(Duration.Sixteenth),
                new NoteElement(Note.D, Octave.Fourth, Duration.Sixteenth),
                new NoteElement(Note.Gb, Octave.Fourth, Duration.Sixteenth),
            };
        }

        private static ListMelodyElement GetVictorySequence()
        {
            return new ListMelodyElement()
            {
                new NoteElement(Note.C, Octave.Fourth, Duration.Sixteenth),
                new NoteElement(Note.C, Octave.Fourth, Duration.Sixteenth),
                new NoteElement(Note.C, Octave.Fourth, Duration.Sixteenth),
                new NoteElement(Note.C, Octave.Fourth, Duration.Quarter) { PauseDurationAfterPlay = 0 },
                new NoteElement(Note.Ab, Octave.Third, Duration.Quarter) { PauseDurationAfterPlay = 0 },
                new NoteElement(Note.Bb, Octave.Third, Duration.Quarter) { PauseDurationAfterPlay = 0 },
                new NoteElement(Note.C, Octave.Fourth, Duration.Eighth),
                new NoteElement(Note.Bb, Octave.Third, Duration.Sixteenth),
                new NoteElement(Note.C, Octave.Fourth, Duration.Half),
            };
        }

        private static ListMelodyElement GetHelloDarknessNotes()
        {
            return new ListMelodyElement()
            {
                new NoteElement(Note.D, Octave.Fourth, Duration.Sixteenth),
                new NoteElement(Note.D, Octave.Fourth, Duration.Quarter),
                new NoteElement(Note.F, Octave.Fourth, Duration.Sixteenth),
                new NoteElement(Note.F, Octave.Fourth, Duration.Quarter),
                new NoteElement(Note.A, Octave.Fourth, Duration.Sixteenth),
                new NoteElement(Note.A, Octave.Fourth, Duration.Quarter),
                new NoteElement(Note.G, Octave.Fourth, Duration.Half)
            };
        }
    }
}
