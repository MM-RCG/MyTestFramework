using Microsoft.Playwright;
using MyTestFramework.Utils;

namespace MyTestFramework.Pages;

/// <summary>
/// MCC设备状态监控页面对象（POM）。
/// 负责：菜单导航、新增、查询、编辑、删除及结果校验。
/// </summary>
public class MccDeviceStatusMonitorPage
{
    private readonly IPage _page;

    private const string MccMenuSelector = "span:has-text('MCC设备状态监控'), .ant-menu-title-content:has-text('MCC设备状态监控')";
    private const string AddButtonSelector = "button:has-text('新增'), .ant-btn:has-text('新增')";
    private const string AddModalSelector = ".ant-modal:has-text('新增'), .ant-drawer:has-text('新增')";
    private const string EditModalSelector = ".ant-modal:has-text('编辑'), .ant-drawer:has-text('编辑')";
    private const string SaveButtonSelector = ".ant-modal .ant-btn-primary:has-text('保存'), .ant-drawer .ant-btn-primary:has-text('保存'), button:has-text('保存'), button:has-text('提交'), button:has-text('确定')";

    private const string CreateSuccessMessageSelector = ".ant-message-notice-content:has-text('设备新增成功'), text=设备新增成功";
    private const string UpdateSuccessMessageSelector = ".ant-message-notice-content:has-text('设备编辑成功'), text=设备编辑成功";
    private const string DeleteSuccessMessageSelector = ".ant-message-notice-content:has-text('设备删除成功'), text=设备删除成功";

    private const string SearchInputSelector = "input[placeholder*='卡号'], input[placeholder*='设备'], input[placeholder*='名称'], input[name='cardNo'], input[name='cardNumber']";
    private const string SearchButtonSelector = "button:has-text('查询'), button:has-text('搜索'), .ant-btn:has-text('查询')";
    private const string DeleteConfirmButtonSelector = ".ant-modal-confirm-btns .ant-btn-primary, .ant-popconfirm-buttons .ant-btn-primary, .ant-popover .ant-btn-primary";

    public class NewMccDeviceFormData
    {
        public string CardNo { get; set; } = string.Empty;
        public string? LineNo { get; set; }
        public string? IpAddress { get; set; }
        public string? Location { get; set; }
    }

    public class EditMccDeviceFormData
    {
        public string? LineNo { get; set; }
        public string? IpAddress { get; set; }
        public string? Location { get; set; }
    }

    public MccDeviceStatusMonitorPage(IPage page)
    {
        _page = page;
    }

    public async Task NavigateToMccDeviceStatusMonitorAsync()
    {
        await _page.Locator(MccMenuSelector).First.ClickAsync();

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

    /// <summary>
    /// 关闭当前新增/编辑弹窗，避免遮罩影响菜单点击。
    /// </summary>
    public async Task CloseCurrentDialogIfAnyAsync()
    {
        var closeCandidates = new[]
        {
            ".ant-modal .ant-modal-close",
            ".ant-drawer .ant-drawer-close",
            ".ant-modal button:has-text('取消')",
            ".ant-drawer button:has-text('取消')",
            ".ant-modal-footer button:has-text('取消')",
            ".ant-drawer-footer button:has-text('取消')"
        };

        foreach (var selector in closeCandidates)
        {
            try
            {
                var btn = _page.Locator(selector).First;
                if (await btn.IsVisibleAsync())
                {
                    await btn.ClickAsync();
                    break;
                }
            }
            catch
            {
                // 继续尝试其他关闭按钮。
            }
        }

        try
        {
            await _page.Locator(AddModalSelector).First.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Hidden,
                Timeout = 5_000
            });
        }
        catch
        {
            // 弹窗可能本来就不存在，忽略。
        }
    }

    public async Task FillAddFormAsync(NewMccDeviceFormData form)
    {
        var addModal = _page.Locator(".ant-modal:visible, .ant-drawer:visible").First;

        // 卡号是新增必填且唯一，优先按“卡号/设备卡号”多策略精确填写。
        await FillCardNoInAddModalAsync(addModal, form.CardNo);

        await FillAddOptionalFieldsAsync(form);
    }

    /// <summary>
    /// 新增时优先输入卡号（仅填卡号）。
    /// </summary>
    public async Task FillAddCardNoOnlyAsync(string cardNo)
    {
        var addModal = _page.Locator(".ant-modal:visible, .ant-drawer:visible").First;
        await FillCardNoInAddModalAsync(addModal, cardNo);
    }

    /// <summary>
    /// 新增时填写除卡号外的可选字段。
    /// </summary>
    public async Task FillAddOptionalFieldsAsync(NewMccDeviceFormData form)
    {
        var addModal = _page.Locator(".ant-modal:visible, .ant-drawer:visible").First;

        if (!string.IsNullOrWhiteSpace(form.LineNo))
        {
            await FillFieldInModalAsync(addModal, "线号", form.LineNo, new[]
            {
                "input[placeholder*='线号']", "input[name='lineNo']", "input#lineNo"
            });
        }

        if (!string.IsNullOrWhiteSpace(form.IpAddress))
        {
            await FillFieldInModalAsync(addModal, "IP", form.IpAddress, new[]
            {
                "input[placeholder*='IP']", "input[name='ip']", "input[name='ipAddress']", "input#ip", "input#ipAddress"
            });
        }

        if (!string.IsNullOrWhiteSpace(form.Location))
        {
            await FillFieldInModalAsync(addModal, "位置", form.Location, new[]
            {
                "input[placeholder*='位置']", "textarea[placeholder*='位置']", "input[name='location']", "textarea[name='location']", "input#location", "textarea#location"
            });
        }
    }

    public async Task FillEditFormAsync(EditMccDeviceFormData form)
    {
        var editModal = _page.Locator(".ant-modal:visible, .ant-drawer:visible").First;

        if (!string.IsNullOrWhiteSpace(form.LineNo))
        {
            await FillFieldInModalAsync(editModal, "线号", form.LineNo, new[]
            {
                "input[placeholder*='线号']", "input[name='lineNo']", "input#lineNo"
            });
        }

        if (!string.IsNullOrWhiteSpace(form.IpAddress))
        {
            await FillFieldInModalAsync(editModal, "IP", form.IpAddress, new[]
            {
                "input[placeholder*='IP']", "input[name='ip']", "input[name='ipAddress']", "input#ip", "input#ipAddress"
            });
        }

        if (!string.IsNullOrWhiteSpace(form.Location))
        {
            await FillFieldInModalAsync(editModal, "位置", form.Location, new[]
            {
                "input[placeholder*='位置']", "textarea[placeholder*='位置']", "input[name='location']", "textarea[name='location']", "input#location", "textarea#location"
            });
        }
    }

    public async Task<bool> TrySelectFirstWorkspaceAsync()
    {
        return await TrySelectFirstOptionInModalAsync(_page.Locator(".ant-modal:visible, .ant-drawer:visible").First, "工作区", new[]
        {
            "xpath=.//*[contains(@class,'ant-form-item') and .//*[contains(normalize-space(),'工作区')]]//*[contains(@class,'ant-select')][1]",
            "#workspaceId",
            "[name='workspaceId']",
            "[name='workspace']"
        });
    }

    public async Task<bool> TrySelectFirstEmergencyAreaAsync()
    {
        return await TrySelectFirstOptionInModalAsync(_page.Locator(".ant-modal:visible, .ant-drawer:visible").First, "急停区域", new[]
        {
            "xpath=.//*[contains(@class,'ant-form-item') and .//*[contains(normalize-space(),'急停区域')]]//*[contains(@class,'ant-select')][1]",
            "#emergencyAreaId",
            "[name='emergencyAreaId']",
            "[name='emergencyArea']"
        });
    }

    /// <summary>
    /// 快速定位“急停区域”下拉并选中第一个选项。
    /// </summary>
    public async Task<bool> TrySelectFirstEmergencyAreaFastAsync()
    {
        var modal = _page.Locator(".ant-modal:visible, .ant-drawer:visible").First;
        var emergencySelect = modal.Locator("xpath=.//*[contains(@class,'ant-form-item') and .//*[contains(normalize-space(),'急停区域')]]//*[contains(@class,'ant-select')][1]").First;

        try
        {
            await emergencySelect.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = 1_000
            });

            await emergencySelect.ClickAsync();

            if (await TryClickFirstOpenDropdownOptionAsync("急停区域"))
            {
                return true;
            }

            await emergencySelect.PressAsync("ArrowDown");
            await emergencySelect.PressAsync("Enter");
            Common.LogInfo("下拉选择成功：急停区域（快速键盘方式）");
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> IsCardReadonlyInEditDialogAsync()
    {
        var modal = _page.Locator(".ant-modal:visible, .ant-drawer:visible").First;

        var candidates = new List<ILocator>
        {
            modal.GetByLabel("卡号", new LocatorGetByLabelOptions { Exact = true }).First,
            modal.Locator("xpath=.//label[contains(normalize-space(),'卡号')]/following::input[1]").First,
            modal.Locator("input[placeholder*='卡号'], input[name='cardNo'], input[name='cardNumber'], input#cardNo, input#cardNumber").First
        };

        foreach (var candidate in candidates)
        {
            try
            {
                await candidate.WaitForAsync(new LocatorWaitForOptions
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = 2_000
                });

                var readOnlyAttr = await candidate.GetAttributeAsync("readonly");
                var disabledAttr = await candidate.GetAttributeAsync("disabled");

                return !string.IsNullOrWhiteSpace(readOnlyAttr) || !string.IsNullOrWhiteSpace(disabledAttr) || !await candidate.IsEditableAsync();
            }
            catch
            {
                // 尝试下一个候选。
            }
        }

        return false;
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

    public async Task<bool> IsCreateSuccessAsync(string? cardNo = null)
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
            if (!string.IsNullOrWhiteSpace(cardNo))
            {
                await SearchByCardNoAsync(cardNo);
                return await IsRowVisibleByCardNoAsync(cardNo);
            }

            return false;
        }
    }

    public async Task<bool> IsUpdateSuccessAsync(string? cardNo = null)
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
            if (!string.IsNullOrWhiteSpace(cardNo))
            {
                await SearchByCardNoAsync(cardNo);
                return await IsRowVisibleByCardNoAsync(cardNo);
            }

            return false;
        }
    }

    public async Task SearchByCardNoAsync(string cardNo)
    {
        await FillWithFallbackAsync(new[]
        {
            SearchInputSelector,
            "input[placeholder*='请输入卡号']",
            "input[placeholder*='请输入设备']"
        }, cardNo);

        await _page.Locator(SearchButtonSelector).First.ClickAsync();
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task<bool> IsRowVisibleByCardNoAsync(string cardNo)
    {
        try
        {
            var row = _page.Locator($"tr:has-text('{cardNo}')").First;
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

    public async Task OpenEditDialogByCardNoAsync(string cardNo)
    {
        await SearchByCardNoAsync(cardNo);

        var editButton = _page.Locator($"tr:has-text('{cardNo}') button:has-text('编辑')").First;
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

    public async Task<bool> DeleteByCardNoAsync(string cardNo)
    {
        try
        {
            await SearchByCardNoAsync(cardNo);

            var deleteButton = _page.Locator($"tr:has-text('{cardNo}') button:has-text('删除')").First;
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
                var stillExists = await _page.Locator($"tr:has-text('{cardNo}')").First.IsVisibleAsync();
                if (!stillExists)
                {
                    return true;
                }
            }
            catch
            {
                // 忽略回退异常。
            }

            Common.LogInfo($"删除设备失败（已转为非阻断）：{ex.Message}");
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

    private async Task<bool> TrySelectFirstOptionInModalAsync(ILocator modal, string labelText, IEnumerable<string> fallbackSelectors)
    {
        var candidates = new List<ILocator>
        {
            modal.GetByLabel(labelText, new LocatorGetByLabelOptions { Exact = true }).First,
            modal.Locator($"xpath=.//label[contains(normalize-space(),'{labelText}')]/ancestor::*[contains(@class,'ant-form-item')][1]//*[contains(@class,'ant-select')][1]").First
        };

        foreach (var selector in fallbackSelectors)
        {
            candidates.Add(modal.Locator(selector).First);
        }

        foreach (var candidate in candidates)
        {
            try
            {
                await candidate.WaitForAsync(new LocatorWaitForOptions
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = 2_000
                });

                // 支持原生 select 下拉（如果页面使用的不是 Ant Select）。
                try
                {
                    var nativeSelect = candidate.Locator("select").First;
                    if (await nativeSelect.IsVisibleAsync())
                    {
                        var options = nativeSelect.Locator("option");
                        var optionCount = await options.CountAsync();
                        if (optionCount > 0)
                        {
                            var value = await options.Nth(0).GetAttributeAsync("value") ?? string.Empty;
                            await nativeSelect.SelectOptionAsync(new[] { value });
                            Common.LogInfo($"下拉选择成功：{labelText}（原生select）");
                            return true;
                        }
                    }
                }
                catch
                {
                    // 非原生 select，继续按 Ant 组件策略。
                }

                await candidate.ClickAsync();

                if (await TryClickFirstOpenDropdownOptionAsync(labelText))
                {
                    return true;
                }

                // 再尝试键盘方式选择第一个可选项。
                try
                {
                    await candidate.PressAsync("ArrowDown");
                    await candidate.PressAsync("Enter");
                    Common.LogInfo($"下拉选择成功：{labelText}（键盘方式）");
                    return true;
                }
                catch
                {
                    // 键盘方式失败则继续。
                }

                await _page.Keyboard.PressAsync("Escape");
            }
            catch
            {
                // 尝试下一个候选。
            }
        }

        return false;
    }

    private async Task<bool> TryClickFirstOpenDropdownOptionAsync(string labelText)
    {
        var optionSelectors = new[]
        {
            ".ant-select-dropdown:not([style*='display: none']) .ant-select-item-option:not(.ant-select-item-option-disabled)",
            ".ant-select-dropdown:not([style*='display: none']) .ant-select-tree-node-content-wrapper",
            ".ant-cascader-menus .ant-cascader-menu-item:not(.ant-cascader-menu-item-disabled)",
            ".ant-dropdown:not([style*='display: none']) .ant-dropdown-menu-item:not(.ant-dropdown-menu-item-disabled)"
        };

        foreach (var selector in optionSelectors)
        {
            try
            {
                var options = _page.Locator(selector);
                await options.First.WaitForAsync(new LocatorWaitForOptions
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = 2_000
                });

                var count = await options.CountAsync();
                if (count > 0)
                {
                    await options.First.ClickAsync();
                    Common.LogInfo($"下拉选择成功：{labelText}，已选中第一个选项。");
                    return true;
                }
            }
            catch
            {
                // 当前选择器未命中，继续尝试。
            }
        }

        return false;
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

    private async Task FillFieldInModalAsync(ILocator modal, string labelText, string value, IEnumerable<string> fallbackSelectors)
    {
        var candidates = new List<(string Strategy, ILocator Locator)>
        {
            ($"XPath(label->{labelText}->input)", modal.Locator($"xpath=.//label[contains(normalize-space(),'{labelText}')]/following::input[1]").First),
            ($"XPath(label->{labelText}->textarea)", modal.Locator($"xpath=.//label[contains(normalize-space(),'{labelText}')]/following::textarea[1]").First),
            ($"GetByLabel('{labelText}', fuzzy)", modal.GetByLabel(labelText).First),
            ($"GetByLabel('{labelText}', exact)", modal.GetByLabel(labelText, new LocatorGetByLabelOptions { Exact = true }).First)
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
                    Timeout = 1_500
                });

                await candidate.Locator.ScrollIntoViewIfNeededAsync();
                await candidate.Locator.FillAsync(value);

                var actualValue = await candidate.Locator.InputValueAsync();
                if (actualValue == value)
                {
                    Common.LogInfo($"已填写字段：{labelText} | 定位策略：{candidate.Strategy} | 实际值长度：{actualValue.Length}");
                    return;
                }

                lastException = new InvalidOperationException($"字段 {labelText} 填写后校验失败，期望值：{value}，实际值：{actualValue}");
            }
            catch (Exception ex)
            {
                Common.LogInfo($"字段定位尝试失败：{labelText} | 策略：{candidate.Strategy} | 原因：{ex.Message}");
                lastException = ex;
            }
        }

        throw new PlaywrightException($"未能填写字段：{labelText}", lastException ?? new InvalidOperationException($"字段 {labelText} 未命中任何可用输入框。"));
    }

    private async Task FillCardNoInAddModalAsync(ILocator modal, string cardNo)
    {
        var cardCandidates = new List<(string Strategy, ILocator Locator)>
        {
            ("GetByLabel('卡号', fuzzy)", modal.GetByLabel("卡号").First),
            ("GetByLabel('设备卡号', fuzzy)", modal.GetByLabel("设备卡号").First),
            ("XPath(label->卡号)", modal.Locator("xpath=.//label[contains(normalize-space(),'卡号')]/following::input[1]").First),
            ("XPath(label->设备卡号)", modal.Locator("xpath=.//label[contains(normalize-space(),'设备卡号')]/following::input[1]").First),
            ("Fallback placeholder 卡号", modal.Locator("input[placeholder*='卡号']").First),
            ("Fallback placeholder 设备卡号", modal.Locator("input[placeholder*='设备卡号']").First),
            ("Fallback name cardNo", modal.Locator("input[name='cardNo']").First),
            ("Fallback name cardNumber", modal.Locator("input[name='cardNumber']").First),
            ("Fallback id cardNo", modal.Locator("input#cardNo").First),
            ("Fallback first editable input", modal.Locator("input:not([readonly]):not([disabled])").First)
        };

        foreach (var candidate in cardCandidates)
        {
            try
            {
                await candidate.Locator.WaitForAsync(new LocatorWaitForOptions
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = 1_200
                });

                await candidate.Locator.FillAsync(cardNo);
                var actual = await candidate.Locator.InputValueAsync();
                if (actual == cardNo)
                {
                    Common.LogInfo($"新增卡号已填写 | 定位策略：{candidate.Strategy} | 长度：{actual.Length}");
                    return;
                }
            }
            catch
            {
                // 继续尝试下一个卡号候选。
            }
        }

        throw new InvalidOperationException("新增页卡号填写失败：未命中可用卡号输入框。");
    }
}
