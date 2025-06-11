using Iot.Device.Pn532;
using Iot.Device.Pn532.ListPassive;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace RfidReader
{
    public class RfidReader(Pn532 reader)
    {
        public delegate void RfidCardReadEvent(object source, RfidCardEvent e);

        public event RfidCardReadEvent OnCardRead;

        public TimeSpan DebounceTimeout { get; set; } = TimeSpan.FromMilliseconds(1000);

        internal void Start()
        {
            new Thread(ReadLoop).Start();
        }

        void ReadLoop()
        {
            byte[] retData = null;
            var lastReadTime = DateTime.MinValue;

            while (true)
            {
                if (DateTime.UtcNow >= lastReadTime + DebounceTimeout)
                {
                    retData = reader.ListPassiveTarget(MaxTarget.One, TargetBaudRate.B106kbpsTypeA);
                    if (retData is object)
                    {
                        var tagData = new SpanByte(retData, 1, retData.Length - 1);
                        var decrypted = reader.TryDecode106kbpsTypeA(tagData);

                        StringBuilder builder = new StringBuilder();
                        foreach (var b in decrypted.NfcId)
                        {
                            builder.Append(b);
                        }

                        OnCardRead?.Invoke(this, new RfidCardEvent { NfcId = builder.ToString() });
                        lastReadTime = DateTime.UtcNow;
                    }
                }

                // Give time to PN532 to process
                Thread.Sleep(200);
            }
        }
    }

    public class RfidCardEvent : EventArgs
    {
        public string NfcId { get; internal set; }
    }
}