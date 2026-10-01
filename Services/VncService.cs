using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;
using VNCManager.Models;

namespace VNCManager.Services;

public class VncService
{
    public async Task<(bool Online, int? Latency)> CheckAsync(VncDevice device, int timeoutMs = 1200)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            using var client = new TcpClient();
            using var cts = new CancellationTokenSource(timeoutMs);
            await client.ConnectAsync(device.Host, device.Port, cts.Token);
            sw.Stop();
            return (true, (int)sw.ElapsedMilliseconds);
        }
        catch
        {
            return (false, null);
        }
    }

    /// <summary>
    /// 查找顺序：内置 tightvnc → 用户设置路径 → 系统安装目录 → 程序目录
    /// </summary>
    public string? FindViewer(string configuredPath)
    {
        var bundled = Path.Combine(AppContext.BaseDirectory, "tightvnc", "tvnviewer.exe");
        if (File.Exists(bundled))
            return bundled;

        if (!string.IsNullOrWhiteSpace(configuredPath) && File.Exists(configuredPath))
            return configuredPath;

        string[] candidates =
        [
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "TightVNC", "tvnviewer.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "TightVNC", "tvnviewer.exe"),
            Path.Combine(AppContext.BaseDirectory, "tvnviewer.exe")
        ];

        return candidates.FirstOrDefault(File.Exists);
    }

    public bool Connect(VncDevice device, string? viewerPath, string plainPassword, out string message)
    {
        message = "";
        try
        {
            if (device.ConnectionType == ConnectionType.Rdp)
                return ConnectRdp(device, plainPassword, out message);

            return ConnectVnc(device, viewerPath, plainPassword, out message);
        }
        catch (Exception ex)
        {
            message = "启动失败：" + ex.Message;
            return false;
        }
    }

    private bool ConnectVnc(VncDevice device, string? viewerPath, string plainPassword, out string message)
    {
        message = "";
        if (string.IsNullOrWhiteSpace(viewerPath) || !File.Exists(viewerPath))
        {
            message = "未找到 TightVNC Viewer。\n请确认程序目录下 tightvnc\\tvnviewer.exe 存在，或在「设置」中指定路径。";
            return false;
        }

        var args = new StringBuilder();
        args.Append(Quote(device.Host + ":" + device.Port));
        if (!string.IsNullOrEmpty(plainPassword))
        {
            args.Append(" -password=");
            args.Append(Quote(plainPassword));
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = viewerPath,
            Arguments = args.ToString(),
            UseShellExecute = false,
            WorkingDirectory = Path.GetDirectoryName(viewerPath) ?? AppContext.BaseDirectory
        });
        return true;
    }

    private bool ConnectRdp(VncDevice device, string plainPassword, out string message)
    {
        message = "";
        var target = device.Host?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(target))
        {
            message = "主机地址为空。";
            return false;
        }

        var port = device.Port > 0 ? device.Port : 3389;
        var server = port == 3389 ? target : target + ":" + port;

        if (!string.IsNullOrWhiteSpace(device.Username) && !string.IsNullOrEmpty(plainPassword))
        {
            var generic = "TERMSRV/" + target;
            var cmdkey = new ProcessStartInfo
            {
                FileName = "cmdkey",
                Arguments = "/generic:" + Quote(generic) + " /user:" + Quote(device.Username) + " /pass:" + Quote(plainPassword),
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var p = Process.Start(cmdkey);
            p?.WaitForExit(3000);
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = "mstsc.exe",
            Arguments = "/v:" + server,
            UseShellExecute = true
        });
        return true;
    }

    private static string Quote(string value) => "\"" + value.Replace("\"", "\\\"") + "\"";
}
