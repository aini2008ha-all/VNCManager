using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VNCManager.Models;

namespace VNCManager.Services;

public static class ShortcutHelper
{
    public static ImageSource? LoadIcon(ShortcutItem item)
    {
        try
        {
            return item.Kind switch
            {
                ShortcutKind.Exe when File.Exists(item.Target) => ExtractExeIcon(item.Target),
                ShortcutKind.Folder => FolderIcon(),
                ShortcutKind.Url => BrowserIcon(),
                _ => null
            };
        }
        catch
        {
            return null;
        }
    }

    public static void Launch(ShortcutItem item)
    {
        switch (item.Kind)
        {
            case ShortcutKind.Exe:
                Process.Start(new ProcessStartInfo
                {
                    FileName = item.Target,
                    UseShellExecute = true,
                    WorkingDirectory = Path.GetDirectoryName(item.Target) ?? ""
                });
                break;
            case ShortcutKind.Folder:
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = "\"" + item.Target + "\"",
                    UseShellExecute = true
                });
                break;
            case ShortcutKind.Url:
                Process.Start(new ProcessStartInfo
                {
                    FileName = item.Target,
                    UseShellExecute = true
                });
                break;
        }
    }

    private static ImageSource? ExtractExeIcon(string path)
    {
        using var icon = Icon.ExtractAssociatedIcon(path);
        if (icon == null) return null;
        return Imaging.CreateBitmapSourceFromHIcon(
            icon.Handle,
            Int32Rect.Empty,
            BitmapSizeOptions.FromWidthAndHeight(32, 32));
    }

    private static ImageSource FolderIcon()
    {
        // 使用系统 shell32 文件夹图标
        try
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "shell32.dll");
            var icon = ExtractIconFromFile(path, 3); // 3 常见为打开的文件夹
            if (icon != null) return icon;
        }
        catch { /* ignore */ }
        return MakeColorIcon(System.Windows.Media.Color.FromRgb(255, 193, 7));
    }

    private static ImageSource BrowserIcon()
    {
        try
        {
            var candidates = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Google", "Chrome", "Application", "chrome.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft", "Edge", "Application", "msedge.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Internet Explorer", "iexplore.exe")
            };
            foreach (var c in candidates)
            {
                if (File.Exists(c))
                {
                    var img = ExtractExeIcon(c);
                    if (img != null) return img;
                }
            }
        }
        catch { /* ignore */ }
        return MakeColorIcon(System.Windows.Media.Color.FromRgb(66, 133, 244));
    }

    private static ImageSource? ExtractIconFromFile(string path, int index)
    {
        var large = IntPtr.Zero;
        var small = IntPtr.Zero;
        try
        {
            var count = ExtractIconEx(path, index, out large, out small, 1);
            if (count == 0 || large == IntPtr.Zero) return null;
            return Imaging.CreateBitmapSourceFromHIcon(
                large, Int32Rect.Empty, BitmapSizeOptions.FromWidthAndHeight(32, 32));
        }
        finally
        {
            if (large != IntPtr.Zero) DestroyIcon(large);
            if (small != IntPtr.Zero) DestroyIcon(small);
        }
    }

    private static ImageSource MakeColorIcon(System.Windows.Media.Color color)
    {
        var dv = new DrawingVisual();
        using (var dc = dv.RenderOpen())
        {
            dc.DrawRoundedRectangle(new SolidColorBrush(color), null, new Rect(0, 0, 32, 32), 6, 6);
        }
        var bmp = new RenderTargetBitmap(32, 32, 96, 96, PixelFormats.Pbgra32);
        bmp.Render(dv);
        bmp.Freeze();
        return bmp;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    private static extern uint ExtractIconEx(string lpszFile, int nIconIndex, out IntPtr phiconLarge, out IntPtr phiconSmall, uint nIcons);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);
}
