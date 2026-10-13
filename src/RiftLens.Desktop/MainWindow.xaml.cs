using System.Windows;

namespace RiftLens.Desktop;

public partial class MainWindow : Window
{
    public MainWindow(IServiceProvider services)
    {
        InitializeComponent();
        WebView.Services = services;
    }
}
