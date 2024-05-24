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
        public async Task Handle(ToneLockSolved message, IMessageHandlerContext context)
        {
            Console.WriteLine("Tone lock solved");

            await context.Publish(new ToneLockCompleted { RunId = message.RunId });
        }
    }
}