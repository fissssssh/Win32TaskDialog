namespace Win32TaskDialog
{
    /// <summary>
    /// 预定义按钮组合。TaskDialog 的自定义按钮由 <see cref="TaskDialogOptions.CustomButtons"/>
    /// 提供,该组合在 <see cref="TaskDialogOptions"/> 构造时自动展开为标准按钮
    /// (英文文字,详见 <see cref="TaskDialogButton"/> 的说明)。
    /// </summary>
    public enum TaskDialogButtons
    {
        /// <summary>
        /// 无预定义按钮;若也未提供 <see cref="TaskDialogOptions.CustomButtons"/>,
        /// 系统会自动显示一个 OK 按钮。
        /// </summary>
        None = 0,

        /// <summary>仅 OK 按钮(IDOK)。</summary>
        Ok,

        /// <summary>OK + Cancel。</summary>
        OkCancel,

        /// <summary>Yes + No。</summary>
        YesNo,

        /// <summary>Yes + No + Cancel。</summary>
        YesNoCancel,

        /// <summary>Retry + Cancel。</summary>
        RetryCancel,

        /// <summary>Abort + Retry + Ignore。</summary>
        AbortRetryIgnore,

        /// <summary>Try Again + Cancel。</summary>
        TryAgainCancel,

        /// <summary>Continue + Cancel。</summary>
        ContinueCancel,
    }
}
