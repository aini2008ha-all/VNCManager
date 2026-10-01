using System.IO;
using System.Linq;
using System.Text;

namespace VNCManager.Services;

public sealed class LogEntry
{
    public DateTime Time { get; init; }
    public string Category { get; init; } = "";
    public string Action { get; init; } = "";
    public string Detail { get; init; } = "";
}

public sealed class LogService
{
    private readonly string _logFolder;
    private readonly string _logFile;
    private readonly object _sync = new();

    public LogService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        if (string.IsNullOrWhiteSpace(appData))
            appData = AppContext.BaseDirectory;

        _logFolder = Path.Combine(appData, "VNCManager", "logs");
        _logFile = Path.Combine(_logFolder, "app.log");

        // 日志不能成为程序启动失败的原因。目录创建失败时仅降级到程序目录。
        try
        {
            Directory.CreateDirectory(_logFolder);
        }
        catch
        {
            _logFolder = Path.Combine(AppContext.BaseDirectory, "logs");
            _logFile = Path.Combine(_logFolder, "app.log");
            try { Directory.CreateDirectory(_logFolder); } catch { }
        }
    }

    public string LogFile => _logFile;

    public void Info(string category, string action, string detail = "") => Write(category, action, detail);

    public List<LogEntry> ReadAll()
    {
        lock (_sync)
        {
            try
            {
                if (!File.Exists(_logFile)) return [];
                return File.ReadAllLines(_logFile, Encoding.UTF8)
                    .Select(Parse)
                    .Where(x => x != null)
                    .Cast<LogEntry>()
                    .OrderByDescending(x => x.Time)
                    .ToList();
            }
            catch
            {
                return [];
            }
        }
    }

    public void Clear()
    {
        lock (_sync)
        {
            try
            {
                if (File.Exists(_logFile)) File.Delete(_logFile);
            }
            catch { }
        }
    }

    public void Export(string path, bool csv)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("导出路径为空。", nameof(path));
        var fullPath = Path.GetFullPath(path);
        var parent = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(parent)) Directory.CreateDirectory(parent);

        var entries = ReadAll().OrderBy(x => x.Time).ToList();
        if (csv)
        {
            var sb = new StringBuilder();
            sb.AppendLine("时间,类型,操作,详情");
            foreach (var e in entries)
                sb.AppendLine(string.Join(",", Quote(e.Time.ToString("yyyy-MM-dd HH:mm:ss")), Quote(e.Category), Quote(e.Action), Quote(e.Detail)));
            File.WriteAllText(fullPath, sb.ToString(), new UTF8Encoding(true));
            return;
        }

        File.WriteAllLines(fullPath, entries.Select(e =>
            $"{e.Time:yyyy-MM-dd HH:mm:ss}\t{e.Category}\t{e.Action}\t{e.Detail}"), Encoding.UTF8);
    }

    private void Write(string category, string action, string detail)
    {
        try
        {
            var line = string.Join("\t",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                Clean(category), Clean(action), Clean(detail));
            lock (_sync)
            {
                try { Directory.CreateDirectory(_logFolder); } catch { }
                File.AppendAllText(_logFile, line + Environment.NewLine, new UTF8Encoding(false));
            }
        }
        catch
        {
            // 日志失败不能影响主程序业务。
        }
    }

    private static LogEntry? Parse(string line)
    {
        var parts = line.Split('\t', 4);
        if (parts.Length < 4 || !DateTime.TryParse(parts[0], out var time)) return null;
        return new LogEntry { Time = time, Category = parts[1], Action = parts[2], Detail = parts[3] };
    }

    private static string Clean(string value) =>
        (value ?? "").Replace("\r", " ").Replace("\n", " ").Replace("\t", " ");

    private static string Quote(string value) => "\"" + value.Replace("\"", "\"\"") + "\"";
}
