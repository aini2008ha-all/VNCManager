using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using VNCManager.Models;

namespace VNCManager.Services;

public class DeviceService
{
    private readonly string _folder;
    private readonly string _devicesFile;
    private readonly string _settingsFile;

    public DeviceService()
    {
        _folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "VNCManager");
        try { Directory.CreateDirectory(_folder); } catch { }
        _devicesFile = Path.Combine(_folder, "devices.json");
        _settingsFile = Path.Combine(_folder, "settings.json");
    }

    public string DataFolder => _folder;

    private JsonSerializerOptions Options => new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    };

    public List<VncDevice> LoadDevices()
    {
        if (!File.Exists(_devicesFile)) return [];
        try
        {
            return JsonSerializer.Deserialize<List<VncDevice>>(File.ReadAllText(_devicesFile), Options) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public void SaveDevices(IEnumerable<VncDevice> devices)
    {
        var json = JsonSerializer.Serialize(devices, Options);
        SafeWrite(_devicesFile, json);
    }

    public AppSettings LoadSettings()
    {
        if (!File.Exists(_settingsFile)) return new AppSettings();
        try
        {
            return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(_settingsFile), Options)
                   ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void SaveSettings(AppSettings settings)
    {
        var json = JsonSerializer.Serialize(settings, Options);
        SafeWrite(_settingsFile, json);
    }

    private static void SafeWrite(string path, string content)
    {
        var temp = path + ".tmp";
        try
        {
            File.WriteAllText(temp, content);
            File.Move(temp, path, true);
        }
        catch
        {
            try { if (File.Exists(temp)) File.Delete(temp); } catch { }
            // 与旧版本一致：配置写入失败不应让 UI 直接崩溃。
        }
    }

    public void ExportDevices(string path, IEnumerable<VncDevice> devices, bool includePasswords)
    {
        var list = devices.Select(d =>
        {
            var copy = new VncDevice
            {
                Id = d.Id,
                Name = d.Name,
                Host = d.Host,
                Port = d.Port,
                Group = d.Group,
                Comment = d.Comment,
                Favorite = d.Favorite,
                LastOnline = d.LastOnline,
                LastConnected = d.LastConnected,
                Password = includePasswords ? d.Password : ""
            };
            return copy;
        }).ToList();
        File.WriteAllText(path, JsonSerializer.Serialize(list, Options));
    }

    public List<VncDevice> ImportDevices(string path)
    {
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<List<VncDevice>>(json, Options) ?? [];
    }
}
