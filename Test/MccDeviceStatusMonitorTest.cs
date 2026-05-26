using Microsoft.Playwright;
using MyTestFramework.Pages;
using MyTestFramework.Utils;

namespace MyTestFramework.Tests;

/// <summary>
/// MCC设备状态监控测试用例：新增 -> 查询 -> 编辑 -> 删除。
/// </summary>
public class MccDeviceStatusMonitorTest
{
    public async Task RunMccDeviceCrudAsync()
    {
        AppConfig config = Common.LoadConfig();
        WorkspaceManagementConfig workspaceConfig = Common.LoadWorkspaceManagementConfig();
        EmergencyStopAreaConfig emergencyConfig = Common.LoadEmergencyStopAreaConfig();
        MccDeviceStatusMonitorConfig mccConfig = Common.LoadMccDeviceStatusMonitorConfig();

        IPage? page = null;
        IBrowserContext? context = null;
        var hasError = false;
        var keepTraceOnSuccess = Common.ShouldKeepTraceOnSuccess();

        try
        {
            Common.LogInfo("开始执行MCC设备状态监控测试...");

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
            Common.LogInfo("步骤1：登录");
            await loginPage.NavigateAsync(config.BaseUrl);
            await loginPage.LoginAsync(config.Username, config.Password);

            if (!await loginPage.IsLoginSuccessAsync())
            {
                var shot = await Common.SafeSaveScreenshotAsync(page, "mcc_login_failed");
                throw new InvalidOperationException($"登录失败，无法继续执行MCC设备状态监控测试。截图：{shot ?? "未生成"}");
            }

            await RunFlowOnLoggedInPageAsync(page, workspaceConfig, emergencyConfig, mccConfig);
            Common.LogInfo("测试通过：MCC设备状态监控新增、编辑、删除流程全部成功。\n");
        }
        catch (Exception ex)
        {
            hasError = true;
            Common.LogInfo($"执行异常：{ex.Message}");

            if (page is not null)
            {
                var shot = await Common.SafeSaveScreenshotAsync(page, "mcc_exception");
                Common.LogInfo($"异常截图已保存：{shot ?? "未生成"}");
            }

            throw;
        }
        finally
        {
            await Common.SafeStopTraceAsync(context, "mcc_device_status_test", saveTrace: hasError || keepTraceOnSuccess);

            Common.LogInfo("MCC设备状态监控测试执行结束。浏览器保持打开状态。");
        }
    }

    public static async Task RunFlowOnLoggedInPageAsync(IPage page, WorkspaceManagementConfig workspaceConfig, EmergencyStopAreaConfig emergencyConfig, MccDeviceStatusMonitorConfig mccConfig)
    {
        var mccPage = new MccDeviceStatusMonitorPage(page);

        Common.LogInfo("步骤2：切换到MCC设备状态监控菜单");
        await mccPage.NavigateToMccDeviceStatusMonitorAsync();

        Common.LogInfo("步骤3：点击新增，打开新增窗口");
        await mccPage.OpenAddDialogAsync();

        var suffix = DateTime.Now.ToString("yyyyMMddHHmmss");
        var createForm = new MccDeviceStatusMonitorPage.NewMccDeviceFormData
        {
            CardNo = $"{mccConfig.CardNoPrefix}{DateTime.Now:HHmmss}",
            LineNo = $"{mccConfig.LineNoPrefix}{DateTime.Now:mmss}",
            IpAddress = BuildIpv4(),
            Location = $"{mccConfig.LocationPrefix}{DateTime.Now:HHmmss}"
        };

        Common.LogInfo("步骤3.1：优先输入卡号");
        await mccPage.FillAddCardNoOnlyAsync(createForm.CardNo);

        if (!await mccPage.TrySelectFirstWorkspaceAsync())
        {
            Common.LogInfo("工作区下拉无数据，先新增一个工作区作为前置数据。");
            await mccPage.CloseCurrentDialogIfAnyAsync();
            await CreateWorkspacePrerequisiteAsync(page, workspaceConfig);
            await mccPage.NavigateToMccDeviceStatusMonitorAsync();
            await mccPage.OpenAddDialogAsync();

            var workspaceSelected = await mccPage.TrySelectFirstWorkspaceAsync();
            if (!workspaceSelected)
            {
                throw new InvalidOperationException("MCC设备状态监控：新增页工作区下拉选择失败（已创建前置数据后仍无可选项）。");
            }
        }

        if (!await TrySelectEmergencyAreaFirstWithRetryAsync(mccPage))
        {
            Common.LogInfo("急停区域下拉无数据，先新增一个急停区域作为前置数据。");
            await mccPage.CloseCurrentDialogIfAnyAsync();
            await CreateEmergencyAreaPrerequisiteAsync(page, emergencyConfig);
            await mccPage.NavigateToMccDeviceStatusMonitorAsync();
            await mccPage.OpenAddDialogAsync();

            var workspaceSelected = await mccPage.TrySelectFirstWorkspaceAsync();
            if (!workspaceSelected)
            {
                throw new InvalidOperationException("MCC设备状态监控：新增页工作区下拉选择失败。无法继续选择急停区域。");
            }

            var emergencySelected = await TrySelectEmergencyAreaFirstWithRetryAsync(mccPage);
            if (!emergencySelected)
            {
                throw new InvalidOperationException("MCC设备状态监控：新增页急停区域下拉选择失败（系统有数据但未选中）。");
            }
        }

        Common.LogInfo("步骤4：填写卡号/线号/IP/位置信息并保存");
        await mccPage.FillAddOptionalFieldsAsync(createForm);
        await mccPage.ClickSaveAsync();

        Common.LogInfo("步骤5：校验提示设备新增成功");
        if (!await mccPage.IsCreateSuccessAsync(createForm.CardNo))
        {
            throw new InvalidOperationException("MCC设备状态监控：设备新增失败。");
        }

        Common.LogInfo("步骤6：查询新增设备并打开编辑页面");
        await mccPage.SearchByCardNoAsync(createForm.CardNo);
        if (!await mccPage.IsRowVisibleByCardNoAsync(createForm.CardNo))
        {
            throw new InvalidOperationException("MCC设备状态监控：查询新增设备失败。");
        }

        await mccPage.OpenEditDialogByCardNoAsync(createForm.CardNo);

        var cardReadonly = await mccPage.IsCardReadonlyInEditDialogAsync();
        if (!cardReadonly)
        {
            Common.LogInfo("提示：编辑页卡号检测为可编辑，当前按业务规则不主动输入卡号继续执行。");
        }

        var editForm = new MccDeviceStatusMonitorPage.EditMccDeviceFormData
        {
            LineNo = $"{mccConfig.LineNoPrefix}U{DateTime.Now:ss}",
            IpAddress = BuildIpv4(),
            Location = $"{mccConfig.LocationPrefix}更新{DateTime.Now:HHmmss}"
        };

        Common.LogInfo("步骤7：修改内容并保存");
        await mccPage.FillEditFormAsync(editForm);
        await mccPage.ClickSaveAsync();

        Common.LogInfo("步骤8：校验提示设备编辑成功");
        if (!await mccPage.IsUpdateSuccessAsync(createForm.CardNo))
        {
            throw new InvalidOperationException("MCC设备状态监控：设备编辑失败。");
        }

        Common.LogInfo("步骤9：删除设备并二次确认，校验设备删除成功");
        var deleted = await mccPage.DeleteByCardNoAsync(createForm.CardNo);
        if (!deleted)
        {
            deleted = await mccPage.DeleteByCardNoAsync(createForm.CardNo);
        }

        if (!deleted)
        {
            throw new InvalidOperationException("MCC设备状态监控：设备删除失败。");
        }
    }

    private static async Task CreateWorkspacePrerequisiteAsync(IPage page, WorkspaceManagementConfig workspaceConfig)
    {
        var workspacePage = new WorkspaceManagementPage(page);
        await workspacePage.NavigateToWorkspaceManagementAsync();
        await workspacePage.OpenAddWorkspaceDialogAsync();

        var suffix = DateTime.Now.ToString("yyyyMMddHHmmss");
        var form = new WorkspaceManagementPage.NewWorkspaceFormData
        {
            ChineseName = $"{workspaceConfig.ChineseNamePrefix}前置{DateTime.Now:HHmmss}",
            EnglishName = $"{workspaceConfig.EnglishNamePrefix}_pre_{suffix}"
        };

        await workspacePage.FillAddWorkspaceFormAsync(form);
        await workspacePage.ClickSaveAsync();

        if (!await workspacePage.IsCreateSuccessAsync(form.ChineseName))
        {
            throw new InvalidOperationException("创建前置工作区失败。无法继续MCC设备状态监控测试。");
        }
    }

    private static async Task CreateEmergencyAreaPrerequisiteAsync(IPage page, EmergencyStopAreaConfig emergencyConfig)
    {
        var areaPage = new EmergencyStopAreaPage(page);
        await areaPage.NavigateToEmergencyStopAreaManagementAsync();
        await areaPage.OpenAddDialogAsync();

        var suffix = DateTime.Now.ToString("yyyyMMddHHmmss");
        var form = new EmergencyStopAreaPage.NewEmergencyStopAreaFormData
        {
            AreaCode = $"{emergencyConfig.AreaCodePrefix}P{DateTime.Now:HHmmss}",
            ChineseName = $"{emergencyConfig.ChineseNamePrefix}前置{DateTime.Now:HHmmss}",
            EnglishName = $"{emergencyConfig.EnglishNamePrefix}_pre_{suffix}"
        };

        await areaPage.FillAddFormAsync(form);
        await areaPage.ClickSaveAsync();

        if (!await areaPage.IsCreateSuccessAsync(form.AreaCode))
        {
            throw new InvalidOperationException("创建前置急停区域失败。无法继续MCC设备状态监控测试。");
        }
    }

    private static string BuildIpv4()
    {
        var now = DateTime.Now;
        var second = now.Second % 250 + 1;
        var minute = now.Minute % 250 + 1;
        var hour = now.Hour % 250 + 1;

        return $"10.{hour}.{minute}.{second}";
    }

    private static async Task<bool> TrySelectEmergencyAreaFirstWithRetryAsync(MccDeviceStatusMonitorPage mccPage)
    {
        for (var i = 0; i < 3; i++)
        {
            if (await mccPage.TrySelectFirstEmergencyAreaFastAsync() || await mccPage.TrySelectFirstEmergencyAreaAsync())
            {
                return true;
            }

            await Task.Delay(800);
        }

        return false;
    }
}
