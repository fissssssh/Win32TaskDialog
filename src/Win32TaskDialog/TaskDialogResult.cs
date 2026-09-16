namespace Win32TaskDialog
{
    /// <summary>
    /// 表示 <see cref="TaskDialog"/> 对话框的返回结果。
    /// </summary>
    public enum TaskDialogResult
    {
        /// <summary>
        /// 未返回任何结果。进度对话框(见 <see cref="TaskDialogExtensions.ShowProgress(string,System.Action{TaskDialogProgressReporter,System.Threading.CancellationToken},TaskDialogProgressOptions)"/>)中,
        /// 用户点击了可见按钮(如 OK)但工作委托尚未完成时返回——工作被提前中断,
        /// 因此不应误报为 <see cref="Ok"/>。
        /// </summary>
        None = 0,

        /// <summary>用户点击了 "OK" 按钮。值为 Win32 的 IDOK。</summary>
        Ok = 1,

        /// <summary>用户点击了 "Cancel" 按钮。值为 Win32 的 IDCANCEL。</summary>
        Cancel = 2,

        /// <summary>用户点击了 "Abort" 按钮。值为 Win32 的 IDABORT。</summary>
        Abort = 3,

        /// <summary>用户点击了 "Retry" 按钮。值为 Win32 的 IDRETRY。</summary>
        Retry = 4,

        /// <summary>用户点击了 "Ignore" 按钮。值为 Win32 的 IDIGNORE。</summary>
        Ignore = 5,

        /// <summary>用户点击了 "Yes" 按钮。值为 Win32 的 IDYES。</summary>
        Yes = 6,

        /// <summary>用户点击了 "No" 按钮。值为 Win32 的 IDNO。</summary>
        No = 7,

        /// <summary>用户点击了 "Close" 按钮。值为 Win32 的 IDCLOSE。</summary>
        Close = 8,

        /// <summary>用户点击了 "Try Again" 按钮。值为 Win32 的 IDTRYAGAIN。</summary>
        TryAgain = 10,

        /// <summary>用户点击了 "Continue" 按钮。值为 Win32 的 IDCONTINUE。</summary>
        Continue = 11,

        /// <summary>
        /// 对话框的超时时间到达(仅当设置了 <see cref="TaskDialogOptions.Timeout"/>)。
        /// 值 0x7FFE 位于 Win32 的用户按钮 ID 范围之外,避免冲突。
        /// </summary>
        Timeout = 0x7FFE,

        /// <summary>
        /// 进度对话框(见 <see cref="TaskDialogExtensions"/>)的工作委托抛出异常,
        /// 且 <see cref="TaskDialogProgressOptions.ThrowOnWorkException"/> 为 false 时返回。
        /// </summary>
        Error = 0x7FFC,
    }
}
