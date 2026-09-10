using System;
using System.Runtime.InteropServices;

namespace Win32TaskDialog.Interop
{
    /// <summary>
    /// comctl32 版本 6(Common Controls)加载辅助,TaskDialog 仅在该版本中提供。
    /// Windows 10 1809+ 默认加载 v6;更老的系统需要宿主应用在清单中声明
    /// common-controls v6 依赖(--;与 WinForms/WPF 默认清单一致)。
    /// </summary>
    internal static class ComCtl32
    {
        private static readonly object _lock = new object();
        private static bool _initialized;
        private static bool _initFailed;
        private static Exception? _initException;

        /// <summary>
        /// 确保 comctl32 v6 被初始化。TaskDialog 只在 v6 中实现,
        /// 若平台不支持(如非 Windows 系统、无 v6 清单的老系统)则抛出
        /// <see cref="PlatformNotSupportedException"/>。调用结果缓存,进程生命周期内只尝试一次。
        /// </summary>
        internal static void EnsureInitialized()
        {
            lock (_lock)
            {
                if (_initialized)
                    return;
                if (_initFailed)
                    throw _initException!;

                try
                {
                    var icc = new NativeMethods.INITCOMMONCONTROLSEX
                    {
                        dwSize = (uint)Marshal.SizeOf<NativeMethods.INITCOMMONCONTROLSEX>(),
                        dwICC = NativeMethods.ICC_STANDARD_CLASSES,
                    };
                    NativeMethods.InitCommonControlsEx(ref icc);
                    // 调用一次以触发 comctl32.dll 的加载;InitCommonControlsEx 失败不影响本库,
                    // 因为 TaskDialogIndirect 的可用性最终以是否抛出 EntryPointNotFoundException 判断。
                    _initialized = true;
                }
                catch (Exception ex)
                {
                    _initFailed = true;
                    _initException = ex;
                    throw;
                }
            }
        }
    }
}
