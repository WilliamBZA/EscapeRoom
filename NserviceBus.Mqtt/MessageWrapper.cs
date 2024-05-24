using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NserviceBus.Mqtt
{
    class MessageWrapper
    {
        public string? Id { get; set; }
        public required Dictionary<string, string> Headers { get; set; }
        public required byte[] Body { get; set; }
    }

    class StringMessageWrapper
    {
        public string? Id { get; set; }
        public required Dictionary<string, string> Headers { get; set; }
        public required string Body { get; set; }
    }
}