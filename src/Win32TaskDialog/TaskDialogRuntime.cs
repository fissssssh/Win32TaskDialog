using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Win32TaskDialog.Interop;

namespace Win32TaskDialog
{
    /// <summary>
    /// 内部运行时上下文:通过 GCHandle 与 <see cref="TASKDIALOGCONFIG.lpCallbackData"/> 关联,
    /// 供回调函数 <see cref="TaskDialog.TaskDialogCallbackProcImpl"/> 取回,并驱动超时与进度条逻辑。
    /// 程序化关闭使用 TDM_CLICK_BUTTON 点击一个"真实存在"的按钮(返回值由托管层按状态映射),
    /// 因为旧版的 TDM_RETURN_VALUE(0x0231) 在现代 comctl32 上已失效(经原生探针验证)。
    /// </summary>
    internal sealed class TaskDialogRuntime
    {
        /// <summary>超时(0 表示不超时)。</summary>
        public TimeSpan Timeout { get; set; }

        /// <summary>超时是否已经发生(托管层据此把结果映射为 <see cref="TaskDialogResult.Timeout"/>)。</summary>
        public bool TimedOut { get; private set; }

        /// <summary>超时后、点击关闭按钮之前执行(进度模式用来取消工作线程)。</summary>
        public Action? OnTimedOut { get; set; }

        /// <summary>是否显示进度条(进度模式);Marquee 为 true 时使用不确定进度。</summary>
        public bool ShowProgressBar { get; set; }

        /// <summary>进度条为不确定模式(循环滚动)。</summary>
        public bool MarqueeProgressBar { get; set; }

        /// <summary>程序化关闭时应点击的按钮 ID(ShowCore 在构造配置时计算:优先 Cancel,其次 Ok,否则第一个按钮)。</summary>
        public int ClickTargetId { get; set; } = 1;

        /// <summary><see cref="Close"/> 置位,期间 TDN_BUTTON_CLICKED 拦截将放行(避免点击关闭时被阻止)。</summary>
        public bool SilentClosePending { get; private set; }

        /// <summary>按钮点击钩子:返回 true 表示保持对话框打开(S_FALSE)。</summary>
        public Func<IntPtr, TaskDialogResult, bool>? OnButtonClickedHandler { get; set; }

        /// <summary>定时器钩子:每次 TDN_TIMER(约 200ms)调用,参数为已用毫秒。</summary>
        public Action<IntPtr, long>? OnTimerHandler { get; set; }

        /// <summary>TASKDIALOGCONFIG 构造时写入对话框的定时起点。</summary>
        public long StartTimestamp { get; set; }

        internal static TaskDialogRuntime? FromRefData(IntPtr lpRefData)
        {
            if (lpRefData == IntPtr.Zero)
                return null;
            try
            {
                return GCHandle.FromIntPtr(lpRefData).Target as TaskDialogRuntime;
            }
            catch (InvalidOperationException)
            {
                return null; // GCHandle 已释放
            }
        }

        internal bool OnButtonClicked(IntPtr hwnd, TaskDialogResult clickId)
        {
            if (SilentClosePending)
                return false; // 程序化关闭:放行,让对话框关闭
            return OnButtonClickedHandler?.Invoke(hwnd, clickId) ?? false;
        }

        internal void OnTimer(IntPtr hwnd, IntPtr wParam)
        {
            long elapsedMs = ElapsedMilliseconds();
            if (Timeout > TimeSpan.Zero && !TimedOut && elapsedMs >= (long)Timeout.TotalMilliseconds)
            {
                TimedOut = true;
                OnTimedOut?.Invoke();
                Close(hwnd);
                return;
            }
            OnTimerHandler?.Invoke(hwnd, elapsedMs);
        }

        internal long ElapsedMilliseconds() =>
            ((Stopwatch.GetTimestamp() - StartTimestamp) * 1000L) / Stopwatch.Frequency;

        /// <summary>
        /// 程序化关闭对话框:通过 TDM_CLICK_BUTTON 点击一个真实存在的按钮。
        /// comctl32 会触发 TDN_BUTTON_CLICKED,<see cref="OnButtonClicked"/> 因
        /// <see cref="SilentClosePending"/> 而放行;最终 TaskDialogIndirect 返回
        /// 该按钮 ID,会话结果由托管层根据状态(超时/完成/异常)重新映射。
        /// </summary>
        internal void Close(IntPtr hwnd)
        {
            SilentClosePending = true;
            NativeMethods.SendMessage(hwnd, TaskDialogNativeConstants.TDM_CLICK_BUTTON, new IntPtr(ClickTargetId), IntPtr.Zero);
        }
    }
}
