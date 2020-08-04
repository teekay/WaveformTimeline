using System;
using System.Diagnostics;
using JetBrains.Annotations;
using WaveformTimeline.Primitives;

namespace WaveformTimeline.Commons
{
    /// <summary>
    /// Encapsulates significant facts about the waveform control's dimensions
    /// </summary>
    [DebuggerDisplay("{CompleteWidth}/{RenderedWidth} - {LeftPadding}/{RightPadding} - {StartsAtPx}")]
    public readonly struct WaveformDimensions
    {
        public WaveformDimensions(TuneDuration coverageArea, double canvasWidth)
            : this(coverageArea, canvasWidth,
                new FiniteDouble(canvasWidth) * LeftSideMarginPercentDefault,
                new FiniteDouble(canvasWidth) * RightSideMarginPercentDefault)
        {
        }

        public WaveformDimensions(TuneDuration coverageArea, double canvasWidth, 
            double leftPadding, double rightPadding) : 
            this(coverageArea, canvasWidth, leftPadding, rightPadding, ZoomedInLeftMarginPxDefault, ZoomedInRightMarginPxDefault)
        {
        }

        public WaveformDimensions(TuneDuration coverageArea, double canvasWidth, 
            double leftPadding, double rightPadding, double zoomedInLeftPadding, double zoomedInRightPadding) : this()
        {
            var safeCanvasWidth = new FiniteDouble(canvasWidth);
            var safeZoom = Math.Max(1d, new FiniteDouble(coverageArea.Zoom, 1.0));
            LeftPadding = safeZoom > 1
                ? zoomedInLeftPadding
                : leftPadding;
            RightPadding = safeZoom > 1
                ? zoomedInRightPadding
                : rightPadding;
            RenderedWidth = Math.Max(0, (safeCanvasWidth - LeftPadding - RightPadding));
            CompleteWidth = RenderedWidth * safeZoom;
            StartsAtPx = coverageArea.HiddenBefore(CompleteWidth);
        }

        private const double LeftSideMarginPercentDefault = 0.01d;
        private const double RightSideMarginPercentDefault = 0.01d;
        private const double ZoomedInLeftMarginPxDefault = 5;
        private const double ZoomedInRightMarginPxDefault = 5;

        /// <summary>
        /// Actual width of the waveform in px
        /// </summary>
        private double RenderedWidth { get; }

        /// <summary>
        /// How wide the current waveform would be
        /// when Zoom > 1 and only a sub-section is displayed
        /// </summary>
        private double CompleteWidth { get; }

        /// <summary>
        /// Pixel "left padding" of the displayed waveform in px
        /// </summary>
        private double LeftPadding { get; }

        /// <summary>
        /// Right stop of the waveform (&lt;= ActualWidth)
        /// </summary>
        private double RightPadding { get; }

        /// <summary>
        /// Position on the complete waveform where the rendered waveform starts
        /// </summary>
        private double StartsAtPx { get; }

        /// <summary>
        /// Returns whether the instance has zero rendered width
        /// </summary>        
        [Pure]
        public bool AreEmpty() => RenderedWidth <= 0;

        /// <summary>
        /// Takes a location on the virtual waveform, and subtracts the pixels hidden from view from the left.
        /// Used to determine the locations of cue points.
        /// Att: can be negative!
        /// </summary>
        /// <param name="location"></param>        
        [Pure]
        public double AbsoluteLocationToRendered(double location) => location - StartsAtPx;

        /// <summary>
        /// Reveals the left margin (no content) in pixels
        /// </summary>
        [Pure]
        public double LeftMargin() => LeftPadding;

        /// <summary>
        /// Reveals the right margin (no content) in pixels
        /// </summary>        
        [Pure]
        public double RightMargin() => RightPadding;

        /// <summary>
        /// Returns the sum of left and right margins in pixels
        /// </summary>        
        [Pure]
        public double Margins() => LeftMargin() + RightMargin();

        /// <summary>
        /// Returns the width of the currently rendered waveform without margins
        /// </summary>        
        [Pure]
        public double Width() => RenderedWidth;

        /// <summary>
        /// Given a number between 0-1, representing the percentage of the rendered area,
        /// provides its location on the X-axis plus the value of the left margin.
        /// </summary>
        /// <param name="point"></param>        
        [Pure]
        public double PositionOnRenderedWaveform(ZeroToOne point) => new FiniteDouble(point) * RenderedWidth + LeftMargin();

        /// <summary>
        /// Given a position on the X-axis of the rendered area, returns a number betweem 0-1
        /// representing the percentage of the area covered to the left of the position.
        /// </summary>
        /// <param name="position"></param>        
        [Pure]
        public ZeroToOne PercentOfRenderedWaveform(double position) => (new FiniteDouble(position) - LeftMargin()) / RenderedWidth;

        /// <summary>
        /// Given a number 0-1, returns its x-location in pixels on the virtual waveform
        /// </summary>
        /// <param name="point"></param>        
        [Pure]
        public double PositionOnCompleteWaveform(ZeroToOne point) => new FiniteDouble(point) * CompleteWidth;

        /// <summary>
        /// Given a location in pixels, returns a number between 0-1 corresponding to where this number is on the complete waveform,
        /// which is to say, on the complete waveform, even though only a portion of it might be rendered.
        /// </summary>
        /// <param name="location"></param>        
        [Pure]
        public ZeroToOne PercentOfCompleteWaveform(double location) =>
           new ZeroToOne(
               new FiniteDouble((location + StartsAtPx - LeftPadding) / CompleteWidth));

        public static bool operator ==(WaveformDimensions w1, WaveformDimensions w2) => w1.Equals(w2);

        public static bool operator !=(WaveformDimensions w1, WaveformDimensions w2) => !(w1 == w2);

        public override bool Equals(object obj)
        {
            return obj is WaveformDimensions dimensions &&
                   dimensions.RenderedWidth.Equals(RenderedWidth) &&
                   dimensions.CompleteWidth.Equals(CompleteWidth) &&
                   dimensions.LeftPadding.Equals(LeftPadding) &&
                   dimensions.RightPadding.Equals(RightPadding) &&
                   dimensions.StartsAtPx.Equals(StartsAtPx);
        }

        public bool Equals(WaveformDimensions other)
        {
            return RenderedWidth.Equals(other.RenderedWidth) && 
                   CompleteWidth.Equals(other.CompleteWidth) && 
                   LeftPadding.Equals(other.LeftPadding) && 
                   RightPadding.Equals(other.RightPadding) && 
                   StartsAtPx.Equals(other.StartsAtPx);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = RenderedWidth.GetHashCode();
                hashCode = (hashCode * 397) ^ CompleteWidth.GetHashCode();
                hashCode = (hashCode * 397) ^ LeftPadding.GetHashCode();
                hashCode = (hashCode * 397) ^ RightPadding.GetHashCode();
                hashCode = (hashCode * 397) ^ StartsAtPx.GetHashCode();
                return hashCode;
            }
        }
    }
}