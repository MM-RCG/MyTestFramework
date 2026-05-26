using MyTestFramework.Tests;
using Xunit;

namespace MyTestFramework.Specs;

/// <summary>
/// xUnit 测试包装类。
/// 作用：把现有 POM 风格的 Playwright 执行类暴露成 dotnet test 可发现的测试用例。
/// </summary>
public class PlaywrightScenariosTests
{
    /// <summary>
    /// 登录场景测试。
    /// </summary>
    [Fact(Skip = "按需单独运行；常规业务回归可直接跑各模块用例。")]
    public async Task Login_Test()
    {
        var test = new LoginTest();
        await test.RunAsync();
    }

    /// <summary>
    /// 新增用户场景测试。
    /// </summary>
    [Fact(Skip = "已并入 All_Modules_Single_Login_Test，避免重复登录。")]
    public async Task User_Create_Test()
    {
        var test = new UserManagementTest();
        await test.RunCreateUserAsync();
    }

    /// <summary>
    /// 编辑用户场景测试。
    /// </summary>
    [Fact(Skip = "已并入 All_Modules_Single_Login_Test，避免重复登录。")]
    public async Task User_Edit_Test()
    {
        var test = new UserManagementEditTest();
        await test.RunEditUserAsync();
    }

    /// <summary>
    /// 工作区管理场景测试。
    /// </summary>
    [Fact(Skip = "已并入 Non_User_Modules_Single_Login_Test，避免非用户模块重复登录。")]
    public async Task Workspace_Management_Test()
    {
        var test = new WorkspaceManagementTest();
        await test.RunWorkspaceCrudAsync();
    }

    /// <summary>
    /// 急停区域管理场景测试。
    /// </summary>
    [Fact(Skip = "已并入 Non_User_Modules_Single_Login_Test，避免非用户模块重复登录。")]
    public async Task Emergency_Stop_Area_Management_Test()
    {
        var test = new EmergencyStopAreaTest();
        await test.RunEmergencyStopAreaCrudAsync();
    }

    /// <summary>
    /// MCC设备状态监控场景测试。
    /// </summary>
    [Fact(Skip = "已并入 Non_User_Modules_Single_Login_Test，避免非用户模块重复登录。")]
    public async Task Mcc_Device_Status_Monitor_Test()
    {
        var test = new MccDeviceStatusMonitorTest();
        await test.RunMccDeviceCrudAsync();
    }

    /// <summary>
    /// 非用户模块单次 admin 登录串行执行（工作区、急停区域、MCC）。
    /// </summary>
    [Fact(Skip = "当前采用 All_Modules_Single_Login_Test：用户管理末次 admin 登录后串行执行后续模块。")]
    public async Task Non_User_Modules_Single_Login_Test()
    {
        var test = new AllModulesSingleLoginTest();
        await test.RunNonUserModulesAsync();
    }

    /// <summary>
    /// 单次 admin 登录串行执行所有模块场景。
    /// </summary>
    [Fact]
    public async Task All_Modules_Single_Login_Test()
    {
        var test = new AllModulesSingleLoginTest();
        await test.RunAsync();
    }
}
