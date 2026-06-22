#nullable enable
using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using WaveformTimelineDemo.Avalonia.Audio;
using WaveformTimelineDemo.Avalonia.Toolbox;

namespace WaveformTimelineDemo.Avalonia;

public class ViewModel : INotifyPropertyChanged
{
    public ViewModel()
    {
        OpenFile = new RelayCommand(async () => await OpenFileCmd());
        Play = new RelayCommand(PlayCmd, () => _fileUri != string.Empty && !Tune.IsPlaying());
        Pause = new RelayCommand(PauseCmd, () => Tune.IsPlaying());
        Stop = new RelayCommand(StopCmd, () => Tune.IsPlaying() || Tune.IsPaused());
    }

    public ICombiPlayer Tune { get; private set; } = new NullPlayer();
    private string _fileUri = string.Empty;
    private string _errorMessage = string.Empty;

    public ICommand OpenFile { get; }
    public ICommand Play { get; }
    public ICommand Pause { get; }
    public ICommand Stop { get; }
    public string Title => new StringWithPlaceholder(Tune.Name(), "No track").Value();
    public string ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (_errorMessage == value) return;
            _errorMessage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasError));
        }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private async Task OpenFileCmd()
    {
        var window = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (window == null) return;

        var files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select an audio file",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Audio files") { Patterns = new[] { "*.wav", "*.mp3", "*.m4a", "*.flac" } }
            }
        });

        var file = files.FirstOrDefault();
        if (file == null) return;

        try
        {
            _fileUri = file.Path.LocalPath;
            Tune = new Tune(_fileUri);
            ClearError();
            OnPropertyChanged(nameof(Title));
            OnPropertyChanged(nameof(Tune));
        }
        catch (Exception e)
        {
            _fileUri = string.Empty;
            Tune = new NullPlayer();
            SetError($"There was an error when trying to load the audio file: {e.Message}");
            OnPropertyChanged(nameof(Title));
            OnPropertyChanged(nameof(Tune));
        }
        finally
        {
            RaiseCanExecuteChanged();
        }
    }

    private void PlayCmd()
    {
        try
        {
            Tune.Play();
            ClearError();
            OnPropertyChanged(nameof(Title));
            RaiseCanExecuteChanged();
        }
        catch (Exception e)
        {
            _fileUri = string.Empty;
            SetError($"There was an error when trying to start playback: {e.Message}");
            RaiseCanExecuteChanged();
        }
    }

    private void PauseCmd()
    {
        try
        {
            Tune.Pause();
            ClearError();
        }
        catch (Exception e)
        {
            SetError($"There was an error when trying to pause playback: {e.Message}");
        }
        RaiseCanExecuteChanged();
    }

    private void StopCmd()
    {
        try
        {
            Tune.Stop();
            ClearError();
        }
        catch (Exception e)
        {
            SetError($"There was an error when trying to stop playback: {e.Message}");
        }
        _fileUri = string.Empty;
        Tune = new NullPlayer();
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(Tune));
        RaiseCanExecuteChanged();
    }

    private void SetError(string message) => ErrorMessage = message;

    private void ClearError() => ErrorMessage = string.Empty;

    private void RaiseCanExecuteChanged()
    {
        (Play as RelayCommand)?.RaiseCanExecuteChanged();
        (Pause as RelayCommand)?.RaiseCanExecuteChanged();
        (Stop as RelayCommand)?.RaiseCanExecuteChanged();
    }
}
