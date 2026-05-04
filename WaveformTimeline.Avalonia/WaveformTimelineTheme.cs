#nullable enable
using System;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace WaveformTimeline
{
    public class WaveformTimelineTheme : Styles
    {
        public WaveformTimelineTheme(IServiceProvider? sp = null)
        {
            AvaloniaXamlLoader.Load(sp, this);
        }
    }
}
