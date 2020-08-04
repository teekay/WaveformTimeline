using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WaveformTimeline.Commons;

namespace WaveformTimeline.Controls.Waveform
{
    internal sealed class WaveformRenderingProgress
    {
        public WaveformRenderingProgress(WaveformDimensions waveformDimensions,
            WaveformSection waveformSection,
            Canvas mainCanvas,
            PolyLineSegment leftWaveformPolyLine, PolyLineSegment rightWaveformPolyLine)
        {
            _waveformDimensions = waveformDimensions;
            _mainCanvas = mainCanvas;
            _leftWaveformPolyLine = leftWaveformPolyLine;
            _rightWaveformPolyLine = rightWaveformPolyLine;
            _pointThickness = _waveformDimensions.Width() / (int)((waveformSection.End - waveformSection.Start) / 2.0d);
            _centerHeight = _mainCanvas.RenderSize.Height / 2.0d;
        }

        private readonly WaveformDimensions _waveformDimensions;
        private readonly Canvas _mainCanvas;
        private readonly PolyLineSegment _leftWaveformPolyLine;
        private readonly PolyLineSegment _rightWaveformPolyLine;
        private readonly object _drawingLock = new object();
        private double _xLocation;
        private readonly double _pointThickness;
        private double _pointsDrawn;
        private readonly double _centerHeight;

        public void DrawWaveform(float[] wf)
        {
            var height = _mainCanvas.RenderSize.Height / 2.0d;
            var leftMargin = _waveformDimensions.LeftMargin();
            var pointsDrawn = _pointsDrawn;
            var location = _xLocation;
            for (var i = 0; i < wf.Length; i += 2)
            {
                location = ((pointsDrawn / 2) * _pointThickness) + leftMargin; // where to draw - increasing by the point thickness
                _leftWaveformPolyLine.Points.Add(new Point(location, _centerHeight - wf[i] * height));
                _rightWaveformPolyLine.Points.Add(new Point(location, _centerHeight + wf[i + 1] * height));
                pointsDrawn += 2;
            }
            (_xLocation, _pointsDrawn) = (location, pointsDrawn);
        }

        public void CompleteWaveform()
        {
            _leftWaveformPolyLine.Points.Add(new Point(_xLocation, _centerHeight));
            _leftWaveformPolyLine.Points.Add(new Point(_waveformDimensions.LeftMargin(), _centerHeight));
            _rightWaveformPolyLine.Points.Add(new Point(_xLocation, _centerHeight));
            _rightWaveformPolyLine.Points.Add(new Point(_waveformDimensions.LeftMargin(), _centerHeight));
        }
    }
}
