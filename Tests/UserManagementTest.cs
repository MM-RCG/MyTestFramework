using Microsoft.Playwright;
using MyTestFramework.Pages;
using MyTestFramework.Utils;

namespace MyTestFramework.Tests;

/// <summary>
/// 用户管理测试用例：新增 -> 查询 -> 第一次编辑(不改密码) -> 第二次编辑(改密码) -> 新密码重新登录验证 -> 管理员删除用户。
/// 测试步骤：
/// 1. 登录成功。
/// 2. 点击菜单“基础管理-用户管理”。
/// 3. 进入用户管理列表页并打开“新增用户”窗口。
/// 4. 输入账号、姓名、密码、确认密码、手机号、邮箱。
/// 5. 点击保存。
/// 6. 查询刚新增的用户。
/// 7. 第一次编辑该用户（不输入密码）。
/// 8. 第二次编辑同一用户（输入新密码）。
/// 9. 使用新密码重新登录验证。
/// 10. 切回管理员并删除该用户。
/// </summary>
public class UserManagementTest
{
    /// <summary>
    /// 执行“修改密码后需重新登录”的用户管理测试。
    /// </summary>
    public async Task RunCreateUserAsync()
    {
        AppConfig config = Common.LoadConfig();
        UserManagementConfig userConfig = Common.LoadUserManagementConfig();

        IBrowser? browser = null;
        IBrowserContext? context = null;
        IPage? page = null;
        var hasError = false;
        var keepTraceOnSuccess = Common.ShouldKeepTraceOnSuccess();

        try
        {
            Common.LogInfo("开始执行用户管理-新增用户测试...");

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

            // Step 1: 登录成功。
            var loginPage = new LoginPage(page);
            Common.LogInfo("步骤1：打开登录页并登录");
            await loginPage.NavigateAsync(config.BaseUrl);
            await loginPage.LoginAsync(config.Username, config.Password);

            var loginSuccess = await loginPage.IsLoginSuccessAsync();
            if (!loginSuccess)
            {
                var shot = await Common.SafeSaveScreenshotAsync(page, "user_mgmt_login_failed");
                throw new InvalidOperationException($"登录失败，无法继续执行用户管理测试。截图：{shot ?? "未生成"}");
            }

            // Step 2-3: 菜单跳转到用户管理并打开新增窗口。
            var userPage = new UserManagementPage(page);

            Common.LogInfo("步骤2：点击菜单'基础管理-用户管理'");
            await userPage.NavigateToUserManagementAsync();

            Common.LogInfo("步骤3：点击'新增用户'按钮，打开新增窗口");
            await userPage.OpenAddUserDialogAsync();

            // Step 4: 组装并填写新增用户数据。
            // 账号要求系统唯一，这里使用时间戳生成唯一账号。
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

            Common.LogInfo("步骤4：填写新增用户表单");
            await userPage.FillAddUserFormAsync(form);

            // Step 5: 点击保存。
            Common.LogInfo("步骤5：点击保存");
            await userPage.ClickSaveAsync();

            // Step 6: 验证“用户创建成功”。
            Common.LogInfo("步骤6：验证提示'用户创建成功'");
            var createSuccess = await userPage.IsCreateSuccessAsync(form.Account);

            if (!createSuccess)
            {
                var shot = await Common.SafeSaveScreenshotAsync(page, "user_create_failed");
                throw new InvalidOperationException($"未出现'用户创建成功'提示，或列表中未找到新用户。截图：{shot ?? "未生成"}");
            }

            Common.LogInfo("测试通过：用户创建成功。\n");
            Common.LogInfo($"本次创建账号：{form.Account}");

            // Step 7: 查询用户。
            Common.LogInfo("步骤7：查询刚新增的用户");
            await userPage.SearchByAccountAsync(form.Account);
            var foundCreatedUser = await userPage.IsRowVisibleByAccountAsync(form.Account);

            if (!foundCreatedUser)
            {
                var shot = await Common.SafeSaveScreenshotAsync(page, "user_query_failed");
                throw new InvalidOperationException($"查询失败：未在列表中找到新增用户。截图：{shot ?? "未生成"}");
            }

            Common.LogInfo("查询通过：已找到新增用户。");

            // Step 8: 第一次编辑用户（不输入密码）。
            Common.LogInfo("步骤8：第一次编辑该用户（不输入密码）");
            await userPage.OpenEditUserDialogByAccountAsync(form.Account);

            var firstEditName = $"{form.Name}_第一次编辑";
            var editForm = new UserManagementPage.EditUserFormData
            {
                Account = form.Account,
                Name = firstEditName,
                Password = null,
                ConfirmPassword = null,
                Phone = BuildPhoneNumber(userConfig.PhonePrefix),
                Email = $"u{DateTime.Now:yyyyMMddHHmmss}@{userConfig.EmailDomain}"
            };

            await userPage.FillEditUserFormAsync(editForm);
            await userPage.ClickSaveAsync();

            var updateSuccess = await userPage.IsUpdateSuccessAsync(form.Account);
            if (!updateSuccess)
            {
                var shot = await Common.SafeSaveScreenshotAsync(page, "user_update_failed");
                throw new InvalidOperationException($"编辑失败：未出现'用户更新成功'提示，或列表中未找到更新后的用户。截图：{shot ?? "未生成"}");
            }

            Common.LogInfo("第一次编辑通过：用户更新成功。\n");

            // Step 9: 第二次编辑同一用户（输入新密码）。
            Common.LogInfo("步骤9：第二次编辑同一用户（输入新密码）");
            await userPage.OpenEditUserDialogByAccountAsync(form.Account);

            var newPassword = $"{userConfig.Password}9";
            var secondEditForm = new UserManagementPage.EditUserFormData
            {
                Account = form.Account,
                Name = $"{firstEditName}_第二次编辑",
                Password = newPassword,
                ConfirmPassword = newPassword,
                Phone = BuildPhoneNumber(userConfig.PhonePrefix),
                Email = $"p{DateTime.Now:yyyyMMddHHmmss}@{userConfig.EmailDomain}"
            };

            await userPage.FillEditUserFormAsync(secondEditForm);
            await userPage.ClickSaveAsync();

            var secondUpdateSuccess = await userPage.IsUpdateSuccessAsync(form.Account);
            if (!secondUpdateSuccess)
            {
                var shot = await Common.SafeSaveScreenshotAsync(page, "user_second_update_failed");
                throw new InvalidOperationException($"第二次编辑失败：未出现'用户更新成功'提示，或列表中未找到用户。截图：{shot ?? "未生成"}");
            }

            Common.LogInfo("第二次编辑通过：新密码已提交。\n");

            // Step 10: 使用新密码重新登录验证。
            Common.LogInfo("步骤10：使用新密码重新登录验证");
            await Common.SafeStopTraceAsync(context, "user_mgmt_admin_before_relogin", saveTrace: keepTraceOnSuccess);
            await context.CloseAsync();

            context = await browser.NewContextAsync();
            await Common.SafeStartTraceAsync(context);

            page = await context.NewPageAsync();
            page.SetDefaultTimeout(config.DefaultTimeout);

            var reloginPage = new LoginPage(page);
            await reloginPage.NavigateAsync(config.BaseUrl);
            await reloginPage.LoginAsync(form.Account, newPassword);

            var reloginSuccess = await reloginPage.IsLoginSuccessAsync();
            if (!reloginSuccess)
            {
                var shot = await Common.SafeSaveScreenshotAsync(page, "user_new_password_relogin_failed");
                throw new InvalidOperationException($"修改密码后重新登录失败。截图：{shot ?? "未生成"}");
            }

            Common.LogInfo("新密码重新登录通过。\n");

            // Step 11: 切回管理员并删除该用户（含二次确认）。
            Common.LogInfo("步骤11：切回管理员并删除该用户");
            await Common.SafeStopTraceAsync(context, "user_mgmt_new_password_verify", saveTrace: keepTraceOnSuccess);
            await context.CloseAsync();

            context = await browser.NewContextAsync();
            await Common.SafeStartTraceAsync(context);

            page = await context.NewPageAsync();
            page.SetDefaultTimeout(config.DefaultTimeout);

            var adminLoginPage = new LoginPage(page);
            await adminLoginPage.NavigateAsync(config.BaseUrl);
            await adminLoginPage.LoginAsync(config.Username, config.Password);

            var adminLoginSuccess = await adminLoginPage.IsLoginSuccessAsync();
            if (!adminLoginSuccess)
            {
                var shot = await Common.SafeSaveScreenshotAsync(page, "user_admin_relogin_for_delete_failed");
                throw new InvalidOperationException($"删除前管理员重新登录失败。截图：{shot ?? "未生成"}");
            }

            userPage = new UserManagementPage(page);
            await userPage.NavigateToUserManagementAsync();

            var deleted = await userPage.DeleteUserByAccountAsync(form.Account);
            if (!deleted)
            {
                Common.LogInfo("删除第1次失败，开始重试删除...");
                deleted = await userPage.DeleteUserByAccountAsync(form.Account);
            }

            if (!deleted)
            {
                var shot = await Common.SafeSaveScreenshotAsync(page, "user_delete_after_password_verify_failed");
                throw new InvalidOperationException($"删除失败：验证新密码登录后，仍未删除该用户。截图：{shot ?? "未生成"}");
            }

            Common.LogInfo("测试通过：修改密码后重新登录验证与删除流程全部成功。\n");
        }
        catch (Exception ex)
        {
            hasError = true;
            Common.LogInfo($"执行异常：{ex.Message}");

            if (page is not null)
            {
                var shot = await Common.SafeSaveScreenshotAsync(page, "user_mgmt_exception");
                Common.LogInfo($"异常截图已保存：{shot ?? "未生成"}");
            }

            throw;
        }
        finally
        {
            await Common.SafeStopTraceAsync(context, "user_mgmt_final", saveTrace: hasError || keepTraceOnSuccess);

            // 按要求保留浏览器窗口，便于人工复核结果。
            Common.LogInfo("用户管理测试执行结束。浏览器保持打开状态。");
        }
    }

    /// <summary>
    /// 生成 11 位手机号（13 开头），满足常见手机号格式校验。
    /// </summary>
    private static string BuildPhoneNumber(string phonePrefix)
    {
        // 号码总长度按 11 位处理，不足部分用时间戳补齐。
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
