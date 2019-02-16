using System;
using WaveformTimeline.Commons;
using WaveformTimeline.Primitives;

namespace WaveformTimeline.Controls.Timeline
{
    public enum TimelineType
    {
        Constant,
        EndRevealing
    }

    public static class TimelineMarkingStrategyConvertedFromTimelineType
    {
        public static ITimelineMarkingStrategy Strategy(this TimelineType type, TuneDuration tuneDuration, TimeSpan firstMark, ZeroToOne end)
        {
            return type == TimelineType.EndRevealing
                ? (ITimelineMarkingStrategy) new EndRevealingTimeline(tuneDuration, firstMark, end)
                : new ConstantTimeline(tuneDuration, firstMark);
        }
    }
}
