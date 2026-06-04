#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.VisualTree;
using WaveformTimeline.Commons;
using WaveformTimeline.Contracts;

namespace WaveformTimeline.Controls.Waveform
{
    public sealed class Waveform : BaseControl
    {
        public Waveform()
        {
            _redrawObservable = new RedrawObservable();
        }

        private SynchronizationContext? _uiContext;
        private readonly RedrawObservable _redrawObservable;
        private IDisposable? _redrawDisposable;
        private IDisposable? _waveformBuildDisposable;
        private IDisposable? _boundsDisposable;
        private readonly Path _leftPath = new();
        private readonly Path _rightPath = new();
        private readonly Line _centerLine = new();
        private readonly List<Line> _leftSideOffsetDashes = new();
        private readonly List<Line> _rightSideOffsetDashes = new();
        private RenderedToDimensions? _lastRenderedToDimensions;
        private BackgroundWorker? _renderingInBackground;

        private class RenderedToDimensions(ITune tune, WaveformDimensions dimensions)
        {
            public ITune Tune { get; } = tune;
            public WaveformDimensions Dimensions { get; } = dimensions;
        }

        public static readonly StyledProperty<IBrush> LeftLevelBrushProperty =
            AvaloniaProperty.Register<Waveform, IBrush>(nameof(LeftLevelBrush),
                defaultValue: new SolidColorBrush(Colors.Blue));

        public IBrush LeftLevelBrush
        {
            get => GetValue(LeftLevelBrushProperty);
            set => SetValue(LeftLevelBrushProperty, value);
        }

        public static readonly StyledProperty<IBrush> RightLevelBrushProperty =
            AvaloniaProperty.Register<Waveform, IBrush>(nameof(RightLevelBrush),
                defaultValue: new SolidColorBrush(Colors.Red));

        public IBrush RightLevelBrush
        {
            get => GetValue(RightLevelBrushProperty);
            set => SetValue(RightLevelBrushProperty, value);
        }

        public static readonly StyledProperty<IBrush> CenterLineBrushProperty =
            AvaloniaProperty.Register<Waveform, IBrush>(nameof(CenterLineBrush),
                defaultValue: new SolidColorBrush(Colors.Black));

        public IBrush CenterLineBrush
        {
            get => GetValue(CenterLineBrushProperty);
            set => SetValue(CenterLineBrushProperty, value);
        }

        public static readonly StyledProperty<double> CenterLineThicknessProperty =
            AvaloniaProperty.Register<Waveform, double>(nameof(CenterLineThickness), defaultValue: 1.0,
                coerce: (_, v) => Math.Max(v, 0.0d));

        public double CenterLineThickness
        {
            get => GetValue(CenterLineThicknessProperty);
            set => SetValue(CenterLineThicknessProperty, value);
        }

        public static readonly StyledProperty<int> WaveformResolutionProperty =
            AvaloniaProperty.Register<Waveform, int>(nameof(WaveformResolution), defaultValue: 2000,
                coerce: (_, v) => Math.Max(1000, Math.Min(16000, v)));

        public int WaveformResolution
        {
            get => GetValue(WaveformResolutionProperty);
            set => SetValue(WaveformResolutionProperty, value);
        }

        public static readonly StyledProperty<bool> AutoScaleWaveformCacheProperty =
            AvaloniaProperty.Register<Waveform, bool>(nameof(AutoScaleWaveformCache), defaultValue: false);

        public bool AutoScaleWaveformCache
        {
            get => GetValue(AutoScaleWaveformCacheProperty);
            set => SetValue(AutoScaleWaveformCacheProperty, value);
        }

        public static readonly StyledProperty<bool> ProgressiveRenderingProperty =
            AvaloniaProperty.Register<Waveform, bool>(nameof(ProgressiveRendering), defaultValue: true);

        public bool ProgressiveRendering
        {
            get => GetValue(ProgressiveRenderingProperty);
            set => SetValue(ProgressiveRenderingProperty, value);
        }

        static Waveform()
        {
            LeftLevelBrushProperty.Changed.AddClassHandler<Waveform>((w, e) => w._leftPath.Fill = (IBrush?)e.NewValue);
            RightLevelBrushProperty.Changed.AddClassHandler<Waveform>((w, e) => w._rightPath.Fill = (IBrush?)e.NewValue);
            CenterLineBrushProperty.Changed.AddClassHandler<Waveform>((w, e) => w._centerLine.Stroke = (IBrush?)e.NewValue);
            CenterLineThicknessProperty.Changed.AddClassHandler<Waveform>((w, e) => w._centerLine.StrokeThickness = (double)(e.NewValue ?? 1.0));
            WaveformResolutionProperty.Changed.AddClassHandler<Waveform>((w, _) => w._redrawObservable.Increment());
            AutoScaleWaveformCacheProperty.Changed.AddClassHandler<Waveform>((w, _) => w.UpdateWaveformCacheScaling());
        }

        protected override void OnTuneChanged() => _redrawObservable.Increment();

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            MainCanvas = e.NameScope.Find<Canvas>("PART_Waveform");
            if (MainCanvas == null) return;

            _uiContext = SynchronizationContext.Current;
            MainCanvas.Background = new SolidColorBrush(Colors.Transparent);
            MainCanvas.Children.Add(_centerLine);
            MainCanvas.Children.Add(_leftPath);
            MainCanvas.Children.Add(_rightPath);

            if (CenterLineBrush != null)
            {
                _centerLine.StartPoint = new Point(0, MainCanvas.Bounds.Height);
                _centerLine.EndPoint = new Point(MainCanvas.Bounds.Width, MainCanvas.Bounds.Height);
            }
            UpdateWaveformCacheScaling();

            if (_uiContext != null && _redrawDisposable == null)
            {
                _redrawDisposable = _redrawObservable.Sample(TimeSpan.FromMilliseconds(100))
                    .ObserveOn(_uiContext)
                    .Subscribe(_ => Render());
            }

            _boundsDisposable?.Dispose();
            _boundsDisposable = MainCanvas.GetObservable(BoundsProperty)
                .Subscribe(_ => _redrawObservable.Increment());
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            _redrawDisposable?.Dispose();
            _boundsDisposable?.Dispose();
        }

        private double AdjustedByTransformM11(ITransform? transform)
        {
            if (transform == null) return 1.0;
            var m = transform.Value;
            return Math.Abs(m.M12) < 0.001 && Math.Abs(m.M21) < 0.001 &&
                   Math.Abs(m.M31) < 0.001 && Math.Abs(m.M32) < 0.001 &&
                   Math.Abs(m.M11 - m.M22) < 0.001
                ? m.M11 : 1.0;
        }

        private double TotalTransformScaleFn()
        {
            double scale = 1.0;
            Visual? current = this;
            while (current != null)
            {
                scale *= AdjustedByTransformM11(current.RenderTransform);
                current = current.GetVisualParent();
            }
            return scale;
        }

        private void UpdateWaveformCacheScaling()
        {
        }

        private static float[] CreateFloats(byte[] bytes)
        {
            var floats = new float[bytes.Length / 4];
            Buffer.BlockCopy(bytes, 0, floats, 0, bytes.Length);
            return floats;
        }

        private bool ShouldRedraw() =>
            MainCanvas != null &&
            (_lastRenderedToDimensions == null || !AreTunesTheSame() || !AreDimensionsSame());

        private bool AreTunesTheSame() => Tune.Name() == _lastRenderedToDimensions?.Tune.Name();
        private bool AreDimensionsSame() => WaveformDimensions.Equals(_lastRenderedToDimensions?.Dimensions);

        protected override void Render()
        {
            MeasureArea();
            if (!ShouldRedraw() || MainCanvas == null) return;

            Clear();
            var centerHeight = MainCanvas.Bounds.Height / 2.0d;
            var availableWidth = MainCanvas.Bounds.Width - WaveformDimensions.RightMargin();
            var leftWaveformPolyLine1 = new PolyLineSegment();
            var rightWaveformPolyLine1 = new PolyLineSegment();
            Point StartPoint() => new(WaveformDimensions.LeftMargin(), centerHeight);
            var leftFigure = new PathFigure { StartPoint = StartPoint(), IsClosed = false };
            leftFigure.Segments!.Add(leftWaveformPolyLine1);
            var leftGeometry = new PathGeometry();
            leftGeometry.Figures!.Add(leftFigure);
            _leftPath.Data = leftGeometry;
            var rightFigure = new PathFigure { StartPoint = StartPoint(), IsClosed = false };
            rightFigure.Segments!.Add(rightWaveformPolyLine1);
            var rightGeometry = new PathGeometry();
            rightGeometry.Figures!.Add(rightFigure);
            _rightPath.Data = rightGeometry;
            _centerLine.StartPoint = new Point(WaveformDimensions.LeftMargin(), centerHeight);
            _centerLine.EndPoint = new Point(availableWidth, centerHeight);
            MainCanvas.Children.Add(_leftPath);
            MainCanvas.Children.Add(_rightPath);
            MainCanvas.Children.Add(_centerLine);
            if (Tune.TotalTime().TotalSeconds > 0)
            {
                CreateDashedPadding(0, WaveformDimensions.LeftMargin(), _leftSideOffsetDashes);
                if (CoverageArea.Includes(1.0))
                    CreateDashedPadding(availableWidth, WaveformDimensions.RightMargin(), _rightSideOffsetDashes);
            }
            var section = new WaveformSection(CoverageArea, Tune, WaveformResolution);
            var renderWaveform = new WaveformRenderingProgress(WaveformDimensions, section, MainCanvas.Bounds.Height, leftWaveformPolyLine1, rightWaveformPolyLine1);
            var renderingMethod = ProgressiveRendering
                ? (Action<WaveformSection, WaveformRenderingProgress>)RenderProgressively
                : BackgroundReadThenRender;
            renderingMethod(section, renderWaveform);
            _lastRenderedToDimensions = new RenderedToDimensions(Tune, WaveformDimensions);
        }

        private void RenderProgressively(WaveformSection section, WaveformRenderingProgress renderWaveform)
        {
            var waveformFloats = CreateFloats(Tune.WaveformData());
            if (waveformFloats.Length > 0)
            {
                renderWaveform.DrawWaveform(waveformFloats);
                return;
            }
            var resolution = WaveformResolution;
            var observable = Tune.WaveformStream();
            var steps = Math.Min(resolution, 1000);
            if (_uiContext == null) return;
            _waveformBuildDisposable?.Dispose();
            _waveformBuildDisposable = observable.ObserveOn(_uiContext)
                .Buffer(steps)
                .Subscribe(e => renderWaveform.DrawWaveform(e.ToArray()),
                    renderWaveform.CompleteWaveform);
            Task.Run(() => observable.Waveform(resolution));
        }

        private void BackgroundReadThenRender(WaveformSection section, WaveformRenderingProgress renderWaveform)
        {
            var waveformFloats = CreateFloats(Tune.WaveformData());
            if (waveformFloats.Length > 0)
            {
                RenderWaveformSync(renderWaveform, waveformFloats);
                return;
            }
            _renderingInBackground = new BackgroundWorker();
            _renderingInBackground.DoWork += ReadWaveformInBackground;
            _renderingInBackground.RunWorkerCompleted += OnBackgroundRenderingCompleted;
            _renderingInBackground.RunWorkerAsync(new BackgroundRenderingArgs(Tune, renderWaveform, WaveformResolution));
        }

        private void RenderWaveformSync(WaveformRenderingProgress renderWaveform, float[] waveformFloats)
        {
            renderWaveform.DrawWaveform(waveformFloats);
            renderWaveform.CompleteWaveform();
        }

        private void ReadWaveformInBackground(object? sender, DoWorkEventArgs e)
        {
            var args = e.Argument as BackgroundRenderingArgs;
            args?.Tune.WaveformStream().Waveform(args.Resolution);
            e.Result = args;
        }

        private void OnBackgroundRenderingCompleted(object? sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null || e.Result is not BackgroundRenderingArgs args) return;
            RenderWaveformSync(args.RenderWaveform, CreateFloats(Tune.WaveformData()));
        }

        private class BackgroundRenderingArgs(ITune tune, WaveformRenderingProgress renderWaveform, int resolution)
        {
            public ITune Tune { get; } = tune;
            public int Resolution { get; } = resolution;
            public WaveformRenderingProgress RenderWaveform { get; } = renderWaveform;
        }

        private Line DrawDash(int i, double centerPos, double startPos, int dashSize, int inBetweenDashesSpace) =>
            new()
            {
                Stroke = CenterLineBrush,
                StrokeThickness = CenterLineThickness,
                StartPoint = new Point(i == 0 ? startPos : startPos + i * dashSize + i * inBetweenDashesSpace, centerPos),
                EndPoint = new Point((i == 0 ? startPos : startPos + i * dashSize + i * inBetweenDashesSpace) + dashSize, centerPos)
            };

        private void CreateDashedPadding(double startPos, double spaceInPx, List<Line> dashes)
        {
            if (MainCanvas == null) return;
            const int minDashSize = 3;
            const int maxDashCount = 5;
            const int minInBetweenDashesSpace = 3;
            int dashSize = minDashSize;
            int dashCount = Math.Min(maxDashCount, (int)Math.Floor(WaveformDimensions.LeftMargin() / dashSize));
            var dashTotalWidth = dashCount * minDashSize;
            dashSize += Math.Max(0, (int)Math.Floor((spaceInPx - dashTotalWidth - ((dashCount - 1) * minInBetweenDashesSpace)) / dashCount));
            int inBetweenDashesSpace = Math.Max(minInBetweenDashesSpace, (int)Math.Floor((WaveformDimensions.LeftMargin() - (dashSize * dashCount)) / dashCount));
            var centerPos = MainCanvas.Bounds.Height / 2;
            var lines = Enumerable.Range(0, dashCount)
                .Select(i => DrawDash(i, centerPos, startPos, dashSize, inBetweenDashesSpace));
            foreach (var dash in lines)
            {
                dashes.Add(dash);
                MainCanvas.Children.Add(dash);
            }
        }

        public void Clear()
        {
            _waveformBuildDisposable?.Dispose();
            MainCanvas?.Children.Clear();
        }
    }
}
