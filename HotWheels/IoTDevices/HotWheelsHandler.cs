using Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoTDevices
{
    internal class HotWheelsHandler : IHandleMessages<CarReleased>, IHandleMessages<DrawbridgeLowered>
    {
        public Task Handle(CarReleased message, IMessageHandlerContext context)
        {
            var sendOptions = new SendOptions();
            // specify the ESP32 queue address
            sendOptions.SetDestination("escaperoom/puzzles/hotwheels/releasecar");

            return context.Send(message, sendOptions);
        }

        public Task Handle(DrawbridgeLowered message, IMessageHandlerContext context)
        {
            var sendOptions = new SendOptions();
            // specify the ESP32 queue address
            sendOptions.SetDestination("escaperoom/puzzles/hotwheels/lowerdrawbridge");

            return context.Send(message, sendOptions);
        }
    }
}