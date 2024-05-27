using NServiceBus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EscapeRoomIoTRunner
{
    public class EscapeRoomIotHandler : IHandleMessages<PuzzleSolved>, IHandleMessages<RunStarted>
    {
        public Task Handle(PuzzleSolved message, IMessageHandlerContext context)
        {
            Console.WriteLine("Tone lock solved");
            return Task.CompletedTask;
        }

        public async Task Handle(RunStarted message, IMessageHandlerContext context)
        {
            await context.Publish(new PuzzleSolved { RunId = message.RunId });
        }
    }
}