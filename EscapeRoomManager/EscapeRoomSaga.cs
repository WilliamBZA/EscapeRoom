﻿using NServiceBus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EscapeRoomManager
{
    public class EscapeRoomSaga : Saga<EscapeRoomRun>, IAmStartedByMessages<RunStarted>, IAmStartedByMessages<PuzzleSolved>
    {
        public Task Handle(PuzzleSolved message, IMessageHandlerContext context)
        {
            DateTime startTime = Data.StartTime ?? DateTime.Now;
            var solveTime = DateTime.Now - startTime;
            Console.WriteLine($"Tone lock solved after {(int)(solveTime.TotalSeconds)} seconds");

            if (solveTime >= TimeSpan.FromMinutes(4))
            {
                //await context.Send(new OpenEasyLabyrinthBox());
            }
            else
            {
                //await context.Send(new OpenNormalLabyrinthBox());
            }

            return Task.CompletedTask;
        }

        public Task Handle(RunStarted message, IMessageHandlerContext context)
        {
            Data.StartTime = Data.StartTime ?? DateTime.Now;

            Console.WriteLine($"Run {Data.RunId} started at {Data.StartTime}");

            return Task.CompletedTask;
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<EscapeRoomRun> mapper)
        {
            mapper.MapSaga(saga => saga.RunId)
                .ToMessage<RunStarted>(tone => tone.RunId)
                .ToMessage<PuzzleSolved>(tone => tone.RunId);
        }
    }
}