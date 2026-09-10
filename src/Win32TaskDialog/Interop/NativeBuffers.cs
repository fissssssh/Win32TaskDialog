using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Win32TaskDialog.Interop
{
    /// <summary>
    /// 非托管缓冲区(字符串 + 原始内存)的分配管理器。
    /// 所有分配在 Dispose 时统一释放,保证每次 Show 的内存封闭性。
    /// </summary>
    internal sealed class NativeBuffers : IDisposable
    {
        private readonly List<IntPtr> _allocations = new List<IntPtr>();

        /// <summary>分配一个 Unicode 字符串;text 为 null 时返回 IntPtr.Zero。</summary>
        public IntPtr AllocUnicodeString(string? text)
        {
            if (text is null)
                return IntPtr.Zero;
            IntPtr ptr = Marshal.StringToHGlobalUni(text);
            _allocations.Add(ptr);
            return ptr;
        }

        /// <summary>分配原始内存。</summary>
        public IntPtr Alloc(int byteCount)
        {
            IntPtr ptr = Marshal.AllocHGlobal(byteCount);
            _allocations.Add(ptr);
            return ptr;
        }

        public void Dispose()
        {
            foreach (IntPtr ptr in _allocations)
                Marshal.FreeHGlobal(ptr);
            _allocations.Clear();
        }
    }
}
