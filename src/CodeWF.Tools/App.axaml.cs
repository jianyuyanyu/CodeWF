﻿using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

using CodeWF.Tools.ViewModels;
using CodeWF.Tools.Views;

using Meadow;
using Meadow.Pinouts;

namespace CodeWF.Tools;

public partial class App : AvaloniaMeadowApplication<Linux<RaspberryPi>>
{

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        LoadMeadowOS();
    }

    public override Task InitializeMeadow()
    {
        var r = Resolver.Services.Get<IMeadowDevice>();

        if (r == null)
        {
            Resolver.Log.Info("IMeadowDevice is null");
        }
        else
        {
            Resolver.Log.Info($"IMeadowDevice is {r.GetType().Name}");
        }

        return Task.CompletedTask;

    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainViewModel()
            };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainView
            {
                DataContext = new MainViewModel()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
