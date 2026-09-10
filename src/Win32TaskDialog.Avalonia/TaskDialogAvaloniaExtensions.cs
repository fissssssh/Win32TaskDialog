using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Win32TaskDialog;

// 注意:using 指令必须位于 namespace 之外。
// 在 namespace Win32TaskDialog.Avalonia 内部书写 Avalonia.* 时,编译器会先按
// “Win32TaskDialog.Avalonia.*” 前缀解析,导致无法找到真实的 Avalonia 命名空间。
namespace Win32TaskDialog.Avalonia
{
    /// <summary>
    /// 面向 Avalonia 的异步扩展方法:
    /// <list type="bullet">
    /// <item><description>自动从 <see cref="TopLevel"/>(窗口)解析 Win32 父窗口句柄(HWND);</description></item>
    /// <item><description>在后台线程显示原生对话框,不阻塞 Avalonia UI 渲染;</description></item>
    /// <item><description>进度对话框直接以 <c>await</c> 使用,工作委托的取消/异常语义与基库一致。</description></item>
    /// </list>
    /// 所有方法都必须在 UI 线程上调用(仅做句柄解析与参数准备,随后立即异步返回)。
    /// </summary>
    public static class TaskDialogAvaloniaExtensions
    {
        // =====================================================================
        // 窗口句柄解析
        // =====================================================================

        /// <summary>
        /// 获取 Avalonia 顶层窗口的原生 Win32 句柄(HWND)。
        /// 仅当后端为 Win32 时返回有效句柄;其他平台(X11 / Wayland / macOS 等)返回
        /// <see cref="IntPtr.Zero"/>,此时基库会抛出 <see cref="PlatformNotSupportedException"/>。
        /// 必须在 UI 线程调用。
        /// </summary>
        public static IntPtr GetWin32Handle(this TopLevel topLevel)
        {
            ArgumentNullException.ThrowIfNull(topLevel);

            var handle = topLevel.TryGetPlatformHandle();
            if (handle is null)
                return IntPtr.Zero;

            // Avalonia 的 Win32 后端以此描述符标识 HWND;其余后端(XID / NSView 等)一律视为不可用。
            return string.Equals(handle.HandleDescriptor, "HWND", StringComparison.OrdinalIgnoreCase)
                ? handle.Handle
                : IntPtr.Zero;
        }

        /// <summary>
        /// 获取控件所在顶层窗口的 Win32 句柄(HWND);控件尚未挂接到窗口树时返回
        /// <see cref="IntPtr.Zero"/>。必须在 UI 线程调用。
        /// </summary>
        public static IntPtr GetWin32Handle(this Control control)
        {
            ArgumentNullException.ThrowIfNull(control);
            return TopLevel.GetTopLevel(control)?.GetWin32Handle() ?? IntPtr.Zero;
        }

        // =====================================================================
        // 基础对话框
        // =====================================================================

        /// <summary>以本窗口为父窗口异步显示一个最简单的消息框(系统自动 OK 按钮)。</summary>
        public static Task<TaskDialogResult> ShowAsync(this TopLevel owner, string message, string? title = null)
        {
            ArgumentNullException.ThrowIfNull(owner);
            ArgumentNullException.ThrowIfNull(message);

            return ShowAsync(owner, new TaskDialogOptions
            {
                Text = message,
                Title = title,
                Buttons = TaskDialogButtons.Ok,
            });
        }

        /// <summary>
        /// 以本窗口为父窗口异步显示对话框;若 <see cref="TaskDialogOptions.OwnerHandle"/>
        /// 未设置,则自动填充为本窗口的 HWND。
        /// </summary>
        public static Task<TaskDialogResult> ShowAsync(this TopLevel owner, TaskDialogOptions options)
        {
            ArgumentNullException.ThrowIfNull(owner);
            ArgumentNullException.ThrowIfNull(options);

            // HWND 必须在 UI 线程解析(Avalonia 对象不可跨线程访问);
            // 原生对话框随后在后台线程显示,期间 UI 线程保持响应。
            IntPtr hwnd = owner.GetWin32Handle();
            if (options.OwnerHandle == IntPtr.Zero)
                options.OwnerHandle = hwnd;

            return Task.Run(() => TaskDialog.Show(options));
        }

        /// <summary>
        /// 以本窗口为父窗口异步显示 Yes/No 确认框,Yes 返回 true。
        /// </summary>
        public static Task<bool> ConfirmAsync(this TopLevel owner, string message, string? title = null,
            TaskDialogIcon icon = TaskDialogIcon.Question, string? instruction = null)
        {
            ArgumentNullException.ThrowIfNull(owner);
            ArgumentNullException.ThrowIfNull(message);

            IntPtr hwnd = owner.GetWin32Handle();
            return Task.Run(() => hwnd.Confirm(message, title, icon, instruction));
        }

        // =====================================================================
        // 进度条对话框
        // =====================================================================

        /// <summary>
        /// 以本窗口为父窗口异步显示进度条对话框;工作委托在后台线程执行,
        /// 点击原生对话框上的 Cancel(或按 ESC)会触发工作委托的取消令牌。
        /// 调用示例:
        /// <code>
        /// TaskDialogResult result = await this.ShowProgressAsync("正在发布…", (progress, token) =>
        /// {
        ///     for (int i = 0; i &lt;= 100; i++)
        ///     {
        ///         token.ThrowIfCancellationRequested();
        ///         progress.Report(i);
        ///         progress.SetText($"已完成 {i}%");
        ///         Thread.Sleep(30);
        ///     }
        /// });
        /// </code>
        /// </summary>
        public static Task<TaskDialogResult> ShowProgressAsync(this TopLevel owner, string message,
            Action<TaskDialogProgressReporter, CancellationToken> work,
            TaskDialogProgressOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(owner);
            ArgumentNullException.ThrowIfNull(message);
            ArgumentNullException.ThrowIfNull(work);

            IntPtr hwnd = owner.GetWin32Handle();
            options ??= new TaskDialogProgressOptions();
            if (options.OwnerHandle == IntPtr.Zero)
                options.OwnerHandle = hwnd;

            return Task.Run(() => message.ShowProgress(work, options));
        }
    }
}
