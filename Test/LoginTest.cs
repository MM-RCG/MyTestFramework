using Microsoft.Playwright;
using MyTestFramework.Pages;
using MyTestFramework.Utils;

namespace MyTestFramework.Tests;

/// <summary>
/// 登录测试用例类。
/// 该类负责组织测试流程：启动浏览器 -> 执行登录 -> 校验结果 -> 异常处理。
/// </summary>
public class LoginTest
{
    /// <summary>
    /// 执行登录测试主流程。
    /// </summary>
    public async Task RunAsync()
    {
        AppConfig config = Common.LoadConfig();

        IBrowser? browser = null;
        IBrowserContext? context = null;
        IPage? page = null;
        var hasError = false;
        var keepTraceOnSuccess = Common.ShouldKeepTraceOnSuccess();

        try
        {
            Common.LogInfo("开始执行登录测试...");

            var playwright = await Playwright.CreateAsync();

            browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = config.Headless,
                SlowMo = 200
            });

            context = await browser.NewContextAsync();
            await Common.SafeStartTraceAsync(context);

            page = await context.NewPageAsync();
            page.SetDefaultTimeout(config.DefaultTimeout);

            // 使用页面对象执行页面动作。
            var loginPage = new LoginPage(page);

            Common.LogInfo("步骤1：打开登录页");
            await loginPage.NavigateAsync(config.BaseUrl);

            Common.LogInfo("步骤2：输入账号密码并点击登录");
            await loginPage.LoginAsync(config.Username, config.Password);

            Common.LogInfo("步骤3：校验是否登录成功");
            var isSuccess = await loginPage.IsLoginSuccessAsync();

            if (isSuccess)
            {
                Common.LogInfo("测试通过：检测到欢迎信息或用户头像元素。");
            }
            else
            {
                Common.LogInfo("测试失败：未检测到欢迎信息/用户头像，准备截图。");
                var shotPath = await Common.SafeSaveScreenshotAsync(page, "login_failed");
                Common.LogInfo($"失败截图已保存：{shotPath ?? "未生成"}");
            }
        }
        catch (Exception ex)
        {
            hasError = true;
            Common.LogInfo($"执行异常：{ex.Message}");

            // 发生异常时尽量截图，保留现场。
            if (page is not null)
            {
                var shotPath = await Common.SafeSaveScreenshotAsync(page, "login_exception");
                Common.LogInfo($"异常截图已保存：{shotPath ?? "未生成"}");
            }

            // 继续抛出异常，方便外层或 CI 感知失败。
            throw;
        }
        finally
        {
            await Common.SafeStopTraceAsync(context, "login_test", saveTrace: hasError || keepTraceOnSuccess);

            // 让测试宿主自己回收浏览器资源，避免连接已释放时再关闭引发异常。

            Common.LogInfo("登录测试执行结束。");
        }
    }
}
