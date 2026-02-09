using System.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace CallCenter.Windows;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var services = new ServiceCollection();
        services.AddWpfBlazorWebView();
#if DEBUG
        services.AddBlazorWebViewDeveloperTools();
#endif
        blazorWebView.Services = services.BuildServiceProvider();
    }
}
