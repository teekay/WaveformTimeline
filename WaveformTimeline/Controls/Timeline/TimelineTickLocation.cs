using System;
using WaveformTimeline.Commons;

namespace WaveformTimeline.Controls.Timeline
{
    public sealed class TimelineTickLocation
    {
        public TimelineTickLocation(TuneDuration tuneDuration, WaveformDimensions waveformDimensions)
        {
            _tuneDuration = tuneDuration;
            _waveformDimensions = waveformDimensions;
        }

        private readonly TuneDuration _tuneDuration;
        private readonly WaveformDimensions _waveformDimensions;

        public double LocationOnXAxis(TimeSpan sec) =>
            _waveformDimensions.LeftMargin() + (sec.TotalSeconds - _tuneDuration.StartingPoint()) / _tuneDuration.Duration() * _waveformDimensions.Width();

    }
}
