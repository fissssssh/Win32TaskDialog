namespace Win32TaskDialog
{
    /// <summary>
    /// 进度条对话框专属选项,由 <see cref="TaskDialogExtensions.ShowProgress(string,System.Action{TaskDialogProgressReporter,System.Threading.CancellationToken},TaskDialogProgressOptions)"/> 使用。
    /// </summary>
    public class TaskDialogProgressOptions : TaskDialogOptions
    {
        /// <summary>
        /// 工作委托完成后自动关闭对话框并返回 <see cref="TaskDialogResult.Ok"/>;
        /// false 时对话框保持打开,直到用户点击按钮,结果即该按钮的 ID。
        /// </summary>
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

        /// <summary>
        /// 复制一份配置。<see cref="TaskDialogExtensions.ShowProgress(string,System.Action{TaskDialogProgressReporter,System.Threading.CancellationToken},TaskDialogProgressOptions)"/>
        /// 在副本上覆盖按钮与取消语义,从而不修改调用方传入的实例。
        /// </summary>
        internal TaskDialogProgressOptions Clone() => new TaskDialogProgressOptions
        {
            OwnerHandle = OwnerHandle,
            Title = Title,
            Instruction = Instruction,
            Text = Text,
            Icon = Icon,
            Buttons = Buttons,
            CustomButtons = CustomButtons,
            DefaultButton = DefaultButton,
            VerificationText = VerificationText,
            VerificationChecked = VerificationChecked,
            ExpandedInformation = ExpandedInformation,
            ExpandedControlText = ExpandedControlText,
            CollapsedControlText = CollapsedControlText,
            FooterText = FooterText,
            UseCommandLinks = UseCommandLinks,
            Cancelable = Cancelable,
            CanBeMinimized = CanBeMinimized,
            Timeout = Timeout,
            MinimumWidth = MinimumWidth,
            AutoCloseOnComplete = AutoCloseOnComplete,
            ShowCancelButton = ShowCancelButton,
            ThrowOnWorkException = ThrowOnWorkException,
            Marquee = Marquee,
        };
    }
}
