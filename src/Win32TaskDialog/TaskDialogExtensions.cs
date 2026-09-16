using System;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Win32TaskDialog.Interop;

namespace Win32TaskDialog
{
    /// <summary>
    /// 针对常见场景的便捷扩展方法:
    /// <list type="bullet">
    /// <item><description><see cref="Confirm(string,string,TaskDialogIcon,string)"/> — 返回 bool 的确认对话框;</description></item>
    /// <item><description><see cref="ShowProgress(string,System.Action{TaskDialogProgressReporter,CancellationToken},TaskDialogProgressOptions)"/> — 带进度条的阻塞对话框。</description></item>
    /// </list>
    /// </summary>
    public static class TaskDialogExtensions
    {
        // =====================================================================
        // Confirm — 返回 bool
        // =====================================================================

        /// <summary>
        /// 显示一个 Yes/No 确认对话框,Yes 返回 true,No 返回 false。
        /// </summary>
        public static bool Confirm(this string message, string? title = null,
            TaskDialogIcon icon = TaskDialogIcon.Question, string? instruction = null)
        {
            if (message is null)
                throw new ArgumentNullException(nameof(message));

            return Confirm(new TaskDialogOptions
            {
                Text = message,
                Title = title,
                Icon = icon,
                Instruction = instruction,
                Buttons = TaskDialogButtons.YesNo,
            });
        }

        /// <summary>
        /// 显示一个 Yes/No 确认对话框,悬挂在指定父窗口(HWND)上。
        /// </summary>
        public static bool Confirm(this IntPtr ownerHandle, string message, string? title = null,
            TaskDialogIcon icon = TaskDialogIcon.Question, string? instruction = null)
        {
            if (message is null)
                throw new ArgumentNullException(nameof(message));

            return Confirm(new TaskDialogOptions
            {
                OwnerHandle = ownerHandle,
                Text = message,
                Title = title,
                Icon = icon,
                Instruction = instruction,
                Buttons = TaskDialogButtons.YesNo,
            });
        }

        /// <summary>
        /// 基于完整选项的确认对话框:Yes/OK/Continue/TryAgain/Retry 判为 true,
        /// No/Cancel/忽略/关闭 或超时判为 false。自定义按钮时,具有上述标准 ID 的按钮同样生效。
        /// </summary>
        public static bool Confirm(this TaskDialogOptions options)
        {
            if (options is null)
                throw new ArgumentNullException(nameof(options));

            TaskDialogResult result = TaskDialog.Show(options);
            switch (result)
            {
                case TaskDialogResult.Yes:
                case TaskDialogResult.Ok:
                case TaskDialogResult.Continue:
                case TaskDialogResult.TryAgain:
                case TaskDialogResult.Retry:
                    return true;
                default:
                    return false;
            }
        }

        // =====================================================================
        // ShowProgress — 进度条对话框
        // =====================================================================

        /// <summary>
        /// 显示一个带进度条的对话框(与 Message 为指令文本),工作委托在后台线程执行。
        /// 阻塞当前线程直到对话框关闭。调用示例:
        /// <code>
        /// TaskDialogResult result = "正在发布".ShowProgress((progress, token) =&gt;
        /// {
        ///     for (int i = 0; i &lt; 100; i++)
        ///     {
        ///         token.ThrowIfCancellationRequested();
        ///         progress.Report(i);
        ///         progress.SetText($"已处理 {i}%");
        ///         Thread.Sleep(50);
        ///     }
        /// });
        /// </code>
        /// </summary>
        /// <param name="message">指令文本(大号主文本)。</param>
        /// <param name="work">后台工作委托,第一个参数为进度报告器(0-100),第二个参数为取消令牌。</param>
        /// <param name="options">进度对话框选项;为 null 时使用默认值(显示 Cancel 按钮、完成后自动关闭)。</param>
        public static TaskDialogResult ShowProgress(this string message,
            Action<TaskDialogProgressReporter, CancellationToken> work,
            TaskDialogProgressOptions? options = null)
        {
            if (message is null)
                throw new ArgumentNullException(nameof(message));
            if (work is null)
                throw new ArgumentNullException(nameof(work));

            return ShowProgressCore(message, IntPtr.Zero, work, options);
        }

        /// <summary>
        /// 与 <see cref="ShowProgress(string,System.Action{TaskDialogProgressReporter,CancellationToken},TaskDialogProgressOptions)"/> 相同,
        /// 但对话框悬挂在指定的父窗口(HWND)上。
        /// </summary>
        public static TaskDialogResult ShowProgress(this IntPtr ownerHandle, string message,
            Action<TaskDialogProgressReporter, CancellationToken> work,
            TaskDialogProgressOptions? options = null)
        {
            if (message is null)
                throw new ArgumentNullException(nameof(message));
            if (work is null)
                throw new ArgumentNullException(nameof(work));

            return ShowProgressCore(message, ownerHandle, work, options);
        }

        private static TaskDialogResult ShowProgressCore(string message, IntPtr ownerHandle,
            Action<TaskDialogProgressReporter, CancellationToken> work,
            TaskDialogProgressOptions? options)
        {
            var reporter = new TaskDialogProgressReporter();
            var cts = new CancellationTokenSource();

            // 取消的宽限期:点击 Cancel 后最多再等 5 秒(等待 work 清理),
            // 超时则强制关闭对话框。
            long cancelRequestedAt = -1;

            // 组装选项:在调用方配置的副本上覆盖(进度对话框禁用预定义组合,按钮由库决定),
            // 从而不修改调用方传入的实例。
            var progressOptions = BuildProgressOptions(message, ownerHandle, options);
            progressOptions.Buttons = TaskDialogButtons.None;
            progressOptions.CustomButtons = progressOptions.ShowCancelButton
                ? new[] { TaskDialogButton.Cancel }
                : new[] { TaskDialogButton.Ok };
            bool autoCloseOnComplete = progressOptions.AutoCloseOnComplete;

            var runtime = new TaskDialogRuntime
            {
                Timeout = progressOptions.Timeout,
                ShowProgressBar = true,
                MarqueeProgressBar = progressOptions.Marquee,
            };
            runtime.OnTimedOut = () => cts.Cancel(); // 超时等价于向工作委托请求取消

            bool rangeSent = false;
            runtime.OnButtonClickedHandler = (hwnd, id) =>
            {
                // 工作已结束:不再拦截,直接关闭(AutoCloseOnComplete=false 时由用户决定何时关闭)
                if (reporter.TryGetOutcome(out _))
                    return false;

                if (id == TaskDialogResult.Cancel)
                {
                    cts.Cancel();
                    Interlocked.Exchange(ref cancelRequestedAt, runtime.ElapsedMilliseconds());
                    return true; // S_FALSE:保持对话框打开,等待工作委托退出
                }

                // 非取消按钮(如 OK 的手动确认):直接关闭
                return false;
            };

            runtime.OnTimerHandler = (hwnd, elapsedMs) =>
            {
                // 取消后宽限期已过:强制关闭
                long requestedAt = Volatile.Read(ref cancelRequestedAt);
                if (requestedAt >= 0 && elapsedMs - requestedAt > 5000)
                {
                    runtime.Close(hwnd);
                    return;
                }

                // 进度条:仅在非 Marquee 模式下更新
                if (!runtime.MarqueeProgressBar)
                {
                    int pos = (int)Math.Round(reporter.CurrentValue);
                    if (!rangeSent)
                    {
                        rangeSent = true;
                        NativeMethods.SendMessage(hwnd,
                            TaskDialogNativeConstants.TDM_SET_PROGRESS_BAR_RANGE,
                            IntPtr.Zero,
                            new IntPtr((100L << 16) | 0)); // MAKELPARAM(0, 100)
                    }
                    NativeMethods.SendMessage(hwnd,
                        TaskDialogNativeConstants.TDM_SET_PROGRESS_BAR_POS,
                        new IntPtr(pos),
                        IntPtr.Zero);
                }

                // 内容文本
                string? text = reporter.ConsumeText();
                if (text != null)
                {
                    var textPtr = Marshal.StringToHGlobalUni(text);
                    try
                    {
                        NativeMethods.SendMessage(hwnd,
                            TaskDialogNativeConstants.TDM_SET_ELEMENT_TEXT,
                            new IntPtr(TaskDialogNativeConstants.TDE_CONTENT),
                            textPtr);
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(textPtr);
                    }
                }

                // 完工/异常:程序化关闭(结果映射在 ShowCore 返回后统一处理);
                // AutoCloseOnComplete=false 时保持打开,由用户点击按钮关闭。
                if (autoCloseOnComplete && reporter.TryGetOutcome(out _))
                    runtime.Close(hwnd);
            };

            using (cts)
            {
                var workTask = Task.Run(() =>
                {
                    try
                    {
                        work(reporter, cts.Token);
                        reporter.SignalCompleted(null);
                    }
                    catch (OperationCanceledException)
                    {
                        // 取消(用户点了 Cancel / 超时)是正常路径,不视为异常。
                        // 注意:不能在此访问已释放的 cts,统一由上层状态判定。
                        reporter.SignalCompleted(null);
                    }
                    catch (Exception ex)
                    {
                        reporter.SignalCompleted(ex);
                    }
                });

                // 结果映射必须在 cts.Cancel()(finally)之前完成,否则取消标志会被
                // 无条件置位,误判为"用户取消"。
                TaskDialogResult raw;
                try
                {
                    raw = TaskDialog.ShowCore(progressOptions, runtime);

                    if (reporter.TryGetOutcome(out Exception? workException) && workException != null)
                    {
                        if (progressOptions.ThrowOnWorkException)
                            ExceptionDispatchInfo.Capture(workException).Throw();

                        return TaskDialogResult.Error;
                    }

                    bool workCompleted = reporter.TryGetOutcome(out _);

                    // 超时:无论工作是否已收尾都返回 Timeout(工作委托已被请求取消),
                    // 与 TaskDialogOptions.Timeout 的文档保持一致。
                    if (runtime.TimedOut)
                        return TaskDialogResult.Timeout;

                    if (workCompleted)
                    {
                        // 用户此前点击过 Cancel:工作已按取消收尾,语义为取消。
                        if (Volatile.Read(ref cancelRequestedAt) >= 0)
                            return TaskDialogResult.Cancel;

                        // AutoCloseOnComplete=true 时对话框由库关闭,语义为正常完成;
                        // false 时对话框仍开着,结果就是用户实际点击的按钮。
                        return autoCloseOnComplete ? TaskDialogResult.Ok : raw;
                    }

                    // 工作尚未收尾:对话框是被用户关闭的。只有取消语义(取消按钮、关闭按钮、
                    // Alt-F4、Esc)才映射为 Cancel;点击 OK 等按钮提前关闭时工作被中断,
                    // 不应误报为 Ok。
                    return raw == TaskDialogResult.Cancel
                        ? TaskDialogResult.Cancel
                        : TaskDialogResult.None;
                }
                finally
                {
                    // 对话框已关闭:通知工作委托停止(不等待其退出,避免长时间悬挂)
                    cts.Cancel();
                }
            }
        }

        private static TaskDialogProgressOptions BuildProgressOptions(
            string message, IntPtr ownerHandle, TaskDialogProgressOptions? source)
        {
            // 在副本上改写,避免污染调用方传入的实例。
            var options = source?.Clone() ?? new TaskDialogProgressOptions();
            if (string.IsNullOrEmpty(options.Instruction))
                options.Instruction = message;
            if (ownerHandle != IntPtr.Zero)
                options.OwnerHandle = ownerHandle;

            // 进度对话框取消/关闭时使用自定义返回码,关闭对话框的按钮组不自动触发。
            options.Cancelable = false;
            options.MinimumWidth = Math.Max(options.MinimumWidth, 0);

            return options;
        }
    }
}
