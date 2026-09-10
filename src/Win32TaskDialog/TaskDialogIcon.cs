namespace Win32TaskDialog
{
    /// <summary>
    /// 任务对话框的图标。除 <see cref="Question"/> 外都映射为系统图标资源
    /// (TD_*_ICON 预定义值);<see cref="Question"/> 在 TaskDialog 中无预定义值,
    /// 由库内部通过 LoadIcon(IDI_QUESTION) 加载。
    /// </summary>
    public enum TaskDialogIcon
    {
        /// <summary>不显示任何图标。</summary>
        None = 0,

        /// <summary>信息图标(i in circle)。</summary>
        Information,

        /// <summary>警告图标(感叹号)。</summary>
        Warning,

        /// <summary>错误图标(停止标志)。</summary>
        Error,

        /// <summary>询问图标(问号)。</summary>
        Question,

        /// <summary>安全盾牌图标(Vista+)。</summary>
        Shield,
    }
}
