using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Win32TaskDialog;
using Win32TaskDialog.Avalonia;

namespace AvaloniaSampleApp;

/// <summary>
/// 演示 Win32TaskDialog.Avalonia 集成包:扩展方法挂在 <see cref="TopLevel"/>(窗口)上,
/// 自动以本窗口为父窗口、在后台线程显示原生对话框,UI 线程保持响应。
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // 自检模式(AVALONIA_SMOKE=1):窗口打开后自动弹出一个 3 秒超时关闭的对话框,
        // 结果输出到 stdout 并退出应用。用于无人值守验证集成链路
        // (comctl32 v6 清单、HWND 解析、后台线程调用)。
        if (Environment.GetEnvironmentVariable("AVALONIA_SMOKE") == "1")
            Opened += (_, _) => RunSelfTestAsync();
    }

    private void SetResult(string text) => ResultText.Text = text;

    private async void RunSelfTestAsync()
    {
        IntPtr hwnd = this.GetWin32Handle();
        Console.WriteLine($"SELFTEST hwnd=0x{hwnd.ToInt64():X} (非零表示成功获取窗口句柄)");

        TaskDialogResult result = await this.ShowAsync(new TaskDialogOptions
        {
            Title = "Avalonia 自检",
            Instruction = "对话框将 3 秒后自动关闭",
            Text = "用于无人值守验证,无需人工操作。",
            Buttons = TaskDialogButtons.Ok,
            Timeout = TimeSpan.FromSeconds(3),
        });

        Console.WriteLine($"SELFTEST result={result} expected={TaskDialogResult.Timeout}");
        Close(); // 关闭主窗口,应用随之退出
    }

    // =====================================================================
    // 基础
    // =====================================================================

    private async void OnSimpleClick(object? sender, RoutedEventArgs e)
    {
        // this 是 Window(继承 TopLevel),扩展方法自动解析 HWND 作为父窗口,
        // 并在后台线程显示对话框 —— 无需手写 Task.Run / TryGetPlatformHandle。
        TaskDialogResult result = await this.ShowAsync(new TaskDialogOptions
        {
            Title = "Hello",
            Text = "来自 Avalonia 的一条简单消息。",
        });

        SetResult($"[简单消息] 返回 {result}");
    }

    private async void OnButtonsClick(object? sender, RoutedEventArgs e)
    {
        TaskDialogResult result = await this.ShowAsync(new TaskDialogOptions
        {
            Title = "未保存的修改",
            Instruction = "确定要放弃未保存的修改吗?",
            Text = "放弃后,当前文档的修改将无法恢复。",
            Icon = TaskDialogIcon.Warning,
            Buttons = TaskDialogButtons.YesNoCancel,
            DefaultButton = TaskDialogButton.No,
            Cancelable = true,
        });

        SetResult($"[按钮组合] 返回 {result}");
    }

    private async void OnConfirmClick(object? sender, RoutedEventArgs e)
    {
        bool confirmed = await this.ConfirmAsync(
            "确定要部署到生产环境吗?",
            title: "部署确认",
            icon: TaskDialogIcon.Shield,
            instruction: "该操作将重启服务,期间服务不可用。");

        SetResult($"[Confirm] 用户选择: {(confirmed ? "Yes → true" : "No → false")}");
    }

    // =====================================================================
    // 完整选项
    // =====================================================================

    private async void OnAdvancedClick(object? sender, RoutedEventArgs e)
    {
        TaskDialogResult result = await this.ShowAsync(new TaskDialogOptions
        {
            Title = "磁盘空间不足",
            Instruction = "请选择下一步操作",
            Text = "检测到 C: 盘剩余空间仅 1.2 GB。",
            Icon = TaskDialogIcon.Warning,
            UseCommandLinks = true, // 自定义按钮渲染为命令链接(文字里用 \n 分隔主文本与说明)
            CustomButtons = new[]
            {
                new TaskDialogButton(100, "立即清理\n自动清理回收站与临时文件"),
                new TaskDialogButton(101, "稍后处理\n3 天后再次提醒"),
                TaskDialogButton.Cancel,
            },
            DefaultButton = new TaskDialogButton(100, "立即清理"),
            VerificationText = "不再提示此问题",
            VerificationChecked = true,
            ExpandedInformation = "详细日志:系统建议 C: 盘至少保留 10 GB 可用空间。",
            FooterText = "© Win32TaskDialog 示例",
        });

        SetResult($"[完整选项] 返回 {result}(自定义按钮 100 = 立即清理,101 = 稍后处理)");
    }

    private async void OnTimeoutClick(object? sender, RoutedEventArgs e)
    {
        TaskDialogResult result = await this.ShowAsync(new TaskDialogOptions
        {
            Title = "超时演示",
            Instruction = "该对话框将在 5 秒后自动关闭",
            Text = "超过 5 秒未操作时,返回 TaskDialogResult.Timeout。",
            Buttons = TaskDialogButtons.Ok,
            Timeout = TimeSpan.FromSeconds(5),
        });

        SetResult($"[超时] 返回 {result}");
    }

    // =====================================================================
    // 进度条
    // =====================================================================

    private async void OnProgressClick(object? sender, RoutedEventArgs e)
    {
        TaskDialogResult result = await this.ShowProgressAsync(
            "正在处理文件…",
            (progress, token) =>
            {
                for (int i = 0; i <= 100; i++)
                {
                    token.ThrowIfCancellationRequested();
                    progress.Report(i);                    // 0-100,驱动原生进度条
                    progress.SetText($"已完成 {i} / 100"); // 更新内容文本
                    Thread.Sleep(30);
                }
            },
            new TaskDialogProgressOptions
            {
                Title = "处理文件",
                Instruction = "正在处理文件…",
                ShowCancelButton = false, // 不可取消的确定性进度
            });

        SetResult($"[进度条] 返回 {result}(工作完成后自动关闭)");
    }

    private async void OnCancellableProgressClick(object? sender, RoutedEventArgs e)
    {
        TaskDialogResult result = await this.ShowProgressAsync(
            "正在执行一个可取消的长任务…",
            (progress, token) =>
            {
                for (int i = 0; i < 600; i++)
                {
                    // 用户点击 Cancel(或按 ESC)后,这里会抛出 OperationCanceledException,
                    // 工作委托随即退出,对话框返回 TaskDialogResult.Cancel。
                    token.ThrowIfCancellationRequested();
                    progress.Report(i / 6.0);
                    progress.SetText($"累计处理 {i} 项");
                    Thread.Sleep(20);
                }
            },
            new TaskDialogProgressOptions
            {
                Title = "可取消任务",
                Instruction = "正在执行一个可取消的长任务…",
                ShowCancelButton = true,
            });

        SetResult($"[可取消进度] 返回 {result}");
    }

    private async void OnMarqueeClick(object? sender, RoutedEventArgs e)
    {
        TaskDialogResult result = await this.ShowProgressAsync(
            "正在连接服务器…",
            (progress, token) =>
            {
                for (int i = 0; i < 30; i++) // 模拟约 3 秒的不确定等待
                {
                    token.ThrowIfCancellationRequested();
                    Thread.Sleep(100);
                }
            },
            new TaskDialogProgressOptions
            {
                Title = "连接中",
                Instruction = "正在连接服务器…",
                Marquee = true, // 不确定进度(循环滚动)
            });

        SetResult($"[不确定进度] 返回 {result}");
    }
}
