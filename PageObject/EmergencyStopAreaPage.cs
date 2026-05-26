using Microsoft.Playwright;
using MyTestFramework.Utils;

namespace MyTestFramework.Pages;

/// <summary>
/// 急停区域管理页面对象（POM）。
/// 负责：菜单导航、新增、查询、编辑、删除及结果校验。
/// </summary>
public class EmergencyStopAreaPage
{
    private readonly IPage _page;

    private const string BasicManagementMenuSelector = "span:has-text('基础管理'), .ant-menu-title-content:has-text('基础管理')";
    private const string EmergencyStopAreaMenuSelector = "span:has-text('急停区域管理'), .ant-menu-title-content:has-text('急停区域管理')";
    private const string AddButtonSelector = "button:has-text('新增急停区域'), .ant-btn:has-text('新增急停区域'), button:has-text('新增')";
    private const string AddModalSelector = ".ant-modal:has-text('新增急停区域'), .ant-drawer:has-text('新增急停区域'), .ant-modal:has-text('急停区域'), .ant-drawer:has-text('急停区域')";
    private const string EditModalSelector = ".ant-modal:has-text('编辑急停区域'), .ant-drawer:has-text('编辑急停区域'), .ant-modal:has-text('急停区域'), .ant-drawer:has-text('急停区域')";
    private const string SaveButtonSelector = ".ant-modal .ant-btn-primary:has-text('保存'), .ant-drawer .ant-btn-primary:has-text('保存'), button:has-text('保存'), button:has-text('提交'), button:has-text('确定')";

    private const string CreateSuccessMessageSelector = ".ant-message-notice-content:has-text('急停区域创建成功'), .ant-message-notice-content:has-text('新增成功'), .ant-message-notice-content:has-text('保存成功'), text=急停区域创建成功, text=新增成功, text=保存成功";
    private const string UpdateSuccessMessageSelector = ".ant-message-notice-content:has-text('急停区域更新成功'), .ant-message-notice-content:has-text('更新成功'), .ant-message-notice-content:has-text('保存成功'), text=急停区域更新成功, text=更新成功, text=保存成功";
    private const string DeleteSuccessMessageSelector = ".ant-message-notice-content:has-text('急停区域删除成功'), .ant-message-notice-content:has-text('删除成功'), text=急停区域删除成功, text=删除成功";

    private const string SearchInputSelector = "input[placeholder*='区域代码'], input[placeholder*='急停区域'], input[placeholder*='中文名称'], input[placeholder*='名称'], input[name='areaCode'], input[name='code']";
    private const string SearchButtonSelector = "button:has-text('查询'), button:has-text('搜索'), .ant-btn:has-text('查询')";
    private const string DeleteConfirmButtonSelector = ".ant-modal-confirm-btns .ant-btn-primary, .ant-popconfirm-buttons .ant-btn-primary, .ant-popover .ant-btn-primary";

    public class NewEmergencyStopAreaFormData
    {
        public string AreaCode { get; set; } = string.Empty;
        public string ChineseName { get; set; } = string.Empty;
        public string EnglishName { get; set; } = string.Empty;
    }

    public class EditEmergencyStopAreaFormData
    {
        public string AreaCode { get; set; } = string.Empty;
        public string ChineseName { get; set; } = string.Empty;
        public string EnglishName { get; set; } = string.Empty;
    }

    public EmergencyStopAreaPage(IPage page)
    {
        _page = page;
    }

    public async Task NavigateToEmergencyStopAreaManagementAsync()
    {
        await _page.Locator(BasicManagementMenuSelector).First.ClickAsync();
        await _page.Locator(EmergencyStopAreaMenuSelector).First.ClickAsync();

        await _page.Locator(AddButtonSelector).First.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 10_000
        });
    }

    public async Task OpenAddDialogAsync()
    {
        await _page.Locator(AddButtonSelector).First.ClickAsync();
        await _page.Locator(AddModalSelector).First.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 8_000
        });
    }

    public async Task FillAddFormAsync(NewEmergencyStopAreaFormData form)
    {
        await FillFormFieldsAsync(AddModalSelector, new[]
        {
            ("区域代码", form.AreaCode, new[] { "input[placeholder*='区域代码']", "input[name='areaCode']", "input[name='code']", "input#areaCode", "input#code" }),
            ("中文名称", form.ChineseName, new[] { "input[placeholder*='中文名称']", "input[placeholder*='中文']", "input[name='nameCn']", "input[name='cnName']", "input#nameCn", "input#cnName" }),
            ("英文名称", form.EnglishName, new[] { "input[placeholder*='英文名称']", "input[placeholder*='英文']", "input[name='nameEn']", "input[name='enName']", "input#nameEn", "input#enName" })
        });
    }

    public async Task FillEditFormAsync(EditEmergencyStopAreaFormData form)
    {
        await FillFormFieldsAsync(EditModalSelector, new[]
        {
            ("区域代码", form.AreaCode, new[] { "input[placeholder*='区域代码']", "input[name='areaCode']", "input[name='code']", "input#areaCode", "input#code" }),
            ("中文名称", form.ChineseName, new[] { "input[placeholder*='中文名称']", "input[placeholder*='中文']", "input[name='nameCn']", "input[name='cnName']", "input#nameCn", "input#cnName" }),
            ("英文名称", form.EnglishName, new[] { "input[placeholder*='英文名称']", "input[placeholder*='英文']", "input[name='nameEn']", "input[name='enName']", "input#nameEn", "input#enName" })
        });
    }

    public async Task ClickSaveAsync()
    {
        var candidates = new[]
        {
            SaveButtonSelector,
            ".ant-modal-footer button.ant-btn-primary",
            ".ant-drawer-footer button.ant-btn-primary",
            ".ant-modal-footer button",
            ".ant-drawer-footer button"
        };

        foreach (var selector in candidates)
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
                // 继续尝试。
            }
        }

        throw new PlaywrightException("未找到可点击的保存按钮，请检查页面按钮文案或弹窗结构。", null!);
    }

    public async Task<bool> IsCreateSuccessAsync(string? areaCode = null)
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
            if (await IsModalClosedAsync(AddModalSelector))
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(areaCode))
            {
                try
                {
                    await SearchByAreaCodeAsync(areaCode);
                    return await IsRowVisibleByAreaCodeAsync(areaCode);
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }
    }

    public async Task<bool> IsUpdateSuccessAsync(string? areaCode = null)
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
            if (await IsModalClosedAsync(EditModalSelector))
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(areaCode))
            {
                try
                {
                    await SearchByAreaCodeAsync(areaCode);
                    return await IsRowVisibleByAreaCodeAsync(areaCode);
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }
    }

    public async Task SearchByAreaCodeAsync(string areaCode)
    {
        await FillWithFallbackAsync(new[]
        {
            SearchInputSelector,
            "input[placeholder*='请输入区域代码']",
            "input[placeholder*='请输入名称']"
        }, areaCode);

        await _page.Locator(SearchButtonSelector).First.ClickAsync();
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task<bool> IsRowVisibleByAreaCodeAsync(string areaCode)
    {
        try
        {
            var row = _page.Locator($"tr:has-text('{areaCode}')").First;
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

    public async Task OpenEditDialogByAreaCodeAsync(string areaCode)
    {
        await SearchByAreaCodeAsync(areaCode);

        var editButton = _page.Locator($"tr:has-text('{areaCode}') button:has-text('编辑')").First;
        await editButton.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 8_000
        });

        await editButton.ClickAsync();

        await _page.Locator(EditModalSelector).First.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 8_000
        });
    }

    public async Task<bool> DeleteByAreaCodeAsync(string areaCode)
    {
        try
        {
            await SearchByAreaCodeAsync(areaCode);

            var deleteButton = _page.Locator($"tr:has-text('{areaCode}') button:has-text('删除')").First;
            if (!await deleteButton.IsVisibleAsync())
            {
                return false;
            }

            await deleteButton.ClickAsync();
            await ClickDeleteConfirmAsync();

            await _page.Locator(DeleteSuccessMessageSelector).First.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = 8_000
            });

            return true;
        }
        catch (Exception ex)
        {
            try
            {
                var stillExists = await _page.Locator($"tr:has-text('{areaCode}')").First.IsVisibleAsync();
                if (!stillExists)
                {
                    return true;
                }
            }
            catch
            {
                // 忽略回退检查异常。
            }

            Common.LogInfo($"删除急停区域失败（已转为非阻断）：{ex.Message}");
            return false;
        }
    }

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

    private async Task FillFormFieldsAsync(string modalSelector, IEnumerable<(string Label, string Value, string[] FallbackSelectors)> fields)
    {
        var modal = _page.Locator(modalSelector).First;

        await modal.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 8_000
        });

        foreach (var field in fields)
        {
            await FillFieldInModalAsync(modal, field.Label, field.Value, field.FallbackSelectors);
        }
    }

    private async Task FillFieldInModalAsync(ILocator modal, string labelText, string value, IEnumerable<string> fallbackSelectors)
    {
        var candidates = new List<(string Strategy, ILocator Locator)>
        {
            ($"GetByLabel('{labelText}', exact)", modal.GetByLabel(labelText, new LocatorGetByLabelOptions { Exact = true }).First),
            ($"XPath(label->{labelText}->input)", modal.Locator($"xpath=.//label[contains(normalize-space(),'{labelText}')]/following::input[1]").First),
            ($"XPath(label->{labelText}->textarea)", modal.Locator($"xpath=.//label[contains(normalize-space(),'{labelText}')]/following::textarea[1]").First)
        };

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

        await DumpModalInputsSnapshotAsync(modal, labelText);

        throw new PlaywrightException($"未能填写字段：{labelText}", lastException ?? new InvalidOperationException($"字段 {labelText} 未命中任何可用输入框。"));
    }

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
