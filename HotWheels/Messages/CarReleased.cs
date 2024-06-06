namespace Messages
{
    public class CarReleased : IEvent
    {
        public TimeSpan TimeUntilNextCarReleased { get; set; }
    }
}