using System.Windows;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;

namespace VNCManager.Views;

public partial class BatchGroupDialog : Window
{
    public string GroupName { get; private set; } = "";

    public BatchGroupDialog(IEnumerable<string> existingGroups, int deviceCount)
    {
        InitializeComponent();
        HintText.Text = "将对选中的 " + deviceCount + " 台设备修改分组。";
        GroupBox.Items.Clear();
        foreach (var g in existingGroups.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct())
            GroupBox.Items.Add(g);
        if (GroupBox.Items.Count > 0)
            GroupBox.SelectedIndex = 0;
        else
            GroupBox.Text = "默认";
        Loaded += (_, _) => GroupBox.Focus();
    }

    private void OkClick(object sender, RoutedEventArgs e)
    {
        var name = (GroupBox.Text ?? "").Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show("请选择或输入分组名称。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (name is "全部设备" or "收藏")
        {
            MessageBox.Show("不能使用保留名称。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        GroupName = name;
        DialogResult = true;
    }

    private void CancelClick(object sender, RoutedEventArgs e) => DialogResult = false;
}
