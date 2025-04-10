using Iot.Device.Ws28xx.Esp32;
using System;
using System.Drawing;
using System.Text;
using System.Threading;

namespace Deployment.NewFolder
{
    public class Knightrider
    {
        public Knightrider(Ws28xx lights)
        {
            _lights = lights;

            _currentCenter = 3;
            _currentTarget = 5;

            _thread = new Thread(new ThreadStart(Loop));
        }

        public void Start()
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

        public void Stop()
        {
            _isRunning = false;
        }

        public void KillThread()
        {
            _isRunning = false;
            _thread.Abort();
            _thread = null;
        }

        public bool IsRunning
        {
            get { return _isRunning; }
        }

        public bool IsTargetHit
        {
            get {return !IsRunning && _currentCenter == _currentTarget; }
        }
        
        void Loop()
        {
            var incrementer = 1;

            while (true)
            {
                if (_isRunning)
                {
                    DisplayState();
                }

                Thread.Sleep(25);

                if (_isRunning)
                {
                    _currentCenter += incrementer;
                    if (_currentCenter >= _numLights)
                    {
                        incrementer = -1;
                    }
                    else if (_currentCenter - 1 <= 0)
                    {
                        incrementer = 1;
                    }
                }
            }
        }

        public void DisplayState()
        {
            for (var j = 0; j < _numLights; j++)
            {
                if (j == _currentCenter - 1)
                {
                    if (j == _currentTarget - 1)
                    {
                        if (_numberHit == 0)
                        {
                            _lights.Image.SetPixel(j, 0, 128, 128, 128);
                        }
                        else if (_numberHit == 1)
                        {
                            _lights.Image.SetPixel(j, 0, 0, 0, 128);
                        }
                        else
                        {
                            _lights.Image.SetPixel(j, 0, 0, 128, 0);
                        }
                    }
                    else
                    {
                        _lights.Image.SetPixel(j, 0, 128, 0, 0);
                    }
                }
                else
                {
                    var distance = j - (_currentCenter - 1);
                    if (distance < 0)
                    {
                        distance *= -1;
                    }

                    if (distance < 3)
                    {
                        _lights.Image.SetPixel(j, 0, (byte)(128 * (3 - distance) / 6), 0, 0);
                    }
                    else
                    {
                        _lights.Image.SetPixel(j, 0, 0, 0, 0);
                    }
                }
            }

            _lights.Update();
        }

        public int ChooseNextTarget()
        {
            _currentTarget = new Random().Next(7) + 1;
            return ++_numberHit;
        }

        public void FlashGreen(bool alreadyCalled = false)
        {
            for (var j = 0; j < _numLights; j++)
            {
                _lights.Image.SetPixel(j, 0, 0, 125, 0);
            }
            _lights.Update();

            Thread.Sleep(250);

            for (var j = 0; j < _numLights; j++)
            {
                _lights.Image.SetPixel(j, 0, 0, 0, 0);
            }
            _lights.Update();

            Thread.Sleep(250);

            if (!alreadyCalled)
            {
                FlashGreen(true);
            }
            else
            {
                DisplayState();
            }
        }

        Thread _thread;
        Ws28xx _lights;
        int _numLights = 9;
        int _currentCenter = 7;
        int _currentTarget = 4;
        int _numberHit = 0;
        bool _isRunning = true;
    }
}