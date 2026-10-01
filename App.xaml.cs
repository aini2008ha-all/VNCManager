using System.Threading.Tasks;
using System.Windows;

namespace VNCManager;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += (_, args) =>
        {
            ShowRuntimeError(args.Exception);
            args.Handled = true;
        };

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            args.SetObserved();
        };
    }

    private static void ShowRuntimeError(Exception ex)
    {
        try
        {
            System.Windows.MessageBox.Show(
                $"程序发生错误：\n{ex.Message}\n\n详细信息：\n{ex}",
                "远程连接管理器 错误",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        catch { }
    }
}
