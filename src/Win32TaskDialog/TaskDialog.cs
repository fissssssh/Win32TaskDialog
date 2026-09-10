using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Win32TaskDialog.Interop;

namespace Win32TaskDialog
{
    /// <summary>
    /// 基于 Win32 TaskDialog(comctl32 v6,Windows Vista+)的消息框。
    /// 为其他 .NET 项目提供现代外观的模态对话框;支持自定义按钮、命令链接、
    /// 验证复选框、"详细信息"展开区、页脚、超时与进度条(见 <see cref="TaskDialogExtensions"/>)。
    /// </summary>
    public static class TaskDialog
    {
        private static readonly TaskDialogCallbackProc CallbackDelegate = TaskDialogCallbackProcImpl;

        /// <summary>使用默认按钮(系统自动 OK)显示最简单的对话框,标题为宿主机程序名。</summary>
        public static TaskDialogResult Show(string message)
        {
            if (message is null)
                throw new ArgumentNullException(nameof(message));

            return ShowCore(new TaskDialogOptions
            {
                Text = message,
                Buttons = TaskDialogButtons.None,
            }, runtime: null);
        }

        /// <summary>使用默认按钮(系统自动 OK)显示带标题的对话框。</summary>
        public static TaskDialogResult Show(string message, string title)
        {
            if (message is null)
                throw new ArgumentNullException(nameof(message));
            if (title is null)
                throw new ArgumentNullException(nameof(title));

            return ShowCore(new TaskDialogOptions
            {
                Text = message,
                Title = title,
                Buttons = TaskDialogButtons.None,
            }, runtime: null);
        }

        /// <summary>根据完整配置显示对话框。</summary>
        public static TaskDialogResult Show(TaskDialogOptions options)
        {
            if (options is null)
                throw new ArgumentNullException(nameof(options));

            // 设置了超时则需要 TDF_CALLBACK_TIMER 回调来检测并关闭对话框
            if (options.Timeout > TimeSpan.Zero)
            {
                var runtime = new TaskDialogRuntime { Timeout = options.Timeout };
                TaskDialogResult result = ShowCore(options, runtime);
                return runtime.TimedOut ? TaskDialogResult.Timeout : result;
            }

            return ShowCore(options, runtime: null);
        }

        /// <summary>
        /// 核心实现:将托管选项序列化为 <see cref="TASKDIALOGCONFIG"/> 并调用 TaskDialogIndirect。
        /// <paramref name="runtime"/> 为内部扩展(超时、进度条)提供定时回调,公开的 Show 重载使用 null。
        /// </summary>
        internal static TaskDialogResult ShowCore(TaskDialogOptions options, TaskDialogRuntime? runtime)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                throw new PlatformNotSupportedException(
                    "Win32TaskDialog 基于 Win32 TaskDialog API,仅支持 Windows(Vista+)。");

            ComCtl32.EnsureInitialized();

            using (var buffers = new NativeBuffers())
            {
                int flags = BuildFlags(options, out IntPtr mainIcon);

                List<TaskDialogButton> buttons = ResolveButtons(options);
                int defaultButtonId = ResolveDefaultButton(options, buttons);
                IntPtr buttonArrayPtr = AllocButtonArray(buffers, buttons);

                // 展开区控制文字(仅当设置了 ExpandedInformation)
                bool hasExpansion = !string.IsNullOrEmpty(options.ExpandedInformation);

                // 回调(超时 / 进度)
                bool needsTimer = runtime != null || options.Timeout > TimeSpan.Zero;
                if (needsTimer)
                    flags |= TaskDialogNativeConstants.TDF_CALLBACK_TIMER;

                // 进度条(由 ShowProgress 扩展挂载到 runtime)
                if (runtime is { ShowProgressBar: true })
                {
                    flags |= runtime.MarqueeProgressBar
                        ? TaskDialogNativeConstants.TDF_SHOW_MARQUEE_PROGRESS_BAR
                        : TaskDialogNativeConstants.TDF_SHOW_PROGRESS_BAR;
                }

                // 程序化关闭(超时/进度完成)需要点击一个真实存在的按钮:
                // 优先 Cancel,其次 OK,否则第一个按钮;无按钮时由系统自动提供 OK(1)。
                if (runtime != null)
                {
                    runtime.ClickTargetId = buttonArrayPtr != IntPtr.Zero
                        ? (HasButtonId(buttons, 2) ? 2 : HasButtonId(buttons, 1) ? 1 : buttons[0].Id)
                        : 1;
                }

                var config = new TASKDIALOGCONFIG
                {
                    cbSize = Marshal.SizeOf<TASKDIALOGCONFIG>(),
                    hwndParent = options.OwnerHandle,
                    hInstance = IntPtr.Zero,
                    dwFlags = flags,
                    pszWindowTitle = buffers.AllocUnicodeString(options.Title),
                    mainIcon = mainIcon,
                    pszMainInstruction = buffers.AllocUnicodeString(options.Instruction),
                    pszContent = buffers.AllocUnicodeString(options.Text),
                    cButtons = (uint)buttons.Count,
                    pButtons = buttonArrayPtr,
                    nDefaultButton = defaultButtonId,
                    pszVerificationText = buffers.AllocUnicodeString(options.VerificationText),
                    pszExpandedInformation = buffers.AllocUnicodeString(options.ExpandedInformation),
                    pszExpandedControlText = hasExpansion
                        ? buffers.AllocUnicodeString(Sanitize(options.ExpandedControlText, "隐藏(&H)"))
                        : IntPtr.Zero,
                    pszCollapsedControlText = hasExpansion
                        ? buffers.AllocUnicodeString(Sanitize(options.CollapsedControlText, "详细信息(&D)"))
                        : IntPtr.Zero,
                    pszFooter = buffers.AllocUnicodeString(options.FooterText),
                    cxWidth = (uint)Math.Max(0, options.MinimumWidth),
                };

                var runtimeHandle = runtime != null ? GCHandle.Alloc(runtime) : default;
                try
                {
                    if (runtime != null && runtimeHandle.IsAllocated)
                    {
                        config.pfCallback = Marshal.GetFunctionPointerForDelegate(CallbackDelegate);
                        config.lpCallbackData = GCHandle.ToIntPtr(runtimeHandle);
                        runtime.StartTimestamp = Stopwatch.GetTimestamp();
                    }

                    int pnButton;
                    int hresult;
                    try
                    {
                        hresult = NativeMethods.TaskDialogIndirect(
                            ref config, out pnButton, out int pnRadioButton, out bool verificationFlag);
                    }
                    catch (EntryPointNotFoundException ex)
                    {
                        throw new PlatformNotSupportedException(
                            "当前系统不支持 TaskDialog(需 Windows Vista+ 与 comctl32 v6;" +
                            "宿主清单需声明 common-controls v6 依赖,Windows 10 1809+ 默认为 v6)。", ex);
                    }
                    catch (DllNotFoundException ex)
                    {
                        throw new PlatformNotSupportedException(
                            "无法加载 comctl32.dll,TaskDialog 需要 Windows Vista+。", ex);
                    }

                    // 注意:TaskDialogIndirect 返回 HRESULT(而非 BOOL),S_OK(0) 为成功。
                    if (hresult < 0)
                    {
                        int error = hresult & 0xFFFF;
                        throw new InvalidOperationException(
                            $"TaskDialogIndirect 调用失败,HRESULT 0x{hresult:X8}(Win32 错误码 {error})。" +
                            "TaskDialog 兼容 Windows Vista+ 且宿主进程需加载 comctl32 v6" +
                            "(常见于带默认清单的 .NET 应用;Windows 10 1809+ 默认为 v6)。");
                    }

                    return (TaskDialogResult)pnButton;
                }
                finally
                {
                    if (runtimeHandle.IsAllocated)
                        runtimeHandle.Free();
                }
            }
        }

        private static int BuildFlags(TaskDialogOptions options, out IntPtr mainIcon)
        {
            int flags = 0;
            mainIcon = IntPtr.Zero;

            switch (options.Icon)
            {
                case TaskDialogIcon.Information:
                    mainIcon = MakeIntResource(TaskDialogNativeConstants.TD_INFORMATION_ICON);
                    break;
                case TaskDialogIcon.Warning:
                    mainIcon = MakeIntResource(TaskDialogNativeConstants.TD_WARNING_ICON);
                    break;
                case TaskDialogIcon.Error:
                    mainIcon = MakeIntResource(TaskDialogNativeConstants.TD_ERROR_ICON);
                    break;
                case TaskDialogIcon.Shield:
                    mainIcon = MakeIntResource(TaskDialogNativeConstants.TD_SHIELD_ICON);
                    break;
                case TaskDialogIcon.Question:
                    // Question 没有预定义的 MAKEINTRESOURCE 值,改用系统图标句柄。
                    mainIcon = NativeMethods.LoadIcon(IntPtr.Zero, new IntPtr(TaskDialogNativeConstants.IDI_QUESTION));
                    flags |= TaskDialogNativeConstants.TDF_USE_HICON_MAIN;
                    break;
                case TaskDialogIcon.None:
                default:
                    mainIcon = IntPtr.Zero;
                    break;
            }

            if (options.Cancelable)
                flags |= TaskDialogNativeConstants.TDF_ALLOW_DIALOG_CANCELLATION;
            if (options.CanBeMinimized)
                flags |= TaskDialogNativeConstants.TDF_CAN_BE_MINIMIZED;
            if (options.VerificationChecked)
                flags |= TaskDialogNativeConstants.TDF_VERIFICATION_FLAG_CHECKED;
            if (!string.IsNullOrEmpty(options.ExpandedInformation))
                flags |= TaskDialogNativeConstants.TDF_EXPAND_FOOTER_AREA;
            if (options.UseCommandLinks && options.CustomButtons is { Count: > 0 })
                flags |= TaskDialogNativeConstants.TDF_USE_COMMAND_LINKS;
            if (options.OwnerHandle != IntPtr.Zero)
                flags |= TaskDialogNativeConstants.TDF_POSITION_RELATIVE_TO_WINDOW;

            return flags;
        }

        /// <summary>解释预定义组合与自定义按钮,返回最终按钮列表(可能为空 → 系统自动提供 OK)。</summary>
        internal static List<TaskDialogButton> ResolveButtons(TaskDialogOptions options)
        {
            if (options.CustomButtons is { Count: > 0 })
                return new List<TaskDialogButton>(options.CustomButtons);

            var list = new List<TaskDialogButton>(8);
            switch (options.Buttons)
            {
                case TaskDialogButtons.None:
                    break;
                case TaskDialogButtons.Ok:
                    list.Add(TaskDialogButton.Ok);
                    break;
                case TaskDialogButtons.OkCancel:
                    list.Add(TaskDialogButton.Ok);
                    list.Add(TaskDialogButton.Cancel);
                    break;
                case TaskDialogButtons.YesNo:
                    list.Add(TaskDialogButton.Yes);
                    list.Add(TaskDialogButton.No);
                    break;
                case TaskDialogButtons.YesNoCancel:
                    list.Add(TaskDialogButton.Yes);
                    list.Add(TaskDialogButton.No);
                    list.Add(TaskDialogButton.Cancel);
                    break;
                case TaskDialogButtons.RetryCancel:
                    list.Add(TaskDialogButton.Retry);
                    list.Add(TaskDialogButton.Cancel);
                    break;
                case TaskDialogButtons.AbortRetryIgnore:
                    list.Add(TaskDialogButton.Abort);
                    list.Add(TaskDialogButton.Retry);
                    list.Add(TaskDialogButton.Ignore);
                    break;
                case TaskDialogButtons.TryAgainCancel:
                    list.Add(TaskDialogButton.TryAgain);
                    list.Add(TaskDialogButton.Cancel);
                    break;
                case TaskDialogButtons.ContinueCancel:
                    list.Add(TaskDialogButton.Continue);
                    list.Add(TaskDialogButton.Cancel);
                    break;
            }
            return list;
        }

        private static bool HasButtonId(List<TaskDialogButton> buttons, int id)
        {
            foreach (var b in buttons)
                if (b.Id == id)
                    return true;
            return false;
        }

        private static int ResolveDefaultButton(TaskDialogOptions options, List<TaskDialogButton> buttons)
        {
            if (options.DefaultButton is not null)
                return options.DefaultButton.Id;
            foreach (var b in buttons)
                if (b.IsDefault)
                    return b.Id;
            return 0; // 0 = 对话框中的第一个按钮
        }

        /// <summary>分配按钮数组(含文字),返回数组指针;无按钮时返回 IntPtr.Zero。</summary>
        private static IntPtr AllocButtonArray(NativeBuffers buffers, List<TaskDialogButton> buttons)
        {
            if (buttons.Count == 0)
                return IntPtr.Zero;

            // TASKDIALOG_BUTTON 在头文件 pshpack1 区域:元素 12 字节,文字指针紧随 int(ID)
            // 之后(偏移 4),不能使用 IntPtr.Size 作为文字偏移(会错位 8 而越界写入)。
            int buttonSize = Marshal.SizeOf<TASKDIALOG_BUTTON>();
            int textOffset = Marshal.OffsetOf<TASKDIALOG_BUTTON>(nameof(TASKDIALOG_BUTTON.pszButtonText)).ToInt32();

            IntPtr arrayPtr = buffers.Alloc(buttonSize * buttons.Count);
            for (int i = 0; i < buttons.Count; i++)
            {
                var b = buttons[i];
                Marshal.WriteInt32(arrayPtr, i * buttonSize, b.Id);
                Marshal.WriteIntPtr(arrayPtr, i * buttonSize + textOffset, buffers.AllocUnicodeString(b.Text));
            }
            return arrayPtr;
        }

        /// <summary>MAKEINTRESOURCE:资源 ID(可为负)先收缩为 USHORT 再扩展为指针。</summary>
        internal static IntPtr MakeIntResource(int value) => new IntPtr((long)(ushort)value);

        private static string Sanitize(string? text, string fallback) =>
            string.IsNullOrEmpty(text) ? fallback : text!;

        // ---- 原生回调 ----

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        internal delegate IntPtr TaskDialogCallbackProc(
            IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam, IntPtr lpRefData);

        private static IntPtr TaskDialogCallbackProcImpl(
            IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam, IntPtr lpRefData)
        {
            var runtime = TaskDialogRuntime.FromRefData(lpRefData);
            if (runtime is null)
                return new IntPtr(TaskDialogNativeConstants.S_OK);

            switch (msg)
            {
                case TaskDialogNativeConstants.TDN_BUTTON_CLICKED:
                    var id = (int)wParam.ToInt64();
                    // 返回 S_FALSE 表示"保持对话框打开"(用于进度取消后等待 work 清理)
                    bool handled = runtime.OnButtonClicked(hwnd, (TaskDialogResult)id);
                    return handled
                        ? new IntPtr(TaskDialogNativeConstants.S_FALSE)
                        : new IntPtr(TaskDialogNativeConstants.S_OK);

                case TaskDialogNativeConstants.TDN_TIMER:
                    runtime.OnTimer(hwnd, wParam);
                    return new IntPtr(TaskDialogNativeConstants.S_OK);

                default:
                    return new IntPtr(TaskDialogNativeConstants.S_OK);
            }
        }
    }
}
