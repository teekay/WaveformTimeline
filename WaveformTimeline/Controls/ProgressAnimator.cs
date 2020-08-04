using System;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using WaveformTimeline.Commons;
using WaveformTimeline.Primitives;

namespace WaveformTimeline.Controls
{
    /// <summary>
    /// Shows and animates the position of playback in time relative to the total length of the audio stream, and lets the DJ change the playback position.
    /// </summary>
    [DisplayName(@"Progress")]
    [Description("Shows and animates the position of playback in time relative to the total length of the audio stream, and lets the DJ change the playback position.")]
    [ToolboxItem(true)]
    [TemplatePart(Name = "PART_ProgressLine", Type = typeof(Canvas))]
    public sealed class ProgressAnimator : BaseControl
    {
        private readonly Storyboard _trackProgressAnimationBoard = new Storyboard();
        private readonly Rectangle _progressRect = new Rectangle();
        private readonly Rectangle _captureMouse = new Rectangle();
        private readonly double _indicatorWidth = 6;
        private readonly Brush _transparentBrush = new SolidColorBrush { Color = Color.FromScRgb(0, 0, 0, 0), Opacity = 0 };
        private readonly Thickness _zeroMargin = new Thickness(0);
        private IDisposable _playbackOnOffNotifier;
        private IDisposable _playbackTempoNotifier;
        private bool StoryboardStarted { get; set; }

        /// <summary>
        /// Identifies the <see cref="ProgressBarBrush" /> dependency property. 
        /// </summary>
        public static readonly DependencyProperty ProgressBarBrushProperty
            = DependencyProperty.Register("ProgressBarBrush", typeof(Brush), typeof(ProgressAnimator),
                new UIPropertyMetadata(new SolidColorBrush(Color.FromArgb(0xCD, 0xBA, 0x00, 0xFF)), OnProgressBarBrushChanged));

        private static void OnProgressBarBrushChanged(DependencyObject o, DependencyPropertyChangedEventArgs e) =>
            (o as ProgressAnimator)?.OnProgressBarBrushChanged();

        /// <summary>
        /// Called after the <see cref="ProgressBarBrush"/> value has changed.
        /// </summary>
        private void OnProgressBarBrushChanged() => Render();

        /// <summary>
        /// Gets or sets a brush used to draw the track progress indicator bar.
        /// </summary>
        [Category("Brushes")]
        public Brush ProgressBarBrush
        {
            get => (Brush)GetValue(ProgressBarBrushProperty);
            set => SetValue(ProgressBarBrushProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ProgressBarThickness" /> dependency property. 
        /// </summary>
        public static readonly DependencyProperty ProgressBarThicknessProperty =
            DependencyProperty.Register("ProgressBarThickness", typeof(double), typeof(ProgressAnimator),
                new UIPropertyMetadata(2.0d, OnProgressBarThicknessChanged, OnCoerceProgressBarThickness));

        private static object OnCoerceProgressBarThickness(DependencyObject o, object value) =>
            (o as ProgressAnimator)?.OnCoerceProgressBarThickness((double)value) ?? value;

        /// <summary>
        /// Coerces the value of <see cref="ProgressBarThickness"/> when a new value is applied.
        /// </summary>
        /// <param name="value">The value that was set on <see cref="ProgressBarThickness"/></param>
        /// <returns>The adjusted value of <see cref="ProgressBarThickness"/></returns>
        private double OnCoerceProgressBarThickness(double value) => Math.Max(value, 0.0d);

        private static void OnProgressBarThicknessChanged(DependencyObject o, DependencyPropertyChangedEventArgs e) =>
            (o as ProgressAnimator)?.OnProgressBarThicknessChanged((double)e.NewValue);

        /// <summary>
        /// Called after the <see cref="ProgressBarThickness"/> value has changed.
        /// </summary>
        /// <param name="newValue">The new value of <see cref="ProgressBarThickness"/></param>
        private void OnProgressBarThicknessChanged(double newValue) =>
            _progressRect.StrokeThickness = newValue;

        /// <summary>
        /// Get or sets the thickness of the progress indicator bar.
        /// </summary>
        [Category("Widths")]
        public double ProgressBarThickness
        {
            get => (double)GetValue(ProgressBarThicknessProperty);
            set => SetValue(ProgressBarThicknessProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="AllowRepositioning"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AllowRepositioningProperty =
            DependencyProperty.Register("AllowRepositioning", typeof(bool), typeof(ProgressAnimator),
                new UIPropertyMetadata(true));

        /// <summary>
        /// Whether the mouse click to the waveform should trigger playback repositioning or not
        /// Default is True
        /// </summary>
        [Category("Playback")]
        public bool AllowRepositioning
        {
            get => (bool)GetValue(AllowRepositioningProperty);
            set => SetValue(AllowRepositioningProperty, value);
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            MainCanvas = GetTemplateChild("PART_ProgressLine") as Canvas;
            _captureMouse.Fill = _transparentBrush;
            MainCanvas.Children.Add(_captureMouse);
            Render();
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            _captureMouse.Width = MainCanvas.RenderSize.Width;
            _captureMouse.Height = MainCanvas.RenderSize.Height;
            Render();
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);
            var currentPoint = e.GetPosition(MainCanvas);
            // ReleaseMouseCapture();
            if (!AllowRepositioning || !MainCanvas.IsMouseOver || Tune is NoTune) return;
            var percProgress = _waveformDimensions.PercentOfRenderedWaveform(currentPoint.X);
            double positionInChannelInSeconds = _coverageArea.ActualPosition(percProgress
                ); //PositionOnVirtualWaveform(currentPoint) * _coverageArea.Duration() + _coverageArea.StartingPoint();
            Tune.Seek(TimeSpan.FromTicks(
                Math.Min(Tune.TotalTime().Ticks,
                    Math.Max(0, TimeSpan.FromSeconds(positionInChannelInSeconds).Ticks))));
            Render();
        }

        protected override void OnTuneChanged() => Render();

        private double StartingX() => new FiniteDouble(_waveformDimensions.PositionOnRenderedWaveform(
            _coverageArea.Progress(Tune.CurrentTime().TotalSeconds))); // this is progress within the rendered area

        protected override void Render()
        {
            Clear();
            MeasureArea();
            var uiContext = SynchronizationContext.Current;
            if (Tune == null || MainCanvas == null || uiContext == null || Tune.TotalTime().TotalSeconds <= 0 || _waveformDimensions.AreEmpty()) return;
            _trackProgressAnimationBoard.Completed += TrackProgressAnimationBoardOnCompleted;
            _playbackOnOffNotifier = Observable.Create<EventArgs>(o =>
                {
                    EventHandler<EventArgs> h = (s, e) => o.OnNext(e);
                    Tune.Transitioned += h;
                    return Disposable.Create(() => Tune.Transitioned -= h);
                })
                .ObserveOn(uiContext)
                .Subscribe(ControlProgressAnimation);
            _playbackTempoNotifier = Observable.Create<EventArgs>(observer =>
                {
                    EventHandler<EventArgs> handler1 = (s, e) => observer.OnNext(e);
                    Tune.TempoShifted += handler1;
                    return Disposable.Create(() => Tune.TempoShifted -= handler1);
                })
                //.Throttle(TimeSpan.FromMilliseconds(100))
                .ObserveOn(uiContext)
                .Subscribe(AlterProgressAnimationSpeed);
            _progressRect.Margin = new Thickness(_waveformDimensions.LeftMargin(), 0, 0, 0);
            _progressRect.Width = 2;
            _progressRect.Height = MainCanvas.RenderSize.Height;
            MainCanvas.Children.Add(_progressRect);
            _progressRect.Stroke = _transparentBrush;
            _progressRect.StrokeThickness = 0d;
            _progressRect.Fill = new SolidColorBrush(Color.FromRgb(0, 0, 0)) { Opacity = 0.4 };
            ControlProgressAnimation(EventArgs.Empty);
        }

        /// <summary>
        /// Set up the animations given the remaining time and tempo
        /// </summary>
        private void SetUpProgressAnimation()
        {
            if (!Tune.PlaybackOn()) return;

            var sourceX = StartingX() - _waveformDimensions.LeftMargin();
            var targetX = _waveformDimensions.Width();
            if (targetX < sourceX || targetX <= 0) return;

            var remainingTimeInSeconds = TimeSpan.FromSeconds(_coverageArea.Remaining(Tune.CurrentTime().TotalSeconds)); // Tip: do not adjust by Tune.Tempo, it will be taken into account by AlterProgressAnimationSpeed()
            DoubleAnimation XAnimation() => new DoubleAnimation(sourceX, targetX, remainingTimeInSeconds);
            var widthAnimation = XAnimation();

            _trackProgressAnimationBoard.Children.Clear();
            _trackProgressAnimationBoard.Children.Add(widthAnimation);
            _trackProgressAnimationBoard.Duration = remainingTimeInSeconds;

            Storyboard.SetTarget(widthAnimation, _progressRect);

            Storyboard.SetTargetProperty(widthAnimation, new PropertyPath(Rectangle.WidthProperty));
            //System.Windows.Media.Animation.Timeline.SetDesiredFrameRate(_trackProgressAnimationBoard, 60);
        }

        /// <summary>
        /// Run, pause, or resume the animation
        /// </summary>
        /// <param name="e"></param>
        private void ControlProgressAnimation(EventArgs e)
        {
            if (Tune.PlaybackOn())
            {
                if (!StoryboardStarted)
                {
                    LaunchAnimation();
                }
                else
                {
                    _trackProgressAnimationBoard.Resume(this);
                }
            }
            else
            {
                if (StoryboardStarted)
                {
                    _trackProgressAnimationBoard.Pause(this);
                }
            }
        }

        private void LaunchAnimation()
        {
            SetUpProgressAnimation();
            _trackProgressAnimationBoard.Begin(this, true);
            _trackProgressAnimationBoard.Seek(this, TimeSpan.Zero, TimeSeekOrigin.BeginTime);
            AlterProgressAnimationSpeed(new PropertyChangedEventArgs(string.Empty));
            StoryboardStarted = true;
        }

        /// <summary>
        /// Stop the animations
        /// </summary>
        private void StopAnimations()
        {
            if (!StoryboardStarted) return;
            StoryboardStarted = false;
            _trackProgressAnimationBoard.Stop(this);
        }

        private void AlterProgressAnimationSpeed(EventArgs e) => _trackProgressAnimationBoard.SetSpeedRatio(this, Tune.Tempo() / 100);

        /// <summary>
        /// Called when the storyboard completes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void TrackProgressAnimationBoardOnCompleted(object sender, EventArgs eventArgs) => StoryboardStarted = false;

        private void Clear()
        {
            _playbackTempoNotifier?.Dispose();
            _playbackOnOffNotifier?.Dispose();
            StopAnimations();
            _trackProgressAnimationBoard.Completed -= TrackProgressAnimationBoardOnCompleted;
            MainCanvas.Children.Remove(_progressRect);
        }
    }
}