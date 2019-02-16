using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using MoreLinq;
using WaveformTimeline.Commons;
using WaveformTimeline.Primitives;
using Brush = System.Windows.Media.Brush;
using Rectangle = System.Windows.Shapes.Rectangle;

namespace WaveformTimeline.Controls.Timeline
{
    /// <summary>
    /// Displays the minute / second marks on a timeline corresponding to the total length of the audio stream.
    /// </summary>
    [DisplayName(@"Timeline")]
    [Description("Displays the minute / second marks on a timeline corresponding to the total length of the audio stream.")]
    [ToolboxItem(true)]
    [TemplatePart(Name = "PART_Timeline", Type = typeof(Canvas))]
    public sealed class Timeline: BaseControl
    {
        public Timeline()
        {
            _redrawObservable = new RedrawObservable();
            Unloaded += OnUnloaded;
        }

        private readonly RedrawObservable _redrawObservable;
        private IDisposable _redrawDisposable;
        private readonly Line _timelineTickLine = new Line();
        private readonly List<Line> _timeLineTicks = new List<Line>();
        private readonly Rectangle _timelineBackgroundRegion = new Rectangle();
        private readonly List<TextBlock> _timestampTextBlocks = new List<TextBlock>();

        protected override void OnTuneChanged() => _redrawObservable.Increment();

        /// <summary>
        /// Identifies the <see cref="TimelineTickBrush" /> dependency property. 
        /// </summary>
        public static readonly DependencyProperty TimelineTickBrushProperty
            = DependencyProperty.Register("TimelineTickBrush", typeof(Brush), typeof(Timeline),
                new UIPropertyMetadata(new SolidColorBrush(Colors.Black), OnTimelineTickBrushChanged));

        private static void OnTimelineTickBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => 
            (d as Timeline)?.OnTimelineTickBrushChanged((Brush)e.NewValue);

        private void OnTimelineTickBrushChanged(Brush newBrush) => MainCanvas.Children.OfType<Line>().ForEach(line => line.Stroke = newBrush);

        /// <summary>
        /// Color of the timeline line and ticks
        /// </summary>
        [Category("Brushes")]
        public Brush TimelineTickBrush
        {
            get => (Brush) GetValue(TimelineTickBrushProperty);
            set => SetValue(TimelineTickBrushProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="MajorTickHeight" /> dependency property. 
        /// </summary>
        public static readonly DependencyProperty MajorTickHeightProperty = DependencyProperty.Register(
            "MajorTickHeight", typeof(int), typeof(Timeline), new PropertyMetadata(10));

        /// <summary>
        /// Height of the major tick in px
        /// </summary>
        [Category("Display")]
        public int MajorTickHeight
        {
            get => (int) GetValue(MajorTickHeightProperty);
            set => SetValue(MajorTickHeightProperty, Math.Max(1, value));
        }

        /// <summary>
        /// Identifies the <see cref="MinorTickHeight" /> dependency property. 
        /// </summary>
        public static readonly DependencyProperty MinorTickHeightProperty = DependencyProperty.Register(
            "MinorTickHeight", typeof(int), typeof(Timeline), new PropertyMetadata(3));

        /// <summary>
        /// Height of the minor tick in px
        /// </summary>
        [Category("Display")]
        public int MinorTickHeight
        {
            get => (int) GetValue(MinorTickHeightProperty);
            set => SetValue(MinorTickHeightProperty, Math.Max(1, value));
        }

        /// <summary>
        /// Identifies the <see cref="EmptyTuneDurationInSeconds" /> dependency property. 
        /// </summary>
        public static readonly DependencyProperty EmptyTuneDurationInSecondsProperty = DependencyProperty.Register(
            "EmptyTuneDurationInSeconds", typeof(int), typeof(Timeline), new PropertyMetadata(180));

        /// <summary>
        /// Duration of the empty tune. The client might want to "fake" a duration when no tune is loaded.
        /// </summary>
        public int EmptyTuneDurationInSeconds
        {
            get => (int)GetValue(EmptyTuneDurationInSecondsProperty);
            set => SetValue(EmptyTuneDurationInSecondsProperty, Math.Max(0, value));
        }

        /// <summary>
        /// Identifies the <see cref="TimelineType" /> dependency property. 
        /// </summary>
        public static readonly DependencyProperty TimelineTypeProperty = DependencyProperty.Register(
            "TimelineType", typeof(TimelineType), typeof(Timeline), new PropertyMetadata(TimelineType.Constant));

        /// <summary>
        /// Whether to show the ticks at constant intervals, or show more ticks and marks toward the end
        /// </summary>
        [Category("Display")]
        public TimelineType TimelineType
        {
            get => (TimelineType) GetValue(TimelineTypeProperty);
            set => SetValue(TimelineTypeProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="EndRevealingMark" /> dependency property. 
        /// </summary>
        public static readonly DependencyProperty EndRevealingMarkProperty = DependencyProperty.Register(
            "EndRevealingMark", typeof(ZeroToOne), typeof(Timeline), new PropertyMetadata(new ZeroToOne(0.75)));

        /// <summary>
        /// At which percentage of the Tune's length should the timeline show more frequent ticks at marks. Ignored for TimelineType.Constant.
        /// </summary>
        [Category("Display")]
        public ZeroToOne EndRevealingMark
        {
            get => (ZeroToOne) GetValue(EndRevealingMarkProperty);
            set => SetValue(EndRevealingMarkProperty, value);
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            MainCanvas = GetTemplateChild("PART_Timeline") as Canvas;
            Debug.Assert(MainCanvas != null, "timeline canvas cannot be null");
            var context = SynchronizationContext.Current;
            if (context != null && _redrawDisposable == null)
            {
                _redrawDisposable = _redrawObservable.Throttle(TimeSpan.FromMilliseconds(100))
                    .ObserveOn(context)
                    .Subscribe(i => Render());
            }
        }

        protected override void OnTemplateChanged(ControlTemplate oldTemplate, ControlTemplate newTemplate)
        {
            base.OnTemplateChanged(oldTemplate, newTemplate);
            MainCanvas.Children.Clear();
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            _redrawObservable.Increment();
        }

        private TextBlock DrawText(string text) => new TextBlock
            {
                FontFamily = FontFamily,
                FontStyle = FontStyle,
                FontWeight = FontWeight,
                FontStretch = FontStretch,
                FontSize = FontSize,
                Foreground = Foreground,
                Text = text
            };

        private Line DrawLine(double xLocation, double bottomLoc) => new Line
        {
            Stroke = TimelineTickBrush,
            StrokeThickness = 1.0d,
            X1 = xLocation,
            Y1 = bottomLoc,
            X2 = xLocation,
            Y2 = bottomLoc - MinorTickHeight
        };

        private Line LinesAtMajorTickAreLonger(Line Line, TimeSpan Second, List<TimeSpan> majorTicksAt, double bottomLoc)
        {
            if (majorTicksAt.Contains(Second))
            {
                Line.Y2 = bottomLoc - MajorTickHeight;
            }
            return Line;
        }


        private TextBlock WithMargin(TextBlock tb, double loc)
        {
            tb.Margin = new Thickness(loc + 2, 0, 0, 0);
            return tb;
        }

        protected override void MeasureArea()
        {
            base.MeasureArea();
            var tune = MainCanvas.RenderSize.Width <= 0.0 
                ? new NoTune() 
                : Tune is NoTune || Math.Abs(new FiniteDouble(Tune.TotalTime().TotalSeconds, 0.0d).Value()) < 0.001
                    ? new NoTune(EmptyTuneDurationInSeconds) // case of no real tune having been supplied
                    : Tune;
            _coverageArea = new TuneDuration(tune, Zoom);
            _waveformDimensions = new WaveformDimensions(_coverageArea, MainCanvas.RenderSize.Width);
        }


        /// <summary>
        /// Update the timeline ticks and minute / second marks
        /// </summary>
        protected override void Render()
        {
            Clear();
            MeasureArea();
            var timelineSource = new TimelineSource(_coverageArea);
            var bottomLoc = MainCanvas.RenderSize.Height - 1;
            var firstMark = timelineSource.Beginning;
            var timelineMarkingStrategy = TimelineType.Strategy(_coverageArea, firstMark, EndRevealingMark);
            var timelineTickLocation = new TimelineTickLocation(_coverageArea, _waveformDimensions);
            var listOfSeconds = timelineSource.Seconds().ToList();
            var majorTicksAt = listOfSeconds.Where(timelineMarkingStrategy.AtMajorTick).ToList();
            _timelineTickLine.X1 = 0;
            _timelineTickLine.X2 = MainCanvas.RenderSize.Width;
            _timelineTickLine.Y1 = MainCanvas.RenderSize.Height;
            _timelineTickLine.Y2 = MainCanvas.RenderSize.Height;
            _timelineTickLine.Stroke = TimelineTickBrush;
            _timelineBackgroundRegion.Width = MainCanvas.RenderSize.Width;
            _timelineBackgroundRegion.Height = MainCanvas.RenderSize.Height;
            MainCanvas.Children.Add(_timelineTickLine);
            MainCanvas.Children.Add(_timelineBackgroundRegion);
            _timeLineTicks.AddRange(
                listOfSeconds.Where(timelineMarkingStrategy.AtMinorTick).Select(sec => (Second: sec, Location: timelineTickLocation.LocationOnXAxis(sec)))
                .Where(t => MainCanvas.RenderSize.Width - t.Location >= 28.0d) // TODO what's this limit of 28.0?
                .Select(t => LinesAtMajorTickAreLonger(DrawLine(t.Location, bottomLoc), t.Second, majorTicksAt, bottomLoc)));
            _timestampTextBlocks.AddRange(majorTicksAt
                .Select(sec => (Second: sec, Location: timelineTickLocation.LocationOnXAxis(sec)))
                .Select(sec => WithMargin(DrawText(timelineSource.TimespanAsString(sec.Second)), sec.Location)));
            _timeLineTicks.ForEach(line => MainCanvas.Children.Add(line));
            _timestampTextBlocks.ForEach(tb => MainCanvas.Children.Add(tb));
        }

        private void Clear()
        {            
            MainCanvas.Children.Clear(); // clear the canvas
            _timestampTextBlocks.ForEach(textblock => MainCanvas.Children.Remove(textblock));
            _timestampTextBlocks.Clear();
            _timeLineTicks.ForEach(line => MainCanvas.Children.Remove(line));
            _timeLineTicks.Clear();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _redrawDisposable?.Dispose();
            Unloaded -= OnUnloaded;
        }
    }
}