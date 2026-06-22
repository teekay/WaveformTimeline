#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using WaveformTimeline.Commons;
using WaveformTimeline.Primitives;
using Rectangle = Avalonia.Controls.Shapes.Rectangle;

namespace WaveformTimeline.Controls.Timeline
{
    public sealed class Timeline : BaseControl
    {
        public Timeline()
        {
            _redrawObservable = new RedrawObservable();
        }

        private readonly RedrawObservable _redrawObservable;
        private IDisposable? _redrawDisposable;
        private IDisposable? _boundsDisposable;
        private readonly Line _timelineTickLine = new();
        private readonly List<Line> _timeLineTicks = new();
        private readonly Rectangle _timelineBackgroundRegion = new();
        private readonly List<TextBlock> _timestampTextBlocks = new();

        protected override void OnTuneChanged() => _redrawObservable.Increment();

        public static readonly StyledProperty<IBrush> TimelineTickBrushProperty =
            AvaloniaProperty.Register<Timeline, IBrush>(nameof(TimelineTickBrush),
                defaultValue: new SolidColorBrush(Colors.Black));

        public IBrush TimelineTickBrush
        {
            get => GetValue(TimelineTickBrushProperty);
            set => SetValue(TimelineTickBrushProperty, value);
        }

        public static readonly StyledProperty<int> MajorTickHeightProperty =
            AvaloniaProperty.Register<Timeline, int>(nameof(MajorTickHeight), defaultValue: 10);

        public int MajorTickHeight
        {
            get => GetValue(MajorTickHeightProperty);
            set => SetValue(MajorTickHeightProperty, Math.Max(1, value));
        }

        public static readonly StyledProperty<int> MinorTickHeightProperty =
            AvaloniaProperty.Register<Timeline, int>(nameof(MinorTickHeight), defaultValue: 3);

        public int MinorTickHeight
        {
            get => GetValue(MinorTickHeightProperty);
            set => SetValue(MinorTickHeightProperty, Math.Max(1, value));
        }

        public static readonly StyledProperty<int> EmptyTuneDurationInSecondsProperty =
            AvaloniaProperty.Register<Timeline, int>(nameof(EmptyTuneDurationInSeconds), defaultValue: 180);

        public int EmptyTuneDurationInSeconds
        {
            get => GetValue(EmptyTuneDurationInSecondsProperty);
            set => SetValue(EmptyTuneDurationInSecondsProperty, Math.Max(0, value));
        }

        public static readonly StyledProperty<TimelineType> TimelineTypeProperty =
            AvaloniaProperty.Register<Timeline, TimelineType>(nameof(TimelineType), defaultValue: TimelineType.Constant);

        public TimelineType TimelineType
        {
            get => GetValue(TimelineTypeProperty);
            set => SetValue(TimelineTypeProperty, value);
        }

        public static readonly StyledProperty<ZeroToOne> EndRevealingMarkProperty =
            AvaloniaProperty.Register<Timeline, ZeroToOne>(nameof(EndRevealingMark), defaultValue: new ZeroToOne(0.75));

        public ZeroToOne EndRevealingMark
        {
            get => GetValue(EndRevealingMarkProperty);
            set => SetValue(EndRevealingMarkProperty, value);
        }

        static Timeline()
        {
            TimelineTickBrushProperty.Changed.AddClassHandler<Timeline>((t, _) =>
            {
                foreach (var line in t.MainCanvas?.Children.OfType<Line>() ?? Enumerable.Empty<Line>())
                    line.Stroke = t.TimelineTickBrush;
            });
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            MainCanvas = e.NameScope.Find<Canvas>("PART_Timeline");
            Debug.Assert(MainCanvas != null, "timeline canvas cannot be null");
            var context = SynchronizationContext.Current;
            if (context != null && _redrawDisposable == null)
            {
                _redrawDisposable = _redrawObservable.Throttle(TimeSpan.FromMilliseconds(100))
                    .ObserveOn(context)
                    .Subscribe(_ => Render());
            }
            _boundsDisposable?.Dispose();
            _boundsDisposable = MainCanvas!.GetObservable(BoundsProperty)
                .Subscribe(_ => _redrawObservable.Increment());
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            _redrawDisposable?.Dispose();
            _boundsDisposable?.Dispose();
        }

        private TextBlock DrawText(string text) => new()
        {
            FontFamily = FontFamily,
            FontStyle = FontStyle,
            FontWeight = FontWeight,
            FontSize = FontSize,
            Foreground = Foreground,
            Text = text
        };

        private Line DrawLine(double xLocation, double bottomLoc) => new()
        {
            Stroke = TimelineTickBrush,
            StrokeThickness = 1.0d,
            StartPoint = new Point(xLocation, bottomLoc),
            EndPoint = new Point(xLocation, bottomLoc - MinorTickHeight)
        };

        private Line LinesAtMajorTickAreLonger(Line line, TimeSpan second, List<TimeSpan> majorTicksAt, double bottomLoc)
        {
            if (majorTicksAt.Contains(second))
                line.EndPoint = new Point(line.EndPoint.X, bottomLoc - MajorTickHeight);
            return line;
        }

        private TextBlock WithMargin(TextBlock tb, double loc)
        {
            tb.Margin = new Thickness(loc + 2, 0, 0, 0);
            return tb;
        }

        protected override void MeasureArea()
        {
            base.MeasureArea();
            var tune = MainCanvas!.Bounds.Width <= 0.0
                ? new NoTune()
                : Tune is NoTune || Math.Abs(new FiniteDouble(Tune.TotalTime().TotalSeconds, 0.0d).Value()) < 0.001
                    ? new NoTune(EmptyTuneDurationInSeconds)
                    : Tune;
            CoverageArea = new TuneDuration(tune, Zoom);
            WaveformDimensions = new WaveformDimensions(CoverageArea, MainCanvas.Bounds.Width);
        }

        protected override void Render()
        {
            Clear();
            MeasureArea();
            var timelineSource = new TimelineSource(CoverageArea);
            var bottomLoc = MainCanvas!.Bounds.Height - 1;
            var firstMark = timelineSource.Beginning;
            var timelineMarkingStrategy = TimelineType.Strategy(CoverageArea, firstMark, EndRevealingMark);
            var timelineTickLocation = new TimelineTickLocation(CoverageArea, WaveformDimensions);
            var listOfSeconds = timelineSource.Seconds().ToList();
            var majorTicksAt = listOfSeconds.Where(timelineMarkingStrategy.AtMajorTick).ToList();
            _timelineTickLine.StartPoint = new Point(0, MainCanvas.Bounds.Height);
            _timelineTickLine.EndPoint = new Point(MainCanvas.Bounds.Width, MainCanvas.Bounds.Height);
            _timelineTickLine.Stroke = TimelineTickBrush;
            _timelineBackgroundRegion.Width = MainCanvas.Bounds.Width;
            _timelineBackgroundRegion.Height = MainCanvas.Bounds.Height;
            MainCanvas.Children.Add(_timelineTickLine);
            MainCanvas.Children.Add(_timelineBackgroundRegion);
            _timeLineTicks.AddRange(
                listOfSeconds.Where(timelineMarkingStrategy.AtMinorTick)
                    .Select(sec => (Second: sec, Location: timelineTickLocation.LocationOnXAxis(sec)))
                    .Where(t => MainCanvas.Bounds.Width - t.Location >= 28.0d)
                    .Select(t => LinesAtMajorTickAreLonger(DrawLine(t.Location, bottomLoc), t.Second, majorTicksAt, bottomLoc)));
            _timestampTextBlocks.AddRange(majorTicksAt
                .Select(sec => (Second: sec, Location: timelineTickLocation.LocationOnXAxis(sec)))
                .Select(sec => WithMargin(DrawText(timelineSource.TimespanAsString(sec.Second)), sec.Location)));
            foreach (var line in _timeLineTicks)
                MainCanvas.Children.Add(line);
            foreach (var tb in _timestampTextBlocks)
                MainCanvas.Children.Add(tb);
        }

        private void Clear()
        {
            MainCanvas?.Children.Clear();
            _timestampTextBlocks.Clear();
            _timeLineTicks.Clear();
        }
    }
}
