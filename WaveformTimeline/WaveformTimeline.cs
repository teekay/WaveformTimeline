// Based on code Copyright (C) 2011 - 2012, Jacob Johnston
// This version Copyright (C) 2013 - 2019, Tomáš Kohl
// Both the original and this version are published under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a 
// copy of this software and associated documentation files (the "Software"), 
// to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, 
// and/or sell copies of the Software, and to permit persons to whom the 
// Software is furnished to do so, subject to the following conditions: 
//
// The above copyright notice and this permission notice shall be included in 
// all copies or substantial portions of the Software. 
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.  IN NO EVENT SHALL 
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE. 

using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WaveformTimeline.Commons;
using WaveformTimeline.Contracts;
using WaveformTimeline.Controls;
using WaveformTimeline.Controls.Timeline;
using WaveformTimeline.Primitives;

namespace WaveformTimeline
{
    /// <summary>
    /// A control that displays a stereo waveform and allows a user to change playback position.
    /// This aggregates all controls available in this library. They can be used standalone, too.
    /// </summary>
    [DisplayName(@"Waveform Timeline")]
    [Description("Displays a stereo waveform and allows a user to change playback position.")]
    [ToolboxItem(true)]
    [TemplatePart(Name = "Component_Timeline", Type = typeof(Timeline)),
     TemplatePart(Name = "Component_Progress", Type = typeof(ProgressAnimator)),
     TemplatePart(Name = "Component_Waveform", Type = typeof(Canvas)),
     TemplatePart(Name = "Component_Curtains", Type = typeof(Canvas))]
    public sealed class WaveformTimeline: Control
    {
        static WaveformTimeline()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(WaveformTimeline), new FrameworkPropertyMetadata(typeof(WaveformTimeline)));
        }

        /// <summary>
        /// Identifies the <see cref="Tune" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty TuneProperty =
            DependencyProperty.Register("Tune", typeof(ITune), typeof(WaveformTimeline),
                new UIPropertyMetadata(null));

        [Category("Playback")]
        public ITune Tune
        {
            get => (ITune)GetValue(TuneProperty) ?? new NoTune(); // no null acceptable
            set
            {
                if (value != Tune)
                    SetValue(TuneProperty, value ?? new NoTune());
            }
        }

        /// <summary>
        /// Identifies the <see cref="Zoom" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty ZoomProperty = DependencyProperty.Register(
            "Zoom", typeof(double), typeof(WaveformTimeline),
            new PropertyMetadata(1.0d));

        /// <summary>
        /// Zoom level. 1 = normal.
        /// </summary>
        [Category("Display")]
        public double Zoom
        {
            get => (double)GetValue(ZoomProperty);
            set => SetValue(ZoomProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="LeftLevelBrush" /> dependency property. 
        /// </summary>
        public static readonly DependencyProperty LeftLevelBrushProperty = DependencyProperty.Register("LeftLevelBrush", typeof(Brush), typeof(WaveformTimeline),
            new UIPropertyMetadata(new SolidColorBrush(Colors.Blue)));

        /// <summary>
        /// Gets or sets a brush used to draw the left channel output on the waveform.
        /// </summary>        
        [Category("Waveform")]
        public Brush LeftLevelBrush
        {
            get => (Brush)GetValue(LeftLevelBrushProperty);
            set => SetValue(LeftLevelBrushProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="RightLevelBrush" /> dependency property. 
        /// </summary>
        public static readonly DependencyProperty RightLevelBrushProperty
            = DependencyProperty.Register("RightLevelBrush", typeof(Brush), typeof(WaveformTimeline),
                new UIPropertyMetadata(new SolidColorBrush(Colors.Red)));

        /// <summary>
        /// Gets or sets a brush used to draw the right speaker levels on the waveform.
        /// </summary>
        [Category("Waveform")]
        public Brush RightLevelBrush
        {
            get => (Brush)GetValue(RightLevelBrushProperty);
            set => SetValue(RightLevelBrushProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="CenterLineBrush" /> dependency property. 
        /// </summary>
        public static readonly DependencyProperty CenterLineBrushProperty
            = DependencyProperty.Register("CenterLineBrush", typeof(Brush), typeof(WaveformTimeline),
                new UIPropertyMetadata(new SolidColorBrush(Colors.Black)));

        /// <summary>
        /// Gets or sets a brush used to draw the center line separating left and right levels.
        /// </summary>
        [Category("Waveform")]
        public Brush CenterLineBrush
        {
            get => (Brush)GetValue(CenterLineBrushProperty);
            set => SetValue(CenterLineBrushProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="CenterLineThickness" /> dependency property. 
        /// </summary>
        public static readonly DependencyProperty CenterLineThicknessProperty =
            DependencyProperty.Register("CenterLineThickness", typeof(double), typeof(WaveformTimeline),
                new UIPropertyMetadata(1.0d));

        /// <summary>
        /// Gets or sets the thickness of the center line separating left and right levels.
        /// </summary>
        [Category("Waveform")]
        public double CenterLineThickness
        {
            get => (double)GetValue(CenterLineThicknessProperty);
            set => SetValue(CenterLineThicknessProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="AutoScaleWaveformCache" /> dependency property. 
        /// </summary>
        public static readonly DependencyProperty AutoScaleWaveformCacheProperty
            = DependencyProperty.Register("AutoScaleWaveformCache", typeof(bool), typeof(WaveformTimeline),
                new UIPropertyMetadata(false));

        /// <summary>
        /// Gets or sets a value indicating whether the waveform should attempt to autoscale
        /// its render buffer in size.
        /// </summary>
        [Category("Waveform")]
        public bool AutoScaleWaveformCache
        {
            get => (bool)GetValue(AutoScaleWaveformCacheProperty);
            set => SetValue(AutoScaleWaveformCacheProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="WaveformResolution"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty WaveformResolutionProperty =
            DependencyProperty.Register("WaveformResolution", typeof(int), typeof(WaveformTimeline),
                new UIPropertyMetadata(2000));

        /// <summary>
        /// Controls the density and accuracy of the generated waveform. Higher number = more detail.
        /// </summary>
        [Category("Waveform")]
        public int WaveformResolution
        {
            get => (int)GetValue(WaveformResolutionProperty);
            set => SetValue(WaveformResolutionProperty, value);
        }

        public static readonly DependencyProperty ProgressiveRenderingProperty = DependencyProperty.Register(
            "ProgressiveRendering", typeof(bool), typeof(WaveformTimeline),
            new PropertyMetadata(true));

        public bool ProgressiveRendering
        {
            get => (bool)GetValue(ProgressiveRenderingProperty);
            set => SetValue(ProgressiveRenderingProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ProgressBarBrush" /> dependency property. 
        /// </summary>
        public static readonly DependencyProperty ProgressBarBrushProperty
            = DependencyProperty.Register("ProgressBarBrush", typeof(Brush), typeof(WaveformTimeline),
                new UIPropertyMetadata(new SolidColorBrush(Color.FromArgb(0xCD, 0xBA, 0x00, 0xFF))));

        /// <summary>
        /// Gets or sets a brush used to draw the track progress indicator bar.
        /// </summary>
        [Category("Progress")]
        public Brush ProgressBarBrush
        {
            get => (Brush)GetValue(ProgressBarBrushProperty);
            set => SetValue(ProgressBarBrushProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ProgressBarThickness" /> dependency property. 
        /// </summary>
        public static readonly DependencyProperty ProgressBarThicknessProperty =
            DependencyProperty.Register("ProgressBarThickness", typeof(double), typeof(WaveformTimeline),
                new UIPropertyMetadata(2.0d));

        /// <summary>
        /// Get or sets the thickness of the progress indicator bar.
        /// </summary>
        [Category("Progress")]
        public double ProgressBarThickness
        {
            get => (double)GetValue(ProgressBarThicknessProperty);
            set => SetValue(ProgressBarThicknessProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="AllowRepositioning"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AllowRepositioningProperty =
            DependencyProperty.Register("AllowRepositioning", typeof(bool), typeof(WaveformTimeline),
                new UIPropertyMetadata(true));

        /// <summary>
        /// Whether the mouse click to the waveform should trigger playback repositioning or not
        /// Default is True
        /// </summary>
        [Category("Progress")]
        public bool AllowRepositioning
        {
            get => (bool)GetValue(AllowRepositioningProperty);
            set => SetValue(AllowRepositioningProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="TimelineTickBrush" /> dependency property. 
        /// </summary>
        public static readonly DependencyProperty TimelineTickBrushProperty
            = DependencyProperty.Register("TimelineTickBrush", typeof(Brush), typeof(WaveformTimeline),
                new UIPropertyMetadata(new SolidColorBrush(Colors.Black)));

        /// <summary>
        /// Gets or sets a brush used to draw the tickmarks on the timeline.
        /// </summary>
        [Category("Timeline")]
        public Brush TimelineTickBrush
        {
            get => (Brush)GetValue(TimelineTickBrushProperty);
            set => SetValue(TimelineTickBrushProperty, value);
        }

        /// <summary>
        /// Whether to show the ticks at constant intervals, or show more ticks and marks toward the end
        /// </summary>
        public static readonly DependencyProperty TimelineTypeProperty = DependencyProperty.Register(
            "TimelineType", typeof(TimelineType), typeof(WaveformTimeline), new PropertyMetadata(TimelineType.Constant));

        [Category("Timeline")]
        public TimelineType TimelineType
        {
            get => (TimelineType)GetValue(TimelineTypeProperty);
            set => SetValue(TimelineTypeProperty, value);
        }

        /// <summary>
        /// At which percentage of the Tune's length should the timeline show more frequent ticks at marks. Ignored for TimelineType.Constant.
        /// </summary>
        public static readonly DependencyProperty EndRevealingMarkProperty = DependencyProperty.Register(
            "EndRevealingMark", typeof(ZeroToOne), typeof(WaveformTimeline), new PropertyMetadata(new ZeroToOne(0.75)));

        [Category("Timeline")]
        public ZeroToOne EndRevealingMark
        {
            get => (ZeroToOne)GetValue(EndRevealingMarkProperty);
            set => SetValue(EndRevealingMarkProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ShowCueMarks"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowCueMarksProperty = DependencyProperty.Register("ShowCueMarks", typeof(bool), typeof(WaveformTimeline), 
            new UIPropertyMetadata(true));

        /// <summary>
        /// Whether to show the bar with cue marks
        /// Default: True
        /// </summary>
        [Category("Curtains")]
        public bool ShowCueMarks
        {
            get => (bool)GetValue(ShowCueMarksProperty);
            set => SetValue(ShowCueMarksProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="EnableCueMarksRepositioning"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty EnableCueMarksRepositioningProperty = DependencyProperty.Register("EnableCueMarksRepositioning", typeof(bool), typeof(WaveformTimeline), 
            new UIPropertyMetadata(true));

        /// <summary>
        /// Whether to enable moving the cue marks around
        /// Default: False
        /// </summary>
        [Category("Curtains")]
        public bool EnableCueMarksRepositioning
        {
            get => (bool)GetValue(EnableCueMarksRepositioningProperty);
            set => SetValue(EnableCueMarksRepositioningProperty, value);
        }
        /// <summary>
        /// Identifies the <see cref="CueMarkBrush" /> dependency property. 
        /// </summary>
        public static readonly DependencyProperty CueMarkBrushProperty
            = DependencyProperty.Register("CueMarkBrush", typeof(Brush), typeof(WaveformTimeline),
                new UIPropertyMetadata(new SolidColorBrush(Color.FromArgb(0xCD, 0xBA, 0x00, 0xFF))));

        /// <summary>
        /// Gets or sets a brush used to draw cue mark triangles.
        /// </summary>
        [Category("Curtains")]
        public Brush CueMarkBrush
        {
            get => (Brush)GetValue(CueMarkBrushProperty);
            set => SetValue(CueMarkBrushProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="CueBarBackgroundBrush" /> dependency property. 
        /// </summary>
        public static readonly DependencyProperty CueBarBackgroundBrushProperty
            = DependencyProperty.Register("CueBarBackgroundBrush", typeof(Brush), typeof(WaveformTimeline),
                new UIPropertyMetadata(new SolidColorBrush(Color.FromArgb(0xCD, 0xBA, 0x00, 0xFF))));

        /// <summary>
        /// Gets or sets a brush used to draw the track progress indicator bar.
        /// </summary>
        [Category("Curtains")]
        public Brush CueBarBackgroundBrush
        {
            // IMPORTANT: To maintain parity between setting a property in XAML and procedural code, do not touch the getter and setter inside this dependency property!
            get => (Brush)GetValue(CueBarBackgroundBrushProperty);
            set => SetValue(CueBarBackgroundBrushProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="CueMarkAccentBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CueMarkAccentBrushProperty = DependencyProperty.Register(
            "CueMarkAccentBrush", typeof(Brush), typeof(WaveformTimeline),
            new PropertyMetadata(new SolidColorBrush(Color.FromRgb(255, 0, 0))));

        [Category("Curtains")]
        public Brush CueMarkAccentBrush
        {
            get => (Brush)GetValue(CueMarkAccentBrushProperty);
            set => SetValue(CueMarkAccentBrushProperty, value);
        }
    }
}