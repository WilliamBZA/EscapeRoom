using NServiceBus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EscapeRoomManager
{
    public class EscapeRoomRun : ContainSagaData
    {
        public string? RunId { get; set; }
        public string? TeamName { get; set; }
        public DateTime? StartTime { get; set; }
    }
}