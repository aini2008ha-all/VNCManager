using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;
using MessageBoxResult = System.Windows.MessageBoxResult;
using Application = System.Windows.Application;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using VNCManager.Models;
using VNCManager.Services;

namespace VNCManager.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly DeviceService _store = new();
    private readonly VncService _vnc = new();
    private AppSettings _settings;
    private string _searchText = "";
    private string _selectedGroup = "全部设备";
    private VncDevice? _selectedDevice;
    private bool _isChecking;
    private bool _selectAll;
    private GroupInfo? _selectedGroupInfo;
    private bool _isCardView;
    private readonly LogService _log = new();

    public ObservableCollection<VncDevice> Devices { get; } = [];
    public ObservableCollection<GroupInfo> ManagedGroups { get; } = [];
    public ObservableCollection<string> CustomGroupNames { get; } = [];
    public ObservableCollection<RecentItem> RecentConnections { get; } = [];
    public ObservableCollection<ShortcutItem> Shortcuts { get; } = [];

    public bool IsCardView
    {
        get => _isCardView;
        private set
        {
            if (Set(ref _isCardView, value))
            {
                _settings.ViewMode = value ? "Card" : "List";
                _store.SaveSettings(_settings);
                OnPropertyChanged(nameof(IsListView));
                OnPropertyChanged(nameof(ViewModeButtonText));
            }
        }
    }
    public bool IsListView => !IsCardView;
    public string ViewModeButtonText => IsCardView ? "列表模式" : "卡片模式";

    public string SearchText { get => _searchText; set { if (Set(ref _searchText, value)) Refresh(); } }
    public string SelectedGroup
    {
        get => _selectedGroup;
        set
        {
            if (Set(ref _selectedGroup, value))
            {
                _settings.LastGroup = value;
                _store.SaveSettings(_settings);
                _selectedGroupInfo = ManagedGroups.FirstOrDefault(g => g.Name == value);
                OnPropertyChanged(nameof(SelectedGroupInfo));
                OnPropertyChanged(nameof(CanDeleteGroup));
                Refresh();
            }
        }
    }
    public GroupInfo? SelectedGroupInfo
    {
        get => _selectedGroupInfo;
        set
        {
            if (Set(ref _selectedGroupInfo, value) && value != null)
                SelectedGroup = value.Name;
        }
    }
    public VncDevice? SelectedDevice { get => _selectedDevice; set => Set(ref _selectedDevice, value); }
    public bool IsChecking { get => _isChecking; set => Set(ref _isChecking, value); }
    public bool SelectAll
    {
        get => _selectAll;
        set
        {
            if (Set(ref _selectAll, value))
            {
                foreach (var d in FilteredDevices)
                    d.IsSelected = value;
                OnPropertyChanged(nameof(SelectedCount));
                OnPropertyChanged(nameof(HasSelection));
            }
        }
    }

    public int DeviceCount => Devices.Count;
    public int OnlineCount => Devices.Count(x => x.Status == "在线");
    public int OfflineCount => Devices.Count(x => x.Status == "离线");
    public int SelectedCount => Devices.Count(x => x.IsSelected);
    public bool HasSelection => SelectedCount > 0;
    public bool CanDeleteGroup =>
        SelectedGroupInfo != null &&
        SelectedGroupInfo.Name is not ("全部设备" or "收藏");

    public ICommand AddCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand ConnectCommand { get; }
    public ICommand CheckAllCommand { get; }
    public ICommand ToggleFavoriteCommand { get; }
    public ICommand SettingsCommand { get; }
    public ICommand ExportCommand { get; }
    public ICommand ImportCommand { get; }
    public ICommand BatchDeleteCommand { get; }
    public ICommand BatchCheckCommand { get; }
    public ICommand BatchGroupCommand { get; }
    public ICommand AddGroupCommand { get; }
    public ICommand RemoveGroupCommand { get; }
    public ICommand RenameGroupCommand { get; }
    public ICommand EditGroupCommand { get; }
    public ICommand ConnectRecentCommand { get; }
    public ICommand SelectionChangedCommand { get; }
    public ICommand ClearRecentCommand { get; }
    public ICommand RemoveRecentCommand { get; }
    public ICommand ShortcutClickCommand { get; }
    public ICommand ShortcutEditCommand { get; }
    public ICommand ShortcutClearCommand { get; }
    public ICommand ToggleViewCommand { get; }
    public ICommand LogsCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action? DevicesChanged;
    public AppSettings Settings => _settings;

    public MainViewModel()
    {
        _settings = _store.LoadSettings();
        if (_settings.Groups == null || _settings.Groups.Count == 0)
            _settings.Groups = [new GroupInfo { Name = "默认", SortOrder = 0 }];

        foreach (var d in _store.LoadDevices())
            Devices.Add(d);

        foreach (var r in (_settings.RecentConnections ?? []).Take(5))
            RecentConnections.Add(r);
        _settings.RecentConnections ??= [];
        _settings.Shortcuts ??= [];
        _settings.Groups ??= [];

        _isCardView = !string.Equals(_settings.ViewMode, "List", StringComparison.OrdinalIgnoreCase);
        InitShortcuts();

        if (!string.IsNullOrWhiteSpace(_settings.LastGroup))
            _selectedGroup = _settings.LastGroup;

        SyncGroupsFromDevicesAndSettings();

        AddCommand = new RelayCommand(_ => AddDevice());
        EditCommand = new RelayCommand(_ => EditDevice(), _ => SelectedDevice != null);
        DeleteCommand = new RelayCommand(_ => DeleteDevice(), _ => SelectedDevice != null);
        ConnectCommand = new RelayCommand(async _ => await ConnectDeviceAsync(), _ => SelectedDevice != null);
        CheckAllCommand = new RelayCommand(async _ => await CheckAllAsync(), _ => !IsChecking);
        ToggleFavoriteCommand = new RelayCommand(_ => ToggleFavorite(), _ => SelectedDevice != null);
        SettingsCommand = new RelayCommand(_ => OpenSettings());
        ExportCommand = new RelayCommand(_ => Export());
        ImportCommand = new RelayCommand(_ => Import());
        BatchDeleteCommand = new RelayCommand(_ => BatchDelete(), _ => HasSelection);
        BatchCheckCommand = new RelayCommand(async _ => await BatchCheckAsync(), _ => HasSelection && !IsChecking);
        BatchGroupCommand = new RelayCommand(_ => BatchChangeGroup(), _ => HasSelection);
        AddGroupCommand = new RelayCommand(_ => AddGroup());
        RemoveGroupCommand = new RelayCommand(_ => RemoveGroup(), _ => CanDeleteGroup);
        RenameGroupCommand = new RelayCommand(_ => RenameGroup(), _ => CanDeleteGroup);
        EditGroupCommand = new RelayCommand(_ => EditGroup(), _ => CanDeleteGroup);
        ConnectRecentCommand = new RelayCommand(async p => await ConnectRecentAsync(p as RecentItem));
        SelectionChangedCommand = new RelayCommand(_ =>
        {
            OnPropertyChanged(nameof(SelectedCount));
            OnPropertyChanged(nameof(HasSelection));
            CommandManager.InvalidateRequerySuggested();
        });
        ClearRecentCommand = new RelayCommand(_ => ClearRecent(), _ => RecentConnections.Count > 0);
        RemoveRecentCommand = new RelayCommand(p => RemoveRecent(p as RecentItem));
        ShortcutClickCommand = new RelayCommand(p => OnShortcutClick(p as ShortcutItem));
        ShortcutEditCommand = new RelayCommand(p => EditShortcut(p as ShortcutItem));
        ShortcutClearCommand = new RelayCommand(p => ClearShortcut(p as ShortcutItem));
        ToggleViewCommand = new RelayCommand(_ => ToggleView());
        LogsCommand = new RelayCommand(_ => OpenLogs());
    }

    public void Refresh() => DevicesChanged?.Invoke();

    private void SyncGroupsFromDevicesAndSettings()
    {
        var map = _settings.Groups
            .Where(g => !string.IsNullOrWhiteSpace(g.Name) && g.Name is not ("全部设备" or "收藏"))
            .GroupBy(g => g.Name)
            .ToDictionary(x => x.Key, x => x.First().SortOrder);

        foreach (var name in Devices.Select(d => d.Group).Where(g => !string.IsNullOrWhiteSpace(g)))
        {
            if (name is "全部设备" or "收藏") continue;
            if (!map.ContainsKey(name))
                map[name] = 0;
        }

        if (!map.ContainsKey("默认"))
            map["默认"] = 0;

        var ordered = map
            .Select(kv => new GroupInfo
            {
                Name = kv.Key,
                SortOrder = kv.Value,
                DeviceCount = Devices.Count(d => d.Group == kv.Key)
            })
            .OrderByDescending(g => g.SortOrder)
            .ThenBy(g => g.Name)
            .ToList();

        ManagedGroups.Clear();
        foreach (var g in ordered)
            ManagedGroups.Add(g);

        CustomGroupNames.Clear();
        foreach (var g in ordered)
            CustomGroupNames.Add(g.Name);

        _settings.Groups = ordered.Select(g => new GroupInfo { Name = g.Name, SortOrder = g.SortOrder }).ToList();
        _store.SaveSettings(_settings);

        _selectedGroupInfo = ManagedGroups.FirstOrDefault(g => g.Name == SelectedGroup);
        OnPropertyChanged(nameof(SelectedGroupInfo));
        OnPropertyChanged(nameof(CanDeleteGroup));
    }

    private void PersistGroups()
    {
        _settings.Groups = ManagedGroups
            .Select(g => new GroupInfo { Name = g.Name, SortOrder = g.SortOrder })
            .ToList();
        _store.SaveSettings(_settings);
        SyncGroupsFromDevicesAndSettings();
    }

    private void AddGroup()
    {
        var maxSort = ManagedGroups.Count == 0 ? 0 : ManagedGroups.Max(g => g.SortOrder);
        var dlg = new Views.GroupDialog("新建分组", "", maxSort + 10)
        {
            Owner = Application.Current.MainWindow
        };
        if (dlg.ShowDialog() != true) return;
        if (ManagedGroups.Any(g => g.Name == dlg.GroupName))
        {
            MessageBox.Show("分组已存在。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        ManagedGroups.Add(new GroupInfo { Name = dlg.GroupName, SortOrder = dlg.SortOrder });
        PersistGroups();
        _log.Info("分组", "新建分组", dlg.GroupName);
        SelectedGroup = dlg.GroupName;
    }

    private void RemoveGroup()
    {
        if (!CanDeleteGroup || SelectedGroupInfo == null) return;
        var name = SelectedGroupInfo.Name;
        if (name == "默认")
        {
            MessageBox.Show("「默认」分组不能删除。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var count = Devices.Count(d => d.Group == name);
        var msg = count > 0
            ? "分组「" + name + "」下还有 " + count + " 台设备。\n删除后这些设备将移到「默认」分组。\n\n确定删除该分组吗？"
            : "确定删除空分组「" + name + "」吗？";

        var r = MessageBox.Show(msg, "删除分组", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (r != MessageBoxResult.Yes) return;

        if (count > 0)
        {
            foreach (var d in Devices.Where(d => d.Group == name))
                d.Group = "默认";
            Save();
        }

        var item = ManagedGroups.FirstOrDefault(g => g.Name == name);
        if (item != null) ManagedGroups.Remove(item);
        PersistGroups();
        _log.Info("分组", "删除分组", name);
        SelectedGroup = "全部设备";
        Refresh();
    }

    private void RenameGroup()
    {
        if (!CanDeleteGroup || SelectedGroupInfo == null) return;
        EditGroup();
    }

    private void EditGroup()
    {
        if (!CanDeleteGroup || SelectedGroupInfo == null) return;
        var old = SelectedGroupInfo;
        var dlg = new Views.GroupDialog("编辑分组", old.Name, old.SortOrder)
        {
            Owner = Application.Current.MainWindow
        };
        if (dlg.ShowDialog() != true) return;

        var newName = dlg.GroupName;
        if (newName != old.Name && ManagedGroups.Any(g => g.Name == newName))
        {
            MessageBox.Show("分组名称已存在。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (newName != old.Name)
        {
            foreach (var d in Devices.Where(d => d.Group == old.Name))
                d.Group = newName;
            Save();
        }

        old.Name = newName;
        old.SortOrder = dlg.SortOrder;
        PersistGroups();
        _log.Info("分组", "编辑分组", $"{old.Name} · 排序 {old.SortOrder}");
        SelectedGroup = newName;
        Refresh();
    }

    private void AddDevice()
    {
        var device = new VncDevice();
        var win = new Views.DeviceWindow(device, false, "", CustomGroupNames.ToList())
        {
            Owner = Application.Current.MainWindow
        };
        if (win.ShowDialog() == true)
        {
            device.Password = PasswordService.Protect(win.PlainPassword);
            Devices.Add(device);
            SyncGroupsFromDevicesAndSettings();
            Save();
            _log.Info("设备", "新增设备", $"{device.Name} · {device.TypeLabel} · {device.Address}");
            SelectedDevice = device;
            OnCounts();
            _ = CheckOneAsync(device);
        }
    }

    private void EditDevice()
    {
        if (SelectedDevice == null) return;
        var copy = Clone(SelectedDevice);
        var win = new Views.DeviceWindow(
            copy, true,
            PasswordService.Unprotect(SelectedDevice.Password),
            CustomGroupNames.ToList())
        {
            Owner = Application.Current.MainWindow
        };

        if (win.ShowDialog() == true)
        {
            SelectedDevice.Name = copy.Name;
            SelectedDevice.Host = copy.Host;
            SelectedDevice.Port = copy.Port;
            SelectedDevice.Group = copy.Group;
            SelectedDevice.Comment = copy.Comment;
            SelectedDevice.Favorite = copy.Favorite;
            SelectedDevice.ConnectionType = copy.ConnectionType;
            SelectedDevice.Username = copy.Username;
            SelectedDevice.Password = string.IsNullOrWhiteSpace(win.PlainPassword)
                ? "" : PasswordService.Protect(win.PlainPassword);
            SyncGroupsFromDevicesAndSettings();
            Save();
            _log.Info("设备", "修改设备", $"{SelectedDevice.Name} · {SelectedDevice.TypeLabel} · {SelectedDevice.Address}");
            _ = CheckOneAsync(SelectedDevice);
        }
    }

    private void DeleteDevice()
    {
        if (SelectedDevice == null) return;
        if (MessageBox.Show("确定删除「" + SelectedDevice.Name + "」吗？", "删除设备",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;
        _log.Info("设备", "删除设备", $"{SelectedDevice.Name} · {SelectedDevice.TypeLabel} · {SelectedDevice.Address}");
        Devices.Remove(SelectedDevice);
        SelectedDevice = null;
        Save();
        OnCounts();
    }

    private async Task ConnectDeviceAsync()
    {
        if (SelectedDevice == null) return;
        await ConnectInternalAsync(SelectedDevice);
    }

    private async Task ConnectRecentAsync(RecentItem? item)
    {
        if (item == null) return;
        var device = Devices.FirstOrDefault(d => d.Id == item.DeviceId)
                     ?? Devices.FirstOrDefault(d => d.Host == item.Host && d.Port == item.Port);
        if (device == null)
        {
            MessageBox.Show("该设备已不存在，已从最近连接中移除。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Information);
            RecentConnections.Remove(item);
            _settings.RecentConnections = RecentConnections.ToList();
            _store.SaveSettings(_settings);
            return;
        }
        SelectedDevice = device;
        await ConnectInternalAsync(device);
    }

    private async Task ConnectInternalAsync(VncDevice device)
    {
        var path = _vnc.FindViewer(_settings.TightVncViewerPath);

        if (_settings.CheckBeforeConnect)
        {
            var result = await _vnc.CheckAsync(device, 1500);
            device.Status = result.Online ? "在线" : "离线";
            device.Latency = result.Latency;
            if (result.Online)
            {
                device.LastOnline = DateTime.Now;
                Save();
            }
            OnCounts();

            if (!result.Online)
            {
                var go = MessageBox.Show(
                    "「" + device.Name + "」当前检测为离线（" + device.Address + "）。\n仍要尝试连接吗？",
                    "连接确认", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (go != MessageBoxResult.Yes) return;
            }
        }

        var plain = PasswordService.Unprotect(device.Password);
        if (!_vnc.Connect(device, path, plain, out var message))
        {
            MessageBox.Show(message, "远程连接管理器", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        device.LastConnected = DateTime.Now;
        AddRecent(device);
        Save();
        _log.Info("连接", "打开连接", $"{device.Name} · {device.TypeLabel} · {device.Address}");
        OnCounts();
    }

    private void AddRecent(VncDevice device)
    {
        var existing = RecentConnections.FirstOrDefault(r => r.DeviceId == device.Id);
        if (existing != null) RecentConnections.Remove(existing);
        RecentConnections.Insert(0, new RecentItem
        {
            DeviceId = device.Id,
            Name = device.Name,
            Host = device.Host,
            Port = device.Port,
            Time = DateTime.Now
        });
        while (RecentConnections.Count > 5)
            RecentConnections.RemoveAt(RecentConnections.Count - 1);
        _settings.RecentConnections = RecentConnections.ToList();
        _store.SaveSettings(_settings);
    }

    public async Task CheckAllAsync()
    {
        if (IsChecking) return;
        IsChecking = true;
        try
        {
            await Task.WhenAll(Devices.Select(async device =>
            {
                var result = await _vnc.CheckAsync(device);
                device.Status = result.Online ? "在线" : "离线";
                device.Latency = result.Latency;
                if (result.Online) device.LastOnline = DateTime.Now;
            }));
            Save();
            OnCounts();
        }
        finally { IsChecking = false; }
    }

    private async Task CheckOneAsync(VncDevice device)
    {
        var result = await _vnc.CheckAsync(device);
        device.Status = result.Online ? "在线" : "离线";
        device.Latency = result.Latency;
        if (result.Online) device.LastOnline = DateTime.Now;
        Save();
        OnCounts();
    }

    private async Task BatchCheckAsync()
    {
        var list = Devices.Where(d => d.IsSelected).ToList();
        if (list.Count == 0) return;
        IsChecking = true;
        try
        {
            await Task.WhenAll(list.Select(async d =>
            {
                var r = await _vnc.CheckAsync(d);
                d.Status = r.Online ? "在线" : "离线";
                d.Latency = r.Latency;
                if (r.Online) d.LastOnline = DateTime.Now;
            }));
            Save();
            OnCounts();
        }
        finally { IsChecking = false; }
    }

    private void BatchDelete()
    {
        var list = Devices.Where(d => d.IsSelected).ToList();
        if (list.Count == 0) return;
        if (MessageBox.Show("确定删除选中的 " + list.Count + " 台设备吗？", "批量删除",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;
        foreach (var d in list) Devices.Remove(d);
        _log.Info("设备", "批量删除设备", $"{list.Count} 台");
        Save();
        OnCounts();
    }

    private void BatchChangeGroup()
    {
        var list = Devices.Where(d => d.IsSelected).ToList();
        if (list.Count == 0) return;
        var dlg = new Views.BatchGroupDialog(CustomGroupNames.ToList(), list.Count)
        {
            Owner = Application.Current.MainWindow
        };
        if (dlg.ShowDialog() != true) return;
        foreach (var d in list) d.Group = dlg.GroupName;
        if (!ManagedGroups.Any(g => g.Name == dlg.GroupName))
            ManagedGroups.Add(new GroupInfo { Name = dlg.GroupName, SortOrder = 0 });
        PersistGroups();
        Save();
        Refresh();
    }

    private void ToggleFavorite()
    {
        if (SelectedDevice == null) return;
        SelectedDevice.Favorite = !SelectedDevice.Favorite;
        Save();
        Refresh();
    }

    private void OpenSettings()
    {
        var win = new Views.SettingsWindow(_settings, _vnc)
        {
            Owner = Application.Current.MainWindow
        };
        if (win.ShowDialog() == true)
        {
            var old = _settings;
            var updated = win.Settings ?? new AppSettings();
            LogSettingsChanges(old, updated);
            _settings = updated;
            _isCardView = !string.Equals(_settings.ViewMode, "List", StringComparison.OrdinalIgnoreCase);
            _store.SaveSettings(_settings);
            OnPropertyChanged(nameof(IsCardView));
            OnPropertyChanged(nameof(IsListView));
        }
    }

    private void Export()
    {
        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "JSON 文件|*.json",
            FileName = "remote-devices-" + DateTime.Now.ToString("yyyyMMdd") + ".json"
        };
        if (dlg.ShowDialog() != true) return;
        var includePwd = MessageBox.Show(
            "是否在导出文件中包含已加密的密码？\n（仅本机 Windows 用户能解密，换电脑无效）",
            "导出选项", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
        try
        {
            _store.ExportDevices(dlg.FileName, Devices, includePwd);
            _log.Info("数据", "导出设备", $"{Devices.Count} 台 → {dlg.FileName}");
            MessageBox.Show("已导出 " + Devices.Count + " 台设备。", "导出成功",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show("导出失败：" + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Import()
    {
        var dlg = new Microsoft.Win32.OpenFileDialog { Filter = "JSON 文件|*.json" };
        if (dlg.ShowDialog() != true) return;
        try
        {
            var imported = _store.ImportDevices(dlg.FileName);
            _log.Info("数据", "读取导入文件", dlg.FileName);
            if (imported.Count == 0)
            {
                MessageBox.Show("文件中没有设备数据。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var mode = MessageBox.Show(
                "导入 " + imported.Count + " 台设备。\n\n是 = 合并（同 ID 覆盖）\n否 = 全部作为新设备添加",
                "导入方式", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            if (mode == MessageBoxResult.Cancel) return;
            if (mode == MessageBoxResult.Yes)
            {
                foreach (var item in imported)
                {
                    var exist = Devices.FirstOrDefault(d => d.Id == item.Id);
                    if (exist != null)
                    {
                        exist.Name = item.Name;
                        exist.Host = item.Host;
                        exist.Port = item.Port;
                        exist.Group = item.Group;
                        exist.Comment = item.Comment;
                        exist.Favorite = item.Favorite;
                        exist.ConnectionType = item.ConnectionType;
                        exist.Username = item.Username;
                        if (!string.IsNullOrEmpty(item.Password))
                            exist.Password = item.Password;
                    }
                    else
                    {
                        if (Devices.Any(d => d.Host == item.Host && d.Port == item.Port))
                            item.Id = Guid.NewGuid();
                        Devices.Add(item);
                    }
                }
            }
            else
            {
                foreach (var item in imported)
                {
                    item.Id = Guid.NewGuid();
                    Devices.Add(item);
                }
            }
            SyncGroupsFromDevicesAndSettings();
            Save();
            _log.Info("数据", "导入设备", $"{imported.Count} 台");
            OnCounts();
            MessageBox.Show("导入完成。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show("导入失败：" + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }


    private void ClearRecent()
    {
        if (RecentConnections.Count == 0) return;
        if (MessageBox.Show("确定清空全部最近连接记录吗？", "清空记录",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;
        RecentConnections.Clear();
        _settings.RecentConnections = [];
        _store.SaveSettings(_settings);
        _log.Info("最近连接", "清空最近连接", "全部记录");
    }

    private void RemoveRecent(RecentItem? item)
    {
        if (item == null) return;
        RecentConnections.Remove(item);
        _settings.RecentConnections = RecentConnections.ToList();
        _store.SaveSettings(_settings);
        _log.Info("最近连接", "删除最近连接", item.Name + " · " + item.Host + ":" + item.Port);
    }

    private void InitShortcuts()
    {
        Shortcuts.Clear();
        var saved = _settings.Shortcuts ?? [];

        if (!_settings.ShortcutsInitialized && saved.Count == 0)
        {
            saved =
            [
                new ShortcutItem { Kind = ShortcutKind.Url, Target = "https://www.52pojie.cn/", DisplayName = "吾爱破解" },
                new ShortcutItem { Kind = ShortcutKind.Url, Target = "https://dbxio.com/cn", DisplayName = "DBXIO" }
            ];
            _settings.ShortcutsInitialized = true;
            _settings.Shortcuts = saved;
            _store.SaveSettings(_settings);
            _log.Info("快捷方式", "初始化默认快捷方式", "52pojie.cn；dbxio.com/cn");
        }
        else if (!_settings.ShortcutsInitialized)
        {
            _settings.ShortcutsInitialized = true;
            _store.SaveSettings(_settings);
        }

        for (var i = 0; i < 8; i++)
        {
            if (i < saved.Count && saved[i].Kind != ShortcutKind.Empty)
            {
                var shortcut = saved[i];
                shortcut.Icon = Services.ShortcutHelper.LoadIcon(shortcut);
                Shortcuts.Add(shortcut);
            }
            else
            {
                Shortcuts.Add(new ShortcutItem { Kind = ShortcutKind.Empty });
            }
        }
        CompactShortcuts(false);
    }

    private void PersistShortcuts()
    {
        _settings.ShortcutsInitialized = true;
        _settings.Shortcuts = Shortcuts
            .Where(s => !s.IsEmpty)
            .Select(s => new ShortcutItem
            {
                Id = s.Id,
                Kind = s.Kind,
                Target = s.Target,
                DisplayName = s.DisplayName
            }).ToList();
        _store.SaveSettings(_settings);
    }

    private void OnShortcutClick(ShortcutItem? item)
    {
        if (item == null) return;
        if (item.IsEmpty)
        {
            EditShortcut(item);
            return;
        }
        try
        {
            Services.ShortcutHelper.Launch(item);
            _log.Info("快捷方式", "打开快捷方式", $"{item.DisplayName} · {item.Target}");
        }
        catch (Exception ex)
        {
            _log.Info("快捷方式", "打开失败", $"{item.DisplayName} · {ex.Message}");
            MessageBox.Show("打开失败：" + ex.Message, "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void EditShortcut(ShortcutItem? item)
    {
        if (item == null) return;
        var isNew = item.IsEmpty;
        var dlg = new Views.ShortcutDialog(item, isNew) { Owner = Application.Current.MainWindow };
        if (dlg.ShowDialog() != true) return;
        item.Kind = dlg.Kind;
        item.Target = dlg.Target;
        item.DisplayName = dlg.DisplayName;
        item.Icon = Services.ShortcutHelper.LoadIcon(item);
        CompactShortcuts(true);
        PersistShortcuts();
        _log.Info("快捷方式", isNew ? "新增快捷方式" : "编辑快捷方式", $"{item.DisplayName} · {item.Target}");
    }

    private void ClearShortcut(ShortcutItem? item)
    {
        if (item == null || item.IsEmpty) return;
        if (MessageBox.Show("确定清除该快捷方式吗？", "清除",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;
        var name = item.DisplayName;
        item.Kind = ShortcutKind.Empty;
        item.Target = "";
        item.DisplayName = "";
        item.Icon = null;
        CompactShortcuts(true);
        PersistShortcuts();
        _log.Info("快捷方式", "删除快捷方式", name);
    }

    private void CompactShortcuts(bool preserveEmptySlots)
    {
        var filled = Shortcuts.Where(s => !s.IsEmpty).ToList();
        if (filled.Count > 8) filled = filled.Take(8).ToList();
        Shortcuts.Clear();
        foreach (var s in filled)
        {
            s.Icon = Services.ShortcutHelper.LoadIcon(s);
            Shortcuts.Add(s);
        }
        while (Shortcuts.Count < 8) Shortcuts.Add(new ShortcutItem { Kind = ShortcutKind.Empty });
        if (!preserveEmptySlots) return;
    }

    private void ToggleView()
    {
        IsCardView = !IsCardView;
        _log.Info("配置", "切换连接视图", IsCardView ? "卡片模式" : "列表模式");
    }

    private void OpenLogs()
    {
        var win = new Views.LogWindow(_log) { Owner = Application.Current.MainWindow };
        win.ShowDialog();
    }

    private void LogSettingsChanges(AppSettings oldSettings, AppSettings newSettings)
    {
        if (!string.Equals(oldSettings.TightVncViewerPath, newSettings.TightVncViewerPath, StringComparison.Ordinal))
            _log.Info("配置", "修改 Viewer 路径", $"{oldSettings.TightVncViewerPath} → {newSettings.TightVncViewerPath}");
        if (oldSettings.CheckBeforeConnect != newSettings.CheckBeforeConnect)
            _log.Info("配置", "修改连接前检测", newSettings.CheckBeforeConnect ? "开启" : "关闭");
        if (oldSettings.MinimizeToTray != newSettings.MinimizeToTray)
            _log.Info("配置", "修改最小化到托盘", newSettings.MinimizeToTray ? "开启" : "关闭");
        if (!string.Equals(oldSettings.ViewMode, newSettings.ViewMode, StringComparison.OrdinalIgnoreCase))
            _log.Info("配置", "修改连接视图", string.Equals(newSettings.ViewMode, "List", StringComparison.OrdinalIgnoreCase) ? "列表模式" : "卡片模式");
    }

    private void Save() => _store.SaveDevices(Devices);

    private void OnCounts()
    {
        OnPropertyChanged(nameof(DeviceCount));
        OnPropertyChanged(nameof(OnlineCount));
        OnPropertyChanged(nameof(OfflineCount));
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(HasSelection));
        foreach (var g in ManagedGroups)
            g.DeviceCount = Devices.Count(d => d.Group == g.Name);
        DevicesChanged?.Invoke();
    }

    private static VncDevice Clone(VncDevice d) => new()
    {
        Id = d.Id, Name = d.Name, Host = d.Host, Port = d.Port,
        Username = d.Username, Password = d.Password, Group = d.Group,
        Comment = d.Comment, Favorite = d.Favorite,
        LastOnline = d.LastOnline, LastConnected = d.LastConnected,
        ConnectionType = d.ConnectionType
    };

    public IEnumerable<VncDevice> FilteredDevices =>
        Devices.Where(d =>
            (SelectedGroup == "全部设备" ||
             (SelectedGroup == "收藏" && d.Favorite) ||
             d.Group == SelectedGroup) &&
            (string.IsNullOrWhiteSpace(SearchText) ||
             d.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
             d.Host.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
             d.Comment.Contains(SearchText, StringComparison.OrdinalIgnoreCase)));

    protected bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(name);
        return true;
    }

    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private class RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null) : ICommand
    {
        private readonly Action<object?> _execute = execute;
        private readonly Predicate<object?>? _canExecute = canExecute;
        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
        public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
        public void Execute(object? parameter) => _execute(parameter);
    }
}
