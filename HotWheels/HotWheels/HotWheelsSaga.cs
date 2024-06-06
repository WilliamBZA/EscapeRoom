using NServiceBus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotWheels
{
    internal class HotWheelsSaga : ContainSagaData
    {
        public string SagaId { get; set; }
    }
}