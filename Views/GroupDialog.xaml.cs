using System.Windows;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;

namespace VNCManager.Views;

public partial class GroupDialog : Window
{
    public string GroupName { get; private set; } = "";
    public int SortOrder { get; private set; }

    public GroupDialog(string title, string name = "", int sortOrder = 0)
    {
        InitializeComponent();
        Title = title;
        TitleText.Text = title;
        NameBox.Text = name;
        SortBox.Text = sortOrder.ToString();
        Loaded += (_, _) => { NameBox.Focus(); NameBox.SelectAll(); };
    }

    private void OkClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameBox.Text))
        {
            MessageBox.Show("请输入分组名称。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var name = NameBox.Text.Trim();
        if (name is "全部设备" or "收藏")
        {
            MessageBox.Show("不能使用保留名称「全部设备」或「收藏」。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (!int.TryParse(SortBox.Text.Trim(), out var sort))
        {
            MessageBox.Show("排序值必须是整数。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        GroupName = name;
        SortOrder = sort;
        DialogResult = true;
    }

    private void CancelClick(object sender, RoutedEventArgs e) => DialogResult = false;
}
