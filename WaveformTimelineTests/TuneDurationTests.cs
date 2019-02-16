using NUnit.Framework;
using WaveformTimeline;
using WaveformTimeline.Commons;
using WaveformTimeline.Primitives;

namespace WaveformTimelineTests
{
    [TestFixture]
    internal class TuneDurationTests
    {
        [Test]
        public void FactsAboutTheFullDuration()
        {
            var td = new TuneDuration(180);
            Assert.AreEqual(180d, td.Duration());
            Assert.AreEqual(180d, td.Remaining(0d));
            Assert.AreEqual(90d, td.Remaining(90d));
            Assert.IsTrue(td.Includes(0));
            Assert.IsTrue(td.Includes(new ZeroToOne(0)));
            Assert.IsTrue(td.Includes(1));
            Assert.IsTrue(td.Includes(new ZeroToOne(1)));
            Assert.AreEqual(0d, td.StartingPoint());
            Assert.AreEqual(0d, td.Progress(0d).Value());
            Assert.AreEqual(1d, td.Progress(180d).Value());
            Assert.AreEqual(0.5d, td.Progress(90d).Value());
            Assert.AreEqual(45d, td.ActualPosition(0.25d));
            Assert.AreEqual(0d, td.HiddenBefore(2450));
        }

        [Test]
        public void FactsAboutZoomedInDurationThatIncludesStart()
        {
            var td = new TuneDuration(18, 180, 2.0);
            Assert.AreEqual(90d, td.Duration());
            Assert.AreEqual(90d, td.Remaining(0d));
            Assert.AreEqual(10, td.Remaining(80d));
            Assert.IsTrue(td.Includes(0));
            Assert.IsTrue(td.Includes(new ZeroToOne(0)));
            Assert.IsFalse(td.Includes(1));
            Assert.IsFalse(td.Includes(new ZeroToOne(1)));
            Assert.IsTrue(td.Includes(0.5));
            Assert.IsTrue(td.Includes(new ZeroToOne(0.5)));
            Assert.AreEqual(0d, td.StartingPoint());
            Assert.AreEqual(0d, td.Progress(0d).Value());
            Assert.AreEqual(1d, td.Progress(90d).Value());
            Assert.AreEqual(0.5d, td.Progress(45d).Value());
            Assert.AreEqual(45d, td.ActualPosition(0.5d));
            Assert.AreEqual(0d, td.HiddenBefore(2450));
        }

        [Test]
        public void FactsAboutZoomedInDurationThatIncludesEnd()
        {
            var td = new TuneDuration(120, 180, 2.0);
            Assert.AreEqual(90d, td.Duration());
            Assert.AreEqual(90d, td.Remaining(90d));
            Assert.AreEqual(80, td.Remaining(100d));
            Assert.IsFalse(td.Includes(0));
            Assert.IsFalse(td.Includes(new ZeroToOne(0)));
            Assert.IsTrue(td.Includes(1));
            Assert.IsTrue(td.Includes(new ZeroToOne(1)));
            Assert.IsTrue(td.Includes(0.5));
            Assert.IsTrue(td.Includes(new ZeroToOne(0.5)));
            Assert.AreEqual(90d, td.StartingPoint());
            Assert.AreEqual(0d, td.Progress(0d).Value());
            Assert.AreEqual(0d, td.Progress(90d).Value());
            Assert.AreEqual(1d, td.Progress(180d).Value());
            Assert.AreEqual(135d, td.ActualPosition(0.5d));
            Assert.AreEqual(1225d, td.HiddenBefore(2450));
        }

        [Test]
        public void GuardsValidPositionForRemaining()
        {
            var td = new TuneDuration(180, 1.0);
            Assert.AreEqual(180d, td.Remaining(-10d));
            Assert.AreEqual(0d, td.Remaining(10020d));
        }

        [Test]
        public void EqualityTest()
        {
            var td1 = new TuneDuration(0, 180, 1.0);
            Assert.IsTrue(td1.Equals(new TuneDuration(50, 180, 1.0d)));
            Assert.IsTrue(td1 == new TuneDuration(0, 180, 1));
            Assert.IsFalse(td1.Equals(new TuneDuration(0, 120, 1)));
            Assert.IsFalse(td1 == new TuneDuration(0, 120, 1));
        }
    }
}
