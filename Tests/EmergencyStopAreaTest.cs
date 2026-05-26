using Microsoft.Playwright;
using MyTestFramework.Pages;
using MyTestFramework.Utils;

namespace MyTestFramework.Tests;

/// <summary>
/// 急停区域管理测试用例：新增 -> 查询 -> 编辑 -> 删除。
/// </summary>
public class EmergencyStopAreaTest
{
    /// <summary>
    /// 执行急停区域管理完整流程。
    /// </summary>
    public async Task RunEmergencyStopAreaCrudAsync()
    {
        AppConfig config = Common.LoadConfig();
        EmergencyStopAreaConfig areaConfig = Common.LoadEmergencyStopAreaConfig();

        IBrowser? browser = null;
        IBrowserContext? context = null;
        IPage? page = null;
        var hasError = false;
        var keepTraceOnSuccess = Common.ShouldKeepTraceOnSuccess();

        try
        {
            Common.LogInfo("开始执行急停区域管理测试...");

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

            var loginPage = new LoginPage(page);
            Common.LogInfo("步骤1：打开登录页并登录");
            await loginPage.NavigateAsync(config.BaseUrl);
            await loginPage.LoginAsync(config.Username, config.Password);

            var loginSuccess = await loginPage.IsLoginSuccessAsync();
            if (!loginSuccess)
            {
                var shot = await Common.SafeSaveScreenshotAsync(page, "emergency_area_login_failed");
                throw new InvalidOperationException($"登录失败，无法继续执行急停区域管理测试。截图：{shot ?? "未生成"}");
            }

            var areaPage = new EmergencyStopAreaPage(page);

            Common.LogInfo("步骤2：切换到'基础管理-急停区域管理'");
            await areaPage.NavigateToEmergencyStopAreaManagementAsync();

            Common.LogInfo("步骤3：点击'新增急停区域'，打开新增窗口");
            await areaPage.OpenAddDialogAsync();

            var suffix = DateTime.Now.ToString("yyyyMMddHHmmss");
            var createForm = new EmergencyStopAreaPage.NewEmergencyStopAreaFormData
            {
                AreaCode = $"{areaConfig.AreaCodePrefix}{DateTime.Now:HHmmss}",
                ChineseName = $"{areaConfig.ChineseNamePrefix}{DateTime.Now:HHmmss}",
                EnglishName = $"{areaConfig.EnglishNamePrefix}_{suffix}"
            };

            Common.LogInfo("步骤4：填写区域代码、中文名称、英文名称并保存");
            await areaPage.FillAddFormAsync(createForm);
            await areaPage.ClickSaveAsync();

            Common.LogInfo("步骤5：校验提示'急停区域创建成功'");
            var createSuccess = await areaPage.IsCreateSuccessAsync(createForm.AreaCode);
            if (!createSuccess)
            {
                var shot = await Common.SafeSaveScreenshotAsync(page, "emergency_area_create_failed");
                throw new InvalidOperationException($"未出现'急停区域创建成功'提示，或列表中未找到新增记录。截图：{shot ?? "未生成"}");
            }

            Common.LogInfo("步骤6：查询新增急停区域并校验可见");
            await areaPage.SearchByAreaCodeAsync(createForm.AreaCode);
            var rowFound = await areaPage.IsRowVisibleByAreaCodeAsync(createForm.AreaCode);
            if (!rowFound)
            {
                var shot = await Common.SafeSaveScreenshotAsync(page, "emergency_area_query_failed");
                throw new InvalidOperationException($"查询失败：未找到新增急停区域。截图：{shot ?? "未生成"}");
            }

            Common.LogInfo("步骤7：打开编辑页面并修改内容后保存");
            await areaPage.OpenEditDialogByAreaCodeAsync(createForm.AreaCode);

            var editForm = new EmergencyStopAreaPage.EditEmergencyStopAreaFormData
            {
                AreaCode = $"{createForm.AreaCode}_U",
                ChineseName = $"{createForm.ChineseName}_改",
                EnglishName = $"{createForm.EnglishName}_upd"
            };

            await areaPage.FillEditFormAsync(editForm);
            await areaPage.ClickSaveAsync();

            Common.LogInfo("步骤8：校验提示'急停区域更新成功'");
            var updateSuccess = await areaPage.IsUpdateSuccessAsync(editForm.AreaCode);
            if (!updateSuccess)
            {
                var shot = await Common.SafeSaveScreenshotAsync(page, "emergency_area_update_failed");
                throw new InvalidOperationException($"未出现'急停区域更新成功'提示，或列表中未找到更新后的记录。截图：{shot ?? "未生成"}");
            }

            Common.LogInfo("步骤9：删除急停区域，二次确认后校验'急停区域删除成功'");
            var deleted = await areaPage.DeleteByAreaCodeAsync(editForm.AreaCode);
            if (!deleted)
            {
                Common.LogInfo("删除第1次失败，开始重试删除...");
                deleted = await areaPage.DeleteByAreaCodeAsync(editForm.AreaCode);
            }

            if (!deleted)
            {
                var shot = await Common.SafeSaveScreenshotAsync(page, "emergency_area_delete_failed");
                throw new InvalidOperationException($"删除失败：未完成二次确认或未出现'急停区域删除成功'提示。截图：{shot ?? "未生成"}");
            }

            Common.LogInfo("测试通过：急停区域新增、编辑、删除流程全部成功。\n");
        }
        catch (Exception ex)
        {
            hasError = true;
            Common.LogInfo($"执行异常：{ex.Message}");

            if (page is not null)
            {
                var shot = await Common.SafeSaveScreenshotAsync(page, "emergency_area_exception");
                Common.LogInfo($"异常截图已保存：{shot ?? "未生成"}");
            }

            throw;
        }
        finally
        {
            await Common.SafeStopTraceAsync(context, "emergency_area_test", saveTrace: hasError || keepTraceOnSuccess);

            // 按要求保留浏览器窗口，便于人工复核结果。
            Common.LogInfo("急停区域管理测试执行结束。浏览器保持打开状态。");
        }
    }
}
