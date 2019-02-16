using System;
using NUnit.Framework;
using WaveformTimeline;
using WaveformTimeline.Commons;
using WaveformTimeline.Contracts;
using WaveformTimeline.Controls.Waveform;

namespace WaveformTimelineTests
{
    [TestFixture]
    internal class WaveformSectionTests
    {
        private class FakeTune : ITimedPlayback
        {
            public FakeTune(): this(0)
            {
            }

            public FakeTune(double duration)
            {
                _duration = duration;
            }

            private readonly double _duration;

            public TimeSpan CurrentTime() => TimeSpan.Zero;

            public TimeSpan TotalTime() => TimeSpan.FromSeconds(120);

            public TimeSpan Duration() => TotalTime();
        }

        [Test]
        public void StartEndAtZoom1()
        {
            var wfs = new WaveformSection(new TuneDuration(120), new FakeTune(120), 8000);
            Assert.AreEqual(0, wfs.Start);
            Assert.AreEqual(8000, wfs.End);
        }

        [Test]
        public void StartEndAtZoom2Pos0()
        {
            var wfs = new WaveformSection(new TuneDuration(120, 2.0), new FakeTune(120), 8000);
            Assert.AreEqual(0, wfs.Start);
            Assert.AreEqual(4000, wfs.End);
        }

        [Test]
        public void StartEndAtZoom2Pos85()
        {
            var wfs = new WaveformSection(new TuneDuration(85, 120, 2.0), new FakeTune(120), 8000);
            Assert.AreEqual(4000, wfs.Start);
            Assert.AreEqual(8000, wfs.End);
        }

    }
}
