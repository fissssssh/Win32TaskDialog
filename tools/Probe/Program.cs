using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Probe
{
    /// <summary>
    /// 诊断:对比 Pack=1 与 Pack=8 两种 TASKDIALOGCONFIG 布局在 comctl32 v6 上的真实行为。
    /// 每个 trial 通过 FindWindow 按唯一标题找窗口(watchdog 线程),以此判定
    /// 标题字符串是否被正确读取(错位会产生乱码标题 → 找不到);内部回调判断
    /// TDF_CALLBACK_TIMER 是否被读取(错位则 flags 读取错误 → 回调不触发)。
    /// </summary>
    internal static class Program
    {
        private const uint TDN_TIMER = 4;
        private const int TDF_CALLBACK_TIMER = 0x0800;
        private const uint TDM_CLICK_BUTTON = 0x0400 + 102;

        [DllImport("comctl32.dll", EntryPoint = "TaskDialogIndirect", CharSet = CharSet.Unicode, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool TaskDialogIndirectPack1(
            ref TASKDIALOGCONFIG_PACK1 pTaskConfig,
            out int pnButton,
            out int pnRadioButton,
            [MarshalAs(UnmanagedType.Bool)] out bool pfVerificationFlagChecked);

        [DllImport("comctl32.dll", EntryPoint = "TaskDialogIndirect", CharSet = CharSet.Unicode, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool TaskDialogIndirectPack8(
            ref TASKDIALOGCONFIG_PACK8 pTaskConfig,
            out int pnButton,
            out int pnRadioButton,
            [MarshalAs(UnmanagedType.Bool)] out bool pfVerificationFlagChecked);

        [DllImport("user32.dll", EntryPoint = "SendMessageW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern IntPtr FindWindowW(string? lpClassName, string lpWindowName);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
        private struct TASKDIALOGCONFIG_PACK1
        {
            public int cbSize;
            public IntPtr hwndParent;
            public IntPtr hInstance;
            public int dwFlags;
            public int dwCommonButtons;
            public IntPtr pszWindowTitle;
            public IntPtr mainIcon;
            public IntPtr pszMainInstruction;
            public IntPtr pszContent;
            public uint cButtons;
            public IntPtr pButtons;
            public int nDefaultButton;
            public uint cRadioButtons;
            public IntPtr pRadioButtons;
            public int nDefaultRadioButton;
            public IntPtr pszVerificationText;
            public IntPtr pszExpandedInformation;
            public IntPtr pszExpandedControlText;
            public IntPtr pszCollapsedControlText;
            public IntPtr footerIcon;
            public IntPtr pszFooter;
            public IntPtr pfCallback;
            public IntPtr lpCallbackData;
            public uint cxWidth;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 8)]
        private struct TASKDIALOGCONFIG_PACK8
        {
            public int cbSize;
            public IntPtr hwndParent;
            public IntPtr hInstance;
            public int dwFlags;
            public int dwCommonButtons;
            public IntPtr pszWindowTitle;
            public IntPtr mainIcon;
            public IntPtr pszMainInstruction;
            public IntPtr pszContent;
            public uint cButtons;
            public IntPtr pButtons;
            public int nDefaultButton;
            public uint cRadioButtons;
            public IntPtr pRadioButtons;
            public int nDefaultRadioButton;
            public IntPtr pszVerificationText;
            public IntPtr pszExpandedInformation;
            public IntPtr pszExpandedControlText;
            public IntPtr pszCollapsedControlText;
            public IntPtr footerIcon;
            public IntPtr pszFooter;
            public IntPtr pfCallback;
            public IntPtr lpCallbackData;
            public uint cxWidth;
        }

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate IntPtr CallbackProc(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam, IntPtr lpRefData);

        private const string Marker = "DIAG-MARKER-9F2E";

        private static void Main()
        {
            Console.WriteLine("=== layout diagnostic ===");
            Console.WriteLine($"Pack=1 sizeof={Marshal.SizeOf<TASKDIALOGCONFIG_PACK1>()}  Pack=8 sizeof={Marshal.SizeOf<TASKDIALOGCONFIG_PACK8>()}");

            string r1 = Trial("PACK1", runPack1: true);
            Console.WriteLine($"trial PACK1 : {r1}");

            string r3 = TrialLibLike("LibLike");
            Console.WriteLine($"trial LibLike: {r3}");

            string r2 = Trial("PACK8", runPack1: false);
            Console.WriteLine($"trial PACK8 : {r2}");
        }

        /// <summary>完全复刻库 ShowCore variant0 的配置:标题 + Information 图标(0xFFFD)+ 无 timer/回调节。</summary>
        private static string TrialLibLike(string tag)
        {
            string title = $"{tag}-{Marker}";
            var found = new int[1];
            var watchdog = new Thread(() =>
            {
                for (int i = 0; i < 40; i++)
                {
                    IntPtr hwnd = FindWindowW(null, title);
                    if (hwnd != IntPtr.Zero)
                    {
                        Interlocked.Exchange(ref found[0], 1);
                        Thread.Sleep(400);
                        SendMessage(hwnd, TDM_CLICK_BUTTON, new IntPtr(1), IntPtr.Zero);
                        return;
                    }
                    Thread.Sleep(300);
                }
            }) { IsBackground = true };
            watchdog.Start();

            IntPtr pTitle = Marshal.StringToHGlobalUni(title);
            try
            {
                var cfg = new TASKDIALOGCONFIG_PACK1
                {
                    cbSize = Marshal.SizeOf<TASKDIALOGCONFIG_PACK1>(),
                    dwFlags = 0,                              // 无 timer
                    pszWindowTitle = pTitle,
                    mainIcon = new IntPtr(unchecked((long)(ushort)(short)-3)), // TD_INFORMATION_ICON
                    // 无 pfCallback / pButtons / iinstruction(与库 variant0 一致)
                };
                bool ok = TaskDialogIndirectPack1(ref cfg, out int btn, out _, out _);
                return $"ok={ok} found={found[0]} pnButton={btn}";
            }
            catch (Exception ex)
            {
                return $"EXCEPTION: {ex.GetType().Name}: {ex.Message}";
            }
            finally
            {
                Marshal.FreeHGlobal(pTitle);
            }
        }

        private static string Trial(string tag, bool runPack1)
        {
            string title = $"{tag}-{Marker}";
            var found = new int[1];      // 0=未发现,1=发现窗口
            var timerFired = new int[1];
            var didClose = new int[1];

            string titleForWatch = title;

            var watchdog = new Thread(() =>
            {
                // 每 300ms 尝试 FindWindow;找到后 600ms 点击 OK(1) 关闭
                for (int i = 0; i < 60; i++)
                {
                    IntPtr hwnd = FindWindowW(null, titleForWatch);
                    if (hwnd != IntPtr.Zero)
                    {
                        Interlocked.Exchange(ref found[0], 1);
                        Thread.Sleep(600);
                        SendMessage(hwnd, TDM_CLICK_BUTTON, new IntPtr(1), IntPtr.Zero);
                        Interlocked.Exchange(ref didClose[0], 1);
                        return;
                    }
                    Thread.Sleep(300);
                }
            }) { IsBackground = true };
            watchdog.Start();

            CallbackProc callback = (hwnd, msg, wParam, lParam, lpRef) =>
            {
                if (msg == TDN_TIMER)
                {
                    Interlocked.Exchange(ref timerFired[0], 1);
                    IntPtr t = Marshal.StringToHGlobalUni($"[tick {wParam.ToString()} {tag}]");
                    try
                    {
                        SendMessage(hwnd, 0x0400 + 108 /* TDM_SET_ELEMENT_TEXT */, new IntPtr(3 /* TDE_MAIN_INSTRUCTION */), t);
                        Console.WriteLine($"  [{tag}] TDN_TIMER fired, sent TDM_SET_ELEMENT_TEXT");
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(t);
                    }
                }
                return IntPtr.Zero;
            };
            IntPtr cbPtr = Marshal.GetFunctionPointerForDelegate(callback);

            IntPtr pTitle = Marshal.StringToHGlobalUni(title);
            IntPtr pInstr = Marshal.StringToHGlobalUni($"指令-{tag}");
            try
            {
                int btn;
                bool ok;
                if (runPack1)
                {
                    var cfg = new TASKDIALOGCONFIG_PACK1
                    {
                        cbSize = Marshal.SizeOf<TASKDIALOGCONFIG_PACK1>(),
                        dwFlags = TDF_CALLBACK_TIMER,
                        pszWindowTitle = pTitle,
                        pszMainInstruction = pInstr,
                        pfCallback = cbPtr,
                    };
                    ok = TaskDialogIndirectPack1(ref cfg, out btn, out _, out _);
                }
                else
                {
                    var cfg = new TASKDIALOGCONFIG_PACK8
                    {
                        cbSize = Marshal.SizeOf<TASKDIALOGCONFIG_PACK8>(),
                        dwFlags = TDF_CALLBACK_TIMER,
                        pszWindowTitle = pTitle,
                        pszMainInstruction = pInstr,
                        pfCallback = cbPtr,
                    };
                    ok = TaskDialogIndirectPack8(ref cfg, out btn, out _, out _);
                }
                return $"ok={ok} found={found[0]} timerFired={timerFired[0]} pnButton={btn}";
            }
            catch (Exception ex)
            {
                return $"EXCEPTION: {ex.GetType().Name}: {ex.Message}";
            }
            finally
            {
                Marshal.FreeHGlobal(pTitle);
                Marshal.FreeHGlobal(pInstr);
            }
        }
    }
}
