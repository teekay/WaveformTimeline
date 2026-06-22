using System;
using WaveformTimeline.Commons;

namespace WaveformTimeline.Controls.Timeline
{
    public sealed class ConstantTimeline : ITimelineMarkingStrategy
    {
        public ConstantTimeline(TuneDuration tuneDuration, TimeSpan firstMark)
        {
            _firstMark = firstMark;
            _tickDuration = new TickDuration(tuneDuration.Duration());
        }

        private readonly TimeSpan _firstMark;
        private readonly TickDuration _tickDuration;

        public bool AtMinorTick(TimeSpan sec)
        {
            return sec == _firstMark || Math.Abs(sec.Seconds % _tickDuration.Minor) < 0.001;
        }

        public bool AtMajorTick(TimeSpan sec)
        {
            return sec == _firstMark || Math.Abs(sec.Seconds % _tickDuration.Major) < 0.001;
        }

    }
}
