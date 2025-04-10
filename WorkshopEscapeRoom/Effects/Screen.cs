using Iot.Device.Ssd13xx;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace Deployment.Effects
{
    public class Screen
    {
        public Screen(Ssd1306 oled)
        {
            _oled = oled;
            _oled.ClearScreen();
            _oled.Font = new BasicFont();

            _thread = new Thread(new ThreadStart(Loop));
        }

        public void Write(string message)
        {
            var lines = message.Split('\n');
            var startY = 2;
            foreach (var line in lines)
            {
                _oled.DrawString(2, startY, line, 1, true);
                startY += 14;
            }

            _oled.Display();
        }

        public void WriteBottom(string message)
        {
            _oled.DrawString(2, 50, message, 1, true);
            _oled.Display();
        }

        public void WriteLarge(string message)
        {
            _oled.DrawString(2, 32, message, 2, true);
            _oled.Display();
        }

        public void ClearScreen()
        {
            _oled.ClearScreen();
        }

        public void StartScreenSaver()
        {
            if (_thread.ThreadState == ThreadState.Unstarted)
            {
                _thread.Start();
            }
            else
            {
                _isRunning = true;
            }
        }

        public void StopScreenSaver()
        {
            ClearScreen();
            _isRunning = false;

            _thread.Abort();
        }

        void Loop()
        {
            ClearScreen();
            Thread.Sleep(5000);

            var counter = 0;
            while (true)
            {
                if (_isRunning)
                {
                    var display = new StringBuilder(10);
                    for (var x = 0; x < counter % 10; x++)
                    {
                        display.Append(".");
                    }
                    display.Append("*");
                    for (var x = counter % 10 + 1; x < 10; x++)
                    {
                        display.Append(".");
                    }

                    WriteBottom(display.ToString());
                    counter++;
                }

                Thread.Sleep(2000);
            }
        }

        Ssd1306 _oled;
        Thread _thread;
        bool _isRunning = true;
    }
}