using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using WaveformTimeline.Commons;
using WaveformTimeline.Contracts;
using WaveformTimeline.Controls.Timeline;
using WaveformTimeline.Primitives;

namespace WaveformTimeline
{
    public sealed class WaveformTimeline : TemplatedControl
    {
        public static readonly StyledProperty<ITune> TuneProperty =
            AvaloniaProperty.Register<WaveformTimeline, ITune>(nameof(Tune));

        public ITune Tune
        {
            get => GetValue(TuneProperty) ?? new NoTune();
            set => SetValue(TuneProperty, value ?? new NoTune());
        }

        public static readonly StyledProperty<double> ZoomProperty =
            AvaloniaProperty.Register<WaveformTimeline, double>(nameof(Zoom), defaultValue: 1.0);

        public double Zoom
        {
            get => GetValue(ZoomProperty);
            set => SetValue(ZoomProperty, value);
        }

        public static readonly StyledProperty<IBrush> LeftLevelBrushProperty =
            AvaloniaProperty.Register<WaveformTimeline, IBrush>(nameof(LeftLevelBrush),
                defaultValue: new SolidColorBrush(Colors.Blue));

        public IBrush LeftLevelBrush
        {
            get => GetValue(LeftLevelBrushProperty);
            set => SetValue(LeftLevelBrushProperty, value);
        }

        public static readonly StyledProperty<IBrush> RightLevelBrushProperty =
            AvaloniaProperty.Register<WaveformTimeline, IBrush>(nameof(RightLevelBrush),
                defaultValue: new SolidColorBrush(Colors.Red));

        public IBrush RightLevelBrush
        {
            get => GetValue(RightLevelBrushProperty);
            set => SetValue(RightLevelBrushProperty, value);
        }

        public static readonly StyledProperty<IBrush> CenterLineBrushProperty =
            AvaloniaProperty.Register<WaveformTimeline, IBrush>(nameof(CenterLineBrush),
                defaultValue: new SolidColorBrush(Colors.Black));

        public IBrush CenterLineBrush
        {
            get => GetValue(CenterLineBrushProperty);
            set => SetValue(CenterLineBrushProperty, value);
        }

        public static readonly StyledProperty<double> CenterLineThicknessProperty =
            AvaloniaProperty.Register<WaveformTimeline, double>(nameof(CenterLineThickness), defaultValue: 1.0);

        public double CenterLineThickness
        {
            get => GetValue(CenterLineThicknessProperty);
            set => SetValue(CenterLineThicknessProperty, value);
        }

        public static readonly StyledProperty<bool> AutoScaleWaveformCacheProperty =
            AvaloniaProperty.Register<WaveformTimeline, bool>(nameof(AutoScaleWaveformCache), defaultValue: false);

        public bool AutoScaleWaveformCache
        {
            get => GetValue(AutoScaleWaveformCacheProperty);
            set => SetValue(AutoScaleWaveformCacheProperty, value);
        }

        public static readonly StyledProperty<int> WaveformResolutionProperty =
            AvaloniaProperty.Register<WaveformTimeline, int>(nameof(WaveformResolution), defaultValue: 2000);

        public int WaveformResolution
        {
            get => GetValue(WaveformResolutionProperty);
            set => SetValue(WaveformResolutionProperty, value);
        }

        public static readonly StyledProperty<bool> ProgressiveRenderingProperty =
            AvaloniaProperty.Register<WaveformTimeline, bool>(nameof(ProgressiveRendering), defaultValue: true);

        public bool ProgressiveRendering
        {
            get => GetValue(ProgressiveRenderingProperty);
            set => SetValue(ProgressiveRenderingProperty, value);
        }

        public static readonly StyledProperty<IBrush> ProgressBarBrushProperty =
            AvaloniaProperty.Register<WaveformTimeline, IBrush>(nameof(ProgressBarBrush),
                defaultValue: new SolidColorBrush(Color.FromArgb(0xCD, 0xBA, 0x00, 0xFF)));

        public IBrush ProgressBarBrush
        {
            get => GetValue(ProgressBarBrushProperty);
            set => SetValue(ProgressBarBrushProperty, value);
        }

        public static readonly StyledProperty<double> ProgressBarThicknessProperty =
            AvaloniaProperty.Register<WaveformTimeline, double>(nameof(ProgressBarThickness), defaultValue: 2.0);

        public double ProgressBarThickness
        {
            get => GetValue(ProgressBarThicknessProperty);
            set => SetValue(ProgressBarThicknessProperty, value);
        }

        public static readonly StyledProperty<bool> AllowRepositioningProperty =
            AvaloniaProperty.Register<WaveformTimeline, bool>(nameof(AllowRepositioning), defaultValue: true);

        public bool AllowRepositioning
        {
            get => GetValue(AllowRepositioningProperty);
            set => SetValue(AllowRepositioningProperty, value);
        }

        public static readonly StyledProperty<IBrush> TimelineTickBrushProperty =
            AvaloniaProperty.Register<WaveformTimeline, IBrush>(nameof(TimelineTickBrush),
                defaultValue: new SolidColorBrush(Colors.Black));

        public IBrush TimelineTickBrush
        {
            get => GetValue(TimelineTickBrushProperty);
            set => SetValue(TimelineTickBrushProperty, value);
        }

        public static readonly StyledProperty<TimelineType> TimelineTypeProperty =
            AvaloniaProperty.Register<WaveformTimeline, TimelineType>(nameof(TimelineType),
                defaultValue: TimelineType.Constant);

        public TimelineType TimelineType
        {
            get => GetValue(TimelineTypeProperty);
            set => SetValue(TimelineTypeProperty, value);
        }

        public static readonly StyledProperty<ZeroToOne> EndRevealingMarkProperty =
            AvaloniaProperty.Register<WaveformTimeline, ZeroToOne>(nameof(EndRevealingMark),
                defaultValue: new ZeroToOne(0.75));

        public ZeroToOne EndRevealingMark
        {
            get => GetValue(EndRevealingMarkProperty);
            set => SetValue(EndRevealingMarkProperty, value);
        }

        public static readonly StyledProperty<bool> ShowCueMarksProperty =
            AvaloniaProperty.Register<WaveformTimeline, bool>(nameof(ShowCueMarks), defaultValue: true);

        public bool ShowCueMarks
        {
            get => GetValue(ShowCueMarksProperty);
            set => SetValue(ShowCueMarksProperty, value);
        }

        public static readonly StyledProperty<bool> ShowCueMarkToolTipProperty =
            AvaloniaProperty.Register<WaveformTimeline, bool>(nameof(ShowCueMarkToolTip), defaultValue: false);

        public bool ShowCueMarkToolTip
        {
            get => GetValue(ShowCueMarkToolTipProperty);
            set => SetValue(ShowCueMarkToolTipProperty, value);
        }

        public static readonly StyledProperty<bool> EnableCueMarksRepositioningProperty =
            AvaloniaProperty.Register<WaveformTimeline, bool>(nameof(EnableCueMarksRepositioning), defaultValue: true);

        public bool EnableCueMarksRepositioning
        {
            get => GetValue(EnableCueMarksRepositioningProperty);
            set => SetValue(EnableCueMarksRepositioningProperty, value);
        }

        public static readonly StyledProperty<IBrush> CueMarkBrushProperty =
            AvaloniaProperty.Register<WaveformTimeline, IBrush>(nameof(CueMarkBrush),
                defaultValue: new SolidColorBrush(Color.FromArgb(0xCD, 0xBA, 0x00, 0xFF)));

        public IBrush CueMarkBrush
        {
            get => GetValue(CueMarkBrushProperty);
            set => SetValue(CueMarkBrushProperty, value);
        }

        public static readonly StyledProperty<IBrush> CueBarBackgroundBrushProperty =
            AvaloniaProperty.Register<WaveformTimeline, IBrush>(nameof(CueBarBackgroundBrush),
                defaultValue: new SolidColorBrush(Color.FromArgb(0xCD, 0xBA, 0x00, 0xFF)));

        public IBrush CueBarBackgroundBrush
        {
            get => GetValue(CueBarBackgroundBrushProperty);
            set => SetValue(CueBarBackgroundBrushProperty, value);
        }

        public static readonly StyledProperty<IBrush> CueMarkAccentBrushProperty =
            AvaloniaProperty.Register<WaveformTimeline, IBrush>(nameof(CueMarkAccentBrush),
                defaultValue: new SolidColorBrush(Color.FromRgb(255, 0, 0)));

        public IBrush CueMarkAccentBrush
        {
            get => GetValue(CueMarkAccentBrushProperty);
            set => SetValue(CueMarkAccentBrushProperty, value);
        }
    }
}
