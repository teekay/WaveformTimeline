using NUnit.Framework;
using WaveformTimeline;
using WaveformTimeline.Primitives;

namespace WaveformTimelineTests
{
    [TestFixture]
    internal class FiniteDoubleTests
    {
        [Test]
        public void NanValueGoesToDefault()
        {
            Assert.AreEqual(new FiniteDouble(double.NaN).Value(), 0d);
            Assert.AreEqual(new FiniteDouble(double.NaN, 1.0).Value(), 1.0d);
        }

        [Test]
        public void NegInfinityValueGoesToDefault()
        {
            Assert.AreEqual(new FiniteDouble(double.NegativeInfinity).Value(), 0d);
            Assert.AreEqual(new FiniteDouble(double.NegativeInfinity, 1.0).Value(), 1.0d);
        }

        [Test]
        public void PosInfinityValueGoesToDefault()
        {
            Assert.AreEqual(new FiniteDouble(double.PositiveInfinity).Value(), 0d);
            Assert.AreEqual(new FiniteDouble(double.PositiveInfinity, 1.0).Value(), 1.0d);
        }

        [Test]
        public void FiniteValueIsKept()
        {
            Assert.AreEqual(23.768169, new FiniteDouble(23.768169).Value());
            Assert.AreEqual(23.768169, new FiniteDouble(23.768169, 1.0).Value());
        }

        [Test]
        public void EqualsTestInRange()
        {
            var fd1 = new FiniteDouble(41.758);
            Assert.IsTrue(fd1.Equals(new FiniteDouble(41.758)));
            Assert.IsTrue(fd1 == new FiniteDouble(41.758));
        }

        [Test]
        public void EqualsTestOutOfRange()
        {
            var fd1 = new FiniteDouble(double.NaN);
            Assert.IsTrue(fd1.Equals(new FiniteDouble(double.PositiveInfinity)));
            Assert.IsTrue(fd1 == new FiniteDouble(double.PositiveInfinity));
        }

    }
}
