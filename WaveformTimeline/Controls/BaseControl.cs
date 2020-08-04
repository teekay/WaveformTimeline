using System;
using System.Windows;
using System.Windows.Controls;
using WaveformTimeline.Commons;
using WaveformTimeline.Contracts;
using WaveformTimeline.Primitives;

namespace WaveformTimeline.Controls
{
    public abstract class BaseControl: Control
    {
        protected Canvas MainCanvas
        {
            get => _mainCanvas;
            set { if (value != null) _mainCanvas = value; } 
        }

        protected TuneDuration _coverageArea;
        protected WaveformDimensions _waveformDimensions;
        private Canvas _mainCanvas = new Canvas();

        public static readonly DependencyProperty TuneProperty =
            DependencyProperty.Register("Tune", typeof(ITune), typeof(BaseControl),
                new UIPropertyMetadata(new NoTune(), OnTuneChanged));

        private static void OnTuneChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
            => (o as BaseControl)?.OnTuneChanged();

        protected abstract void OnTuneChanged(); 

        public ITune Tune
        {
            get => (ITune)GetValue(TuneProperty) ?? new NoTune(); // no null acceptable
            set
            {
                (Tune as IDisposable)?.Dispose(); // TODO revisit this, ITune does not include IDisposable
                SetValue(TuneProperty, value ?? new NoTune());
            }
        }

        public static readonly DependencyProperty ZoomProperty = DependencyProperty.Register(
            "Zoom", typeof(double), typeof(BaseControl),
            new PropertyMetadata(1.0d, OnZoomChanged, OnCoerceZoom));

        private static void OnZoomChanged(DependencyObject o, DependencyPropertyChangedEventArgs e) => (o as BaseControl)?.Render();

        private static object OnCoerceZoom(DependencyObject o, object value) => 
            Math.Max(1.0, new FiniteDouble((double)value, 1.0).Value());

        public double Zoom
        {
            get => (double)GetValue(ZoomProperty);
            set => SetValue(ZoomProperty, value);
        }

        protected virtual void MeasureArea()
        {
            _coverageArea = new TuneDuration(Tune, Zoom);
            _waveformDimensions = new WaveformDimensions(_coverageArea, MainCanvas.RenderSize.Width);
        }

        protected abstract void Render();
    }
}