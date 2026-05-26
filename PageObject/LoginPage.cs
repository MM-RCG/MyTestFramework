using Microsoft.Playwright;

namespace MyTestFramework.Pages;

/// <summary>
/// 登录页面对象（POM）。
/// 只负责：页面元素定位 + 页面操作，不放测试断言逻辑。
/// </summary>
public class LoginPage
{
    private readonly IPage _page;

    // 登录页面常用元素选择器。
    // 如果后续页面改版，只需要在这里修改一次。
    private const string UsernameSelector = "input[name='username'], input#username, input[placeholder*='账号'], input[placeholder*='用户名']";
    private const string PasswordSelector = "input[type='password'][name='password'], input#password, input[type='password'][placeholder*='密码'], input[type='password']";
    private const string LoginButtonSelector = "button:has-text('登录'), button:has-text('Login'), input[type='submit'][value='登录'], input[type='submit'], button[type='submit'], button.ant-btn-primary, .ant-btn-primary, button#login, .login-btn, button";

    // 登录成功判断元素A：页面出现“欢迎”文字。
    private const string WelcomeSelector = "text=欢迎";

    // 登录成功判断元素B：你提供的 Ant Design 用户头像结构。
    private const string AvatarSelector = "span.ant-avatar.ant-avatar-circle.ant-avatar-icon:has(span[role='img'][aria-label='user'])";

    public LoginPage(IPage page)
    {
        _page = page;
    }

    /// <summary>
    /// 打开登录页面。
    /// </summary>
    /// <param name="url">登录地址</param>
    public async Task NavigateAsync(string url)
    {
        const int maxAttempts = 3;
        const int retryDelayMs = 2_000;

        Exception? lastException = null;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await _page.GotoAsync(url, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.DOMContentLoaded,
                    Timeout = 20_000
                });

                return;
            }
            catch (Exception ex)
            {
                lastException = ex;

                if (attempt == maxAttempts)
                {
                    break;
                }

                Console.WriteLine($"[重试] 打开登录页失败，第 {attempt} 次重试中：{ex.Message}");
                await Task.Delay(retryDelayMs);
            }
        }

        throw new PlaywrightException($"打开登录页失败，已重试 {maxAttempts} 次。", lastException ?? new InvalidOperationException("登录页访问失败。"));
    }

    /// <summary>
    /// 执行登录动作：输入账号密码并点击登录。
    /// </summary>
    public async Task LoginAsync(string username, string password)
    {
        await _page.FillAsync(UsernameSelector, username);
        await _page.FillAsync(PasswordSelector, password);

        // 优先点击最可能的登录按钮；如果页面结构和预期不一致，则退回到回车提交。
        try
        {
            await _page.Locator(LoginButtonSelector).First.ClickAsync();
        }
        catch
        {
            await _page.Locator(PasswordSelector).First.PressAsync("Enter");
        }
    }

    /// <summary>
    /// 判断是否登录成功。
    /// 条件：出现“欢迎”文本 或 出现用户头像。
    /// </summary>
    /// <returns>成功返回 true；失败返回 false。</returns>
    public async Task<bool> IsLoginSuccessAsync()
    {
        // 不强制等待 networkidle，避免某些页面持续请求导致长时间卡住。
        // 这里直接检查欢迎信息/头像是否出现，更适合登录后跳转场景。
        var hasWelcome = await IsVisibleSafelyAsync(WelcomeSelector);
        var hasAvatar = await IsVisibleSafelyAsync(AvatarSelector);

        return hasWelcome || hasAvatar;
    }

    /// <summary>
    /// 安全判断元素是否可见；元素不存在时返回 false，不抛异常。
    /// </summary>
    private async Task<bool> IsVisibleSafelyAsync(string selector)
    {
        try
        {
            return await _page.Locator(selector).First.IsVisibleAsync();
        }
        catch
        {
            return false;
        }
    }
}
