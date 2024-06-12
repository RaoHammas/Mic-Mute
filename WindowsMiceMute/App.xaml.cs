using System.Windows;
using System.Windows.Threading;

namespace WindowsMicMute;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public App()
    {
        DispatcherUnhandledException += App_OnDispatcherUnhandledException;
    }

    private void App_OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        try
        {
            MessageBox.Show("An unexpected error occurred.", "Unexpected Error");
            e.Handled = true;
        }
        catch
        {
            //TODO
        }
    }
}