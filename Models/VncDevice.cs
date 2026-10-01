using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace VNCManager.Models;

public enum ConnectionType
{
    Vnc = 0,
    Rdp = 1
}

public class VncDevice : INotifyPropertyChanged
{
    private string _name = "";
    private string _host = "";
    private int _port = 5900;
    private string _username = "";
    private string _password = "";
    private string _group = "默认";
    private string _comment = "";
    private bool _favorite;
    private string _status = "未检测";
    private int? _latency;
    private DateTime? _lastOnline;
    private DateTime? _lastConnected;
    private bool _isSelected;
    private ConnectionType _connectionType = ConnectionType.Vnc;

    public Guid Id { get; set; } = Guid.NewGuid();

    public ConnectionType ConnectionType
    {
        get => _connectionType;
        set
        {
            if (Set(ref _connectionType, value))
            {
                OnPropertyChanged(nameof(TypeLabel));
                OnPropertyChanged(nameof(TypeIcon));
                OnPropertyChanged(nameof(IsRdp));
                OnPropertyChanged(nameof(IsVnc));
            }
        }
    }

    public string Name { get => _name; set => Set(ref _name, value); }
    public string Host { get => _host; set => Set(ref _host, value); }
    public int Port { get => _port; set => Set(ref _port, value); }
    public string Username { get => _username; set => Set(ref _username, value); }
    public string Password { get => _password; set => Set(ref _password, value); }
    public string Group { get => _group; set => Set(ref _group, value); }
    public string Comment { get => _comment; set => Set(ref _comment, value); }
    public bool Favorite { get => _favorite; set => Set(ref _favorite, value); }

    public DateTime? LastOnline
    {
        get => _lastOnline;
        set { if (Set(ref _lastOnline, value)) OnPropertyChanged(nameof(LastOnlineText)); }
    }

    public DateTime? LastConnected
    {
        get => _lastConnected;
        set { if (Set(ref _lastConnected, value)) OnPropertyChanged(nameof(LastConnectedText)); }
    }

    [JsonIgnore]
    public string Status
    {
        get => _status;
        set { if (Set(ref _status, value)) OnPropertyChanged(nameof(StatusText)); }
    }

    [JsonIgnore]
    public int? Latency
    {
        get => _latency;
        set { if (Set(ref _latency, value)) OnPropertyChanged(nameof(StatusText)); }
    }

    [JsonIgnore]
    public bool IsSelected
    {
        get => _isSelected;
        set => Set(ref _isSelected, value);
    }

    [JsonIgnore] public string Address => $"{Host}:{Port}";
    [JsonIgnore] public bool IsRdp => ConnectionType == ConnectionType.Rdp;
    [JsonIgnore] public bool IsVnc => ConnectionType == ConnectionType.Vnc;
    [JsonIgnore] public string TypeLabel => ConnectionType == ConnectionType.Rdp ? "RDP" : "VNC";
    [JsonIgnore] public string TypeIcon => ConnectionType == ConnectionType.Rdp ? "🖥" : "📡";

    [JsonIgnore]
    public string StatusText =>
        Status == "在线" && Latency.HasValue ? $"在线  {Latency.Value} ms" : Status;

    [JsonIgnore]
    public string LastOnlineText =>
        LastOnline.HasValue ? LastOnline.Value.ToString("MM-dd HH:mm") : "从未在线";

    [JsonIgnore]
    public string LastConnectedText =>
        LastConnected.HasValue ? LastConnected.Value.ToString("MM-dd HH:mm") : "从未连接";

    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(name);
        if (name is nameof(Host) or nameof(Port)) OnPropertyChanged(nameof(Address));
        return true;
    }

    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
