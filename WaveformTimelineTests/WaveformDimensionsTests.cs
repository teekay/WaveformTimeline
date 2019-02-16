using NUnit.Framework;
using WaveformTimeline;
using WaveformTimeline.Commons;

namespace WaveformTimelineTests
{
    [TestFixture]
    internal class WaveformDimensionsTests
    {
        [Test]
        public void EmptyDimensionsAreEmpty()
        {
            Assert.IsTrue(new WaveformDimensions(new TuneDuration(0, 120, 1), 0).AreEmpty());
        }

        [Test]
        public void NonEmptyDimensionsAreNotEmpty()
        {
            Assert.IsFalse(new WaveformDimensions(new TuneDuration(0, 120, 1), 800).AreEmpty());
        }

        [Test]
        public void NonZoomedRenderedLocationEqualsActual()
        {
            Assert.AreEqual(50, 
                new WaveformDimensions(new TuneDuration(0, 120, 1), 800)
                    .AbsoluteLocationToRendered(50));
        }

        [Test]
        public void ZoomedInRenderedLocationDiffersFromActual()
        {
            Assert.AreEqual(-800.0d, 
                new WaveformDimensions(
                        new TuneDuration(60, 120, 2), 
                        800, 0, 0, 0, 0)
                    .AbsoluteLocationToRendered(0));
        }

        [Test]
        public void RenderedWidthIsWithoutMargins()
        {
            Assert.AreEqual(700, 
                new WaveformDimensions(
                    new TuneDuration(0, 120, 1), 
                    800, 50, 50, 0, 0)
                    .Width());
        }


    }
}
