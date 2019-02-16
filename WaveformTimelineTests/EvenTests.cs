using NUnit.Framework;
using WaveformTimeline;
using WaveformTimeline.Primitives;

namespace WaveformTimelineTests
{
    [TestFixture]
    internal class EvenTests
    {
        [Test]
        public void ThreeEqualsFour()
        {
            Assert.AreEqual(4, new Even(3).Value());
            Assert.AreEqual(4, new Even(3, 1).Value());
            Assert.AreEqual(4, new Even(3, 7).Value());
            Assert.AreEqual(4, new Even(3, 0).Value());
            Assert.AreEqual(4, new Even(3, -5).Value());
        }

        [Test]
        public void ThreeEqualsTwo()
        {
            Assert.AreEqual(2, new Even(3, -1).Value());
        }

        [Test]
        public void FourEqualsFour()
        {
            Assert.AreEqual(4, new Even(4).Value());
            Assert.AreEqual(4, new Even(4, 1).Value());
            Assert.AreEqual(4, new Even(4, -1).Value());
        }

    }
}
