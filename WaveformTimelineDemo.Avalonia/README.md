# WaveformTimelineDemo.Avalonia

Avalonia demo application for `WaveformTimeline.Avalonia`, modeled after the WPF `WaveformTimelineDemo` project.

## What it demonstrates

- Loading an audio file with Avalonia's storage picker
- Binding `WaveformTimeline.Tune` to an `ITune` implementation
- Rendering waveform, timeline, cue bar, and playback progress
- Playing, pausing, and stopping audio via the demo `NAudio` player

## Run

From the repository root:

```bash
dotnet run --project WaveformTimelineDemo.Avalonia/WaveformTimelineDemo.Avalonia.csproj
```

Then click **Load audio file** and choose a `.wav`, `.mp3`, `.m4a`, or `.flac` file.
