using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MoreLinq;
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

        public void DrawWfPointByPoint(IList<float> leftRight) => 
            Enumerable.Range(0, leftRight.Count / 2).Select(x => x * 2).ForEach(x => DrawWfPointByPointIter(leftRight, x));

        private void DrawWfPointByPointIter(IList<float> leftRight, int x) =>
            (_xLocation, _pointsDrawn) = AddWfPoints(leftRight[x], leftRight[x + 1]);

        private (double, double) AddWfPoints(float left, float right)
        {
            lock (_drawingLock)
            {
                var height = _mainCanvas.RenderSize.Height / 2.0d;
                var location = ((_pointsDrawn / 2) * _pointThickness) + _waveformDimensions.LeftMargin(); // where to draw - increasing by the point thickness
                _leftWaveformPolyLine.Points.Add(new Point(_xLocation, _centerHeight - left * height));
                _rightWaveformPolyLine.Points.Add(new Point(_xLocation, _centerHeight + right * height));
                return (location, _pointsDrawn + 2);
            }
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
