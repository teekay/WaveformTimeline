using System;
using System.Collections.Generic;
using System.Linq;
using WaveformTimeline.Commons;
using WaveformTimeline.Primitives;

namespace WaveformTimeline.Controls.Timeline
{
    public sealed class TimelineSource
    {
        public TimelineSource(TuneDuration duration) : this(duration, 10)
        {
        }

        public TimelineSource(TuneDuration duration, int alignmentOfStart)
        {
            _workingDuration = TimeSpan.FromSeconds(duration.Duration() >= 0 ? duration.Duration() : DefaultDuration);
            Beginning = StartingPointAlignedToSecond(
                TimeSpan.FromSeconds(new FiniteDouble(duration.StartingPoint())),
                alignmentOfStart);
        }

        private readonly TimeSpan _workingDuration;
        private int _remainder;
        private const double DefaultDuration = 180d; // for empty timeline

        /// <summary>
        /// Tells at which second the timeline should start
        /// </summary>
        public TimeSpan Beginning { get; }

        private TimeSpan StartingPointAlignedToSecond(TimeSpan point, int alignment)
        {
            _remainder = point.Seconds % alignment;
            return _remainder == 0
                ? point
                : point.Add(TimeSpan.FromSeconds(Math.Abs(alignment - _remainder)));
        }

        /// <summary>
        /// Provide a list of seconds that can be displayed on a timeline
        /// </summary>
        /// <returns></returns>
        public IEnumerable<TimeSpan> Seconds() =>
            Enumerable.Range((int) Beginning.TotalSeconds, (int)(_workingDuration.TotalSeconds - _remainder))
                .Select(sec => TimeSpan.FromSeconds(sec));

        /// <summary>
        /// Formats a TimeSpan value as a string that makes sense on a timeline
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public string TimespanAsString(TimeSpan time) => time.TotalHours > 1.0
            ? $@"{time:hh\:mm\:ss}"
            : $@"{time:mm\:ss}";

        public override string ToString() => TimespanAsString(_workingDuration);
    }
}
