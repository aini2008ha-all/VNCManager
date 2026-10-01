using System.IO;
using System.Windows;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;
using VNCManager.Models;

namespace VNCManager.Views;

public partial class ShortcutDialog : Window
{
    public ShortcutKind Kind { get; private set; } = ShortcutKind.Exe;
    public string Target { get; private set; } = "";
    public string DisplayName { get; private set; } = "";

    public ShortcutDialog()
    {
        InitializeComponent();
    }

    public ShortcutDialog(ShortcutItem existing, bool isNew) : this()
    {
        TitleText.Text = isNew ? "添加快捷方式" : "编辑快捷方式";
        if (existing == null || existing.IsEmpty) return;

        TargetBox.Text = existing.Target;
        NameBox.Text = existing.DisplayName;
        TypeExe.IsChecked = existing.Kind == ShortcutKind.Exe;
        TypeFolder.IsChecked = existing.Kind == ShortcutKind.Folder;
        TypeUrl.IsChecked = existing.Kind == ShortcutKind.Url;
    }

    private void BrowseClick(object sender, RoutedEventArgs e)
    {
        if (TypeUrl.IsChecked == true)
        {
            MessageBox.Show("网页类型请直接输入网址，例如 https://www.example.com", "提示",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (TypeFolder.IsChecked == true)
        {
            using var dlg = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "选择文件夹"
            };
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                TargetBox.Text = dlg.SelectedPath;
            return;
        }

        var ofd = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "可执行文件|*.exe|所有文件|*.*",
            Title = "选择程序"
        };
        if (ofd.ShowDialog() == true)
            TargetBox.Text = ofd.FileName;
    }

    private void OkClick(object sender, RoutedEventArgs e)
    {
        var target = (TargetBox.Text ?? "").Trim();
        if (string.IsNullOrWhiteSpace(target))
        {
            MessageBox.Show("请填写路径或网址。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (TypeUrl.IsChecked == true)
        {
            Kind = ShortcutKind.Url;
            if (!target.Contains("://"))
                target = "https://" + target;
        }
        else if (TypeFolder.IsChecked == true)
        {
            Kind = ShortcutKind.Folder;
            if (!Directory.Exists(target))
            {
                MessageBox.Show("文件夹不存在。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
        }
        else
        {
            Kind = ShortcutKind.Exe;
            if (!File.Exists(target))
            {
                MessageBox.Show("文件不存在。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
        }

        Target = target;
        var custom = (NameBox.Text ?? "").Trim();
        if (!string.IsNullOrEmpty(custom))
            DisplayName = custom;
        else if (Kind == ShortcutKind.Url)
            DisplayName = target.Replace("https://", "").Replace("http://", "").TrimEnd('/');
        else
            DisplayName = Path.GetFileNameWithoutExtension(target);

        if (string.IsNullOrWhiteSpace(DisplayName))
            DisplayName = "快捷方式";

        DialogResult = true;
    }

    private void CancelClick(object sender, RoutedEventArgs e) => DialogResult = false;
}
