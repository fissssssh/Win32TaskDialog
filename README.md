# Win32TaskDialog

[![NuGet](https://img.shields.io/nuget/v/Win32TaskDialog.svg)](https://www.nuget.org/packages/Win32TaskDialog)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

基于 **Win32 TaskDialog API**(comctl32 v6,Windows Vista+)的消息框库,供其他 .NET 项目引用。
相比经典 MessageBox,它具有现代的 Windows 原生外观,支持自定义按钮、命令链接、验证复选框、
"详细信息"展开区、页脚、超时与进度条,而**无需任何 WPF/WinForms 依赖**。

- 直接调用原生 `TaskDialogIndirect`,外观与系统一致(含深色模式、高 DPI);
- 同时提供 `string` / `IntPtr` 扩展方法:`Confirm(...)` 与 `ShowProgress(...)`;
- 另有 Avalonia 集成包,自动解析父窗口句柄并在后台线程显示,不阻塞 UI 渲染。

## 平台要求

- `netstandard2.0` 目标:.NET Framework 4.6.1+ / .NET Core 2.0+ / 现代 .NET 均可引用
- `net8.0-windows` 目标:面向现代 .NET 8 项目
- 运行时:Windows Vista+;宿主需加载 comctl32 v6
  - 控制台/无 UI 依赖的宿主请在后缀为 `.winmanifest`/`app.manifest` 中显式声明
    `Microsoft.Windows.Common-Controls` 6.0 依赖(见下文示例);
    WinForms/WPF 项目默认清单已包含,可直接使用;
  - 非 Windows 平台调用会抛出 `PlatformNotSupportedException`。

<details>
<summary>无清单宿主的最小 app.manifest</summary>

```xml
<?xml version="1.0" encoding="utf-8"?>
<assembly manifestVersion="1.0" xmlns="urn:schemas-microsoft-com:asm.v1">
  <dependency>
    <dependentAssembly>
      <assemblyIdentity type="win32"
        name="Microsoft.Windows.Common-Controls"
        version="6.0.0.0" processorArchitecture="*"
        publicKeyToken="6595b64144ccf1df" language="*" />
    </dependentAssembly>
  </dependency>
</assembly>
```

在 csproj 中引用:`<ApplicationManifest>app.manifest</ApplicationManifest>`。
</details>

## 安装

```bash
dotnet add package Win32TaskDialog
```

Avalonia 项目请改用集成包(它会传递依赖核心库):

```bash
dotnet add package Win32TaskDialog.Avalonia
```

| 包 | 目标框架 | 依赖 |
| --- | --- | --- |
| `Win32TaskDialog` | netstandard2.0 + net8.0-windows | 无 |
| `Win32TaskDialog.Avalonia` | net8.0 | `Win32TaskDialog`(同版本)+ `Avalonia` 12.0.0 |

## 快速开始

```csharp
using Win32TaskDialog;

// 最简
TaskDialog.Show("一个简单的消息框");

// 带按钮组合、图标、默认按钮、复选框
TaskDialog.Show("确定要放弃未保存的修改吗?", new TaskDialogOptions
{
    Text = "当前文档包含未保存的修改。",
    Title = "未保存的修改",
    Icon = TaskDialogIcon.Warning,
    Buttons = TaskDialogButtons.YesNoCancel,
    DefaultButton = TaskDialogButton.No,
    VerificationText = "不再询问我",
});
```

## 扩展方法:Confirm / 进度条

```csharp
// Confirm —— 返回 bool
bool ok = "确认要部署到生产环境吗?".Confirm(
    title: "部署确认",
    icon: TaskDialogIcon.Shield,
    instruction: "该操作将重启服务");

// 进度条 —— 阻塞显示,工作在后台线程执行
TaskDialogResult result = "正在批量处理文件…".ShowProgress((progress, token) =>
{
    for (int i = 1; i <= 100; i++)
    {
        token.ThrowIfCancellationRequested(); // 点"取消"后工作收到取消信号
        progress.Report(i);                   // 0-100
        progress.SetText($"已处理 {i} / 100 个文件");
        Thread.Sleep(50);
    }
});
// 返回 TaskDialogResult.Ok / Cancel;工作抛异常时原样抛回调用线程
```

## API 一览

| 入口 | 说明 |
| --- | --- |
| `TaskDialog.Show(message)` | 最简对话框(系统自动 OK 按钮) |
| `TaskDialog.Show(message, title)` | 带标题 |
| `TaskDialog.Show(options)` | 完整配置 |
| `string.Confirm(...)` / `IntPtr.Confirm(...)` | Yes/No 确认,返回 `bool` |
| `string.ShowProgress(...)` / `IntPtr.ShowProgress(...)` | 进度条对话框(支持取消、Marquee、异常传播) |

`TaskDialogOptions` 支持:标题、主文本(Instruction)、内容、图标、预定义按钮组合
(`OkCancel` / `YesNo` / `AbortRetryIgnore` 等)、自定义按钮(`CustomButtons`,文本内可用换行做命令链接)、
默认按钮、验证复选框、展开区、页脚、`UseCommandLinks`、`Cancelable`、`CanBeMinimized`、
`Timeout`、`MinimumWidth` 与 `OwnerHandle`(父窗口 HWND)。

按钮文字使用英文内置(如 OK / Cancel / Yes),需要本地化时请使用 `CustomButtons` 自定义文字。

## 在 Avalonia 中使用

Avalonia 项目推荐引用集成包 `Win32TaskDialog.Avalonia`(net8.0,Avalonia 12+):
扩展方法挂在 `TopLevel`(窗口)上,自动以该窗口为父窗口、在后台线程显示原生对话框,
UI 线程保持响应。

```csharp
using Win32TaskDialog;           // 选项与枚举类型
using Win32TaskDialog.Avalonia;  // 扩展方法

// 基础:自动解析 HWND 作为 OwnerHandle
TaskDialogResult r = await this.ShowAsync(new TaskDialogOptions { Title = "Hello", Text = "来自 Avalonia 的消息" });
bool ok = await this.ConfirmAsync("确定要删除吗?", title: "确认", instruction: "该操作不可撤销。");

// 进度条:直接 await;取消令牌与异常语义和核心库一致
TaskDialogResult progress = await this.ShowProgressAsync("正在处理…", (p, token) =>
{
    for (int i = 0; i <= 100; i++)
    {
        token.ThrowIfCancellationRequested();
        p.Report(i);
        Thread.Sleep(30);
    }
});
```

| 扩展方法 | 说明 |
| --- | --- |
| `TopLevel.GetWin32Handle()` / `Control.GetWin32Handle()` | 解析窗口 HWND;非 Win32 后端返回 `IntPtr.Zero` |
| `ShowAsync(message, title?)` / `ShowAsync(options)` | 异步消息框;`options.OwnerHandle` 未设置时自动填充 |
| `ConfirmAsync(...)` | 异步 Yes/No 确认,返回 `Task<bool>` |
| `ShowProgressAsync(...)` | 异步进度条对话框(含取消、Marquee、异常传播) |

所有扩展方法需在 **UI 线程调用**(仅做句柄解析与参数准备,随后立即异步返回;
HWND 必须在 UI 线程解析,Avalonia 对象不可跨线程访问)。

> **注意**:Avalonia 应用的默认清单**不含** common-controls v6 依赖,需要手动补充
> (见上文"无清单宿主的最小 app.manifest");[`samples/AvaloniaSampleApp/app.manifest`](https://github.com/fissssssh/Win32TaskDialog/blob/main/samples/AvaloniaSampleApp/app.manifest)
> 是一个完整示例。

### 不使用集成包

也可以只引用核心库自行封装:

```csharp
private IntPtr WindowHandle => TryGetPlatformHandle()?.Handle ?? IntPtr.Zero; // 须在 UI 线程解析

var result = await Task.Run(() => TaskDialog.Show(new TaskDialogOptions
{
    OwnerHandle = WindowHandle,
    Text = "来自 Avalonia 的消息",
}));
```

## 构建与测试

用 Visual Studio / Rider 打开根目录的 `Win32TaskDialog.sln`,或使用 CLI:

```bash
dotnet build Win32TaskDialog.sln   # 构建全部
dotnet run --project tests/Smoke   # 自动化冒烟测试(会短暂弹出原生对话框后自动点击)
```

仓库另含控制台与 Avalonia 两个示例项目,可直接运行查看效果:

```bash
dotnet run --project samples/SampleApp          # 控制台宿主,覆盖核心与扩展 API
dotnet run --project samples/AvaloniaSampleApp  # Avalonia 桌面应用

# Avalonia 示例支持无人值守自检(自动弹出 3 秒超时对话框并验证返回值)
AVALONIA_SMOKE=1 dotnet run --project samples/AvaloniaSampleApp
```

## 贡献

欢迎提交 Issue 与 Pull Request。

1. Fork 本仓库并从 `main` 切出特性分支;
2. 保持与现有代码风格一致(中文注释、半角标点),新增公开 API 请补齐 XML 文档注释;
3. 涉及对话框行为的改动,请运行 `tests/Smoke` 冒烟测试确认未回归;
4. 提交 PR 时说明改动动机与验证方式。

若发现 Bug,请在 Issue 中附上 Windows 版本、目标框架与最小复现步骤。

## 许可证

本项目采用 [MIT 许可证](LICENSE)。
