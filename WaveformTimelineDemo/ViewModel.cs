using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using JetBrains.Annotations;
using Microsoft.Win32;
using WaveformTimelineDemo.Audio;
using WaveformTimelineDemo.Toolbox;

namespace WaveformTimelineDemo
{
    internal class ViewModel: INotifyPropertyChanged
    {
        public ViewModel()
        {
            OpenFile = new RelayCommand(OpenFileCmd);
            Play = new RelayCommand(PlayCmd, () => _fileUri != string.Empty && !Tune.IsPlaying());
            Pause = new RelayCommand(PauseCmd, () => Tune.IsPlaying());
            Stop = new RelayCommand(StopCmd, () => Tune.IsPlaying() || Tune.IsPaused());
        }

        [NotNull] public ICombiPlayer Tune { get; private set; } = new NullPlayer();
        private string _fileUri = string.Empty;

        public ICommand OpenFile { get; }
        public ICommand Play { get; }
        public ICommand Pause { get; }
        public ICommand Stop { get; }
        public string Title => new StringWithPlaceholder(Tune.Name(), "No track").Value();

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private void OpenFileCmd()
        {
            // Configure open file dialog box
            var dlg = new OpenFileDialog
            {
                DefaultExt = ".mp3",
                Multiselect = false,
                Filter = "Audio files|*.wav;*.mp3;*.m4a"
            };

            bool? result = dlg.ShowDialog();
            if (result != true) return;
            _fileUri = dlg.FileName;
            Tune = new Tune(_fileUri);
            OnPropertyChanged(nameof(Title));
            OnPropertyChanged(nameof(Tune));
        }

        private void PlayCmd()
        {
            try
            {
                Tune.Play();
                OnPropertyChanged(nameof(Title));
            }
            catch(Exception e)
            {
                MessageBox.Show($"There was an error when trying to start playback: {e.Message}");
                _fileUri = string.Empty;
            }
        }

        private void PauseCmd()
        {
            try
            {
                Tune.Pause();
            }
            catch (Exception e)
            {
                MessageBox.Show($"There was an error when trying to pause playback: {e.Message}");
            }
        }

        private void StopCmd()
        {
            try
            {
                Tune.Stop();
            }
            catch (Exception e)
            {
                MessageBox.Show($"There was an error when trying to stop playback: {e.Message}");
            }
            _fileUri = string.Empty;
            Tune = new NullPlayer();
            OnPropertyChanged(nameof(Title));
            OnPropertyChanged(nameof(Tune));
        }
    }
}
