using System;
using WaveformTimeline.Commons;
using WaveformTimeline.Primitives;

namespace WaveformTimeline.Controls.Timeline
{
    public sealed class EndRevealingTimeline : ITimelineMarkingStrategy
    {
        public EndRevealingTimeline(TuneDuration tuneDuration, TimeSpan firstMark): this(tuneDuration, firstMark, 0.8) {}

        public EndRevealingTimeline(TuneDuration tuneDuration, TimeSpan firstMark, ZeroToOne end)
        {
            _firstMark = firstMark;
            var duration = tuneDuration.Duration();
            _tickDuration = new TickDuration(duration);
            _lastThirdMarkTs = TimeSpan.FromSeconds(duration * end);
            _lastThirdDuration = new TickDuration(duration - _lastThirdMarkTs.TotalSeconds);
        }

        private readonly TimeSpan _firstMark;
        private readonly TickDuration _lastThirdDuration;
        private readonly TimeSpan _lastThirdMarkTs;
        private readonly TickDuration _tickDuration;

        public bool AtMinorTick(TimeSpan sec)
        {
            return sec == _firstMark ||
                   (sec < _lastThirdMarkTs && Math.Abs(sec.Seconds % _tickDuration.Minor) < 0.001) ||
                   (sec >= _lastThirdMarkTs && Math.Abs(sec.Seconds % _lastThirdDuration.Minor) < 0.001);
        }

        public bool AtMajorTick(TimeSpan sec)
        {
            return sec == _firstMark ||
                   (sec < _lastThirdMarkTs && Math.Abs(sec.Seconds % _tickDuration.Major) < 0.001) ||
                   (sec >= _lastThirdMarkTs && Math.Abs(sec.Seconds % _lastThirdDuration.Major) < 0.001);
        }
    }
}