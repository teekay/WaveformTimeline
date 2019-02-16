using System;
using System.Linq;
using NUnit.Framework;
using WaveformTimeline;
using WaveformTimeline.Commons;
using WaveformTimeline.Controls.Timeline;

namespace WaveformTimelineTests
{
    [TestFixture]
    internal class TimelineSourceTests
    {
        [Test]
        public void TimelineThatStartsAtZeroHasABeginningOfZero()
        {
            Assert.AreEqual(TimeSpan.Zero, new TimelineSource(new TuneDuration(120)).Beginning);
        }

        [Test]
        public void TimelineOfNonZoomedDurationProvidesAllSeconds()
        {
            var secs = new TimelineSource(new TuneDuration(125)).Seconds().ToList();
            Assert.AreEqual(125, secs.Count);
            Assert.AreEqual(TimeSpan.Zero, secs.First());
            Assert.AreEqual(TimeSpan.FromSeconds(124), secs.Last());
        }

        [Test]
        public void TimelineThatStartsAt60sHasABeginningOf60s()
        {
            Assert.AreEqual(TimeSpan.FromSeconds(60), new TimelineSource(new TuneDuration(60, 120, 2)).Beginning);
        }

        [Test]
        public void TimelineOfZoomedInDurationProvidesSecondsCorrespondingOfCoveredDuration()
        {
            var td = new TuneDuration(120, 360, 4);
            var secs = new TimelineSource(td, 10).Seconds().ToList();
            Assert.AreEqual(85, secs.Count);
            Assert.AreEqual(120 - 45d, td.StartingPoint());
            Assert.AreEqual(TimeSpan.FromSeconds(80), secs.First());
            Assert.AreEqual(TimeSpan.FromSeconds(120 + 44d), secs.Last());
        }


    }
}
