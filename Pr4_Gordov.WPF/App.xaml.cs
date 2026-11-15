using System.Windows;
using Pr4_Gordov.WPF.Services;
using Pr4_Gordov.WPF.Views;

namespace Pr4_Gordov.WPF;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var api = new ApiClient();
        var loginWindow = new LoginWindow(api);
        loginWindow.Show();
    }
}