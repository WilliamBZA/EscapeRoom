using NServiceBus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotWheels
{
    internal class StartHotWheelsTrack : ICommand
    {
        public string Event { get; set; }
    }
}