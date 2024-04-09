using NServiceBus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EscapeRoomManager
{
    public class EscapeRoomSaga : Saga<RoomProgress>, IAmStartedByMessages<RunStarted>, IHandleMessages<ToneLockSolved>
    {
        public async Task Handle(ToneLockSolved message, IMessageHandlerContext context)
        {
            DateTime startTime = Data.StartTime ?? DateTime.Now;
            var solveTime = DateTime.Now - startTime;
            Console.WriteLine($"Tone lock solved after {(int)(solveTime.TotalSeconds)} seconds");

            if (solveTime >= TimeSpan.FromMinutes(4))
            {
                await context.Send(new OpenEasyLabyrinthBox());
            }
            else
            {
                await context.Send(new OpenNormalLabyrinthBox());
            }
        }

        public Task Handle(RunStarted message, IMessageHandlerContext context)
        {
            Data.StartTime = Data.StartTime ?? DateTime.Now;

            Console.WriteLine($"Run {Data.RunId} started at {Data.StartTime}");

            return Task.CompletedTask;
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<RoomProgress> mapper)
        {
            mapper.MapSaga(saga => saga.RunId)
                .ToMessage<RunStarted>(tone => tone.RunId)
                .ToMessage<ToneLockSolved>(tone => tone.RunId);
        }
    }
}