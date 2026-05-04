#nullable enable
using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using WaveformTimeline.Commons;
using WaveformTimeline.Primitives;

namespace WaveformTimeline.Controls
{
    public sealed class ProgressAnimator : BaseControl
    {
        private readonly Rectangle _progressRect = new();
        private readonly Rectangle _captureMouse = new();
        private readonly IBrush _transparentBrush = new SolidColorBrush { Color = Color.FromArgb(0, 0, 0, 0), Opacity = 0 };
        private IDisposable? _playbackOnOffNotifier;
        private IDisposable? _playbackTempoNotifier;
        private DispatcherTimer? _progressTimer;
        private IDisposable? _boundsDisposable;

        public static readonly StyledProperty<IBrush> ProgressBarBrushProperty =
            AvaloniaProperty.Register<ProgressAnimator, IBrush>(nameof(ProgressBarBrush),
                defaultValue: new SolidColorBrush(Color.FromArgb(0xCD, 0xBA, 0x00, 0xFF)));

        public IBrush ProgressBarBrush
        {
            get => GetValue(ProgressBarBrushProperty);
            set => SetValue(ProgressBarBrushProperty, value);
        }

        public static readonly StyledProperty<double> ProgressBarThicknessProperty =
            AvaloniaProperty.Register<ProgressAnimator, double>(nameof(ProgressBarThickness), defaultValue: 2.0,
                coerce: (_, v) => Math.Max(v, 0.0d));

        public double ProgressBarThickness
        {
            get => GetValue(ProgressBarThicknessProperty);
            set => SetValue(ProgressBarThicknessProperty, value);
        }

        public static readonly StyledProperty<bool> AllowRepositioningProperty =
            AvaloniaProperty.Register<ProgressAnimator, bool>(nameof(AllowRepositioning), defaultValue: true);

        public bool AllowRepositioning
        {
            get => GetValue(AllowRepositioningProperty);
            set => SetValue(AllowRepositioningProperty, value);
        }

        static ProgressAnimator()
        {
            ProgressBarBrushProperty.Changed.AddClassHandler<ProgressAnimator>((p, _) => p.Render());
            ProgressBarThicknessProperty.Changed.AddClassHandler<ProgressAnimator>((p, e) =>
                p._progressRect.StrokeThickness = (double)(e.NewValue ?? 2.0));
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            MainCanvas = e.NameScope.Find<Canvas>("PART_ProgressLine");
            if (MainCanvas == null) return;
            _captureMouse.Fill = _transparentBrush;
            MainCanvas.Children.Add(_captureMouse);
            _boundsDisposable?.Dispose();
            _boundsDisposable = MainCanvas.GetObservable(BoundsProperty).Subscribe(_ =>
            {
                if (MainCanvas == null) return;
                _captureMouse.Width = MainCanvas.Bounds.Width;
                _captureMouse.Height = MainCanvas.Bounds.Height;
                Render();
            });
            Render();
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            _boundsDisposable?.Dispose();
            Clear();
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);
            if (MainCanvas == null || !AllowRepositioning || !MainCanvas.IsPointerOver || Tune is NoTune)
                return;
            var currentPoint = e.GetPosition(MainCanvas);
            var percProgress = WaveformDimensions.PercentOfRenderedWaveform(currentPoint.X);
            double positionInChannelInSeconds = CoverageArea.ActualPosition(percProgress);
            Tune.Seek(TimeSpan.FromTicks(
                Math.Min(Tune.TotalTime().Ticks,
                    Math.Max(0, TimeSpan.FromSeconds(positionInChannelInSeconds).Ticks))));
            Render();
        }

        protected override void OnTuneChanged() => Render();

        protected override void Render()
        {
            Clear();
            MeasureArea();
            if (MainCanvas == null ||
                Tune.TotalTime().TotalSeconds <= 0 ||
                WaveformDimensions.AreEmpty())
                return;

            var uiContext = SynchronizationContext.Current;
            if (uiContext == null) return;

            _playbackOnOffNotifier = Observable.Create<EventArgs>(o =>
                {
                    EventHandler<EventArgs> h = (_, e) => o.OnNext(e);
                    Tune.Transitioned += h;
                    return Disposable.Create(() => Tune.Transitioned -= h);
                })
                .ObserveOn(uiContext)
                .Subscribe(ControlProgressAnimation);
            _playbackTempoNotifier = Observable.Create<EventArgs>(o =>
                {
                    EventHandler<EventArgs> h = (_, e) => o.OnNext(e);
                    Tune.TempoShifted += h;
                    return Disposable.Create(() => Tune.TempoShifted -= h);
                })
                .ObserveOn(uiContext)
                .Subscribe(_ => { }); // tempo handled naturally by timer polling CurrentTime()

            _progressRect.Margin = new Thickness(WaveformDimensions.LeftMargin(), 0, 0, 0);
            _progressRect.Width = 0;
            _progressRect.Height = MainCanvas.Bounds.Height;
            MainCanvas.Children.Add(_progressRect);
            _progressRect.Stroke = _transparentBrush;
            _progressRect.StrokeThickness = 0d;
            _progressRect.Fill = new SolidColorBrush(Color.FromRgb(0, 0, 0)) { Opacity = 0.4 };
            ControlProgressAnimation(EventArgs.Empty);
        }

        private void ControlProgressAnimation(EventArgs e)
        {
            if (Tune.PlaybackOn())
            {
                StartTimer();
            }
            else
            {
                StopTimer();
                UpdateProgressPosition();
            }
        }

        private void StartTimer()
        {
            if (_progressTimer != null) return;
            _progressTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) };
            _progressTimer.Tick += OnTimerTick;
            _progressTimer.Start();
        }

        private void StopTimer()
        {
            if (_progressTimer == null) return;
            _progressTimer.Tick -= OnTimerTick;
            _progressTimer.Stop();
            _progressTimer = null;
        }

        private void OnTimerTick(object? sender, EventArgs e) => UpdateProgressPosition();

        private void UpdateProgressPosition()
        {
            if (MainCanvas == null || WaveformDimensions.AreEmpty()) return;
            var progress = CoverageArea.Progress(Tune.CurrentTime().TotalSeconds);
            var width = new FiniteDouble(progress * WaveformDimensions.Width());
            _progressRect.Width = Math.Max(0, Math.Min(width, WaveformDimensions.Width()));
        }

        private void Clear()
        {
            _playbackTempoNotifier?.Dispose();
            _playbackOnOffNotifier?.Dispose();
            StopTimer();
            MainCanvas?.Children.Remove(_progressRect);
        }
    }
}
