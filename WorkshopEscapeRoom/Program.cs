using nanoFramework.Networking;
using nanoFramework.WebServer;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Security;
using System.Threading;
using System.IO;
using System.Text;
using nanoFramework.Hardware.Esp32;
using System.Device.Gpio;
using nanoFramework.Runtime.Native;
using nanoFramework.System.IO.FileSystem;
using Deployment.NewFolder;
using Iot.Device.Ssd13xx;
using System.Device.I2c;
using Deployment;
using Iot.Device.Ws28xx.Esp32;
using System.Device.Pwm;
using Iot.Device.ServoMotor;
using Deployment.Effects;

namespace Deployment
{
    public class Program
    {
        private static string MySsid = "introtomessaging";
        private static string MyPassword = "IsY2TPxx0TI9";
        private static int numberOfRequestsCurrentlyBeingServed = 0;

        public static void Main()
        {
            Configuration.SetPinFunction(Gpio.IO15, DeviceFunction.I2C1_DATA);
            Configuration.SetPinFunction(Gpio.IO22, DeviceFunction.I2C1_CLOCK);
            Configuration.SetPinFunction(Gpio.IO12, DeviceFunction.PWM1);
            Configuration.SetPinFunction(Gpio.IO23, DeviceFunction.SPI1_MOSI);
            Configuration.SetPinFunction(Gpio.IO19, DeviceFunction.SPI1_MISO);
            Configuration.SetPinFunction(Gpio.IO18, DeviceFunction.SPI1_CLOCK);

            var gpioController = new GpioController();

            var maglock = gpioController.OpenPin(26, PinMode.Output);
            var blueLed = gpioController.OpenPin(4, PinMode.Output);
            var redLed = gpioController.OpenPin(21, PinMode.Output);

            var morse = new MorseCodeFlasher(blueLed);

            using PwmChannel pwmChannel = PwmChannel.CreateFromPin(12, 50);
            ServoMotor servoMotor = new ServoMotor(pwmChannel, 180, 500, 2500);
            servoMotor.Start();

            servoMotor.WriteAngle(0);
            maglock.Write(PinValue.High);

            var ledStrip = new Sk6812(32, 9);
            ledStrip.TurnAllLightsOff();

            var sudokuUnlocked = false;
            var knightRider = new Knightrider(ledStrip);

            using Ssd1306 oledscreen = new Ssd1306(I2cDevice.Create(new I2cConnectionSettings(1, Ssd1306.DefaultI2cAddress)), Ssd13xx.DisplayResolution.OLED128x64, DisplayOrientation.Landscape180);
            var screen = new Screen(oledscreen);

            screen.Write("Lily & Willow's\nescaperoom\nConnecting...");
            if (!ConnectToWiFi(screen))
            {
                return;
            }

            var hallSensor = gpioController.OpenPin(13, PinMode.InputPullDown);
            hallSensor.DebounceTimeout = TimeSpan.FromMilliseconds(150);
            hallSensor.ValueChanged += (s, e) =>
            {
                if (!sudokuUnlocked && e.ChangeType == PinEventTypes.Rising)
                {
                    sudokuUnlocked = true;
                    screen.StopScreenSaver();

                    knightRider.Start();
                }

                if (e.ChangeType == PinEventTypes.Rising)
                {
                    redLed.Write(PinValue.High);
                }
                else
                {
                    redLed.Write(PinValue.Low);
                }
            };

            var leftButton = gpioController.OpenPin(25, PinMode.InputPullDown);
            leftButton.DebounceTimeout = TimeSpan.FromMilliseconds(50);

            leftButton.ValueChanged += (s, e) =>
            {
                if (sudokuUnlocked)
                {
                    if (e.ChangeType == PinEventTypes.Rising)
                    {
                        if (knightRider.IsRunning)
                        {
                            knightRider.Stop();
                            if (knightRider.IsTargetHit)
                            {
                                knightRider.FlashGreen();
                                var numberOfTargetsHit = knightRider.ChooseNextTarget();

                                if (numberOfTargetsHit < 3)
                                {
                                    Thread.Sleep(500);
                                    knightRider.Start();
                                }
                                else
                                {
                                    ledStrip.TurnAllLightsOff();
                                    knightRider.KillThread();
                                    maglock.Write(PinValue.Low);
                                    screen.WriteLarge("OPEN ME!");
                                    morse.Start();
                                }
                            }
                        }
                        else
                        {
                            knightRider.Start();
                        }
                    }
                }
            };

            var rightButton = gpioController.OpenPin(33, PinMode.InputPullDown);
            rightButton.DebounceTimeout = TimeSpan.FromMilliseconds(50);
            rightButton.ValueChanged += (s, e) =>
            {
            };

            var keyPadSignal = gpioController.OpenPin(14, PinMode.Input);
            keyPadSignal.DebounceTimeout = TimeSpan.FromMilliseconds(150);
            keyPadSignal.ValueChanged += (s, e) =>
            {
                if (e.ChangeType == PinEventTypes.Rising)
                {
                    servoMotor.WriteAngle(90);
                    morse.Stop();
                }
            };

            try
            {
                var fileSystem = new SDCard(new SDCardSpiParameters
                {
                    chipSelectPin = 5,
                    spiBus = 1
                });

                //fileSystem.Mount();
                //Debug.WriteLine("Card Mounted");

                redLed.Write(PinValue.Low);
                blueLed.Write(PinValue.Low);
            }
            catch (Exception ex)
            {
                redLed.Write(PinValue.Low);
                Thread.Sleep(250);
                redLed.Write(PinValue.High);
                Thread.Sleep(250);
                redLed.Write(PinValue.Low);
                Thread.Sleep(250);
                redLed.Write(PinValue.High);

                Debug.WriteLine($"Card failed to mount : {ex.Message}");
                screen.ClearScreen();
                screen.WriteLarge("SD FAIL");
                //Power.RebootDevice(nanoFramework.Runtime.Native.RebootOption.NormalReboot);
            }

            using (WebServer server = new WebServer(80, HttpProtocol.Http, new Type[] { typeof(FileController) }))
            {
                server.CommandReceived += ServerCommandReceived;
                server.Start();

                screen.ClearScreen();
                screen.Write("Ready...");
                screen.StartScreenSaver();
                Thread.Sleep(Timeout.Infinite);
            }
        }

        private static bool ConnectToWiFi(Screen screen)
        {
            bool success = false;
            CancellationTokenSource cs = new(30000);
            try
            {
                success = WifiNetworkHelper.ConnectDhcp(MySsid, MyPassword, System.Device.Wifi.WifiReconnectionKind.Automatic, requiresDateTime: false, token: cs.Token);
            }
            catch (Exception) { }

            if (!success)
            {
                Debug.WriteLine($"Can't get a proper IP address and DateTime, error: {NetworkHelper.Status}.");
                if (NetworkHelper.HelperException != null)
                {
                    Debug.WriteLine($"Exception: {NetworkHelper.HelperException}");
                }
                screen.ClearScreen();
                screen.WriteLarge("NETWORK FAIL");

                return false;
            }
            else
            {
                screen.ClearScreen();
                var ipaddress = IPGlobalProperties.GetIPAddress();
                Debug.WriteLine($"Connected with IP Address: {ipaddress.ToString()}");
                screen.Write(ipaddress.ToString());
            }

            return true;
        }

        private static void ServerCommandReceived(object source, WebServerEventArgs e)
        {
            try
            {
                // Limit the response concurreny to not overload the SD card
                while (numberOfRequestsCurrentlyBeingServed >= 5)
                {
                    Thread.Sleep(50);
                }

                Interlocked.Increment(ref numberOfRequestsCurrentlyBeingServed);

                var url = e.Context.Request.RawUrl;
                Debug.WriteLine($"Command received: {url}, Method: {e.Context.Request.HttpMethod}");

                var fileName = url.Substring(1);
                if (string.IsNullOrEmpty(fileName))
                {
                    fileName = "index.html";
                }
                else
                {
                    var stringBuilder = new StringBuilder(fileName);
                    stringBuilder.Replace("/", "\\");

                    fileName = stringBuilder.ToString();
                }

                Debug.WriteLine($"Request file: '{fileName}'");

                WebServer.SendFileOverHTTP(e.Context.Response, FileController.DirectoryPath + fileName, GetContentTypeFromFileName(fileName));
                return;
            }
            catch (IOException)
            {
                WebServer.OutputHttpCode(e.Context.Response, HttpStatusCode.NotFound);
            }
            catch (Exception)
            {
                WebServer.OutputHttpCode(e.Context.Response, HttpStatusCode.InternalServerError);
            }
            finally
            {
                Interlocked.Decrement(ref numberOfRequestsCurrentlyBeingServed);
            }
        }

        private static string GetContentTypeFromFileName(string filename)
        {
            if (filename.EndsWith(".js"))
            {
                return "text/javascript";
            }

            return "";
        }
    }
}