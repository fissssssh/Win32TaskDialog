using System;
using System.Threading;
using Win32TaskDialog;

namespace SampleApp
{
    /// <summary>
    /// 演示 Win32TaskDialog 库的用法。
    /// 此示例为控制台应用,对话框由 Win32 TaskDialog 呈现(单窗口无 WPF/WinForms 依赖)。
    /// </summary>
    internal static class Program
    {
        private static void Main()
        {
            Console.WriteLine("===== Win32TaskDialog 示例 =====");

            // ---- 1. 最简用法:系统自动 OK 按钮,标题为应用程序名 ----
            TaskDialog.Show("一个简单的消息框,点击 OK 关闭。");
            Console.WriteLine("=> 简单 Show 完成");

            // ---- 2. 带标题与按钮组合 ----
            TaskDialogResult r1 = TaskDialog.Show(new TaskDialogOptions
            {
                Text = "当前文档包含未保存的修改,放弃后将无法恢复。",
                Title = "未保存的修改",
                Icon = TaskDialogIcon.Warning,
                Buttons = TaskDialogButtons.YesNoCancel,
                DefaultButton = TaskDialogButton.No,
                VerificationText = "不再询问我",
                Cancelable = true,
            });
            Console.WriteLine($"=> YesNoCancel 结果: {r1}");

            // ---- 3. Confirm 扩展方法:返回 bool ----
            bool ok = "确认要部署到生产环境吗?".Confirm(
                title: "部署确认",
                icon: TaskDialogIcon.Shield,
                instruction: "该操作将重启服务");
            Console.WriteLine($"=> Confirm 结果: {(ok ? "Yes" : "No")}");

            // ---- 4. 完整选项:命令链接 + "详细信息" + 页脚 + 超时 ----
            var custom = TaskDialog.Show(new TaskDialogOptions
            {
                Instruction = "请选择下一步操作",
                Text = "根据分析,你的磁盘空间不足。",
                Title = "磁盘空间不足",
                Icon = TaskDialogIcon.Warning,
                UseCommandLinks = true,
                CustomButtons = new[]
                {
                    new TaskDialogButton(100, "立即清理\n自动清理回收站与临时文件"),
                    new TaskDialogButton(101, "稍后处理\n3 天后提醒我"),
                    TaskDialogButton.Cancel,
                },
                DefaultButton = new TaskDialogButton(100, "立即清理\n自动清理回收站与临时文件"),
                ExpandedInformation = "详细日志:2026-09-09 17:00 检测到 C:\\ 盘剩余空间仅 1.2 GB,系统建议至少保留 10 GB。",
                FooterText = "© Win32TaskDialog 示例",
                Timeout = TimeSpan.FromSeconds(15),
            });
            Console.WriteLine($"=> 命令链接结果: {custom}");

            // ---- 5. 进度条对话框:模拟工作 ----
            TaskDialogResult pr = "正在批量处理文件…".ShowProgress((progress, token) =>
            {
                for (int i = 1; i <= 100; i++)
                {
                    token.ThrowIfCancellationRequested();
                    progress.Report(i);
                    progress.SetText($"已处理 {i} / 100 个文件");
                    Thread.Sleep(50);
                }
            });
            Console.WriteLine($"=> 进度条结果: {pr}");

            // ---- 6. Marquee(不确定进度)+ 取消按钮 ----
            var marquee = new TaskDialogProgressOptions
            {
                Marquee = true,
                // 设置了 Instruction 时,ShowProgress 的 message 参数仅作为空值时的回退文本
                Instruction = "正在连接网络…",
            };
            TaskDialogResult mr = "Marquee(不确定进度)演示:工作期间可点击“取消”中断".ShowProgress((progress, token) =>
            {
                for (int i = 0; i < 10; i++)
                {
                    token.ThrowIfCancellationRequested();
                    Thread.Sleep(400);
                }
            }, marquee);
            Console.WriteLine($"=> Marquee 进度条结果: {mr}");

            // ---- 7. 异常传播演示 ----
            try
            {
                "故意失败的工作".ShowProgress((progress, token) =>
                {
                    Thread.Sleep(200);
                    throw new InvalidOperationException("工作委托抛出的一个模拟异常");
                });
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"=> 异常正确传播到调用线程: {ex.Message}");
            }

            Console.WriteLine();
            Console.WriteLine("按任意键退出…");
            Console.ReadKey();
        }
    }
}
