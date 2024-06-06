using Messages;
using NServiceBus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotWheels
{
    internal class DrawBridgeController : IHandleMessages<CarReleased>
    {
        public async Task Handle(CarReleased message, IMessageHandlerContext context)
        {
            // TODO: Send a "LowerDrawbridge" command at the right time
            var so = new SendOptions();
            so.DelayDeliveryWith(message.TimeUntilNextCarReleased + TimeSpan.FromSeconds(2));

            await context.Send(new LowerDrawbridge(), so);
            await Task.Delay(1, context.CancellationToken);
        }
    }
}