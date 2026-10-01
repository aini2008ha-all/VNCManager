using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Windows.Media;

namespace VNCManager.Models;

public class AppSettings
{
    public string TightVncViewerPath { get; set; } = "";
    public string LastGroup { get; set; } = "全部设备";
    public double WindowWidth { get; set; } = 1280;
    public double WindowHeight { get; set; } = 780;
    public bool MinimizeToTray { get; set; } = true;
    public bool CheckBeforeConnect { get; set; } = true;
    public List<RecentItem> RecentConnections { get; set; } = [];
    public List<GroupInfo> Groups { get; set; } = [new GroupInfo { Name = "默认", SortOrder = 0 }];
    public List<ShortcutItem> Shortcuts { get; set; } = [];
    public bool ShortcutsInitialized { get; set; }
    public string ViewMode { get; set; } = "Card";
}

public class RecentItem
{
    public Guid DeviceId { get; set; }
    public string Name { get; set; } = "";
    public string Host { get; set; } = "";
    public int Port { get; set; } = 5900;
    public DateTime Time { get; set; } = DateTime.Now;
}

public class GroupInfo : INotifyPropertyChanged
{
    private string _name = "默认";
    private int _sortOrder;
    private int _deviceCount;

    public string Name
    {
        get => _name;
        set { if (_name != value) { _name = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayName)); } }
    }

    public int SortOrder
    {
        get => _sortOrder;
        set { if (_sortOrder != value) { _sortOrder = value; OnPropertyChanged(); } }
    }

    public int DeviceCount
    {
        get => _deviceCount;
        set { if (_deviceCount != value) { _deviceCount = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayName)); } }
    }

    public string DisplayName => Name + "  (" + DeviceCount + ")";

    public event PropertyChangedEventHandler? PropertyChanged;
    void OnPropertyChanged([CallerMemberName] string? n = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
}

public enum ShortcutKind
{
    Empty = 0,
    Exe = 1,
    Folder = 2,
    Url = 3
}

public class ShortcutItem : INotifyPropertyChanged
{
    private string _displayName = "";
    private string _target = "";
    private ShortcutKind _kind = ShortcutKind.Empty;
    private ImageSource? _icon;

    public Guid Id { get; set; } = Guid.NewGuid();

    public ShortcutKind Kind
    {
        get => _kind;
        set { if (_kind != value) { _kind = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsEmpty)); OnPropertyChanged(nameof(KindLabel)); } }
    }

    public string Target
    {
        get => _target;
        set { if (_target != value) { _target = value; OnPropertyChanged(); } }
    }

    public string DisplayName
    {
        get => _displayName;
        set { if (_displayName != value) { _displayName = value; OnPropertyChanged(); } }
    }

    [JsonIgnore]
    public ImageSource? Icon
    {
        get => _icon;
        set { if (_icon != value) { _icon = value; OnPropertyChanged(); } }
    }

    [JsonIgnore] public bool IsEmpty => Kind == ShortcutKind.Empty;
    [JsonIgnore] public string KindLabel => Kind switch
    {
        ShortcutKind.Exe => "程序",
        ShortcutKind.Folder => "文件夹",
        ShortcutKind.Url => "网页",
        _ => "空"
    };

    public event PropertyChangedEventHandler? PropertyChanged;
    void OnPropertyChanged([CallerMemberName] string? n = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
}
