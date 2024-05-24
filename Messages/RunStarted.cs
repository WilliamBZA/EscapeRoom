using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class RunStarted : IEvent
{
    public required string RunId { get; set; }
    public required string TeamName { get; set; }
}