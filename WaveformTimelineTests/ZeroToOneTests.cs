using NUnit.Framework;
using WaveformTimeline;
using WaveformTimeline.Primitives;

namespace WaveformTimelineTests
{
    [TestFixture]
    internal class ZeroToOneTests
    {
        [Test]
        public void NanIsZero()
        {
            Assert.AreEqual(0, new ZeroToOne(double.NaN).Value());
        }

        [Test]
        public void NegInfinityIsZero()
        {
            Assert.AreEqual(0, new ZeroToOne(double.NegativeInfinity).Value());
        }

        [Test]
        public void PosInfinityIsOne()
        {
            Assert.AreEqual(1, new ZeroToOne(double.PositiveInfinity).Value());
        }

        [Test]
        public void CannotGoBelowZero()
        {
            Assert.AreEqual(0, new ZeroToOne(-0.0001).Value());
        }

        [Test]
        public void CannotGoAboveOne()
        {
            Assert.AreEqual(1, new ZeroToOne(1.0001).Value());
        }

        [Test]
        public void ValueBetweenZeroAndOneIsKept()
        {
            Assert.AreEqual(0.5478, new ZeroToOne(0.5478).Value());
        }

        [Test]
        public void EqualsTestWhenInRange()
        {
            var zto1 = new ZeroToOne(0.4);
            Assert.IsTrue(zto1 == new ZeroToOne(0.4));
            Assert.IsTrue(zto1.Equals(new ZeroToOne(0.4)));
        }

        [Test]
        public void EqualsTestWhenOutOfRangeNeg()
        {
            var zto1 = new ZeroToOne(-0.5);
            Assert.IsTrue(zto1 == new ZeroToOne(-10d));
            Assert.IsTrue(zto1.Equals(new ZeroToOne(-10d)));
        }

        [Test]
        public void EqualsTestWhenOutOfRangePos()
        {
            var zto1 = new ZeroToOne(1.5);
            Assert.IsTrue(zto1 == new ZeroToOne(20d));
            Assert.IsTrue(zto1.Equals(new ZeroToOne(20d)));
        }

    }
}
