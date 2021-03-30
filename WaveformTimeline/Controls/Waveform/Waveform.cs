using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using JetBrains.Annotations;
using MoreLinq.Extensions;
using WaveformTimeline.Commons;
using WaveformTimeline.Contracts;

namespace WaveformTimeline.Controls.Waveform
{
    /// <summary>
    /// Responsible for drawing the waveform shape on the provided Canvas using data from the provided ITune instance.
    /// </summary>
    [DisplayName(@"Waveform")]
    [Description("Responsible for drawing the waveform shape on the provided Canvas using data from the provided ITune instance")]
    [ToolboxItem(true)]
    [TemplatePart(Name = "PART_Waveform", Type = typeof(Canvas))]
    public sealed class Waveform: BaseControl
    {
        public Waveform()
        {
            _uiContext = SynchronizationContext.Current;
            _redrawObservable = new RedrawObservable();
            Unloaded += OnUnloaded;
        }

        private readonly SynchronizationContext _uiContext;
        private readonly RedrawObservable _redrawObservable;
        private IDisposable _redrawDisposable;
        private readonly Path _leftPath = new Path();
        private readonly Path _rightPath = new Path();
        private readonly Line _centerLine = new Line();
        private readonly List<Line> _leftSideOffsetDashes = new List<Line>();
        private readonly List<Line> _rightSideOffsetDashes = new List<Line>();
        private IDisposable _waveformBuildDisposable;
        private RenderedToDimensions _lastRenderedToDimensions;
        private BackgroundWorker _renderingInBackground;
        //private BlockingCollection<float> _waveformDatapump;

        private class RenderedToDimensions
        {
            public RenderedToDimensions(ITune tune, WaveformDimensions dimensions)
            {
                Tune = tune;
                Dimensions = dimensions;
            }

            public ITune Tune { get; }
            public WaveformDimensions Dimensions { get; }
        }

        /// <summary>
        /// Identifies the <see cref="LeftLevelBrush" /> dependency property. 
        /// </summary>
        public static readonly DependencyProperty LeftLevelBrushProperty = DependencyProperty.Register("LeftLevelBrush", 
            typeof(Brush), typeof(Waveform),
            new UIPropertyMetadata(new SolidColorBrush(Colors.Blue), OnLeftLevelBrushChanged));

        private static void OnLeftLevelBrushChanged(DependencyObject o, DependencyPropertyChangedEventArgs e) =>
            (o as Waveform)?.OnLeftLevelBrushChanged((Brush)e.NewValue);

        /// <summary>
        /// Called after the <see cref="LeftLevelBrush"/> value has changed.
        /// </summary>
        /// <param name="newValue">The new value of <see cref="LeftLevelBrush"/></param>
        private void OnLeftLevelBrushChanged(Brush newValue) => _leftPath.Fill = newValue;

        /// <summary>
        /// Gets or sets a brush used to draw the left channel output on the waveform.
        /// </summary>        
        [Category("Brushes")]
        public Brush LeftLevelBrush
        {
            get => (Brush)GetValue(LeftLevelBrushProperty);
            set => SetValue(LeftLevelBrushProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="RightLevelBrush" /> dependency property. 
        /// </summary>
        public static readonly DependencyProperty RightLevelBrushProperty
            = DependencyProperty.Register("RightLevelBrush", typeof(Brush), typeof(Waveform),
                new UIPropertyMetadata(new SolidColorBrush(Colors.Red), OnRightLevelBrushChanged));

        private static void OnRightLevelBrushChanged(DependencyObject o, DependencyPropertyChangedEventArgs e) =>
            (o as Waveform)?.OnRightLevelBrushChanged((Brush)e.NewValue);

        /// <summary>
        /// Called after the <see cref="RightLevelBrush"/> value has changed.
        /// </summary>
        /// <param name="brush">The new value of <see cref="RightLevelBrush"/></param>
        private void OnRightLevelBrushChanged(Brush brush) => _rightPath.Fill = brush;

        /// <summary>
        /// Gets or sets a brush used to draw the right speaker levels on the waveform.
        /// </summary>
        [Category("Brushes")]
        public Brush RightLevelBrush
        {
            get => (Brush)GetValue(RightLevelBrushProperty);
            set => SetValue(RightLevelBrushProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="CenterLineBrush" /> dependency property. 
        /// </summary>
        public static readonly DependencyProperty CenterLineBrushProperty
            = DependencyProperty.Register("CenterLineBrush", typeof(Brush), typeof(Waveform),
                new UIPropertyMetadata(new SolidColorBrush(Colors.Black), OnCenterLineBrushChanged));

        private static void OnCenterLineBrushChanged(DependencyObject o, DependencyPropertyChangedEventArgs e) =>
            (o as Waveform)?.OnCenterLineBrushChanged((Brush)e.NewValue);

        /// <summary>
        /// Called after the <see cref="CenterLineBrush"/> value has changed.
        /// </summary>
        /// <param name="newValue">The new value of <see cref="CenterLineBrush"/></param>
        private void OnCenterLineBrushChanged(Brush newValue) => _centerLine.Stroke = newValue;

        /// <summary>
        /// Gets or sets a brush used to draw the center line separating left and right levels.
        /// </summary>
        [Category("Brushes")]
        public Brush CenterLineBrush
        {
            get => (Brush)GetValue(CenterLineBrushProperty);
            set => SetValue(CenterLineBrushProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="CenterLineThickness" /> dependency property. 
        /// </summary>
        public static readonly DependencyProperty CenterLineThicknessProperty =
            DependencyProperty.Register("CenterLineThickness", typeof(double), typeof(Waveform),
                new UIPropertyMetadata(1.0d, OnCenterLineThicknessChanged, OnCoerceCenterLineThickness));

        private static object OnCoerceCenterLineThickness(DependencyObject o, object value) =>
            (o as Waveform)?.OnCoerceCenterLineThickness((double)value) ?? value;

        /// <summary>
        /// Coerces the value of <see cref="CenterLineThickness"/> when a new value is applied.
        /// </summary>
        /// <param name="value">The value that was set on <see cref="CenterLineThickness"/></param>
        /// <returns>The adjusted value of <see cref="CenterLineThickness"/></returns>
        private double OnCoerceCenterLineThickness(double value) =>
            Math.Max(value, 0.0d);

        private static void OnCenterLineThicknessChanged(DependencyObject o, DependencyPropertyChangedEventArgs e) =>
            (o as Waveform)?.OnCenterLineThicknessChanged((double)e.NewValue);

        /// <summary>
        /// Called after the <see cref="CenterLineThickness"/> value has changed.
        /// </summary>
        /// <param name="newValue">The new value of <see cref="CenterLineThickness"/></param>
        private void OnCenterLineThicknessChanged(double newValue) => _centerLine.StrokeThickness = newValue;

        /// <summary>
        /// Gets or sets the thickness of the center line separating left and right levels.
        /// </summary>
        [Category("Widths")]
        public double CenterLineThickness
        {
            get => (double)GetValue(CenterLineThicknessProperty);
            set => SetValue(CenterLineThicknessProperty, value);
        }

        public static readonly DependencyProperty WaveformResolutionProperty =
            DependencyProperty.Register("WaveformResolution", typeof(int), typeof(Waveform),
                new UIPropertyMetadata(2000, OnWaveformResolutionChanged, OnCoerceWaveformResolution));

        private static void OnWaveformResolutionChanged(DependencyObject o, DependencyPropertyChangedEventArgs e) =>
            (o as Waveform)?._redrawObservable.Increment();

        private static object OnCoerceWaveformResolution(DependencyObject d, object basevalue)
        {
            var desiredWaveformResolution = (int)basevalue;
            return Math.Max(1000, Math.Min(16000, desiredWaveformResolution));
        }

        /// <summary>
        /// Controls the density and accuracy of the generated waveform. Higher number = more detail.
        /// </summary>
        [Category("Display")]
        public int WaveformResolution
        {
            get => (int)GetValue(WaveformResolutionProperty);
            set => SetValue(WaveformResolutionProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="AutoScaleWaveformCache" /> dependency property. 
        /// </summary>
        public static readonly DependencyProperty AutoScaleWaveformCacheProperty
            = DependencyProperty.Register("AutoScaleWaveformCache", typeof(bool), typeof(Waveform),
                new UIPropertyMetadata(false, OnAutoScaleWaveformCacheChanged));

        private static void OnAutoScaleWaveformCacheChanged(DependencyObject o, DependencyPropertyChangedEventArgs e) =>
            (o as Waveform)?.OnAutoScaleWaveformCacheChanged();

        /// <summary>
        /// Called after the <see cref="AutoScaleWaveformCache"/> value has changed.
        /// </summary>
        private void OnAutoScaleWaveformCacheChanged() => UpdateWaveformCacheScaling();

        /// <summary>
        /// Gets or sets a value indicating whether the waveform should attempt to autoscale
        /// its render buffer in size.
        /// </summary>
        /// <remarks>
        /// If true, the control will attempt to set the waveform's bitmap cache
        /// at a resolution based on the sum of all ScaleTransforms applied
        /// in the control's visual tree heirarchy. This can make the waveform appear
        /// less blurry if a ScaleTransform is applied at a higher level.
        /// The only ScaleTransforms that are considered here are those that have 
        /// uniform vertical and horizontal scaling (generally used to "zoom in"
        /// on a window or controls).
        /// </remarks>
        [Category("Display")]
        public bool AutoScaleWaveformCache
        {
            get => (bool)GetValue(AutoScaleWaveformCacheProperty);
            set => SetValue(AutoScaleWaveformCacheProperty, value);
        }

        public static readonly DependencyProperty ProgressiveRenderingProperty = DependencyProperty.Register(
            "ProgressiveRendering", typeof(bool), typeof(Waveform), 
            new PropertyMetadata(true));

        public bool ProgressiveRendering
        {
            get => (bool) GetValue(ProgressiveRenderingProperty);
            set => SetValue(ProgressiveRenderingProperty, value);
        }

        private IEnumerable<DependencyObject> ThisAndParentsOf(DependencyObject element)
        {
            if (element == null) return new List<DependencyObject>();
            var list = new List<DependencyObject> { element };
            list.AddRange(ThisAndParentsOf(VisualTreeHelper.GetParent(element)));
            return list;
        }

        private double AdjustedByTransformM11(Transform transform) =>
            (transform != null) &&
            (Math.Abs(transform.Value.M12) < 0.001) &&
            (Math.Abs(transform.Value.M21) < 0.001) &&
            (Math.Abs(transform.Value.OffsetX) < 0.001) &&
            (Math.Abs(transform.Value.OffsetY) < 0.001) &&
            (Math.Abs(transform.Value.M11 - transform.Value.M22) < 0.001)
                ? transform.Value.M11
                : 1.0;


        private double TotalTransformScaleFn() =>
            ThisAndParentsOf(this).Where(element => element is Visual)
                .Select(visual => AdjustedByTransformM11(VisualTreeHelper.GetTransform((Visual)visual)))
                .Aggregate(1.0, (acc, x) => acc * x);

        // ReSharper disable once UnusedMember.Local
        private double TotalTransformScale()
        {
            double totalTransform = 1.0d;
            DependencyObject currentVisualTreeElement = this;
            do
            {
                if (currentVisualTreeElement is Visual visual)
                {
                    Transform transform = VisualTreeHelper.GetTransform(visual);
                    // This condition is a way of determining if it
                    // was a uniform scale transform. Is there some better way?
                    if ((transform != null) &&
                        (Math.Abs(transform.Value.M12) < 0.001) &&
                        (Math.Abs(transform.Value.M21) < 0.001) &&
                        (Math.Abs(transform.Value.OffsetX) < 0.001) &&
                        (Math.Abs(transform.Value.OffsetY) < 0.001) &&
                        (Math.Abs(transform.Value.M11 - transform.Value.M22) < 0.001))
                    {
                        totalTransform *= transform.Value.M11;
                    }
                }
                currentVisualTreeElement = VisualTreeHelper.GetParent(currentVisualTreeElement);
            } while (currentVisualTreeElement != null);
            return totalTransform;
        }

        private void UpdateWaveformCacheScaling()
        {
            if (MainCanvas == null) return;
            BitmapCache waveformCache = (BitmapCache)MainCanvas.CacheMode;
            if (!AutoScaleWaveformCache)
            {
                waveformCache.RenderAtScale = 1.0d;
                return;
            }
            double totalTransformScale = TotalTransformScaleFn();
            if (Math.Abs(waveformCache.RenderAtScale - totalTransformScale) > 0.001)
                waveformCache.RenderAtScale = totalTransformScale;
        }


        protected override void OnTuneChanged()
        {
            _redrawObservable.Increment();
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            MainCanvas = GetTemplateChild("PART_Waveform") as Canvas;
            Debug.Assert(MainCanvas != null, nameof(MainCanvas) + " != null");
            MainCanvas.CacheMode = new BitmapCache();
            // Used to make the transparent regions clickable.
            MainCanvas.Background = new SolidColorBrush(Colors.Transparent);

            MainCanvas.Children.Add(_centerLine);
            MainCanvas.Children.Add(_leftPath);
            MainCanvas.Children.Add(_rightPath);

            if (CenterLineBrush != null)
            {
                _centerLine.X1 = 0;
                _centerLine.X2 = MainCanvas.RenderSize.Width;
                _centerLine.Y1 = MainCanvas.RenderSize.Height;
                _centerLine.Y2 = MainCanvas.RenderSize.Height;
            }
            UpdateWaveformCacheScaling();

            var context = SynchronizationContext.Current;
            if (context != null && _redrawDisposable == null)
            {
                _redrawDisposable = _redrawObservable.Sample(TimeSpan.FromMilliseconds(100))
                    .ObserveOn(context)
                    .Subscribe(_ => Render());
            }
        }

        protected override void OnTemplateChanged(ControlTemplate oldTemplate, ControlTemplate newTemplate)
        {
            base.OnTemplateChanged(oldTemplate, newTemplate);
            MainCanvas.Children.Clear();
            _lastRenderedToDimensions = null;
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            _redrawObservable.Increment();
        }


        private static float[] CreateFloats([NotNull]byte[] bytes)
        {
            var floats = new float[bytes.Length / 4];
            Buffer.BlockCopy(bytes, 0, floats, 0, bytes.Length);
            return floats;
        }

        private bool ShouldRedraw()
        {
            if (MainCanvas == null ||
                (_lastRenderedToDimensions != null &&
                (AreTunesTheSame() && AreDimensionsSame()))) return false;
            return true;
        }

        private bool AreTunesTheSame() => Tune.Name() == _lastRenderedToDimensions.Tune.Name();

        private bool AreDimensionsSame() => _waveformDimensions.Equals(_lastRenderedToDimensions.Dimensions);

        /// <summary>
        /// Show the waveform
        /// </summary>
        protected override void Render()
        {
            MeasureArea();
            if (!ShouldRedraw()) return;
            Clear();
            var (leftWaveformPolyLine, rightWaveformPolyLine) = PreparedDrawingArea();
            var section = new WaveformSection(_coverageArea, Tune, WaveformResolution);
            var renderWaveform = new WaveformRenderingProgress(_waveformDimensions, section, MainCanvas, leftWaveformPolyLine, rightWaveformPolyLine);
            var renderingMethod = ProgressiveRendering 
                ? (Action<WaveformSection, WaveformRenderingProgress>)RenderProgressively 
                : BackgroundReadThenRender;
            renderingMethod(section, renderWaveform);
            _lastRenderedToDimensions = new RenderedToDimensions(Tune, _waveformDimensions);
        }

        private (PolyLineSegment, PolyLineSegment) PreparedDrawingArea()
        {
            var centerHeight = MainCanvas.RenderSize.Height / 2.0d;
            var availableWidth = MainCanvas.RenderSize.Width - _waveformDimensions.RightMargin();
            var leftWaveformPolyLine = new PolyLineSegment();
            var rightWaveformPolyLine = new PolyLineSegment();
            Point StartPoint() => new Point(_waveformDimensions.LeftMargin(), centerHeight);
            _leftPath.Data = new PathGeometry(new[] { new PathFigure(StartPoint(), new[] { leftWaveformPolyLine }, false) });
            _rightPath.Data = new PathGeometry(new[] { new PathFigure(StartPoint(), new[] { rightWaveformPolyLine }, false) });
            _centerLine.X1 = _waveformDimensions.LeftMargin();
            _centerLine.X2 = availableWidth;
            var halfHeight = MainCanvas.RenderSize.Height / 2.0d;
            _centerLine.Y1 = halfHeight;
            _centerLine.Y2 = halfHeight;
            MainCanvas.Children.Add(_leftPath);
            MainCanvas.Children.Add(_rightPath);
            MainCanvas.Children.Add(_centerLine);
            // The following section adds an empty space before the beginning of the waveform with leading dashes
            if (Tune.TotalTime().TotalSeconds > 0)
            {
                CreateDashedPadding(0, _waveformDimensions.LeftMargin(), _leftSideOffsetDashes);
                if (_coverageArea.Includes(1.0))
                    CreateDashedPadding(availableWidth, _waveformDimensions.RightMargin(), _rightSideOffsetDashes);
            }
            return (leftWaveformPolyLine, rightWaveformPolyLine);
        }

        private void RenderProgressively(WaveformSection section, WaveformRenderingProgress renderWaveform)
        {
            var waveformFloats = CreateFloats(Tune.WaveformData());
            if (waveformFloats.Length > 0)
            {
                renderWaveform.DrawWaveform(waveformFloats);
                return;
            }
            var resolution = WaveformResolution; // can't inline this because it cannot be accessed safely from another thread
            var observable = Tune.WaveformStream();
            var steps = Math.Min(resolution, 1000);
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
            _renderingInBackground.RunWorkerAsync(
                    new BackgroundRenderingArgs(Tune, renderWaveform, WaveformResolution));
        }

        private void RenderWaveformSync(WaveformRenderingProgress renderWaveform, float[] waveformFloats)
        {
            renderWaveform.DrawWaveform(waveformFloats);
            renderWaveform.CompleteWaveform();
        }

        private void ReadWaveformInBackground(object sender, DoWorkEventArgs e)
        {
            var args = (BackgroundRenderingArgs)e.Argument;
            args!.Tune.WaveformStream().Waveform(args.Resolution);
            e.Result = args;
        }

        private void OnBackgroundRenderingCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null || !(e.Result is BackgroundRenderingArgs args)) return;
            RenderWaveformSync(args.RenderWaveform, CreateFloats(Tune.WaveformData()));
        }

        private class BackgroundRenderingArgs
        {
            public BackgroundRenderingArgs(ITune tune, WaveformRenderingProgress renderWaveform, int resolution)
            {
                Tune = tune;
                RenderWaveform = renderWaveform;
                Resolution = resolution;
            }
            public ITune Tune { get; }
            public int Resolution { get; }
            public WaveformRenderingProgress RenderWaveform { get; }
        }

        private Line DrawDash(int i, double centerPos, double startPos, int dashSize, int inInBetweenDashesSpace)
        {
            Line dash = new Line
            {
                Stroke = CenterLineBrush,
                StrokeThickness = CenterLineThickness,
                X1 = i == 0 ? startPos : startPos + i * dashSize + i * inInBetweenDashesSpace
            };
            dash.X2 = dash.X1 + dashSize;
            dash.Y1 = centerPos;
            dash.Y2 = centerPos;
            return dash;
        }

        private void CreateDashedPadding(double startPos, double spaceInPx, List<Line> dashes)
        {
            const int minDashSize = 3;
            const int maxDashCount = 5;
            const int minInBetweenDashesSpace = 3;
            int dashSize = minDashSize;
            int dashCount = Math.Min(maxDashCount, (int)Math.Floor(_waveformDimensions.LeftMargin() / dashSize));
            var dashTotalWidth = dashCount * minDashSize;
            dashSize += Math.Max(0, (int)Math.Floor(((spaceInPx - dashTotalWidth - ((dashCount - 1) * minInBetweenDashesSpace)) / dashCount)));
            int inInBetweenDashesSpace = Math.Max(minInBetweenDashesSpace, (int)Math.Floor((_waveformDimensions.LeftMargin() - (dashSize * dashCount)) / dashCount));
            var centerPos = MainCanvas.RenderSize.Height / 2;
            var lines = Enumerable.Range(0, dashCount)
                .Select(i => DrawDash(i, centerPos, startPos, dashSize, inInBetweenDashesSpace));
            void AddDash(Line dash)
            {
                dashes.Add(dash);
                MainCanvas.Children.Add(dash);
            }
            lines.ForEach(AddDash);
        }

        public void Clear()
        {
            _waveformBuildDisposable?.Dispose();
            MainCanvas.Children.Clear();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _redrawDisposable?.Dispose();
            Unloaded -= OnUnloaded;
        }
    }
}