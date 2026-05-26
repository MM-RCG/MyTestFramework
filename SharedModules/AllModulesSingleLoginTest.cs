using Microsoft.Playwright;
using MyTestFramework.Pages;
using MyTestFramework.Utils;

namespace MyTestFramework.Tests;

/// <summary>
/// 全模块单会话测试：一次 admin 登录完成用户管理、工作区管理、急停区域管理全流程。
/// </summary>
public class AllModulesSingleLoginTest
{
    /// <summary>
    /// 执行单次登录串行非用户模块流程（工作区、急停区域、MCC）。
    /// </summary>
    public Task RunNonUserModulesAsync()
    {
        return RunCoreAsync(includeUserFlow: false);
    }

    /// <summary>
    /// 执行单次登录串行全流程。
    /// </summary>
    public async Task RunAsync()
    {
        await RunCoreAsync(includeUserFlow: true);
    }

    private static async Task RunCoreAsync(bool includeUserFlow)
    {
        AppConfig config = Common.LoadConfig();
        UserManagementConfig userConfig = Common.LoadUserManagementConfig();
        WorkspaceManagementConfig workspaceConfig = Common.LoadWorkspaceManagementConfig();
        EmergencyStopAreaConfig emergencyConfig = Common.LoadEmergencyStopAreaConfig();
        MccDeviceStatusMonitorConfig mccConfig = Common.LoadMccDeviceStatusMonitorConfig();

        IBrowserContext? context = null;
        IPage? page = null;
        var hasError = false;
        var keepTraceOnSuccess = Common.ShouldKeepTraceOnSuccess();
        var tracePrefix = includeUserFlow ? "all_modules_single_login" : "non_user_modules_single_login";

        try
        {
            Common.LogInfo(includeUserFlow ? "开始执行全模块单次登录测试..." : "开始执行非用户模块单次登录测试...");

            var playwright = await Playwright.CreateAsync();
            var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = config.Headless,
                SlowMo = 200
            });

            context = await browser.NewContextAsync();
            await Common.SafeStartTraceAsync(context);

            page = await context.NewPageAsync();
            page.SetDefaultTimeout(config.DefaultTimeout);

            var loginPage = new LoginPage(page);
            Common.LogInfo("步骤1：admin 登录");
            await loginPage.NavigateAsync(config.BaseUrl);
            await loginPage.LoginAsync(config.Username, config.Password);

            var loginSuccess = await loginPage.IsLoginSuccessAsync();
            if (!loginSuccess)
            {
                var shot = await Common.SafeSaveScreenshotAsync(page, "all_modules_login_failed");
                throw new InvalidOperationException($"admin 登录失败，无法继续。截图：{shot ?? "未生成"}");
            }

            if (includeUserFlow)
            {
                (page, context) = await RunUserManagementFlowAsync(browser, context, page, config, userConfig, tracePrefix, keepTraceOnSuccess);
            }

            await RunWorkspaceManagementFlowAsync(page, workspaceConfig);
            await RunEmergencyStopAreaFlowAsync(page, emergencyConfig);
            await MccDeviceStatusMonitorTest.RunFlowOnLoggedInPageAsync(page, workspaceConfig, emergencyConfig, mccConfig);

            Common.LogInfo(includeUserFlow
                ? "测试通过：一次 admin 登录完成全部模块流程。\n"
                : "测试通过：一次 admin 登录完成非用户模块流程。\n");
        }
        catch (Exception ex)
        {
            hasError = true;
            Common.LogInfo($"执行异常：{ex.Message}");

            if (page is not null)
            {
                var shot = await Common.SafeSaveScreenshotAsync(page, "all_modules_exception");
                Common.LogInfo($"异常截图已保存：{shot ?? "未生成"}");
            }

            throw;
        }
        finally
        {
            await Common.SafeStopTraceAsync(context, $"{tracePrefix}_final", saveTrace: hasError || keepTraceOnSuccess);

            // 按要求保留浏览器窗口，便于人工复核结果。
            Common.LogInfo(includeUserFlow
                ? "全模块单次登录测试执行结束。浏览器保持打开状态。"
                : "非用户模块单次登录测试执行结束。浏览器保持打开状态。");
        }
    }

    private static async Task<(IPage Page, IBrowserContext Context)> RunUserManagementFlowAsync(IBrowser browser, IBrowserContext context, IPage page, AppConfig config, UserManagementConfig userConfig, string tracePrefix, bool keepTraceOnSuccess)
    {
        Common.LogInfo("模块A：用户管理流程开始");

        var userPage = new UserManagementPage(page);
        await userPage.NavigateToUserManagementAsync();
        await userPage.OpenAddUserDialogAsync();

        var uniqueSuffix = DateTime.Now.ToString("yyyyMMddHHmmss");
        var form = new UserManagementPage.NewUserFormData
        {
            Account = $"{userConfig.AccountPrefix}_{uniqueSuffix}",
            Name = $"{userConfig.NamePrefix}{DateTime.Now:HHmmss}",
            Password = userConfig.Password,
            ConfirmPassword = userConfig.Password,
            Phone = BuildPhoneNumber(userConfig.PhonePrefix),
            Email = $"{userConfig.AccountPrefix}_{uniqueSuffix}@{userConfig.EmailDomain}"
        };

        await userPage.FillAddUserFormAsync(form);
        await userPage.ClickSaveAsync();

        var createSuccess = await userPage.IsCreateSuccessAsync(form.Account);
        if (!createSuccess)
        {
            throw new InvalidOperationException("用户管理：创建用户失败。未出现成功提示或未查询到数据。");
        }

        await userPage.SearchByAccountAsync(form.Account);
        if (!await userPage.IsRowVisibleByAccountAsync(form.Account))
        {
            throw new InvalidOperationException("用户管理：查询新增用户失败。");
        }

        await userPage.OpenEditUserDialogByAccountAsync(form.Account);
        var firstEditName = $"{form.Name}_第一次编辑";
        var firstEdit = new UserManagementPage.EditUserFormData
        {
            Account = form.Account,
            Name = firstEditName,
            Password = null,
            ConfirmPassword = null,
            Phone = BuildPhoneNumber(userConfig.PhonePrefix),
            Email = $"u{DateTime.Now:yyyyMMddHHmmss}@{userConfig.EmailDomain}"
        };

        await userPage.FillEditUserFormAsync(firstEdit);
        await userPage.ClickSaveAsync();

        if (!await userPage.IsUpdateSuccessAsync(form.Account))
        {
            throw new InvalidOperationException("用户管理：第一次编辑失败。");
        }

        await userPage.OpenEditUserDialogByAccountAsync(form.Account);
        var secondEdit = new UserManagementPage.EditUserFormData
        {
            Account = form.Account,
            Name = $"{firstEditName}_第二次编辑",
            Password = $"{userConfig.Password}9",
            ConfirmPassword = $"{userConfig.Password}9",
            Phone = BuildPhoneNumber(userConfig.PhonePrefix),
            Email = $"p{DateTime.Now:yyyyMMddHHmmss}@{userConfig.EmailDomain}"
        };

        await userPage.FillEditUserFormAsync(secondEdit);
        await userPage.ClickSaveAsync();

        if (!await userPage.IsUpdateSuccessAsync(form.Account))
        {
            throw new InvalidOperationException("用户管理：第二次编辑失败。");
        }

        // 修改密码后，先用新密码重新登录，再切回管理员继续后续模块。
        await Common.SafeStopTraceAsync(context, $"{tracePrefix}_user_admin_session", saveTrace: keepTraceOnSuccess);
        await context.CloseAsync();

        context = await browser.NewContextAsync();
        await Common.SafeStartTraceAsync(context);

        page = await context.NewPageAsync();
        page.SetDefaultTimeout(config.DefaultTimeout);

        var reloginPage = new LoginPage(page);
        await reloginPage.NavigateAsync(config.BaseUrl);
        await reloginPage.LoginAsync(form.Account, $"{userConfig.Password}9");

        if (!await reloginPage.IsLoginSuccessAsync())
        {
            throw new InvalidOperationException("用户管理：修改密码后新密码重新登录失败。");
        }

        await Common.SafeStopTraceAsync(context, $"{tracePrefix}_user_new_password_verify", saveTrace: keepTraceOnSuccess);
        await context.CloseAsync();

        context = await browser.NewContextAsync();
        await Common.SafeStartTraceAsync(context);

        page = await context.NewPageAsync();
        page.SetDefaultTimeout(config.DefaultTimeout);

        var adminLoginPage = new LoginPage(page);
        await adminLoginPage.NavigateAsync(config.BaseUrl);
        await adminLoginPage.LoginAsync(config.Username, config.Password);

        if (!await adminLoginPage.IsLoginSuccessAsync())
        {
            throw new InvalidOperationException("用户管理：切回管理员账号失败。");
        }

        userPage = new UserManagementPage(page);
        await userPage.NavigateToUserManagementAsync();

        var deleted = await userPage.DeleteUserByAccountAsync(form.Account);
        if (!deleted)
        {
            deleted = await userPage.DeleteUserByAccountAsync(form.Account);
        }

        if (!deleted)
        {
            throw new InvalidOperationException("用户管理：删除用户失败。");
        }

        Common.LogInfo("模块A：用户管理流程完成");
        return (page, context);
    }

    private static async Task RunWorkspaceManagementFlowAsync(IPage page, WorkspaceManagementConfig workspaceConfig)
    {
        Common.LogInfo("模块B：工作区管理流程开始");

        var workspacePage = new WorkspaceManagementPage(page);
        await workspacePage.NavigateToWorkspaceManagementAsync();
        await workspacePage.OpenAddWorkspaceDialogAsync();

        var uniqueSuffix = DateTime.Now.ToString("yyyyMMddHHmmss");
        var createForm = new WorkspaceManagementPage.NewWorkspaceFormData
        {
            ChineseName = $"{workspaceConfig.ChineseNamePrefix}{DateTime.Now:HHmmss}",
            EnglishName = $"{workspaceConfig.EnglishNamePrefix}_{uniqueSuffix}"
        };

        await workspacePage.FillAddWorkspaceFormAsync(createForm);
        await workspacePage.ClickSaveAsync();

        if (!await workspacePage.IsCreateSuccessAsync(createForm.ChineseName))
        {
            throw new InvalidOperationException("工作区管理：新增失败。");
        }

        await workspacePage.SearchByChineseNameAsync(createForm.ChineseName);
        if (!await workspacePage.IsRowVisibleByChineseNameAsync(createForm.ChineseName))
        {
            throw new InvalidOperationException("工作区管理：查询失败。");
        }

        await workspacePage.OpenEditWorkspaceDialogByChineseNameAsync(createForm.ChineseName);

        var editForm = new WorkspaceManagementPage.EditWorkspaceFormData
        {
            ChineseName = $"{createForm.ChineseName}_改",
            EnglishName = $"{createForm.EnglishName}_upd"
        };

        await workspacePage.FillEditWorkspaceFormAsync(editForm);
        await workspacePage.ClickSaveAsync();

        if (!await workspacePage.IsUpdateSuccessAsync(editForm.ChineseName))
        {
            throw new InvalidOperationException("工作区管理：编辑失败。");
        }

        var deleted = await workspacePage.DeleteWorkspaceByChineseNameAsync(editForm.ChineseName);
        if (!deleted)
        {
            deleted = await workspacePage.DeleteWorkspaceByChineseNameAsync(editForm.ChineseName);
        }

        if (!deleted)
        {
            throw new InvalidOperationException("工作区管理：删除失败。");
        }

        Common.LogInfo("模块B：工作区管理流程完成");
    }

    private static async Task RunEmergencyStopAreaFlowAsync(IPage page, EmergencyStopAreaConfig emergencyConfig)
    {
        Common.LogInfo("模块C：急停区域管理流程开始");

        var areaPage = new EmergencyStopAreaPage(page);
        await areaPage.NavigateToEmergencyStopAreaManagementAsync();
        await areaPage.OpenAddDialogAsync();

        var suffix = DateTime.Now.ToString("yyyyMMddHHmmss");
        var createForm = new EmergencyStopAreaPage.NewEmergencyStopAreaFormData
        {
            AreaCode = $"{emergencyConfig.AreaCodePrefix}{DateTime.Now:HHmmss}",
            ChineseName = $"{emergencyConfig.ChineseNamePrefix}{DateTime.Now:HHmmss}",
            EnglishName = $"{emergencyConfig.EnglishNamePrefix}_{suffix}"
        };

        await areaPage.FillAddFormAsync(createForm);
        await areaPage.ClickSaveAsync();

        if (!await areaPage.IsCreateSuccessAsync(createForm.AreaCode))
        {
            throw new InvalidOperationException("急停区域管理：新增失败。");
        }

        await areaPage.SearchByAreaCodeAsync(createForm.AreaCode);
        if (!await areaPage.IsRowVisibleByAreaCodeAsync(createForm.AreaCode))
        {
            throw new InvalidOperationException("急停区域管理：查询失败。");
        }

        await areaPage.OpenEditDialogByAreaCodeAsync(createForm.AreaCode);

        var editForm = new EmergencyStopAreaPage.EditEmergencyStopAreaFormData
        {
            AreaCode = $"{createForm.AreaCode}_U",
            ChineseName = $"{createForm.ChineseName}_改",
            EnglishName = $"{createForm.EnglishName}_upd"
        };

        await areaPage.FillEditFormAsync(editForm);
        await areaPage.ClickSaveAsync();

        if (!await areaPage.IsUpdateSuccessAsync(editForm.AreaCode))
        {
            throw new InvalidOperationException("急停区域管理：编辑失败。");
        }

        var deleted = await areaPage.DeleteByAreaCodeAsync(editForm.AreaCode);
        if (!deleted)
        {
            deleted = await areaPage.DeleteByAreaCodeAsync(editForm.AreaCode);
        }

        if (!deleted)
        {
            throw new InvalidOperationException("急停区域管理：删除失败。");
        }

        Common.LogInfo("模块C：急停区域管理流程完成");
    }

    private static string BuildPhoneNumber(string phonePrefix)
    {
        var prefix = string.IsNullOrWhiteSpace(phonePrefix) ? "13" : phonePrefix;

        if (prefix.Length >= 11)
        {
            return prefix[..11];
        }

        var need = 11 - prefix.Length;
        var seed = DateTime.Now.ToString("yyyyMMddHHmmssfff");
        var tail = seed.Length >= need ? seed[^need..] : seed.PadLeft(need, '0');

        return $"{prefix}{tail}";
    }
}
