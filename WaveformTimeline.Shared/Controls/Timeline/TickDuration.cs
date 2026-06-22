namespace WaveformTimeline.Controls.Timeline
{
    /// <summary>
    /// Informed by a track's duration, provides a tuple of minor and major values
    /// that should be used for drawing ticks on the timeline.
    /// </summary>
    internal struct TickDuration
    {
        public TickDuration(double channelDuration)
        {
            if (channelDuration >= 120.0d) // >= 2 minutes: Major tick = 1 minute, Minor tick = 15 seconds.
            {
                Minor = 15.0d;
                Major = 60.0d; // 
            }
            else if (channelDuration > 60.0d) // >= 1 minute: Major tick = 30 seconds, Minor tick = 5.0 seconds.
            {
                Minor = 5.0d;
                Major = 15.0d;
            }
            else if (channelDuration > 10.0d) // >= 30 seconds Major tick = 10 seconds, Minor tick = 2.0 seconds.
            {
                Minor = 2.0d;
                Major = 10.0d;
            }
            else
            {
                Minor = 2.0d; // < 30 seconds: Major tick: 5 seconds, Minor tick = 1 second
                Major = 5.0d;
            }
        }

        /// <summary>
        /// Number of seconds after which a minor tick should be drawn
        /// </summary>
        public double Minor { get; }

        /// <summary>
        /// Number of seconds after which a major tick should be drawn
        /// </summary>
        public double Major { get; }

        public override bool Equals(object obj) => obj is TickDuration duration && Equals(duration);

        public bool Equals(TickDuration other) => Minor.Equals(other.Minor) && Major.Equals(other.Major);

        public override int GetHashCode()
        {
            unchecked
            {
                return (Minor.GetHashCode() * 397) ^ Major.GetHashCode();
            }
        }

        public override string ToString() => $"Ticks at: {Minor} sec., {Major} sec.";
    }
}
