#nullable enable
using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using WaveformTimeline.Commons;
using WaveformTimeline.Contracts;
using WaveformTimeline.Primitives;

namespace WaveformTimeline.Controls
{
    public abstract class BaseControl : TemplatedControl
    {
        protected Canvas? MainCanvas
        {
            get => _mainCanvas;
            set { if (value != null) _mainCanvas = value; }
        }

        protected TuneDuration CoverageArea;
        protected WaveformDimensions WaveformDimensions;
        private Canvas _mainCanvas = new();

        public static readonly StyledProperty<ITune> TuneProperty =
            AvaloniaProperty.Register<BaseControl, ITune>(nameof(Tune), defaultValue: new NoTune());

        protected abstract void OnTuneChanged();

        public ITune Tune
        {
            get => GetValue(TuneProperty) ?? new NoTune();
            set
            {
                if (value == Tune) return;
                if (Tune is IDisposable disposable) disposable.Dispose();
                SetValue(TuneProperty, value ?? new NoTune());
            }
        }

        public static readonly StyledProperty<double> ZoomProperty =
            AvaloniaProperty.Register<BaseControl, double>(nameof(Zoom), defaultValue: 1.0,
                coerce: (_, v) => Math.Max(1.0, new FiniteDouble(v, 1.0).Value()));

        public double Zoom
        {
            get => GetValue(ZoomProperty);
            set => SetValue(ZoomProperty, value);
        }

        static BaseControl()
        {
            TuneProperty.Changed.AddClassHandler<BaseControl>((x, _) => x.OnTuneChanged());
            ZoomProperty.Changed.AddClassHandler<BaseControl>((x, _) => x.Render());
        }

        protected virtual void MeasureArea()
        {
            CoverageArea = new TuneDuration(Tune, Zoom);
            WaveformDimensions = new WaveformDimensions(CoverageArea, MainCanvas?.Bounds.Width ?? 0d);
        }

        protected abstract void Render();
    }
}
