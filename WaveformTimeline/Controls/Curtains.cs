#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using MoreLinq;
using WaveformTimeline.Primitives;

namespace WaveformTimeline.Controls
{
    /// <inheritdoc />
    /// <summary>
    /// Shows how the leading and trailing portions of a track can / is being cut off.
    /// Allows those portions to be defined, resized,
    /// and informs the encapsulated ITune instance about it.
    /// </summary>
    [DisplayName(@"Curtains")]
    [Description("Shows how the leading and trailing portions of a track can / is being cut off. Allows those portions to be defined, resized, and informs the encapsulated ITune instance about it.")]
    [ToolboxItem(true)]
    [TemplatePart(Name = "PART_Curtains", Type = typeof(Canvas)),
     TemplatePart(Name = "PART_LeftCurtain", Type = typeof(Canvas)),
     TemplatePart(Name = "PART_RightCurtain", Type = typeof(Canvas)),
     TemplatePart(Name = "PART_CueMarks", Type = typeof(Canvas))]
    internal sealed class Curtains: BaseControl
    {
        private Canvas? _cueMarksCanvas;
        private Canvas? _leftSideCurtain;
        private Canvas? _rightSideCurtain;
        private readonly List<ZeroToOne> _cuePoints = new();
        private readonly List<Shape> _cuePointMarks = new();
        private readonly List<Line> _cuePointLines = new();
        private readonly Brush _transparentBrush = new SolidColorBrush { Color = Color.FromScRgb(0, 0, 0, 0), Opacity = 0 };
        private readonly Dictionary<double, Shape> _cueMap = new();
        private ZeroToOne _selectedCuePoint;
        private Shape? _selectedCuePointMark;
        private Line? _selectedCuePointLine;
        private double _lastKnownGoodX;
        private Canvas? _animatedCurtain;
        private bool _isMouseDown;
        private IDisposable? _watchesCues;

        /// <summary>
        /// Identifies the <see cref="CueMarkBrush" /> dependency property. 
        /// </summary>
        public static readonly DependencyProperty CueMarkBrushProperty
            = DependencyProperty.Register("CueMarkBrush", typeof(Brush), typeof(Curtains),
                new UIPropertyMetadata(new SolidColorBrush(Color.FromArgb(0xCD, 0xBA, 0x00, 0xFF)), OnCueMarkBrushChanged));

        private static void OnCueMarkBrushChanged(DependencyObject o, DependencyPropertyChangedEventArgs e) =>
            (o as Curtains)?.OnCueMarkBrushChanged();

        /// <summary>
        /// Called after the <see cref="Curtains.CueMarkBrush"/> value has changed.
        /// </summary>
        private void OnCueMarkBrushChanged() => Render();

        /// <summary>
        /// Gets or sets a brush used to draw cue mark triangles.
        /// </summary>
        [Category("Brushes")]
        public Brush CueMarkBrush
        {
            get => (Brush)GetValue(CueMarkBrushProperty);
            set => SetValue(CueMarkBrushProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="CueBarBackgroundBrush" /> dependency property. 
        /// </summary>
        public static readonly DependencyProperty CueBarBackgroundBrushProperty
            = DependencyProperty.Register("CueBarBackgroundBrush", typeof(Brush), typeof(Curtains),
                new UIPropertyMetadata(new SolidColorBrush(Color.FromArgb(0xCD, 0xBA, 0x00, 0xFF)), OnCueBarBackgroundBrushChanged));

        private static void OnCueBarBackgroundBrushChanged(DependencyObject o, DependencyPropertyChangedEventArgs e) =>
            (o as Curtains)?.OnCueBarBackgroundBrushChanged((Brush)e.NewValue);

        /// <summary>
        /// Called after the <see cref="CueBarBackgroundBrush"/> value has changed.
        /// </summary>
        /// <param name="newValue">The new value of <see cref="CueBarBackgroundBrush"/></param>
        private void OnCueBarBackgroundBrushChanged(Brush newValue)
        {
            if (_cueMarksCanvas != null)
                _cueMarksCanvas.Background = newValue;
        }

        /// <summary>
        /// Gets or sets a brush used to draw the track progress indicator bar.
        /// </summary>
        [Category("Brushes")]
        public Brush CueBarBackgroundBrush
        {
            get => (Brush)GetValue(CueBarBackgroundBrushProperty);
            set => SetValue(CueBarBackgroundBrushProperty, value);
        }

        public static readonly DependencyProperty CueMarkAccentBrushProperty = DependencyProperty.Register(
            "CueMarkAccentBrush", typeof(Brush), typeof(Curtains),
            new PropertyMetadata(new SolidColorBrush(Color.FromRgb(255, 0, 0))));

        [Category("Brushes")]
        public Brush CueMarkAccentBrush
        {
            get => (Brush)GetValue(CueMarkAccentBrushProperty);
            set => SetValue(CueMarkAccentBrushProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ShowCueMarks"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowCueMarksProperty = DependencyProperty.Register("ShowCueMarks", 
            typeof(bool), typeof(Curtains),
            new UIPropertyMetadata(true, OnShowCueMarksChanged));

        private static void OnShowCueMarksChanged(DependencyObject o, DependencyPropertyChangedEventArgs e) => (o as Curtains)?.Render();

        /// <summary>
        /// Whether to show the bar with cue marks
        /// Default: True
        /// </summary>
        [Category("Display")]
        public bool ShowCueMarks
        {
            get => (bool)GetValue(ShowCueMarksProperty);
            set => SetValue(ShowCueMarksProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="EnableCueMarksRepositioning"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty EnableCueMarksRepositioningProperty = DependencyProperty.Register("EnableCueMarksRepositioning",
            typeof(bool), typeof(Curtains),
            new UIPropertyMetadata(true, OnEnableCueMarksRepositioningChanged));

        private static void OnEnableCueMarksRepositioningChanged(DependencyObject o, DependencyPropertyChangedEventArgs e) => (o as Curtains)?.Render();

        /// <summary>
        /// Whether to enable moving the cue marks around
        /// Default: False
        /// </summary>
        [Category("Playback")]
        public bool EnableCueMarksRepositioning
        {
            get => (bool)GetValue(EnableCueMarksRepositioningProperty);
            set => SetValue(EnableCueMarksRepositioningProperty, value);
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            MainCanvas = GetTemplateChild("PART_Curtains") as Canvas;
            _cueMarksCanvas = GetTemplateChild("PART_CueMarks") as Canvas;
            _leftSideCurtain = GetTemplateChild("PART_LeftCurtain") as Canvas;
            _rightSideCurtain = GetTemplateChild("PART_RightCurtain") as Canvas;
            if (_leftSideCurtain != null)
            {
                _leftSideCurtain.Width = 0;
            }
            if (_rightSideCurtain != null)
            {
                _rightSideCurtain.Width = 0;
            }
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            Render();
        }

        protected override void OnTuneChanged()
        {
            _watchesCues?.Dispose();
            _watchesCues = Observable.FromEventPattern<EventArgs>(
                    ev => Tune.CuesChanged += ev,
                    ev => Tune.CuesChanged -= ev)
                .Subscribe(TuneOnCuesChanged);
            TuneOnCuesChanged();
        }

        private void TuneOnCuesChanged(EventPattern<EventArgs>? obj = null)
        {
            var newCues = Tune.Cues().Select(d => new ZeroToOne(new FiniteDouble(d))).ToList();
            if (newCues.Count == 0 || 
                (_cuePoints.Count > 0 &&
                 newCues.Intersect(_cuePoints).Count() == _cuePoints.Count))
            {
                // no change
                return;
            }
            _cuePoints.Clear();
            _cuePoints.AddRange(newCues);
            Render();
        }

        /// <summary>
        /// Invoked when an unhandled MouseLeftButtonDown routed event is raised on this element. Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="e">The MouseButtonEventArgs that contains the event data. The event data reports that the left mouse button was pressed.</param>
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            if (!EnableCueMarksRepositioning)
            {
                AfterMouseLeftButtonDown();
                return;
            }
            CurtainMoving();
            AfterMouseLeftButtonDown();
        }

        /// <summary>
        /// A utility method
        /// </summary>
        private void AfterMouseLeftButtonDown()
        {
            //CaptureMouse();
            _isMouseDown = true;
        }

        /// <summary>
        /// Move a cue point, or a repeat region
        /// </summary>
        /// <param name="e">The MouseEventArgs that contains the event data.</param>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (!_isMouseDown || _cueMarksCanvas == null) return;
            var currentPoint = e.GetPosition(MainCanvas);
            /* sanitization */
            if (currentPoint.X < _waveformDimensions.LeftMargin())
            {
                currentPoint.X = _waveformDimensions.LeftMargin();
            }
            if (currentPoint.X > MainCanvas.RenderSize.Width - _waveformDimensions.RightMargin())
            {
                currentPoint.X = MainCanvas.RenderSize.Width - _waveformDimensions.RightMargin();
            }

            var leftCorner = currentPoint.X - (_cueMarksCanvas.RenderSize.Height / 2.5d);
            var rightCorner = currentPoint.X + (_cueMarksCanvas.RenderSize.Height / 2.5d);
            if (EnableCueMarksRepositioning
                && leftCorner >= 0 && rightCorner <= MainCanvas.RenderSize.Width)
            {
                MoveCuePoint(currentPoint, leftCorner, rightCorner);
            }
        }

        /// <summary>
        /// Invoked when an unhandled MouseLeftButtonUp routed event reaches an element in its route that is derived from this class. 
        /// Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="e">The MouseButtonEventArgs that contains the event data. The event data reports that the left mouse button was released.</param>
        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);
            //ReleaseMouseCapture();
            if (!_isMouseDown || !EnableCueMarksRepositioning) return;
            _isMouseDown = false;
            MeasureArea();
            CurtainMoved(_waveformDimensions.PercentOfCompleteWaveform(e.GetPosition(MainCanvas).X));
        }

        protected override void Render()
        {
            Clear();
            MeasureArea();
            if (!ShowCueMarks || _cuePoints.Count == 0 || _leftSideCurtain == null || _rightSideCurtain == null || MainCanvas == null || _cueMarksCanvas == null || _waveformDimensions.AreEmpty()) return;

            double minCuePoint = Math.Max(_cuePoints.Min(), 0);
            if (_coverageArea.Includes(minCuePoint))
            {
                _leftSideCurtain.Margin = new Thickness(_waveformDimensions.LeftMargin(), 0, 0, 0);
                _leftSideCurtain.Width = Math.Max(0, _waveformDimensions.PositionOnCompleteWaveform(minCuePoint));
            }

            double maxCuePoint = _cuePoints.Count == 1 ? 1 : Math.Min(_cuePoints.Max(), 1);
            if (_coverageArea.Includes(maxCuePoint))
            {
                double rightSideCurtainLeftX =  _waveformDimensions.LeftMargin() + _waveformDimensions.AbsoluteLocationToRendered(_waveformDimensions.PositionOnCompleteWaveform(maxCuePoint));
                double rightSideCurtainRightX = _waveformDimensions.LeftMargin() + _waveformDimensions.Width();
                _rightSideCurtain.Width = Math.Max(0,
                    (rightSideCurtainRightX - rightSideCurtainLeftX));
                _rightSideCurtain.Margin = new Thickness(0, 0, _waveformDimensions.RightMargin(), 0);
            }

            _cuePoints.Select(cue => (Cue: cue, Location: 
                    new FiniteDouble(_waveformDimensions.LeftMargin() + _waveformDimensions.PositionOnCompleteWaveform(cue))))
                .Select(t => (Cue: t.Cue, Location:
                    new FiniteDouble(_waveformDimensions.AbsoluteLocationToRendered(t.Location))))
                .Select(t => (Line: DrawLine(t.Cue, t.Location), Polygon: DrawPolygon(t.Cue, t.Location, _cueMarksCanvas.RenderSize.Height / 2.5d)))
                .ForEach(AddCurtain);
        }

        private void AddCurtain((Line Line, Polygon Polygon) t)
        {
            if (_cueMarksCanvas == null) return;
            _cuePointLines.Add(t.Line);
            MainCanvas.Children.Add(t.Line);
            _cuePointMarks.Add(t.Polygon);
            _cueMarksCanvas.Children.Add(t.Polygon);
        }

        private Polygon DrawPolygon(double cp, double xLocation, double centerOffset)
        {
            Polygon cue = new()
            {
                Points = new PointCollection
                {
                    new Point(xLocation, 0), // top
                    new Point(xLocation - centerOffset, _cueMarksCanvas!.RenderSize.Height / 2), // left middle
                    new Point(xLocation - centerOffset, _cueMarksCanvas.RenderSize.Height), // left bottom
                    new Point(xLocation + centerOffset, _cueMarksCanvas.RenderSize.Height),  // right
                    new Point(xLocation + centerOffset, _cueMarksCanvas.RenderSize.Height / 2)  // right
                }
            };
            var cueStyle = Application.Current.FindResource("CueMarkPolygonStyle") as Style;
            var invisibleCueStyle = Application.Current.FindResource("InvisibleCueMarkPolygonStyle") as Style;
            cue.Style = _coverageArea.Includes(cp) ? cueStyle : invisibleCueStyle;
            return cue;
        }

        private Line DrawLine(double cp, double xLocation)
        {
            return new Line
            {
                Stroke = _coverageArea.Includes(cp) ? CueMarkBrush : _transparentBrush,
                StrokeThickness = 1.0d,
                X1 = xLocation,
                X2 = xLocation,
                Y1 = 0,
                Y2 = MainCanvas.RenderSize.Height
            };
        }

        public void Clear()
        {
            _cuePointMarks.ForEach(mark => _cueMarksCanvas?.Children.Remove(mark));
            _cuePointMarks.Clear();
            _cuePointLines.ForEach(line => MainCanvas.Children.Remove(line));
            _cuePointLines.Clear();
            if (_leftSideCurtain != null)
                _leftSideCurtain.Width = 0;
            if (_rightSideCurtain != null)
                _rightSideCurtain.Width = 0;
        }

        private void CurtainMoving()
        {
            var cueMarkSelected = _cuePointMarks.FirstOrDefault(cue => cue.IsMouseOver);
            if (cueMarkSelected == null)
            {
                _selectedCuePointMark = _selectedCuePointLine = null;
                _animatedCurtain = null;
                _selectedCuePoint = 0.0d;
                _lastKnownGoodX = 0.0d;
                _cueMap.Clear();
                return;
            }

            _selectedCuePointMark = cueMarkSelected;
            _selectedCuePointMark.Fill = CueMarkAccentBrush;
            _selectedCuePointLine = _cuePointLines[_cuePointMarks.IndexOf(_selectedCuePointMark)];

            _cueMap.Clear();
            for (int i = 0; i < _cuePoints.Count && i < _cuePointMarks.Count; i++)
            {
                if (_cueMap.ContainsKey(_cuePoints[i]))
                    continue;
                _cueMap.Add(_cuePoints[i], _cuePointMarks[i]);

                if (ReferenceEquals(_cuePointMarks[i], cueMarkSelected))
                    _selectedCuePoint = _cuePoints[i]; // that's the cue point whose shape is being moved
            }

            // figure out which cue point we are moving
            _animatedCurtain = _cuePoints.Count == 1
                ? _leftSideCurtain
                : (_selectedCuePoint < _cuePoints.Max() ? _leftSideCurtain : _rightSideCurtain);
        }

        private void MoveCuePoint(Point currentPoint, double leftCorner, double rightCorner)
        {
            Debug.Assert(_cueMarksCanvas != null);
            Debug.Assert(_selectedCuePointLine != null);
            Debug.Assert(_leftSideCurtain != null);
            Debug.Assert(_rightSideCurtain != null);
            if (_selectedCuePointMark == null) return;
            _lastKnownGoodX = currentPoint.X;
            ((Polygon) _selectedCuePointMark).Points = new PointCollection()
            {
                new Point(currentPoint.X, 0),
                new Point(leftCorner, _cueMarksCanvas.RenderSize.Height / 2),
                new Point(leftCorner, _cueMarksCanvas.RenderSize.Height),
                new Point(rightCorner, _cueMarksCanvas.RenderSize.Height),
                new Point(rightCorner, _cueMarksCanvas.RenderSize.Height / 2)
            };

            _selectedCuePointLine.X1 = _lastKnownGoodX;
            _selectedCuePointLine.X2 = _lastKnownGoodX;
            _selectedCuePointLine.Y1 = 0;
            _selectedCuePointLine.Y2 = MainCanvas.RenderSize.Height;
            if (ReferenceEquals(_animatedCurtain, _leftSideCurtain))
            {
                _leftSideCurtain.Margin = new Thickness(_waveformDimensions.LeftMargin(), 0, 0, 0);
                _leftSideCurtain.Width = Math.Max(0, _lastKnownGoodX - _waveformDimensions.LeftMargin());
            }
            else
            {
                _rightSideCurtain.Width = Math.Max(0,
                    MainCanvas.RenderSize.Width - _lastKnownGoodX - _waveformDimensions.RightMargin());
            }
        }

        /// <summary>
        /// Moved the curtain, add a new cue
        /// </summary>
        /// <param name="newCue"></param>
        private void CurtainMoved(ZeroToOne newCue)
        {
            if (_selectedCuePointMark == null || !(_lastKnownGoodX > 0.0d)) return;            
            _cuePoints.Remove(_selectedCuePoint); 
            AddCuePoint(newCue);
            Render();
            _animatedCurtain = null;
            _selectedCuePointMark = _selectedCuePointLine = null;
            _selectedCuePoint = 0d;
            _lastKnownGoodX = 0.0d;
        }

        /// <summary>
        /// Add a cue point at the specified position on the timeline
        /// </summary>
        /// <param name="pos"></param>
        private void AddCuePoint(ZeroToOne pos)
        {
            if (!_cuePoints.Contains(pos)) _cuePoints.Add(pos);
            SyncTrackStartEndTimes(_cuePoints.ToArray());
        }

        private void SyncTrackStartEndTimes(ZeroToOne[] inputs)
        {
            var values = inputs.OrderBy(x => x).ToArray();
            if (values.Length != 2 || values[1] < values[0]) return;
            Tune.TrimStart(TimeSpan.FromTicks((long)(Math.Min(Math.Max(0d, values[0]), values[1]) * Tune.Duration().Ticks)));
            Tune.TrimEnd(TimeSpan.FromTicks((long)(Math.Min(Math.Max(values[0], values[1]), values[1]) * Tune.Duration().Ticks)));
        }
    }
}