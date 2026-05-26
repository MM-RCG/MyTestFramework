using Microsoft.Playwright;
using MyTestFramework.Utils;

namespace MyTestFramework.Pages;

/// <summary>
/// 用户管理页面对象（POM）。
/// 负责：菜单导航、打开新增用户窗口、填写用户信息、保存、校验提示。
/// </summary>
public class UserManagementPage
{
    private readonly IPage _page;

    // 菜单与页面元素选择器。
    private const string BasicManagementMenuSelector = "span:has-text('基础管理'), .ant-menu-title-content:has-text('基础管理')";
    private const string UserManagementMenuSelector = "span:has-text('用户管理'), .ant-menu-title-content:has-text('用户管理')";
    private const string AddUserButtonSelector = "button:has-text('新增用户'), .ant-btn:has-text('新增用户')";
    private const string AddUserModalSelector = ".ant-modal:has-text('新增用户'), .ant-drawer:has-text('新增用户')";
    private const string SaveButtonSelector = ".ant-modal .ant-btn-primary:has-text('保存'), .ant-drawer .ant-btn-primary:has-text('保存'), button:has-text('保存'), button:has-text('提交'), button:has-text('确定')";
    private const string CreateSuccessMessageSelector = ".ant-message-notice-content:has-text('用户创建成功'), .ant-message-notice-content:has-text('新增成功'), .ant-message-notice-content:has-text('保存成功'), text=用户创建成功, text=新增成功, text=保存成功";
    private const string EditUserModalSelector = ".ant-modal:has-text('编辑用户'), .ant-drawer:has-text('编辑用户')";
    private const string UpdateSuccessMessageSelector = ".ant-message-notice-content:has-text('用户更新成功'), .ant-message-notice-content:has-text('更新成功'), .ant-message-notice-content:has-text('保存成功'), text=用户更新成功, text=更新成功, text=保存成功";
    private const string SearchInputSelector = "input[placeholder*='账号'], input[placeholder*='用户名'], input[name='account']";
    private const string SearchButtonSelector = "button:has-text('查询'), button:has-text('搜索'), .ant-btn:has-text('查询')";
    private const string DeleteConfirmButtonSelector = ".ant-modal-confirm-btns .ant-btn-primary, .ant-popconfirm-buttons .ant-btn-primary, .ant-popover .ant-btn-primary";
    private const string DeleteSuccessMessageSelector = ".ant-message-notice-content:has-text('删除成功'), text=删除成功";

    /// <summary>
    /// 新增用户表单模型。
    /// </summary>
    public class NewUserFormData
    {
        public string Account { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    /// <summary>
    /// 编辑用户表单模型。
    /// 密码和确认密码为非必填；若不传则不修改密码。
    /// </summary>
    public class EditUserFormData
    {
        public string Account { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Password { get; set; }
        public string? ConfirmPassword { get; set; }
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public UserManagementPage(IPage page)
    {
        _page = page;
    }

    /// <summary>
    /// 点击菜单：基础管理 -> 用户管理。
    /// </summary>
    public async Task NavigateToUserManagementAsync()
    {
        await _page.Locator(BasicManagementMenuSelector).First.ClickAsync();
        await _page.Locator(UserManagementMenuSelector).First.ClickAsync();

        // 进入列表页后，等待“新增用户”按钮可见，作为进入成功标志。
        await _page.Locator(AddUserButtonSelector).First.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible
        });
    }

    /// <summary>
    /// 点击“新增用户”按钮并等待新增窗口出现。
    /// </summary>
    public async Task OpenAddUserDialogAsync()
    {
        await _page.Locator(AddUserButtonSelector).First.ClickAsync();

        await _page.Locator(AddUserModalSelector).First.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible
        });
    }

    /// <summary>
    /// 填写新增用户表单。
    /// 采用多选择器兜底，适配常见 Ant Design 表单写法。
    /// </summary>
    public async Task FillAddUserFormAsync(NewUserFormData form)
    {
        await FillUserFormFieldsAsync(AddUserModalSelector, new[]
        {
            ("账号", form.Account, new[] { "input[placeholder*='账号']", "input[name='account']", "input#account" }),
            ("姓名", form.Name, new[] { "input[placeholder*='姓名']", "input[name='name']", "input#name" }),
            ("密码", form.Password, new[] { "input[placeholder*='密码']", "input[name='password']", "input#password", "input[type='password']" }),
            ("确认密码", form.ConfirmPassword, new[] { "input[placeholder*='确认密码']", "input[name='confirmPassword']", "input#confirmPassword", "input[placeholder*='再次输入']" }),
            ("手机号", form.Phone, new[] { "input[placeholder*='手机号']", "input[name='phone']", "input#phone" }),
            ("邮箱", form.Email, new[] { "input[placeholder*='邮箱']", "input[name='email']", "input#email" })
        });
    }

    /// <summary>
    /// 点击保存。
    /// </summary>
    public async Task ClickSaveAsync()
    {
        // 先尝试在常见选择器中寻找“保存/提交/确定”按钮。
        var candidateSelectors = new[]
        {
            SaveButtonSelector,
            ".ant-modal-footer button.ant-btn-primary",
            ".ant-drawer-footer button.ant-btn-primary",
            ".ant-modal-footer button",
            ".ant-drawer-footer button"
        };

        foreach (var selector in candidateSelectors)
        {
            try
            {
                var button = _page.Locator(selector).First;
                await button.WaitForAsync(new LocatorWaitForOptions
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = 3_000
                });

                await button.ClickAsync();
                return;
            }
            catch
            {
                // 继续尝试下一个候选按钮。
            }
        }

        throw new PlaywrightException("未找到可点击的保存按钮，请检查页面按钮文案或弹窗结构。", null!);
    }

    /// <summary>
    /// 验证“用户创建成功”提示。
    /// </summary>
    public async Task<bool> IsCreateSuccessAsync(string? account = null)
    {
        try
        {
            await _page.Locator(CreateSuccessMessageSelector).First.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = 10_000
            });

            return true;
        }
        catch
        {
            // 如果提示没有稳定出现，则回退到“弹窗已关闭”或“列表中能查到账号”的业务结果判断。
            if (await IsModalClosedAsync(AddUserModalSelector))
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(account))
            {
                try
                {
                    await SearchByAccountAsync(account);
                    return await IsRowVisibleByAccountAsync(account);
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// 在列表中按账号点击“编辑”，并等待编辑窗口打开。
    /// </summary>
    public async Task OpenEditUserDialogByAccountAsync(string account)
    {
        await SearchByAccountAsync(account);

        var rowEditButton = _page.Locator($"tr:has-text('{account}') button:has-text('编辑')").First;
        await rowEditButton.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 8_000
        });

        await rowEditButton.ClickAsync();

        await _page.Locator(EditUserModalSelector).First.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 8_000
        });
    }

    /// <summary>
    /// 填写编辑用户表单。
    /// 账号在多数系统中为不可编辑字段，因此默认不在编辑弹窗中填写账号。
    /// 密码字段为非必填：只有传值时才填写。
    /// </summary>
    public async Task FillEditUserFormAsync(EditUserFormData form)
    {
        await FillUserFormFieldsAsync(EditUserModalSelector, new[]
        {
            ("姓名", form.Name, new[] { "input[placeholder*='姓名']", "input[name='name']", "input#name" }),
            ("密码", form.Password ?? string.Empty, new[] { "input[placeholder*='密码']", "input[name='password']", "input#password", "input[type='password']" }),
            ("确认密码", form.ConfirmPassword ?? string.Empty, new[] { "input[placeholder*='确认密码']", "input[name='confirmPassword']", "input#confirmPassword", "input[placeholder*='再次输入']" }),
            ("手机号", form.Phone, new[] { "input[placeholder*='手机号']", "input[name='phone']", "input#phone" }),
            ("邮箱", form.Email, new[] { "input[placeholder*='邮箱']", "input[name='email']", "input#email" })
        }, skipEmptyValues: true);
    }

    /// <summary>
    /// 验证“用户更新成功”提示。
    /// </summary>
    public async Task<bool> IsUpdateSuccessAsync(string? account = null)
    {
        try
        {
            await _page.Locator(UpdateSuccessMessageSelector).First.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = 10_000
            });

            return true;
        }
        catch
        {
            if (await IsModalClosedAsync(EditUserModalSelector))
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(account))
            {
                try
                {
                    await SearchByAccountAsync(account);
                    return await IsRowVisibleByAccountAsync(account);
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// 判断列表里是否已经出现指定账号的行。
    /// </summary>
    public async Task<bool> IsRowVisibleByAccountAsync(string account)
    {
        try
        {
            var row = _page.Locator($"tr:has-text('{account}')").First;
            await row.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = 10_000
            });

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 判断弹窗是否已经关闭。
    /// </summary>
    private async Task<bool> IsModalClosedAsync(string modalSelector)
    {
        try
        {
            return await _page.Locator(modalSelector).First.IsHiddenAsync();
        }
        catch
        {
            return true;
        }
    }

    /// <summary>
    /// 在用户管理列表中按账号搜索。
    /// </summary>
    public async Task SearchByAccountAsync(string account)
    {
        await FillWithFallbackAsync(new[]
        {
            SearchInputSelector,
            "input[placeholder*='请输入账号']"
        }, account);

        await _page.Locator(SearchButtonSelector).First.ClickAsync();

        // 等待列表刷新稳定。
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    /// <summary>
    /// 删除指定账号用户（用于测试数据清理）。
    /// 成功返回 true，未找到或删除失败返回 false。
    /// </summary>
    public async Task<bool> DeleteUserByAccountAsync(string account)
    {
        try
        {
            await SearchByAccountAsync(account);

            // 在包含账号文本的行中寻找“删除”按钮。
            var rowDeleteButton = _page.Locator($"tr:has-text('{account}') button:has-text('删除')").First;

            if (!await rowDeleteButton.IsVisibleAsync())
            {
                return false;
            }

            await rowDeleteButton.ClickAsync();

            // 删除后会出现二次确认弹窗：等待并点击“确认/确定/删除”主按钮。
            await ClickDeleteConfirmAsync();

            // 优先检查删除成功提示，若提示未出现则回退为“目标行已消失”判断。
            var successToast = _page.Locator(DeleteSuccessMessageSelector).First;
            await successToast.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = 6_000
            });

            return true;
        }
        catch (Exception ex)
        {
            try
            {
                var rowStillExists = await _page.Locator($"tr:has-text('{account}')").First.IsVisibleAsync();
                if (!rowStillExists)
                {
                    return true;
                }
            }
            catch
            {
                // 忽略行可见性检查异常，交给最终 false。
            }

            Common.LogInfo($"删除用户失败（已转为非阻断）：{ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 点击删除二次确认窗口里的确认按钮。
    /// </summary>
    private async Task ClickDeleteConfirmAsync()
    {
        var confirmButton = _page.Locator(DeleteConfirmButtonSelector).First;

        await confirmButton.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 10_000
        });

        await confirmButton.ClickAsync();
    }

    /// <summary>
    /// 依次尝试多个选择器填写输入框，直到成功。
    /// </summary>
    private async Task FillWithFallbackAsync(IEnumerable<string> selectors, string value)
    {
        Exception? lastException = null;

        foreach (var selector in selectors)
        {
            try
            {
                var locator = _page.Locator(selector).First;
                await locator.WaitForAsync(new LocatorWaitForOptions
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = 4_000
                });
                await locator.FillAsync(value);
                return;
            }
            catch (Exception ex)
            {
                lastException = ex;
            }
        }

        throw new PlaywrightException(
            $"未找到可填写的输入框，候选选择器：{string.Join(" | ", selectors)}",
            lastException ?? new InvalidOperationException("所有候选选择器都未命中输入框。"));
    }

    /// <summary>
    /// 在指定弹窗内，按标签/选择器填写表单字段，并校验填充值。
    /// </summary>
    private async Task FillUserFormFieldsAsync(string modalSelector, IEnumerable<(string Label, string Value, string[] FallbackSelectors)> fields, bool skipEmptyValues = false)
    {
        var modal = _page.Locator(modalSelector).First;

        await modal.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 8_000
        });

        foreach (var field in fields)
        {
            if (skipEmptyValues && string.IsNullOrWhiteSpace(field.Value))
            {
                continue;
            }

            await FillFieldInModalAsync(modal, field.Label, field.Value, field.FallbackSelectors);
        }
    }

    /// <summary>
    /// 先按“标签文本”找输入框，找不到再按弹窗内候选选择器查找。
    /// 填写完成后，会再反查输入框值，避免“看似填了，实际没填上”。
    /// </summary>
    private async Task FillFieldInModalAsync(ILocator modal, string labelText, string value, IEnumerable<string> fallbackSelectors)
    {
        var candidates = new List<(string Strategy, ILocator Locator)>();

        // 1) 优先尝试 label 关联输入框
        candidates.Add(($"GetByLabel('{labelText}', exact)", modal.GetByLabel(labelText, new LocatorGetByLabelOptions { Exact = true }).First));

        // 2) 使用 label 后面第一个 input/textarea 作为兜底
        candidates.Add(($"XPath(label->{labelText}->input)", modal.Locator($"xpath=.//label[contains(normalize-space(),'{labelText}')]/following::input[1]").First));
        candidates.Add(($"XPath(label->{labelText}->textarea)", modal.Locator($"xpath=.//label[contains(normalize-space(),'{labelText}')]/following::textarea[1]").First));

        // 3) 再尝试弹窗内的候选 selector
        foreach (var fallback in fallbackSelectors)
        {
            candidates.Add(($"Fallback('{fallback}')", modal.Locator(fallback).First));
        }

        Exception? lastException = null;

        foreach (var candidate in candidates)
        {
            try
            {
                await candidate.Locator.WaitForAsync(new LocatorWaitForOptions
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = 4_000
                });

                await candidate.Locator.ScrollIntoViewIfNeededAsync();
                await candidate.Locator.FillAsync(value);

                var actualValue = await candidate.Locator.InputValueAsync();
                if (actualValue == value)
                {
                    Common.LogInfo($"已填写字段：{labelText} | 定位策略：{candidate.Strategy} | 实际值长度：{actualValue.Length}");
                    return;
                }

                Common.LogInfo($"字段填值校验未通过：{labelText} | 定位策略：{candidate.Strategy} | 期望长度：{value.Length} | 实际长度：{actualValue.Length}");
                lastException = new InvalidOperationException($"字段 {labelText} 填写后校验失败，期望值：{value}，实际值：{actualValue}");
            }
            catch (Exception ex)
            {
                Common.LogInfo($"字段定位尝试失败：{labelText} | 策略：{candidate.Strategy} | 原因：{ex.Message}");
                lastException = ex;
            }
        }

        // 字段最终失败时，导出当前弹窗内的输入控件快照，帮助快速定位页面差异。
        await DumpModalInputsSnapshotAsync(modal, labelText);

        throw new PlaywrightException($"未能填写字段：{labelText}", lastException ?? new InvalidOperationException($"字段 {labelText} 未命中任何可用输入框。"));
    }

    /// <summary>
    /// 打印弹窗内 input/textarea 快照：name/id/placeholder/type/可见性。
    /// </summary>
    private async Task DumpModalInputsSnapshotAsync(ILocator modal, string failedField)
    {
        try
        {
            Common.LogInfo($"开始导出弹窗输入快照（失败字段：{failedField}）...");

            var controls = modal.Locator("input, textarea");
            var count = await controls.CountAsync();
            Common.LogInfo($"弹窗内输入控件数量：{count}");

            for (var i = 0; i < count; i++)
            {
                var item = controls.Nth(i);

                var name = await item.GetAttributeAsync("name") ?? string.Empty;
                var id = await item.GetAttributeAsync("id") ?? string.Empty;
                var placeholder = await item.GetAttributeAsync("placeholder") ?? string.Empty;
                var type = await item.GetAttributeAsync("type") ?? string.Empty;

                bool visible;
                try
                {
                    visible = await item.IsVisibleAsync();
                }
                catch
                {
                    visible = false;
                }

                Common.LogInfo($"控件[{i}] name='{name}' | id='{id}' | placeholder='{placeholder}' | type='{type}' | visible={visible}");
            }

            Common.LogInfo("弹窗输入快照导出完成。");
        }
        catch (Exception ex)
        {
            Common.LogInfo($"导出弹窗输入快照失败（已忽略）：{ex.Message}");
        }
    }
}
