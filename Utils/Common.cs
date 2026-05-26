using System.Text.Json;
using Microsoft.Playwright;

namespace MyTestFramework.Utils;

/// <summary>
/// 配置模型：映射 appsettings.json 中的 PlaywrightSettings。
/// </summary>
public class AppConfig
{
    public string BaseUrl { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool Headless { get; set; } = false;
    public float DefaultTimeout { get; set; } = 15000;
}

/// <summary>
/// 用户管理测试数据配置模型：映射 appsettings.json 中的 UserManagementSettings。
/// </summary>
public class UserManagementConfig
{
    public string AccountPrefix { get; set; } = "auto_user";
    public string NamePrefix { get; set; } = "自动化用户";
    public string Password { get; set; } = "new123456";
    public string PhonePrefix { get; set; } = "13";
    public string EmailDomain { get; set; } = "test.com";
    public bool CleanupCreatedUser { get; set; } = true;
}

/// <summary>
/// 工作区管理测试数据配置模型：映射 appsettings.json 中的 WorkspaceManagementSettings。
/// </summary>
public class WorkspaceManagementConfig
{
    public string ChineseNamePrefix { get; set; } = "自动化工作区";
    public string EnglishNamePrefix { get; set; } = "auto_workspace";
}

/// <summary>
/// 急停区域管理测试数据配置模型：映射 appsettings.json 中的 EmergencyStopAreaSettings。
/// </summary>
public class EmergencyStopAreaConfig
{
    public string AreaCodePrefix { get; set; } = "ESA";
    public string ChineseNamePrefix { get; set; } = "自动化急停区域";
    public string EnglishNamePrefix { get; set; } = "auto_emergency_area";
}

/// <summary>
/// MCC设备状态监控测试数据配置模型：映射 appsettings.json 中的 MccDeviceStatusMonitorSettings。
/// </summary>
public class MccDeviceStatusMonitorConfig
{
    public string CardNoPrefix { get; set; } = "CARD";
    public string LineNoPrefix { get; set; } = "LINE";
    public string LocationPrefix { get; set; } = "MCC位置";
}

/// <summary>
/// 公共工具类：配置读取、日志输出、截图保存。
/// </summary>
public static class Common
{
    private const string ScreenshotFolder = "screenshots";
    private const string TraceFolder = "traces";

    /// <summary>
    /// 读取 appsettings.json 中的 PlaywrightSettings 配置。
    /// </summary>
    /// <param name="filePath">配置文件路径，默认 appsettings.json</param>
    /// <returns>解析后的配置对象</returns>
    /// <exception cref="InvalidOperationException">配置缺失或解析失败时抛出</exception>
    public static AppConfig LoadConfig(string filePath = "appsettings.json")
    {
        if (!File.Exists(filePath))
        {
            throw new InvalidOperationException($"配置文件不存在：{filePath}");
        }

        var jsonText = File.ReadAllText(filePath);

        using var jsonDoc = JsonDocument.Parse(jsonText);
        if (!jsonDoc.RootElement.TryGetProperty("PlaywrightSettings", out var settingsElement))
        {
            throw new InvalidOperationException("配置文件缺少 PlaywrightSettings 节点。");
        }

        var config = settingsElement.Deserialize<AppConfig>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (config is null)
        {
            throw new InvalidOperationException("配置解析失败，请检查 appsettings.json 内容。");
        }

        if (string.IsNullOrWhiteSpace(config.BaseUrl) ||
            string.IsNullOrWhiteSpace(config.Username) ||
            string.IsNullOrWhiteSpace(config.Password))
        {
            throw new InvalidOperationException("BaseUrl / Username / Password 不能为空。");
        }

        return config;
    }

    /// <summary>
    /// 读取 appsettings.json 中的 UserManagementSettings 配置。
    /// </summary>
    /// <param name="filePath">配置文件路径，默认 appsettings.json</param>
    /// <returns>用户管理测试数据配置对象</returns>
    /// <exception cref="InvalidOperationException">配置缺失或解析失败时抛出</exception>
    public static UserManagementConfig LoadUserManagementConfig(string filePath = "appsettings.json")
    {
        if (!File.Exists(filePath))
        {
            throw new InvalidOperationException($"配置文件不存在：{filePath}");
        }

        var jsonText = File.ReadAllText(filePath);

        using var jsonDoc = JsonDocument.Parse(jsonText);
        if (!jsonDoc.RootElement.TryGetProperty("UserManagementSettings", out var settingsElement))
        {
            throw new InvalidOperationException("配置文件缺少 UserManagementSettings 节点。");
        }

        var config = settingsElement.Deserialize<UserManagementConfig>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (config is null)
        {
            throw new InvalidOperationException("UserManagementSettings 解析失败，请检查配置内容。");
        }

        if (string.IsNullOrWhiteSpace(config.Password) ||
            string.IsNullOrWhiteSpace(config.PhonePrefix) ||
            string.IsNullOrWhiteSpace(config.EmailDomain))
        {
            throw new InvalidOperationException("UserManagementSettings 中 Password / PhonePrefix / EmailDomain 不能为空。");
        }

        return config;
    }

    /// <summary>
    /// 读取 appsettings.json 中的 WorkspaceManagementSettings 配置。
    /// </summary>
    /// <param name="filePath">配置文件路径，默认 appsettings.json</param>
    /// <returns>工作区管理测试数据配置对象</returns>
    /// <exception cref="InvalidOperationException">配置缺失或解析失败时抛出</exception>
    public static WorkspaceManagementConfig LoadWorkspaceManagementConfig(string filePath = "appsettings.json")
    {
        if (!File.Exists(filePath))
        {
            throw new InvalidOperationException($"配置文件不存在：{filePath}");
        }

        var jsonText = File.ReadAllText(filePath);

        using var jsonDoc = JsonDocument.Parse(jsonText);
        if (!jsonDoc.RootElement.TryGetProperty("WorkspaceManagementSettings", out var settingsElement))
        {
            throw new InvalidOperationException("配置文件缺少 WorkspaceManagementSettings 节点。");
        }

        var config = settingsElement.Deserialize<WorkspaceManagementConfig>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (config is null)
        {
            throw new InvalidOperationException("WorkspaceManagementSettings 解析失败，请检查配置内容。");
        }

        if (string.IsNullOrWhiteSpace(config.ChineseNamePrefix) ||
            string.IsNullOrWhiteSpace(config.EnglishNamePrefix))
        {
            throw new InvalidOperationException("WorkspaceManagementSettings 中 ChineseNamePrefix / EnglishNamePrefix 不能为空。");
        }

        return config;
    }

    /// <summary>
    /// 读取 appsettings.json 中的 EmergencyStopAreaSettings 配置。
    /// </summary>
    /// <param name="filePath">配置文件路径，默认 appsettings.json</param>
    /// <returns>急停区域管理测试数据配置对象</returns>
    /// <exception cref="InvalidOperationException">配置缺失或解析失败时抛出</exception>
    public static EmergencyStopAreaConfig LoadEmergencyStopAreaConfig(string filePath = "appsettings.json")
    {
        if (!File.Exists(filePath))
        {
            throw new InvalidOperationException($"配置文件不存在：{filePath}");
        }

        var jsonText = File.ReadAllText(filePath);

        using var jsonDoc = JsonDocument.Parse(jsonText);
        if (!jsonDoc.RootElement.TryGetProperty("EmergencyStopAreaSettings", out var settingsElement))
        {
            throw new InvalidOperationException("配置文件缺少 EmergencyStopAreaSettings 节点。");
        }

        var config = settingsElement.Deserialize<EmergencyStopAreaConfig>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (config is null)
        {
            throw new InvalidOperationException("EmergencyStopAreaSettings 解析失败，请检查配置内容。");
        }

        if (string.IsNullOrWhiteSpace(config.AreaCodePrefix) ||
            string.IsNullOrWhiteSpace(config.ChineseNamePrefix) ||
            string.IsNullOrWhiteSpace(config.EnglishNamePrefix))
        {
            throw new InvalidOperationException("EmergencyStopAreaSettings 中 AreaCodePrefix / ChineseNamePrefix / EnglishNamePrefix 不能为空。");
        }

        return config;
    }

    /// <summary>
    /// 读取 appsettings.json 中的 MccDeviceStatusMonitorSettings 配置。
    /// </summary>
    /// <param name="filePath">配置文件路径，默认 appsettings.json</param>
    /// <returns>MCC设备状态监控测试数据配置对象</returns>
    /// <exception cref="InvalidOperationException">配置缺失或解析失败时抛出</exception>
    public static MccDeviceStatusMonitorConfig LoadMccDeviceStatusMonitorConfig(string filePath = "appsettings.json")
    {
        if (!File.Exists(filePath))
        {
            throw new InvalidOperationException($"配置文件不存在：{filePath}");
        }

        var jsonText = File.ReadAllText(filePath);

        using var jsonDoc = JsonDocument.Parse(jsonText);
        if (!jsonDoc.RootElement.TryGetProperty("MccDeviceStatusMonitorSettings", out var settingsElement))
        {
            throw new InvalidOperationException("配置文件缺少 MccDeviceStatusMonitorSettings 节点。");
        }

        var config = settingsElement.Deserialize<MccDeviceStatusMonitorConfig>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (config is null)
        {
            throw new InvalidOperationException("MccDeviceStatusMonitorSettings 解析失败，请检查配置内容。");
        }

        if (string.IsNullOrWhiteSpace(config.CardNoPrefix) ||
            string.IsNullOrWhiteSpace(config.LineNoPrefix) ||
            string.IsNullOrWhiteSpace(config.LocationPrefix))
        {
            throw new InvalidOperationException("MccDeviceStatusMonitorSettings 中 CardNoPrefix / LineNoPrefix / LocationPrefix 不能为空。");
        }

        return config;
    }

    /// <summary>
    /// 统一日志输出（带时间戳）。
    /// </summary>
    public static void LogInfo(string message)
    {
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}");
    }

    /// <summary>
    /// 失败或异常时截图，自动创建截图目录，文件名自动拼接时间戳。
    /// </summary>
    /// <param name="page">当前页面对象</param>
    /// <param name="prefix">截图文件名前缀，如 login_failed / login_exception</param>
    /// <returns>截图完整路径</returns>
    public static async Task<string> SaveScreenshotAsync(IPage page, string prefix)
    {
        Directory.CreateDirectory(ScreenshotFolder);

        var fileName = $"{prefix}_{DateTime.Now:yyyyMMdd_HHmmss}.png";
        var fullPath = Path.Combine(ScreenshotFolder, fileName);

        await page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = fullPath,
            FullPage = true
        });

        return fullPath;
    }

    /// <summary>
    /// 安全截图：即使页面已经关闭，也不会让主流程二次失败。
    /// </summary>
    public static async Task<string?> SafeSaveScreenshotAsync(IPage? page, string prefix)
    {
        if (page is null)
        {
            return null;
        }

        try
        {
            return await SaveScreenshotAsync(page, prefix);
        }
        catch (Exception ex)
        {
            LogInfo($"截图失败已忽略：{ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 安全关闭浏览器：清理阶段如果连接已被释放，不让测试失败。
    /// </summary>
    public static async Task SafeCloseBrowserAsync(IBrowser? browser)
    {
        if (browser is null)
        {
            return;
        }

        try
        {
            await browser.CloseAsync();
        }
        catch (Exception ex)
        {
            LogInfo($"浏览器关闭时忽略异常：{ex.Message}");
        }
    }

    /// <summary>
    /// 开启 Playwright Trace（截图、快照、源码）。
    /// </summary>
    public static async Task SafeStartTraceAsync(IBrowserContext? context)
    {
        if (context is null)
        {
            return;
        }

        try
        {
            await context.Tracing.StartAsync(new TracingStartOptions
            {
                Screenshots = true,
                Snapshots = true,
                Sources = true
            });
        }
        catch (Exception ex)
        {
            LogInfo($"开启 Trace 失败已忽略：{ex.Message}");
        }
    }

    /// <summary>
    /// 停止 Trace 并保存 zip，失败时返回 null。
    /// </summary>
    public static async Task<string?> SafeStopTraceAsync(IBrowserContext? context, string traceNamePrefix)
    {
        return await SafeStopTraceAsync(context, traceNamePrefix, saveTrace: true);
    }

    /// <summary>
    /// 停止 Trace，可按策略决定是否落盘 zip。
    /// </summary>
    public static async Task<string?> SafeStopTraceAsync(IBrowserContext? context, string traceNamePrefix, bool saveTrace)
    {
        if (context is null)
        {
            return null;
        }

        try
        {
            if (!saveTrace)
            {
                await context.Tracing.StopAsync();
                LogInfo("Trace 已停止（按策略不落盘）。");
                return null;
            }

            Directory.CreateDirectory(TraceFolder);

            var safePrefix = SanitizeFileName(traceNamePrefix);
            var fileName = $"{safePrefix}_{DateTime.Now:yyyyMMdd_HHmmss_fff}.zip";
            var fullPath = Path.Combine(TraceFolder, fileName);

            await context.Tracing.StopAsync(new TracingStopOptions
            {
                Path = fullPath
            });

            LogInfo($"Trace 已保存：{fullPath}");
            return fullPath;
        }
        catch (Exception ex)
        {
            LogInfo($"保存 Trace 失败已忽略：{ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 是否在成功场景保留 Trace。
    /// 默认 false，可通过环境变量 KEEP_TRACE_ON_SUCCESS=true 开启。
    /// </summary>
    public static bool ShouldKeepTraceOnSuccess()
    {
        var value = Environment.GetEnvironmentVariable("KEEP_TRACE_ON_SUCCESS");
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return string.Equals(value, "1", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase);
    }

    private static string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return "trace";
        }

        var result = fileName;
        foreach (var c in Path.GetInvalidFileNameChars())
        {
            result = result.Replace(c, '_');
        }

        return result;
    }
}
