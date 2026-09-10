# Win32TaskDialog

基于 **Win32 TaskDialog API**(comctl32 v6,Windows Vista+)的消息框库,供其他 .NET 项目引用。
相比经典 MessageBox,它具有现代的 Windows 原生外观,支持自定义按钮、命令链接、
验证复选框、"详细信息"展开区、页脚、超时与进度条,而无需任何 WPF/WinForms 依赖。

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

## 实现说明

- 核心为 `TaskDialogIndirect` P/Invoke(comctl32.dll v6,SDK 26100 头文件核对);
- `TASKDIALOGCONFIG` / `TASKDIALOG_BUTTON` 均以 `Pack = 1` 声明(与 COMCTL32 头文件的
  `<pshpack1.h>` 一致)——这是 x64 下布局正确性的关键,已在运行时验证;
- 结构体使用现行 SDK 布局(含 `dwCommonButtons`,本库恒为 0,按钮一律经 `pButtons` 数组);
- 图标走 `pszMainIcon` 预定义值(`TD_*_ICON`),Question 图标经 `LoadIcon(IDI_QUESTION)` + `TDF_USE_HICON_MAIN`;
- 超时与进度均通过 `TDF_CALLBACK_TIMER` 的 `TDN_TIMER` 轮询实现(约 200ms 一次);
- 进度条消息:`TDM_SET_PROGRESS_BAR_POS` / `TDM_SET_PROGRESS_BAR_RANGE` / `TDM_SET_ELEMENT_TEXT`;
- 程序化关闭(超时/完成)使用 `TDM_CLICK_BUTTON` 点击真实存在的按钮,结果由托管层按状态
  重新映射为 Ok/Cancel/Timeout/Error。注意:`TDM_RETURN_VALUE`(0x0231,旧版消息)
  在现代 comctl32 上已失效(经原生探针验证),故未使用。

## 在 Avalonia 中使用(Win32TaskDialog.Avalonia)

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

注意:Avalonia 应用的默认清单**不含** common-controls v6 依赖,需要手动补充
(见上文"无清单宿主的最小 app.manifest"),`samples/AvaloniaSampleApp/app.manifest` 是一个完整示例。

## 解决方案结构

用 Visual Studio / Rider 打开根目录的 `Win32TaskDialog.sln`,或使用 CLI:

```bash
dotnet build Win32TaskDialog.sln   # 构建全部
dotnet run --project tests/Smoke               # 自动化冒烟测试(会短暂弹出原生对话框后自动点击)
```

```
Win32TaskDialog.sln
src/Win32TaskDialog/                 # 核心类库(netstandard2.0 + net8.0-windows)
src/Win32TaskDialog.Avalonia/        # Avalonia 12 集成包(net8.0)
samples/SampleApp/                   # 控制台示例
samples/AvaloniaSampleApp/           # Avalonia 12 桌面应用示例
tests/Smoke/                         # 自动化冒烟测试(FindWindow + TDM_CLICK_BUTTON 驱动)
tools/Probe/                         # 开发期验证工具(布局/关闭机制的诊断探针)
```

## 版本与打包

版本号由 [Nerdbank.GitVersioning](https://github.com/dotnet/Nerdbank.GitVersioning)(NBGV)
从 git 提交历史自动推导,配置见根目录 `version.json`(`"version": "1.0.0-beta.{height}"`,
`{height}` 随每次提交自动递增):

- **main 分支 / `v*` 标签**上构建 → 干净的预发布版本,如 `1.0.0-beta-0002`;
- **其他分支**上构建 → 自动附加提交号后缀(如 `1.0.0-beta-0002-g1a2b3c4`),便于区分;
- **发布正式版**:把 `version.json` 改为 `"1.0.0"`(或 `"1.1.0"` 等)提交后重新打包即可。

打包(输出到 `artifacts/`):

```bash
dotnet pack Win32TaskDialog.sln -c Release -o artifacts
```

| 包 | 目标框架 | 依赖 |
| --- | --- | --- |
| `Win32TaskDialog` | netstandard2.0 + net8.0-windows | 无 |
| `Win32TaskDialog.Avalonia` | net8.0 | `Win32TaskDialog`(同版本)+ `Avalonia` 12.1.x |

推送到 NuGet.org:

```bash
dotnet nuget push "artifacts/*.nupkg" --api-key <API_KEY> --source https://api.nuget.org/v3/index.json
```

## 样例

| 样例 | 说明 |
| --- | --- |
| `samples/SampleApp` | 控制台宿主(`dotnet run --project samples/SampleApp`),覆盖核心与扩展 API |
| `samples/AvaloniaSampleApp` | Avalonia 12 桌面应用(`dotnet run --project samples/AvaloniaSampleApp`),通过 `Win32TaskDialog.Avalonia` 集成包演示全部对话框 |

Avalonia 示例支持无人值守自检(自动弹出 3 秒超时对话框并验证返回值):

```bash
AVALONIA_SMOKE=1 dotnet run --project samples/AvaloniaSampleApp
```
