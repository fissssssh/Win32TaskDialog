using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Win32TaskDialog;

namespace Smoke
{
    /// <summary>
    /// 自动冒烟测试:通过 FindWindow 监听任务对话框窗口并用 TDM_CLICK_BUTTON 模拟点击,
    /// 完整验证库的 Show / 超时 / Confirm / 进度条 / 取消 / 异常传播 行为。
    /// </summary>
    internal static class Program
    {
        private const uint TDM_CLICK_BUTTON = 0x0400 + 102;

        [DllImport("user32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern IntPtr FindWindowW(string? lpClassName, string lpWindowName);

        [DllImport("user32.dll", EntryPoint = "SendMessageW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        private static int _pass;
        private static int _fail;

        private static void Main()
        {
            if (Environment.GetEnvironmentVariable("SMOKE_ONLY") == "timeout")
            {
                bool ok = TestTimeout();
                Console.WriteLine($"timeout-only: {(ok ? "PASS" : "FAIL")}");
                return;
            }

            Expect(TestSimpleShow(), "简单 Show(自动点 OK)");
            Expect(TestYesNo(), "按钮组合(点 Yes)");
            Expect(TestTimeout(), "超时自动关闭 → Timeout");
            Expect(TestConfirm(), "Confirm(Yes=true / No=false)");
            Expect(TestProgressOk(), "进度条正常完成 → Ok");
            Expect(TestProgressCancel(), "进度条取消 → Cancel,work 提前停止");
            Expect(TestProgressException(), "进度条异常传播 → 原异常");

            Console.WriteLine($"\n== SMOKE 结果: 通过 {_pass},失败 {_fail} ==");
            Environment.Exit(_fail > 0 ? 1 : 0);
        }

        private static void Expect(bool ok, string name)
        {
            Console.WriteLine($"{(ok ? "PASS" : "FAIL")}  {name}");
            if (ok) _pass++;
            else _fail++;
        }

        private static bool TestSimpleShow()
        {
            const string title = "smoke-simple-show";
            Task<bool> watch = ArmClick(title, 300, 1); // 点 OK
            var sw = Stopwatch.StartNew();
            TaskDialogResult r = TaskDialog.Show(new TaskDialogOptions
            {
                Title = title,
                Text = "简单的",
                Buttons = TaskDialogButtons.Ok,
            });
            sw.Stop();
            return watch.Wait(2000) && r == TaskDialogResult.Ok && sw.ElapsedMilliseconds < 5000;
        }

        private static bool TestYesNo()
        {
            const string title = "smoke-yesno";
            Task<bool> watch = ArmClick(title, 300, 6); // 点 Yes
            TaskDialogResult r = TaskDialog.Show(new TaskDialogOptions
            {
                Title = title,
                Text = "配合",
                Buttons = TaskDialogButtons.YesNo,
            });
            return watch.Wait(2000) && r == TaskDialogResult.Yes;
        }

        private static bool TestTimeout()
        {
            const string title = "smoke-timeout";
            var sw = Stopwatch.StartNew();
            TaskDialogResult r = TaskDialog.Show(new TaskDialogOptions
            {
                Title = title,
                Text = "2 秒后自动关闭",
                Buttons = TaskDialogButtons.Ok,
                Timeout = TimeSpan.FromSeconds(2),
            });
            sw.Stop();
            return r == TaskDialogResult.Timeout && sw.ElapsedMilliseconds >= 1800;
        }

        private static bool TestConfirm()
        {
            bool first = OnlyYesOnce();   // 点 Yes → true
            return first && OnlyNoOnce(); // 另一次点 No → false
        }

        private static bool OnlyYesOnce()
        {
            const string title = "smoke-confirm-yes";
            Task<bool> watch = ArmClick(title, 300, 6);
            bool r = "确认吗".Confirm(title);
            return watch.Wait(2000) && r;
        }

        private static bool OnlyNoOnce()
        {
            const string title = "smoke-confirm-no";
            Task<bool> watch = ArmClick(title, 300, 7);
            bool r = "确认吗".Confirm(title);
            return watch.Wait(2000) && !r;
        }

        private static bool TestProgressOk()
        {
            const string title = "smoke-progress-ok";
            var sw = Stopwatch.StartNew();
            var opts = new TaskDialogProgressOptions { Title = title, Instruction = "处理中" };
            TaskDialogResult r = "模拟工作…".ShowProgress((p, token) =>
            {
                for (int i = 0; i < 100; i++) { p.Report(i); Thread.Sleep(20); }
            }, opts);
            sw.Stop();
            return r == TaskDialogResult.Ok && sw.ElapsedMilliseconds >= 1500 && sw.ElapsedMilliseconds < 8000;
        }

        private static bool TestProgressCancel()
        {
            const string title = "smoke-progress-cancel";
            var stopped = new int[1];
            var opts = new TaskDialogProgressOptions { Title = title, Instruction = "长任务(10 秒)" };
            var sw = Stopwatch.StartNew();
            Task<bool> watch = ArmClick(title, 800, 2); // 点 Cancel
            TaskDialogResult r = "长任务".ShowProgress((p, token) =>
            {
                for (int i = 0; i < 1000; i++)
                {
                    token.ThrowIfCancellationRequested();
                    p.Report(i % 100);
                    Thread.Sleep(10);
                }
                Interlocked.Exchange(ref stopped[0], 1); // 未取消时才会走到这里
            }, opts);
            sw.Stop();
            bool workStopped = Volatile.Read(ref stopped[0]) == 0;
            return watch.Wait(2000) && r == TaskDialogResult.Cancel && workStopped && sw.ElapsedMilliseconds < 8000;
        }

        private static bool TestProgressException()
        {
            const string title = "smoke-progress-ex";
            var opts = new TaskDialogProgressOptions { Title = title, Instruction = "即将失败" };
            try
            {
                "故意失败".ShowProgress((p, token) =>
                {
                    Thread.Sleep(500);
                    throw new InvalidOperationException("BOOM-42");
                }, opts);
                return false; // 应抛异常
            }
            catch (InvalidOperationException ex)
            {
                return ex.Message == "BOOM-42";
            }
        }

        /// <summary>
        /// 装备自动点击:后台线程在 Show 阻塞期间按标题找到窗口并点击按钮。
        /// 返回的 Task 在"找到窗口"时完成,Show 返回后可等待其确认。
        /// </summary>
        private static Task<bool> ArmClick(string title, int delayMs, int buttonId)
        {
            var tcs = new TaskCompletionSource<bool>();
            _ = Task.Run(() =>
            {
                IntPtr hwnd = IntPtr.Zero;
                for (int i = 0; i < 100; i++)
                {
                    hwnd = FindWindowW(null, title);
                    if (hwnd != IntPtr.Zero) break;
                    Thread.Sleep(100);
                }
                if (hwnd == IntPtr.Zero)
                    return;
                tcs.TrySetResult(true);
                Thread.Sleep(delayMs);
                SendMessage(hwnd, TDM_CLICK_BUTTON, new IntPtr(buttonId), IntPtr.Zero);
            });
            return tcs.Task;
        }
    }
}
