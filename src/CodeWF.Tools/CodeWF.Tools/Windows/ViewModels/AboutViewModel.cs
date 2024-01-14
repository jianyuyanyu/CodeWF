﻿namespace CodeWF.Tools.Windows.ViewModels;

public sealed class AboutViewModel : ViewModelBase
{
    private bool _darkTheme;

    public bool DarkTheme
    {
        get => _darkTheme;
        set => this.RaiseAndSetIfChanged(ref _darkTheme, value);
    }

    public AboutViewModel(bool darkTheme)
    {
        DarkTheme = darkTheme;
    }

    public void OpenProjectRepoLink() => GlobalCommand.OpenProjectRepoLink();
}