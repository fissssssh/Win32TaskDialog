using System;
using System.Runtime.InteropServices;

namespace Win32TaskDialog.Interop
{
    /// <summary>
    /// 标记常量与结构体,与 commctrl.h (Windows 10 SDK 26100) 一致。
    /// 注意:TASKDIALOGCONFIG 在 C 头文件中被 &lt;pshpack1.h&gt; 包裹(since the packing of
    /// the structure is set to 1),因此布局必须使用 Pack = 1,否则 64 位下字段偏移错位。
    /// </summary>
    internal static class TaskDialogNativeConstants
    {
        // TASKDIALOG_FLAGS(来自 Windows 10 SDK 26100 的 commctrl.h)
        public const int TDF_ENABLE_HYPERLINKS = 0x0001;
        public const int TDF_USE_HICON_MAIN = 0x0002;
        public const int TDF_USE_HICON_FOOTER = 0x0004;
        public const int TDF_ALLOW_DIALOG_CANCELLATION = 0x0008;
        public const int TDF_USE_COMMAND_LINKS = 0x0010;
        public const int TDF_USE_COMMAND_LINKS_NO_ICON = 0x0020;
        public const int TDF_EXPAND_FOOTER_AREA = 0x0040;
        public const int TDF_EXPANDED_BY_DEFAULT = 0x0080;
        public const int TDF_VERIFICATION_FLAG_CHECKED = 0x0100;
        public const int TDF_SHOW_PROGRESS_BAR = 0x0200;
        public const int TDF_SHOW_MARQUEE_PROGRESS_BAR = 0x0400;
        public const int TDF_CALLBACK_TIMER = 0x0800;
        public const int TDF_POSITION_RELATIVE_TO_WINDOW = 0x1000;
        public const int TDF_RTL_LAYOUT = 0x2000;
        public const int TDF_NO_DEFAULT_RADIO_BUTTON = 0x4000;
        public const int TDF_CAN_BE_MINIMIZED = 0x8000;
        public const int TDF_NO_SET_FOREGROUND = 0x00010000;
        public const int TDF_SIZE_TO_CONTENT = 0x01000000;

        // TASKDIALOG_MESSAGES(WM_USER = 0x0400)
        public const uint TDM_CLICK_BUTTON = 0x0400 + 102;
        public const uint TDM_SET_MARQUEE_PROGRESS_BAR = 0x0400 + 103;
        public const uint TDM_SET_PROGRESS_BAR_STATE = 0x0400 + 104;
        public const uint TDM_SET_PROGRESS_BAR_RANGE = 0x0400 + 105;
        public const uint TDM_SET_PROGRESS_BAR_POS = 0x0400 + 106;
        public const uint TDM_SET_PROGRESS_BAR_MARQUEE = 0x0400 + 107;
        public const uint TDM_SET_ELEMENT_TEXT = 0x0400 + 108;
        public const uint TDM_ENABLE_BUTTON = 0x0400 + 111;
        public const uint TDM_CLICK_VERIFICATION = 0x0400 + 113;
        public const uint TDM_UPDATE_ELEMENT_TEXT = 0x0400 + 114;

        // TASKDIALOG_NOTIFICATIONS
        public const uint TDN_CREATED = 0;
        public const uint TDN_NAVIGATED = 1;
        public const uint TDN_BUTTON_CLICKED = 2;
        public const uint TDN_HYPERLINK_CLICKED = 3;
        public const uint TDN_TIMER = 4;
        public const uint TDN_DESTROYED = 5;
        public const uint TDN_RADIO_BUTTON_CLICKED = 6;
        public const uint TDN_DIALOG_CONSTRUCTED = 7;
        public const uint TDN_VERIFICATION_CLICKED = 8;

        // TASKDIALOG_ELEMENTS
        public const int TDE_CONTENT = 0;
        public const int TDE_EXPANDED_INFORMATION = 1;
        public const int TDE_FOOTER = 2;
        public const int TDE_MAIN_INSTRUCTION = 3;

        // 预定义图标(pszMainIcon 的 MAKEINTRESOURCE 值)
        public const int TD_WARNING_ICON = -1;
        public const int TD_ERROR_ICON = -2;
        public const int TD_INFORMATION_ICON = -3;
        public const int TD_SHIELD_ICON = -4;

        // 常用系统图标 ID(user32,IDI_*;用于 Question 图标,需要 TDF_USE_HICON_MAIN + hMainIcon)
        public const int IDI_QUESTION = 32514;

        // callback 返回
        public const int S_OK = 0;
        public const int S_FALSE = 1;
    }

    /// <summary>
    /// 原生 TASKDIALOG_BUTTON(同样位于头文件 &lt;pshpack1.h&gt; 区域,必须 Pack = 1,
    /// 否则 x64 下元素大小 16 字节而 comctl32 按 12 字节读取,按钮文字指针错位)。
    /// 文本为独立分配的非托管字符串,由调用方释放。
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
    internal struct TASKDIALOG_BUTTON
    {
        public int nButtonID;
        public IntPtr pszButtonText;
    }

    /// <summary>
    /// 原生 TASKDIALOGCONFIG(现行 SDK 布局,含 dwCommonButtons 字段)。
    /// 字符串统一为 IntPtr(由调用方 AllocHGlobal/释放),避免封送器分配难以释放;
    /// 按钮通过 pButtons 自定义数组提供,dwCommonButtons 恒为 0(受检后由运行时验证)。
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
    internal struct TASKDIALOGCONFIG
    {
        public int cbSize;
        public IntPtr hwndParent;
        public IntPtr hInstance;
        public int dwFlags;
        public int dwCommonButtons;      // TDCBF_*(本库恒为 0,始终使用 pButtons 自定义按钮)
        public IntPtr pszWindowTitle;
        public IntPtr mainIcon;          // union: HICON(设 TDF_USE_HICON_MAIN)或 MAKEINTRESOURCE PCWSTR
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
        public IntPtr footerIcon;        // union: HICON 或 MAKEINTRESOURCE PCWSTR
        public IntPtr pszFooter;
        public IntPtr pfCallback;        // 回调委托指针(由调用方 GCHandle 保活)
        public IntPtr lpCallbackData;
        public uint cxWidth;
    }
}
