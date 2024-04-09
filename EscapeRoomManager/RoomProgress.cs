using NServiceBus;

namespace EscapeRoomManager
{
    public class RoomProgress : ContainSagaData
    {
        public string? RunId { get; set; }
        public DateTime? StartTime { get; set; }
    }
}