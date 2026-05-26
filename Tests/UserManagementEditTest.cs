using Microsoft.Playwright;
using MyTestFramework.Pages;
using MyTestFramework.Utils;

namespace MyTestFramework.Tests;

/// <summary>
/// 用户管理测试用例：编辑用户。
/// 测试目标：
/// 1. 登录成功。
/// 2. 进入“基础管理-用户管理”。
/// 3. 点击“编辑”打开编辑窗口。
/// 4. 输入账号、姓名、密码（非必填）、确认密码（非必填）、手机号、邮箱。
/// 5. 点击保存。
/// 6. 验证提示“用户更新成功”。
/// </summary>
public class UserManagementEditTest
{
    /// <summary>
    /// 执行编辑用户测试。
    /// 说明：
    /// - 为保证可重复运行，先创建一个临时用户，再编辑该用户。
    /// - 编辑成功后会尝试删除测试数据，避免环境污染。
    /// </summary>
    public async Task RunEditUserAsync()
    {
        AppConfig config = Common.LoadConfig();
        UserManagementConfig userConfig = Common.LoadUserManagementConfig();

        IBrowser? browser = null;
        IBrowserContext? context = null;
        IPage? page = null;
        var hasError = false;
        var keepTraceOnSuccess = Common.ShouldKeepTraceOnSuccess();

        // oldAccount：用于点击“编辑”前定位旧记录。
        // updatedAccount：编辑后新账号，便于后续清理。
        string? oldAccount = null;
        string? updatedAccount = null;

        try
        {
            Common.LogInfo("开始执行用户管理-编辑用户测试...");

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
                var shot = await Common.SafeSaveScreenshotAsync(page, "user_edit_login_failed");
                throw new InvalidOperationException($"登录失败，无法继续执行编辑用户测试。截图：{shot ?? "未生成"}");
            }

            var userPage = new UserManagementPage(page);

            // Step 2: 点击菜单基础管理-用户管理。
            Common.LogInfo("步骤2：点击菜单'基础管理-用户管理'");
            await userPage.NavigateToUserManagementAsync();

            // 前置数据：创建一个可编辑的测试用户。
            // 这样不依赖环境里已有固定账号，避免脚本偶发失败。
            var baseSuffix = DateTime.Now.ToString("yyyyMMddHHmmss");
            oldAccount = $"{userConfig.AccountPrefix}_edit_{baseSuffix}";

            var createForm = new UserManagementPage.NewUserFormData
            {
                Account = oldAccount,
                Name = $"{userConfig.NamePrefix}编辑前",
                Password = userConfig.Password,
                ConfirmPassword = userConfig.Password,
                Phone = BuildPhoneNumber(userConfig.PhonePrefix),
                Email = $"e{baseSuffix}@{userConfig.EmailDomain}"
            };

            Common.LogInfo("前置步骤：创建待编辑用户");
            await userPage.OpenAddUserDialogAsync();
            await userPage.FillAddUserFormAsync(createForm);
            await userPage.ClickSaveAsync();

            var createOk = await userPage.IsCreateSuccessAsync(createForm.Account);
            if (!createOk)
            {
                var shot = await Common.SafeSaveScreenshotAsync(page, "user_edit_prepare_create_failed");
                throw new InvalidOperationException($"前置创建用户失败，无法继续编辑测试。截图：{shot ?? "未生成"}");
            }

            // Step 3: 进入列表后点击“编辑”按钮，打开编辑用户窗口。
            Common.LogInfo("步骤3：点击'编辑'按钮，打开编辑用户窗口");
            await userPage.OpenEditUserDialogByAccountAsync(oldAccount);

            // Step 4: 输入编辑后的字段值。
            // 密码与确认密码是非必填，这里按你的要求演示“填写新密码”。
            var updatedSuffix = DateTime.Now.ToString("yyyyMMddHHmmss");
            updatedAccount = oldAccount;

            var editForm = new UserManagementPage.EditUserFormData
            {
                Account = string.Empty,
                Name = $"{userConfig.NamePrefix}编辑后",
                Password = userConfig.Password,
                ConfirmPassword = userConfig.Password,
                Phone = BuildPhoneNumber(userConfig.PhonePrefix),
                Email = $"u{updatedSuffix}@{userConfig.EmailDomain}"
            };

            Common.LogInfo("步骤4：填写编辑用户表单");
            await userPage.FillEditUserFormAsync(editForm);

            // Step 5: 点击保存。
            Common.LogInfo("步骤5：点击保存");
            await userPage.ClickSaveAsync();

            // Step 6: 验证“用户更新成功”。
            Common.LogInfo("步骤6：验证提示'用户更新成功'");
            var updateSuccess = await userPage.IsUpdateSuccessAsync(updatedAccount);

            if (!updateSuccess)
            {
                var shot = await Common.SafeSaveScreenshotAsync(page, "user_edit_failed");
                throw new InvalidOperationException($"未出现'用户更新成功'提示，或列表中未找到更新后的用户。截图：{shot ?? "未生成"}");
            }

            Common.LogInfo("测试通过：用户更新成功。");
            Common.LogInfo($"编辑前账号：{oldAccount}");
            Common.LogInfo($"编辑后账号：{updatedAccount}");

            // 清理测试数据：删除编辑后的账号。
            if (userConfig.CleanupCreatedUser && !string.IsNullOrWhiteSpace(updatedAccount))
            {
                Common.LogInfo("步骤7：清理测试数据（删除编辑后的测试用户）");
                var deleted = await userPage.DeleteUserByAccountAsync(updatedAccount);

                if (deleted)
                {
                    Common.LogInfo("清理完成：已删除编辑后的测试用户。");
                }
                else
                {
                    var firstShot = await Common.SafeSaveScreenshotAsync(page, "user_edit_cleanup_retry1_failed");
                    Common.LogInfo($"清理第1次失败，已截图：{firstShot ?? "未生成"}");

                    Common.LogInfo("开始第2次重试删除...");
                    var retryDeleted = await userPage.DeleteUserByAccountAsync(updatedAccount);

                    if (retryDeleted)
                    {
                        Common.LogInfo("清理完成：第2次重试删除成功。");
                    }
                    else
                    {
                        var secondShot = await Common.SafeSaveScreenshotAsync(page, "user_edit_cleanup_retry2_failed");
                        Common.LogInfo($"清理失败：第2次重试仍未删除，已截图：{secondShot ?? "未生成"}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            hasError = true;
            Common.LogInfo($"执行异常：{ex.Message}");

            if (page is not null)
            {
                var shot = await Common.SafeSaveScreenshotAsync(page, "user_edit_exception");
                Common.LogInfo($"异常截图已保存：{shot ?? "未生成"}");
            }

            throw;
        }
        finally
        {
            await Common.SafeStopTraceAsync(context, "user_edit_test", saveTrace: hasError || keepTraceOnSuccess);

            // 让测试宿主自己回收浏览器资源，避免收尾阶段重复关闭导致异常。

            Common.LogInfo("编辑用户测试执行结束。");
        }
    }

    /// <summary>
    /// 生成 11 位手机号，前缀可配置，尾部用时间戳补齐。
    /// </summary>
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
