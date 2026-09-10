using System;
using System.Runtime.InteropServices;

namespace Win32TaskDialog.Interop
{
    /// <summary>Win32 P/Invoke 声明。所有结构体布局均由本库手工管理(参见 TaskDialogConfig.cs)。</summary>
    internal static partial class NativeMethods
    {
        // ---- comctl32.dll ----

        [DllImport("comctl32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int TaskDialogIndirect(
            ref TASKDIALOGCONFIG pTaskConfig,
            out int pnButton,
            out int pnRadioButton,
            [MarshalAs(UnmanagedType.Bool)] out bool pfVerificationFlagChecked);

        [StructLayout(LayoutKind.Sequential)]
        internal struct INITCOMMONCONTROLSEX
        {
            public uint dwSize;
            public uint dwICC;
        }

        [DllImport("comctl32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool InitCommonControlsEx(ref INITCOMMONCONTROLSEX lpInitCtrls);

        internal const uint ICC_STANDARD_CLASSES = 0x0001;

        // ---- user32.dll ----

        [DllImport("user32.dll", EntryPoint = "SendMessageW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "LoadIconW", ExactSpelling = true)]
        internal static extern IntPtr LoadIcon(IntPtr hInstance, IntPtr lpIconName);
    }
}
