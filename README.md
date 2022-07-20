A set of controls that display a stereo waveform, time, and progress information about an audio stream.

This can be useful to you when you develop WPF apps that deal with audio.

# Implementation overview
* Implement the interface ITune that represents a song being played back
* Implement the IAudioWaveformStream such that you can generate waveform data in any resolution you want to use
* Use the Timeline, Waveform, Progress, and Curtain controls in any combination you see fit, or together using the aggregate
WaveformTimeline control built for your convenience.

# Dependencies
* .NET 6
* JetBrains.Annotations 2020.1.0
* morelinq 3.3.2
* System.Reactive 5.0.0
* System.Reactive.Linq 5.0.0

# License
The MIT License (MIT)

Copyright (c) 2011-2021 Jacob Johnston, Tomáš Kohl

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
