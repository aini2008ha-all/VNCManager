using System.Windows;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;
using VNCManager.Models;

namespace VNCManager.Views;

public partial class DeviceWindow : Window
{
    private readonly VncDevice _device = null!;
    private bool _loading = true;

    public bool IsEditing { get; set; }
    public string ExistingPassword { get; set; } = "";
    public string PlainPassword { get; private set; } = "";

    public DeviceWindow(
        VncDevice device,
        bool isEditing = false,
        string existingPassword = "",
        IEnumerable<string>? existingGroups = null)
    {
        _loading = true;
        InitializeComponent();

        _device = device;
        IsEditing = isEditing;
        ExistingPassword = existingPassword ?? "";

        GroupBox.Items.Clear();
        var groups = existingGroups?.Where(g => !string.IsNullOrWhiteSpace(g)).Distinct().ToList()
                     ?? ["默认"];
        foreach (var g in groups)
            GroupBox.Items.Add(g);

        LoadDevice();
        _loading = false;
    }

    private void LoadDevice()
    {
        _loading = true;
        TitleText.Text = IsEditing ? "编辑远程设备" : "添加远程设备";
        NameBox.Text = _device.Name;
        HostBox.Text = _device.Host;
        PortBox.Text = (_device.Port > 0 ? _device.Port : 5900).ToString();
        UserBox.Text = _device.Username;
        var group = string.IsNullOrWhiteSpace(_device.Group) ? "默认" : _device.Group;
        GroupBox.Text = group;
        if (!GroupBox.Items.Contains(group))
            GroupBox.Items.Add(group);
        CommentBox.Text = _device.Comment;

        if (_device.ConnectionType == ConnectionType.Rdp)
            TypeRdp.IsChecked = true;
        else
            TypeVnc.IsChecked = true;

        ApplyTypeUi(updatePort: false);

        if (IsEditing && !string.IsNullOrEmpty(ExistingPassword))
        {
            PasswordBox.Password = ExistingPassword;
            PasswordHint.Text = "已加载保存的密码，可直接修改；清空则表示删除密码。";
        }
        else if (IsEditing)
            PasswordHint.Text = "当前未设置密码。";
        else
            PasswordHint.Text = "密码使用 Windows DPAPI 加密保存在本机。";
    }

    private void TypeChanged(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        if (UserLabel == null || PasswordHint == null || PortBox == null) return;
        ApplyTypeUi(updatePort: true);
    }

    private void ApplyTypeUi(bool updatePort)
    {
        // InitializeComponent 过程中控件可能尚未全部创建
        if (UserLabel == null || PasswordHint == null || PortBox == null || TypeRdp == null)
            return;

        var isRdp = TypeRdp.IsChecked == true;
        UserLabel.Text = isRdp ? "用户名（RDP 建议填写）" : "用户名（VNC 一般不需要）";
        if (updatePort)
            PortBox.Text = isRdp ? "3389" : "5900";

        if (isRdp)
        {
            PasswordHint.Text = "RDP 会通过 Windows 凭据管理器尝试自动登录；也可在弹出窗口中手动输入。";
        }
        else if (IsEditing && !string.IsNullOrEmpty(ExistingPassword))
        {
            PasswordHint.Text = "已加载保存的密码，可直接修改；清空则表示删除密码。";
        }
        else
        {
            PasswordHint.Text = "密码使用 Windows DPAPI 加密保存在本机，连接 VNC 时自动填入。";
        }
    }

    private void SaveClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameBox.Text))
        {
            MessageBox.Show("请输入设备名称。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (string.IsNullOrWhiteSpace(HostBox.Text))
        {
            MessageBox.Show("请输入 IP 地址或主机名。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (!int.TryParse(PortBox.Text, out var port) || port is < 1 or > 65535)
        {
            MessageBox.Show("端口必须是 1-65535 的数字。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        _device.ConnectionType = TypeRdp.IsChecked == true ? ConnectionType.Rdp : ConnectionType.Vnc;
        _device.Name = NameBox.Text.Trim();
        _device.Host = HostBox.Text.Trim();
        _device.Port = port;
        _device.Username = UserBox.Text.Trim();
        _device.Group = string.IsNullOrWhiteSpace(GroupBox.Text) ? "默认" : GroupBox.Text.Trim();
        _device.Comment = CommentBox.Text.Trim();
        PlainPassword = PasswordBox.Password;
        DialogResult = true;
    }

    private void CancelClick(object sender, RoutedEventArgs e) => DialogResult = false;
}
