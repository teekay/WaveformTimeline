﻿using System.Windows;
using System.Windows.Media;
using WaveformTimeline.Commons;

namespace WaveformTimeline.Controls.Waveform
{
    internal sealed class WaveformRenderingProgress
    {
        public WaveformRenderingProgress(WaveformDimensions waveformDimensions,
            WaveformSection waveformSection,
            UIElement mainCanvas,
            PolyLineSegment leftWaveformPolyLine, PolyLineSegment rightWaveformPolyLine)
        {
            _leftWaveformPolyLine = leftWaveformPolyLine;
            _rightWaveformPolyLine = rightWaveformPolyLine;
            _pointThickness = waveformDimensions.Width() / (int)((waveformSection.End - waveformSection.Start) / 2.0d);
            _height = mainCanvas.RenderSize.Height / 2.0d;
            _leftMargin = waveformDimensions.LeftMargin();
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
            var pointsDrawn = _pointsDrawn;
            var location = _xLocation;
            for (var i = 0; i < wf.Length; i += 2)
            {
                location = ((pointsDrawn / 2) * _pointThickness) + _leftMargin; // where to draw - increasing by the point thickness
                _leftWaveformPolyLine.Points.Add(new Point(location, _height + wf[i] * _height));
                _rightWaveformPolyLine.Points.Add(new Point(location, _height - wf[i + 1] * _height));
                pointsDrawn += 2;
            }
            (_xLocation, _pointsDrawn) = (location, pointsDrawn);
        }

        public void CompleteWaveform()
        {
            _leftWaveformPolyLine.Freeze();
            _rightWaveformPolyLine.Freeze();
        }
    }
}
