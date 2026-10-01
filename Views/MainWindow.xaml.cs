using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using VNCManager.Models;
using VNCManager.ViewModels;
using Application = System.Windows.Application;
using Button = System.Windows.Controls.Button;
using MessageBox = System.Windows.MessageBox;

namespace VNCManager.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;
    private NotifyIcon? _tray;
    private System.Drawing.Icon? _trayIcon;
    private bool _reallyExit;

    public MainWindow()
    {
        InitializeComponent();
        _vm = new MainViewModel();
        DataContext = _vm;
        _vm.DevicesChanged += RefreshList;
        Loaded += async (_, _) =>
        {
            RefreshList();
            InitTray();
            await _vm.CheckAllAsync();
        };
        Closed += (_, _) => DisposeTray();
    }

    private void RefreshList()
    {
        DeviceList.ItemsSource = _vm.FilteredDevices.ToList();
        DeviceTable.ItemsSource = _vm.FilteredDevices.ToList();
    }

    private void InitTray()
    {
        _trayIcon = LoadAppIcon();
        _tray = new NotifyIcon
        {
            Icon = _trayIcon ?? SystemIcons.Application,
            Text = "远程连接管理器",
            Visible = true
        };
        _tray.DoubleClick += (_, _) => RestoreFromTray();

        var menu = new ContextMenuStrip();
        menu.Items.Add("显示主窗口", null, (_, _) => RestoreFromTray());
        menu.Items.Add("检查全部在线", null, async (_, _) => await _vm.CheckAllAsync());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("退出", null, (_, _) =>
        {
            _reallyExit = true;
            Close();
        });
        _tray.ContextMenuStrip = menu;
    }

    /// <summary>优先使用与 EXE 相同的应用图标</summary>
    private static System.Drawing.Icon? LoadAppIcon()
    {
        try
        {
            var exe = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(exe) && File.Exists(exe))
            {
                var icon = System.Drawing.Icon.ExtractAssociatedIcon(exe);
                if (icon != null) return (System.Drawing.Icon)icon.Clone();
            }
        }
        catch { /* ignore */ }

        try
        {
            var icoPath = Path.Combine(AppContext.BaseDirectory, "app.ico");
            if (File.Exists(icoPath))
                return new System.Drawing.Icon(icoPath);
        }
        catch { /* ignore */ }

        return null;
    }

    private void DisposeTray()
    {
        if (_tray != null)
        {
            _tray.Visible = false;
            _tray.Dispose();
            _tray = null;
        }
        _trayIcon?.Dispose();
        _trayIcon = null;
    }

    private void RestoreFromTray()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void Window_StateChanged(object sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized && _vm.Settings.MinimizeToTray)
        {
            Hide();
            if (_tray != null)
            {
                _tray.BalloonTipTitle = "远程连接管理器";
                _tray.BalloonTipText = "程序已在托盘运行，双击图标可恢复。";
                _tray.ShowBalloonTip(1500);
            }
        }
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_reallyExit || !_vm.Settings.MinimizeToTray)
        {
            DisposeTray();
            return;
        }
        e.Cancel = true;
        WindowState = WindowState.Minimized;
        Hide();
    }

    private void GroupClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button b && b.Tag is string group)
            _vm.SelectedGroup = group;
        RefreshList();
    }

    private void RecentClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button b && b.Tag is RecentItem item)
            _vm.ConnectRecentCommand.Execute(item);
    }

    private void CardMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left || e.ClickCount != 2) return;
        if (sender is Border border && border.Tag is VncDevice d)
        {
            _vm.SelectedDevice = d;
            _vm.ConnectCommand.Execute(null);
        }
    }


    private void ListMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DeviceTable.SelectedItem is VncDevice d)
        {
            _vm.SelectedDevice = d;
            _vm.ConnectCommand.Execute(null);
        }
    }
    private void ConnectClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button b && b.Tag is VncDevice d)
        {
            _vm.SelectedDevice = d;
            _vm.ConnectCommand.Execute(null);
        }
    }

    private void SelectionChanged(object sender, RoutedEventArgs e)
    {
        _vm.SelectionChangedCommand.Execute(null);
    }

    private void MoreClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button b && b.Tag is VncDevice d)
        {
            _vm.SelectedDevice = d;
            var menu = new ContextMenu();

            var edit = new MenuItem { Header = "编辑设备" };
            edit.Click += (_, _) => _vm.EditCommand.Execute(null);

            var fav = new MenuItem { Header = d.Favorite ? "取消收藏" : "加入收藏" };
            fav.Click += (_, _) => _vm.ToggleFavoriteCommand.Execute(null);

            var delete = new MenuItem { Header = "删除设备" };
            delete.Click += (_, _) => _vm.DeleteCommand.Execute(null);

            menu.Items.Add(edit);
            menu.Items.Add(fav);
            menu.Items.Add(new Separator());
            menu.Items.Add(delete);
            menu.IsOpen = true;
        }
    }

    private void GroupListRightClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is System.Windows.Controls.ListBox lb && lb.InputHitTest(e.GetPosition(lb)) is DependencyObject hit)
        {
            var item = ItemsControl.ContainerFromElement(lb, hit) as ListBoxItem;
            if (item != null)
            {
                item.IsSelected = true;
                item.Focus();
            }
        }
    }

    private void GroupEditMenuClick(object sender, RoutedEventArgs e)
    {
        _vm.EditGroupCommand.Execute(null);
    }

    private void GroupDeleteMenuClick(object sender, RoutedEventArgs e)
    {
        _vm.RemoveGroupCommand.Execute(null);
    }

    private void RemoveRecentClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button b && b.Tag is RecentItem item)
            _vm.RemoveRecentCommand.Execute(item);
    }

    private void ShortcutClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is ShortcutItem item)
            _vm.ShortcutClickCommand.Execute(item);
    }

    private void ShortcutRightClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.Tag is not ShortcutItem item) return;
        var menu = new ContextMenu();
        if (item.IsEmpty)
        {
            var add = new MenuItem { Header = "添加快捷方式" };
            add.Click += (_, _) => _vm.ShortcutEditCommand.Execute(item);
            menu.Items.Add(add);
        }
        else
        {
            var open = new MenuItem { Header = "打开" };
            open.Click += (_, _) => _vm.ShortcutClickCommand.Execute(item);
            var edit = new MenuItem { Header = "重新设置" };
            edit.Click += (_, _) => _vm.ShortcutEditCommand.Execute(item);
            var clear = new MenuItem { Header = "清除" };
            clear.Click += (_, _) => _vm.ShortcutClearCommand.Execute(item);
            menu.Items.Add(open);
            menu.Items.Add(edit);
            menu.Items.Add(new Separator());
            menu.Items.Add(clear);
        }
        menu.IsOpen = true;
        e.Handled = true;
    }
}
