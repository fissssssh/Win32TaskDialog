namespace Win32TaskDialog
{
    /// <summary>
    /// 进度条对话框专属选项,由 <see cref="TaskDialogExtensions.ShowProgress(string,System.Action{TaskDialogProgressReporter,System.Threading.CancellationToken},TaskDialogProgressOptions)"/> 使用。
    /// </summary>
    public class TaskDialogProgressOptions : TaskDialogOptions
    {
        /// <summary>工作委托完成后自动关闭对话框(超时 5 秒内),返回 <see cref="TaskDialogResult.Ok"/>;false 时保持打开直到用户点击按钮。</summary>
        public bool AutoCloseOnComplete { get; set; } = true;

        /// <summary>显示 Cancel 按钮;点击后触发工作委托中的 <see cref="System.Threading.CancellationToken"/>。</summary>
        public bool ShowCancelButton { get; set; } = true;

        /// <summary>
        /// 工作委托抛异常时,在调用线程重新抛出(异常原样保留,含堆栈);
        /// false 时对话框正常关闭并返回 <see cref="TaskDialogResult.Error"/>。
        /// </summary>
        public bool ThrowOnWorkException { get; set; } = true;

        /// <summary>不确定进度(循环滚动的 Marquee 模式),忽略报告的具体值。</summary>
        public bool Marquee { get; set; }
    }
}
