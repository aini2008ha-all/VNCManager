using System.Windows;
using System.IO;
using VNCManager.Services;

namespace VNCManager.Views;

public partial class LogWindow : Window
{
    private readonly LogService _log;

    public LogWindow(LogService log)
    {
        InitializeComponent();
        _log = log;
        RefreshLogs();
    }

    private void RefreshLogs()
    {
        var entries = _log.ReadAll();
        LogGrid.ItemsSource = entries;
        LogCountText.Text = $"共 {entries.Count} 条记录";
    }

    private void RefreshClick(object sender, RoutedEventArgs e) => RefreshLogs();

    private void ExportClick(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "CSV 文件|*.csv|文本文件|*.txt",
            FileName = $"VNCManager-log-{DateTime.Now:yyyyMMdd-HHmmss}.csv",
            AddExtension = true
        };
        if (dlg.ShowDialog() != true) return;
        try
        {
            _log.Export(dlg.FileName, Path.GetExtension(dlg.FileName).Equals(".csv", StringComparison.OrdinalIgnoreCase));
            System.Windows.MessageBox.Show("日志已导出。", "导出成功", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show("导出失败：" + ex.Message, "错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private void ClearClick(object sender, RoutedEventArgs e)
    {
        if (System.Windows.MessageBox.Show("确定清空全部操作日志吗？此操作不可恢复。", "清空日志",
                System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning) != System.Windows.MessageBoxResult.Yes)
            return;
        _log.Clear();
        RefreshLogs();
    }
}
