using Messages;
using NServiceBus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotWheels
{
    internal class TrackSaga : Saga<HotWheelsSaga>, IAmStartedByMessages<StartHotWheelsTrack>, IHandleTimeouts<ReleaseCar>, IHandleMessages<LowerDrawbridge>
    {
        public async Task Handle(StartHotWheelsTrack message, IMessageHandlerContext context)
        {
            await RequestTimeout<ReleaseCar>(context, TimeSpan.FromSeconds(0));
        }

        public async Task Handle(LowerDrawbridge message, IMessageHandlerContext context)
        {
            await context.Publish(new DrawbridgeLowered());
        }

        public async Task Timeout(ReleaseCar state, IMessageHandlerContext context)
        {
            var random = new Random();
            var timeUntilNextRelease = TimeSpan.FromSeconds(30 + random.Next(30));

            await context.Publish(new CarReleased { TimeUntilNextCarReleased = timeUntilNextRelease });

            await RequestTimeout<ReleaseCar>(context, timeUntilNextRelease);
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<HotWheelsSaga> mapper)
        {
            mapper.MapSaga(saga => saga.SagaId)
                .ToMessage<StartHotWheelsTrack>(msg => msg.Event);

            mapper.MapSaga(saga => saga.SagaId)
                .ToMessage<LowerDrawbridge>(msg => msg.Event);
        }
    }
}