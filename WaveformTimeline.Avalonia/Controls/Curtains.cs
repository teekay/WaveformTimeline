#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using WaveformTimeline.Primitives;

namespace WaveformTimeline.Controls
{
    public sealed class Curtains : BaseControl
    {
        private Canvas? _cueMarksCanvas;
        private Canvas? _leftSideCurtain;
        private Canvas? _rightSideCurtain;
        private readonly List<ZeroToOne> _cuePoints = new();
        private readonly List<Polygon> _cuePointMarks = new();
        private readonly List<Line> _cuePointLines = new();
        private readonly IBrush _transparentBrush = new SolidColorBrush { Color = Color.FromArgb(0, 0, 0, 0), Opacity = 0 };
        private readonly Dictionary<double, Polygon> _cueMap = new();
        private ZeroToOne _selectedCuePoint;
        private Polygon? _selectedCuePointMark;
        private Line? _selectedCuePointLine;
        private double _lastKnownGoodX;
        private Canvas? _animatedCurtain;
        private bool _isMouseDown;
        private IDisposable? _watchesCues;

        public static readonly StyledProperty<IBrush> CueMarkBrushProperty =
            AvaloniaProperty.Register<Curtains, IBrush>(nameof(CueMarkBrush),
                defaultValue: new SolidColorBrush(Color.FromArgb(0xCD, 0xBA, 0x00, 0xFF)));

        public IBrush CueMarkBrush
        {
            get => GetValue(CueMarkBrushProperty);
            set => SetValue(CueMarkBrushProperty, value);
        }

        public static readonly StyledProperty<IBrush> CueBarBackgroundBrushProperty =
            AvaloniaProperty.Register<Curtains, IBrush>(nameof(CueBarBackgroundBrush),
                defaultValue: new SolidColorBrush(Color.FromArgb(0xCD, 0xBA, 0x00, 0xFF)));

        public IBrush CueBarBackgroundBrush
        {
            get => GetValue(CueBarBackgroundBrushProperty);
            set => SetValue(CueBarBackgroundBrushProperty, value);
        }

        public static readonly StyledProperty<IBrush> CueMarkAccentBrushProperty =
            AvaloniaProperty.Register<Curtains, IBrush>(nameof(CueMarkAccentBrush),
                defaultValue: new SolidColorBrush(Color.FromRgb(255, 0, 0)));

        public IBrush CueMarkAccentBrush
        {
            get => GetValue(CueMarkAccentBrushProperty);
            set => SetValue(CueMarkAccentBrushProperty, value);
        }

        public static readonly StyledProperty<bool> ShowCueMarksProperty =
            AvaloniaProperty.Register<Curtains, bool>(nameof(ShowCueMarks), defaultValue: true);

        public bool ShowCueMarks
        {
            get => GetValue(ShowCueMarksProperty);
            set => SetValue(ShowCueMarksProperty, value);
        }

        public static readonly StyledProperty<bool> ShowCueMarkToolTipProperty =
            AvaloniaProperty.Register<Curtains, bool>(nameof(ShowCueMarkToolTip), defaultValue: false);

        public bool ShowCueMarkToolTip
        {
            get => GetValue(ShowCueMarkToolTipProperty);
            set => SetValue(ShowCueMarkToolTipProperty, value);
        }

        public static readonly StyledProperty<bool> EnableCueMarksRepositioningProperty =
            AvaloniaProperty.Register<Curtains, bool>(nameof(EnableCueMarksRepositioning), defaultValue: true);

        public bool EnableCueMarksRepositioning
        {
            get => GetValue(EnableCueMarksRepositioningProperty);
            set => SetValue(EnableCueMarksRepositioningProperty, value);
        }

        static Curtains()
        {
            CueMarkBrushProperty.Changed.AddClassHandler<Curtains>((c, _) => c.Render());
            ShowCueMarksProperty.Changed.AddClassHandler<Curtains>((c, _) => c.Render());
            EnableCueMarksRepositioningProperty.Changed.AddClassHandler<Curtains>((c, _) => c.Render());
            ShowCueMarkToolTipProperty.Changed.AddClassHandler<Curtains>((c, _) => c.Render());
            CueBarBackgroundBrushProperty.Changed.AddClassHandler<Curtains>((c, e) =>
            {
                if (c._cueMarksCanvas != null)
                    c._cueMarksCanvas.Background = (IBrush?)e.NewValue;
            });
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            MainCanvas = e.NameScope.Find<Canvas>("PART_Curtains");
            _cueMarksCanvas = e.NameScope.Find<Canvas>("PART_CueMarks");
            _leftSideCurtain = e.NameScope.Find<Canvas>("PART_LeftCurtain");
            _rightSideCurtain = e.NameScope.Find<Canvas>("PART_RightCurtain");
            if (_leftSideCurtain != null) _leftSideCurtain.Width = 0;
            if (_rightSideCurtain != null) _rightSideCurtain.Width = 0;

            var boundsObs = MainCanvas?.GetObservable(BoundsProperty);
            boundsObs?.Subscribe(_ => Render());
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
                return;
            _cuePoints.Clear();
            _cuePoints.AddRange(newCues);
            Render();
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);
            if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
            if (!EnableCueMarksRepositioning)
            {
                _isMouseDown = true;
                return;
            }
            CurtainMoving();
            _isMouseDown = true;
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);
            if (!_isMouseDown || _cueMarksCanvas == null || MainCanvas == null) return;

            var currentPoint = e.GetPosition(MainCanvas);
            if (currentPoint.X < WaveformDimensions.LeftMargin())
                currentPoint = currentPoint.WithX(WaveformDimensions.LeftMargin());
            if (currentPoint.X > MainCanvas.Bounds.Width - WaveformDimensions.RightMargin())
                currentPoint = currentPoint.WithX(MainCanvas.Bounds.Width - WaveformDimensions.RightMargin());

            var leftCorner = currentPoint.X - (_cueMarksCanvas.Bounds.Height / 2.5d);
            var rightCorner = currentPoint.X + (_cueMarksCanvas.Bounds.Height / 2.5d);
            if (EnableCueMarksRepositioning && leftCorner >= 0 && rightCorner <= MainCanvas.Bounds.Width)
                MoveCuePoint(currentPoint, leftCorner, rightCorner);
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);
            FinishCurtainMovement(e.GetPosition(MainCanvas!).X);
        }

        private void FinishCurtainMovement(double xPosition)
        {
            if (!_isMouseDown || !EnableCueMarksRepositioning) return;
            _isMouseDown = false;
            MeasureArea();
            CurtainMoved(WaveformDimensions.PercentOfCompleteWaveform(xPosition));
        }

        protected override void OnPointerExited(PointerEventArgs e)
        {
            base.OnPointerExited(e);
            if (MainCanvas != null)
                FinishCurtainMovement(e.GetPosition(MainCanvas).X);
        }

        protected override void Render()
        {
            Clear();
            MeasureArea();
            if (!ShowCueMarks || _cuePoints.Count == 0 || _leftSideCurtain == null || _rightSideCurtain == null ||
                MainCanvas == null || _cueMarksCanvas == null || WaveformDimensions.AreEmpty())
                return;

            double minCuePoint = Math.Max(_cuePoints.Min(), 0);
            if (CoverageArea.Includes(minCuePoint))
            {
                _leftSideCurtain.Margin = new Thickness(WaveformDimensions.LeftMargin(), 0, 0, 0);
                _leftSideCurtain.Width = Math.Max(0, WaveformDimensions.PositionOnCompleteWaveform(minCuePoint));
            }

            double maxCuePoint = _cuePoints.Count == 1 ? 1 : Math.Min(_cuePoints.Max(), 1);
            if (CoverageArea.Includes(maxCuePoint))
            {
                double rightSideCurtainLeftX = WaveformDimensions.LeftMargin() + WaveformDimensions.AbsoluteLocationToRendered(WaveformDimensions.PositionOnCompleteWaveform(maxCuePoint));
                double rightSideCurtainRightX = WaveformDimensions.LeftMargin() + WaveformDimensions.Width();
                _rightSideCurtain.Width = Math.Max(0, rightSideCurtainRightX - rightSideCurtainLeftX);
                _rightSideCurtain.Margin = new Thickness(0, 0, WaveformDimensions.RightMargin(), 0);
            }

            var curtains = _cuePoints
                .Select(cue => (
                    Cue: cue,
                    Location: new FiniteDouble(WaveformDimensions.LeftMargin() + WaveformDimensions.PositionOnCompleteWaveform(cue))))
                .Select(t => (
                    t.Cue,
                    Location: new FiniteDouble(WaveformDimensions.AbsoluteLocationToRendered(t.Location))))
                .Select(t => (
                    Line: CueLine(t.Cue, t.Location),
                    Polygon: CueHandle(t.Cue, t.Location, _cueMarksCanvas.Bounds.Height / 2)));
            foreach (var curtain in curtains)
                AddCurtain(curtain);
        }

        private void AddCurtain((Line Line, Polygon Polygon) t)
        {
            if (_cueMarksCanvas == null || MainCanvas == null) return;
            _cuePointLines.Add(t.Line);
            MainCanvas.Children.Add(t.Line);
            _cuePointMarks.Add(t.Polygon);
            _cueMarksCanvas.Children.Add(t.Polygon);
        }

        private Polygon CueHandle(double cp, double xLocation, double centerOffset)
        {
            var cue = new Polygon
            {
                Points = new List<Point>
                {
                    new(xLocation, 0),
                    new(xLocation - centerOffset, _cueMarksCanvas!.Bounds.Height / 2),
                    new(xLocation - centerOffset, _cueMarksCanvas.Bounds.Height),
                    new(xLocation + centerOffset, _cueMarksCanvas.Bounds.Height),
                    new(xLocation + centerOffset, _cueMarksCanvas.Bounds.Height / 2)
                }
            };

            if (ShowCueMarkToolTip)
                ToolTip.SetTip(cue, TimeSpan.FromTicks((long)(Tune.TotalTime().Ticks * cp)).ToString(@"m\:ss"));

            cue.Fill = CoverageArea.Includes(cp) ? Brushes.White : Brushes.Transparent;
            return cue;
        }

        private Line CueLine(double cp, double xLocation) => new()
        {
            Stroke = CoverageArea.Includes(cp) ? CueMarkBrush : _transparentBrush,
            StrokeThickness = 1.0d,
            StartPoint = new Point(xLocation, 0),
            EndPoint = new Point(xLocation, MainCanvas?.Bounds.Height ?? 0)
        };

        public void Clear()
        {
            foreach (var mark in _cuePointMarks)
                _cueMarksCanvas?.Children.Remove(mark);
            _cuePointMarks.Clear();
            foreach (var line in _cuePointLines)
                MainCanvas?.Children.Remove(line);
            _cuePointLines.Clear();
            if (_leftSideCurtain != null) _leftSideCurtain.Width = 0;
            if (_rightSideCurtain != null) _rightSideCurtain.Width = 0;
        }

        private void CurtainMoving()
        {
            var cueMarkSelected = _cuePointMarks.FirstOrDefault(cue => cue.IsPointerOver);
            if (cueMarkSelected == null)
            {
                _selectedCuePointMark = null;
                _selectedCuePointLine = null;
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
                if (_cueMap.ContainsKey(_cuePoints[i])) continue;
                _cueMap.Add(_cuePoints[i], _cuePointMarks[i]);
                if (ReferenceEquals(_cuePointMarks[i], cueMarkSelected))
                    _selectedCuePoint = _cuePoints[i];
            }

            _animatedCurtain = _cuePoints.Count == 1
                ? _leftSideCurtain
                : (_selectedCuePoint < _cuePoints.Max() ? _leftSideCurtain : _rightSideCurtain);
        }

        private void MoveCuePoint(Point currentPoint, double leftCorner, double rightCorner)
        {
            if (MainCanvas == null || _cueMarksCanvas == null || _selectedCuePointMark == null ||
                _leftSideCurtain == null || _rightSideCurtain == null || _selectedCuePointLine == null)
                return;

            _lastKnownGoodX = currentPoint.X;
            _selectedCuePointMark.Points = new List<Point>
            {
                new(currentPoint.X, 0),
                new(leftCorner, _cueMarksCanvas.Bounds.Height / 2),
                new(leftCorner, _cueMarksCanvas.Bounds.Height),
                new(rightCorner, _cueMarksCanvas.Bounds.Height),
                new(rightCorner, _cueMarksCanvas.Bounds.Height / 2)
            };

            _selectedCuePointLine.StartPoint = new Point(_lastKnownGoodX, 0);
            _selectedCuePointLine.EndPoint = new Point(_lastKnownGoodX, MainCanvas.Bounds.Height);
            if (ReferenceEquals(_animatedCurtain, _leftSideCurtain))
            {
                _leftSideCurtain.Margin = new Thickness(WaveformDimensions.LeftMargin(), 0, 0, 0);
                _leftSideCurtain.Width = Math.Max(0, _lastKnownGoodX - WaveformDimensions.LeftMargin());
            }
            else
            {
                _rightSideCurtain.Width = Math.Max(0,
                    MainCanvas.Bounds.Width - _lastKnownGoodX - WaveformDimensions.RightMargin());
            }
        }

        private void CurtainMoved(ZeroToOne newCue)
        {
            if (_selectedCuePointMark == null || !(_lastKnownGoodX > 0.0d)) return;
            _cuePoints.Remove(_selectedCuePoint);
            AddCuePoint(newCue);
            Render();
            _animatedCurtain = null;
            _selectedCuePointMark = null;
            _selectedCuePointLine = null;
            _selectedCuePoint = 0d;
            _lastKnownGoodX = 0.0d;
        }

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
