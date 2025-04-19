using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Text;
using System.Threading;

namespace Deployment
{
    public class MorseCodeFlasher
    {
        public MorseCodeFlasher(GpioPin flashPin)
        {
            _flashPin = flashPin;

            _thread = new Thread(new ThreadStart(Loop));
        }

        void WriteMorseCharacter(bool isDash)
        {
            _flashPin.Write(PinValue.High);
            Thread.Sleep(isDash ? 500 : 250);
            _flashPin.Write(PinValue.Low);
            Thread.Sleep(250);
        }

        public void Start()
        {
            _isRunning = true;

            if (_thread.ThreadState == ThreadState.Unstarted)
            {
                _thread.Start();
            }
        }

        public bool IsRunning
        {
            get { return _isRunning; }
        }

        public void Stop()
        {
            _isRunning = false;
        }

        void Loop()
        {
            while (_isRunning)
            {
                C();
                O();
                D();
                E();
                S();

                Thread.Sleep(2000);
            }
        }

        public void Zero()
        {
            WriteMorseCharacter(true);
            WriteMorseCharacter(true);
            WriteMorseCharacter(true);
            WriteMorseCharacter(true);
            WriteMorseCharacter(true);
         
            Thread.Sleep(750);
        }

        public void One()
        {
            WriteMorseCharacter(false);
            WriteMorseCharacter(true);
            WriteMorseCharacter(true);
            WriteMorseCharacter(true);
            WriteMorseCharacter(true);

            Thread.Sleep(750);
        }

        public void Two()
        {
            WriteMorseCharacter(false);
            WriteMorseCharacter(false);
            WriteMorseCharacter(true);
            WriteMorseCharacter(true);
            WriteMorseCharacter(true);

            Thread.Sleep(750);
        }

        public void Three()
        {
            WriteMorseCharacter(false);
            WriteMorseCharacter(false);
            WriteMorseCharacter(false);
            WriteMorseCharacter(true);
            WriteMorseCharacter(true);

            Thread.Sleep(750);
        }

        public void Four()
        {
            WriteMorseCharacter(false);
            WriteMorseCharacter(false);
            WriteMorseCharacter(false);
            WriteMorseCharacter(false);
            WriteMorseCharacter(true);

            Thread.Sleep(750);
        }

        public void Five()
        {
            WriteMorseCharacter(false);
            WriteMorseCharacter(false);
            WriteMorseCharacter(false);
            WriteMorseCharacter(false);
            WriteMorseCharacter(false);

            Thread.Sleep(750);
        }

        public void Six()
        {
            WriteMorseCharacter(true);
            WriteMorseCharacter(false);
            WriteMorseCharacter(false);
            WriteMorseCharacter(false);
            WriteMorseCharacter(false);

            Thread.Sleep(750);
        }

        public void Seven()
        {
            WriteMorseCharacter(true);
            WriteMorseCharacter(true);
            WriteMorseCharacter(false);
            WriteMorseCharacter(false);
            WriteMorseCharacter(false);

            Thread.Sleep(750);
        }

        public void Eight()
        {
            WriteMorseCharacter(true);
            WriteMorseCharacter(true);
            WriteMorseCharacter(true);
            WriteMorseCharacter(false);
            WriteMorseCharacter(false);

            Thread.Sleep(750);
        }

        public void Nine()
        {
            WriteMorseCharacter(true);
            WriteMorseCharacter(true);
            WriteMorseCharacter(true);
            WriteMorseCharacter(true);
            WriteMorseCharacter(false);

            Thread.Sleep(750);
        }

        public void A()
        {
            WriteMorseCharacter(false);
            WriteMorseCharacter(true);

            Thread.Sleep(750);
        }

        public void B()
        {
            WriteMorseCharacter(true);
            WriteMorseCharacter(false);
            WriteMorseCharacter(false);
            WriteMorseCharacter(false);

            Thread.Sleep(750);
        }

        public void C()
        {
            WriteMorseCharacter(true);
            WriteMorseCharacter(false);
            WriteMorseCharacter(true);
            WriteMorseCharacter(false);

            Thread.Sleep(750);
        }

        public void D()
        {
            WriteMorseCharacter(true);
            WriteMorseCharacter(false);
            WriteMorseCharacter(false);

            Thread.Sleep(750);
        }

        public void E()
        {
            WriteMorseCharacter(false);

            Thread.Sleep(750);
        }

        public void F()
        {
            WriteMorseCharacter(false);
            WriteMorseCharacter(false);
            WriteMorseCharacter(true);
            WriteMorseCharacter(false);

            Thread.Sleep(750);
        }

        public void G()
        {
            WriteMorseCharacter(true);
            WriteMorseCharacter(true);
            WriteMorseCharacter(false);

            Thread.Sleep(750);
        }

        public void H()
        {
            WriteMorseCharacter(false);
            WriteMorseCharacter(false);
            WriteMorseCharacter(false);
            WriteMorseCharacter(false);

            Thread.Sleep(750);
        }

        public void I()
        {
            WriteMorseCharacter(false);
            WriteMorseCharacter(false);

            Thread.Sleep(750);
        }

        public void J()
        {
            WriteMorseCharacter(false);
            WriteMorseCharacter(true);
            WriteMorseCharacter(true);
            WriteMorseCharacter(true);

            Thread.Sleep(750);
        }

        public void K()
        {
            WriteMorseCharacter(true);
            WriteMorseCharacter(false);
            WriteMorseCharacter(true);

            Thread.Sleep(750);
        }

        public void L()
        {
            WriteMorseCharacter(false);
            WriteMorseCharacter(true);
            WriteMorseCharacter(false);
            WriteMorseCharacter(false);

            Thread.Sleep(750);
        }

        public void M()
        {
            WriteMorseCharacter(true);
            WriteMorseCharacter(true);

            Thread.Sleep(750);
        }

        public void N()
        {
            WriteMorseCharacter(true);
            WriteMorseCharacter(false);

            Thread.Sleep(750);
        }

        public void O()
        {
            WriteMorseCharacter(true);
            WriteMorseCharacter(true);
            WriteMorseCharacter(true);

            Thread.Sleep(750);
        }

        public void P()
        {
            WriteMorseCharacter(false);
            WriteMorseCharacter(true);
            WriteMorseCharacter(true);
            WriteMorseCharacter(false);

            Thread.Sleep(750);
        }

        public void Q()
        {
            WriteMorseCharacter(true);
            WriteMorseCharacter(true);
            WriteMorseCharacter(false);
            WriteMorseCharacter(true);

            Thread.Sleep(750);
        }

        public void R()
        {
            WriteMorseCharacter(false);
            WriteMorseCharacter(true);
            WriteMorseCharacter(false);

            Thread.Sleep(750);
        }

        public void S()
        {
            WriteMorseCharacter(false);
            WriteMorseCharacter(false);
            WriteMorseCharacter(false);

            Thread.Sleep(750);
        }

        public void T()
        {
            WriteMorseCharacter(true);

            Thread.Sleep(750);
        }

        public void U()
        {
            WriteMorseCharacter(false);
            WriteMorseCharacter(false);
            WriteMorseCharacter(true);

            Thread.Sleep(750);
        }

        public void W()
        {
            WriteMorseCharacter(false);
            WriteMorseCharacter(true);
            WriteMorseCharacter(true);

            Thread.Sleep(750);
        }

        public void X()
        {
            WriteMorseCharacter(true);
            WriteMorseCharacter(false);
            WriteMorseCharacter(false);
            WriteMorseCharacter(true);

            Thread.Sleep(750);
        }

        public void Y()
        {
            WriteMorseCharacter(true);
            WriteMorseCharacter(false);
            WriteMorseCharacter(true);
            WriteMorseCharacter(true);

            Thread.Sleep(750);
        }

        public void Z()
        {
            WriteMorseCharacter(true);
            WriteMorseCharacter(true);
            WriteMorseCharacter(false);
            WriteMorseCharacter(false);

            Thread.Sleep(750);
        }

        GpioPin _flashPin;

        Thread _thread;
        bool _isRunning = true;
    }
}