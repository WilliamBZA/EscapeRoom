using NServiceBus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EscapeRoomIoTRunner
{
    public class EscapeRoomIotHandler : IHandleMessages<ToneLockSolved>
    {
        public Task Handle(ToneLockSolved message, IMessageHandlerContext context)
        {
            Console.WriteLine("Tone lock solved");
            return Task.CompletedTask;
        }
    }
}