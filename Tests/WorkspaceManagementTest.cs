using Microsoft.Playwright;
using MyTestFramework.Pages;
using MyTestFramework.Utils;

namespace MyTestFramework.Tests;

/// <summary>
/// 工作区管理测试用例：新增 -> 查询 -> 编辑 -> 删除。
/// </summary>
public class WorkspaceManagementTest
{
    /// <summary>
    /// 执行工作区管理完整流程。
    /// </summary>
    public async Task RunWorkspaceCrudAsync()
    {
        AppConfig config = Common.LoadConfig();
        WorkspaceManagementConfig workspaceConfig = Common.LoadWorkspaceManagementConfig();

        IBrowser? browser = null;
        IBrowserContext? context = null;
        IPage? page = null;
        var hasError = false;
        var keepTraceOnSuccess = Common.ShouldKeepTraceOnSuccess();

        try
        {
            Common.LogInfo("开始执行工作区管理测试...");

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
                var shot = await Common.SafeSaveScreenshotAsync(page, "workspace_login_failed");
                throw new InvalidOperationException($"登录失败，无法继续执行工作区管理测试。截图：{shot ?? "未生成"}");
            }

            var workspacePage = new WorkspaceManagementPage(page);

            Common.LogInfo("步骤2：切换到'基础管理-工作区管理菜单'");
            await workspacePage.NavigateToWorkspaceManagementAsync();

            Common.LogInfo("步骤3：点击'新增工作区'，打开新增窗口");
            await workspacePage.OpenAddWorkspaceDialogAsync();

            var uniqueSuffix = DateTime.Now.ToString("yyyyMMddHHmmss");
            var createForm = new WorkspaceManagementPage.NewWorkspaceFormData
            {
                ChineseName = $"{workspaceConfig.ChineseNamePrefix}{DateTime.Now:HHmmss}",
                EnglishName = $"{workspaceConfig.EnglishNamePrefix}_{uniqueSuffix}"
            };

            Common.LogInfo("步骤4：填写中文名称和英文名称并保存");
            await workspacePage.FillAddWorkspaceFormAsync(createForm);
            await workspacePage.ClickSaveAsync();

            Common.LogInfo("步骤5：校验提示'工作区创建成功'");
            var createSuccess = await workspacePage.IsCreateSuccessAsync(createForm.ChineseName);
            if (!createSuccess)
            {
                var shot = await Common.SafeSaveScreenshotAsync(page, "workspace_create_failed");
                throw new InvalidOperationException($"未出现'工作区创建成功'提示，或列表中未找到新增工作区。截图：{shot ?? "未生成"}");
            }

            Common.LogInfo("步骤6：查询新增的工作区并校验可见");
            await workspacePage.SearchByChineseNameAsync(createForm.ChineseName);
            var rowFound = await workspacePage.IsRowVisibleByChineseNameAsync(createForm.ChineseName);
            if (!rowFound)
            {
                var shot = await Common.SafeSaveScreenshotAsync(page, "workspace_query_failed");
                throw new InvalidOperationException($"查询失败：未找到新增工作区。截图：{shot ?? "未生成"}");
            }

            Common.LogInfo("步骤7：打开编辑页面并修改内容后保存");
            await workspacePage.OpenEditWorkspaceDialogByChineseNameAsync(createForm.ChineseName);

            var editForm = new WorkspaceManagementPage.EditWorkspaceFormData
            {
                ChineseName = $"{createForm.ChineseName}_改",
                EnglishName = $"{createForm.EnglishName}_upd"
            };

            await workspacePage.FillEditWorkspaceFormAsync(editForm);
            await workspacePage.ClickSaveAsync();

            Common.LogInfo("步骤8：校验提示'工作区更新成功'");
            var updateSuccess = await workspacePage.IsUpdateSuccessAsync(editForm.ChineseName);
            if (!updateSuccess)
            {
                var shot = await Common.SafeSaveScreenshotAsync(page, "workspace_update_failed");
                throw new InvalidOperationException($"未出现'工作区更新成功'提示，或列表中未找到更新后的工作区。截图：{shot ?? "未生成"}");
            }

            Common.LogInfo("步骤9：删除该工作区，二次确认后校验'工作区删除成功'");
            var deleted = await workspacePage.DeleteWorkspaceByChineseNameAsync(editForm.ChineseName);
            if (!deleted)
            {
                Common.LogInfo("删除第1次失败，开始重试删除...");
                deleted = await workspacePage.DeleteWorkspaceByChineseNameAsync(editForm.ChineseName);
            }

            if (!deleted)
            {
                var shot = await Common.SafeSaveScreenshotAsync(page, "workspace_delete_failed");
                throw new InvalidOperationException($"删除失败：未完成二次确认或未出现'工作区删除成功'提示。截图：{shot ?? "未生成"}");
            }

            Common.LogInfo("测试通过：工作区新增、编辑、删除流程全部成功。\n");
        }
        catch (Exception ex)
        {
            hasError = true;
            Common.LogInfo($"执行异常：{ex.Message}");

            if (page is not null)
            {
                var shot = await Common.SafeSaveScreenshotAsync(page, "workspace_exception");
                Common.LogInfo($"异常截图已保存：{shot ?? "未生成"}");
            }

            throw;
        }
        finally
        {
            await Common.SafeStopTraceAsync(context, "workspace_mgmt_test", saveTrace: hasError || keepTraceOnSuccess);

            // 按要求保留浏览器窗口，便于人工复核结果。
            Common.LogInfo("工作区管理测试执行结束。浏览器保持打开状态。");
        }
    }
}
