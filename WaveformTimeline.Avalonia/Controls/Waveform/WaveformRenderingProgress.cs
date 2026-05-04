using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using WaveformTimeline.Commons;

namespace WaveformTimeline.Controls.Waveform
{
    internal sealed class WaveformRenderingProgress
    {
        public WaveformRenderingProgress(WaveformDimensions waveformDimensions,
            WaveformSection waveformSection,
            double canvasHeight,
            PolyLineSegment leftWaveformPolyLine, PolyLineSegment rightWaveformPolyLine)
        {
            _leftWaveformPolyLine = leftWaveformPolyLine;
            _rightWaveformPolyLine = rightWaveformPolyLine;
            _pointThickness = waveformDimensions.Width() / (int)((waveformSection.End - waveformSection.Start + 1) / 2.0d);
            _height = canvasHeight / 2.0d;
            _leftMargin = waveformDimensions.LeftMargin();
            _leftWaveformPolyLine.Points.Add(new Point(_xLocation, _height));
            _rightWaveformPolyLine.Points.Add(new Point(_xLocation, _height));
        }

        private readonly PolyLineSegment _leftWaveformPolyLine;
        private readonly PolyLineSegment _rightWaveformPolyLine;
        private readonly double _height;
        private readonly double _leftMargin;
        private readonly double _pointThickness;
        private double _xLocation;
        private double _pointsDrawn;

        public void DrawWaveform(float[] wf)
        {
            _rightWaveformPolyLine.Points.RemoveAt(_rightWaveformPolyLine.Points.Count - 1);
            _leftWaveformPolyLine.Points.RemoveAt(_leftWaveformPolyLine.Points.Count - 1);
            var pointsDrawn = _pointsDrawn;
            var location = _xLocation;
            for (var i = 0; i < wf.Length - 1; i += 2)
            {
                location = ((pointsDrawn / 2) * _pointThickness) + _leftMargin;
                _leftWaveformPolyLine.Points.Add(new Point(location, _height + wf[i] * _height));
                _rightWaveformPolyLine.Points.Add(new Point(location, _height - wf[i + 1] * _height));
                pointsDrawn += 2;
            }
            (_xLocation, _pointsDrawn) = (location, pointsDrawn);
            _rightWaveformPolyLine.Points.Add(new Point(_xLocation, _height));
            _leftWaveformPolyLine.Points.Add(new Point(_xLocation, _height));

            // Reassign Points to trigger StyledProperty change notification,
            // which invalidates the geometry up through PathFigure → PathGeometry → Path.
            _leftWaveformPolyLine.Points = new List<Point>(_leftWaveformPolyLine.Points);
            _rightWaveformPolyLine.Points = new List<Point>(_rightWaveformPolyLine.Points);
        }

        public void CompleteWaveform()
        {
        }
    }
}
