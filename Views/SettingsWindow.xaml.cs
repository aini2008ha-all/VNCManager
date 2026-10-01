using System.IO;
using System.Windows;
using Microsoft.Win32;
using VNCManager.Models;
using VNCManager.Services;

namespace VNCManager.Views;

public partial class SettingsWindow : Window
{
    public AppSettings Settings { get; private set; }
    private readonly VncService _vnc;

    public SettingsWindow(AppSettings settings, VncService vnc)
    {
        InitializeComponent();
        Settings = new AppSettings
        {
            TightVncViewerPath = settings.TightVncViewerPath,
            LastGroup = settings.LastGroup,
            WindowWidth = settings.WindowWidth,
            WindowHeight = settings.WindowHeight,
            MinimizeToTray = settings.MinimizeToTray,
            CheckBeforeConnect = settings.CheckBeforeConnect,
            RecentConnections = (settings.RecentConnections ?? []).ToList(),
            Groups = (settings.Groups ?? []).Select(g => new GroupInfo { Name = g.Name, SortOrder = g.SortOrder }).ToList(),
            ShortcutsInitialized = settings.ShortcutsInitialized,
            ViewMode = settings.ViewMode,
            Shortcuts = (settings.Shortcuts ?? []).Select(s => new ShortcutItem
            {
                Id = s.Id, Kind = s.Kind, Target = s.Target, DisplayName = s.DisplayName
            }).ToList()
        };
        _vnc = vnc;

        ViewerPathBox.Text = Settings.TightVncViewerPath;
        CheckBeforeBox.IsChecked = Settings.CheckBeforeConnect;
        TrayBox.IsChecked = Settings.MinimizeToTray;
        DataFolderText.Text = new DeviceService().DataFolder;
        UpdateViewerStatus();
        ViewerPathBox.TextChanged += (_, _) => UpdateViewerStatus();
    }

    private void UpdateViewerStatus()
    {
        var path = ViewerPathBox.Text.Trim();
        var found = _vnc.FindViewer(path);
        ViewerStatus.Text = found != null
            ? $"已找到：{found}"
            : "未找到 Viewer。将优先使用程序目录 tightvnc\\tvnviewer.exe。";
    }

    private void BrowseClick(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "可执行文件|*.exe",
            FileName = "tvnviewer.exe"
        };
        if (dlg.ShowDialog() == true)
            ViewerPathBox.Text = dlg.FileName;
    }

    private void SaveClick(object sender, RoutedEventArgs e)
    {
        Settings.TightVncViewerPath = ViewerPathBox.Text.Trim();
        Settings.CheckBeforeConnect = CheckBeforeBox.IsChecked == true;
        Settings.MinimizeToTray = TrayBox.IsChecked == true;
        DialogResult = true;
    }

    private void LogsClick(object sender, RoutedEventArgs e)
    {
        var win = new LogWindow(new LogService()) { Owner = this };
        win.ShowDialog();
    }

    private void CancelClick(object sender, RoutedEventArgs e) => DialogResult = false;
}
