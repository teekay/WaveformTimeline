using System;

namespace WaveformTimeline.Controls.Timeline
{
    public interface ITimelineMarkingStrategy
    {
        bool AtMinorTick(TimeSpan sec);
        bool AtMajorTick(TimeSpan sec);
    }
}