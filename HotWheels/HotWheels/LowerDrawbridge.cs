using NServiceBus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotWheels
{
    internal class LowerDrawbridge : ICommand
    {
        public LowerDrawbridge()
        {
            Event = "NDC Oslo";
        }

        public string Event { get; set; }
    }
}
