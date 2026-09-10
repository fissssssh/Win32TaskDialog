using System;
using System.Collections.Generic;

namespace Win32TaskDialog
{
    /// <summary>
    /// 任务对话框的完整配置。由 <see cref="TaskDialog.Show(TaskDialogOptions)"/> 使用。
    /// </summary>
    public class TaskDialogOptions
    {
        /// <summary>父窗口的 HWND;0 表示无父窗口,对话框居中于显示器。</summary>
        public IntPtr OwnerHandle { get; set; } = IntPtr.Zero;

        /// <summary>窗口标题;null 时显示宿主机可执行文件名。</summary>
        public string? Title { get; set; }

        /// <summary>大号主文本(Instruction)。</summary>
        public string? Instruction { get; set; }

        /// <summary>常规内容文本。</summary>
        public string? Text { get; set; }

        /// <summary>对话框图标。</summary>
        public TaskDialogIcon Icon { get; set; } = TaskDialogIcon.Information;

        /// <summary>预定义按钮组合(与本组合互斥,见 <see cref="CustomButtons"/>)。</summary>
        public TaskDialogButtons Buttons { get; set; } = TaskDialogButtons.Ok;

        /// <summary>自定义按钮;设置后优先于 <see cref="Buttons"/>。文字允许包含换行以使用"主文本 + 说明"式的命令链接。</summary>
        public IReadOnlyList<TaskDialogButton>? CustomButtons { get; set; }

        /// <summary>默认按钮;不设置时是对话框中的第一个按钮。</summary>
        public TaskDialogButton? DefaultButton { get; set; }

        /// <summary>验证(勾选)框文字;null 时不显示复选框。</summary>
        public string? VerificationText { get; set; }

        /// <summary>验证框初始勾选状态。</summary>
        public bool VerificationChecked { get; set; }

        /// <summary>"详细信息"展开区文本;null 时不显示展开区。</summary>
        public string? ExpandedInformation { get; set; }

        /// <summary>展开按钮文字("详细信息");缺省时库默认 "详细信息(&amp;D)"。</summary>
        public string? ExpandedControlText { get; set; }

        /// <summary>收起按钮文字("隐藏");缺省时库默认 "隐藏(&amp;H)"。</summary>
        public string? CollapsedControlText { get; set; }

        /// <summary>页脚文本(null 时不显示页脚)。</summary>
        public string? FooterText { get; set; }

        /// <summary>自定义按钮以命令链接(带项表示)渲染。</summary>
        public bool UseCommandLinks { get; set; }

        /// <summary>允许通过 ESC 键、Alt+F4 与窗口关闭按钮关闭对话框。</summary>
        public bool Cancelable { get; set; }

        /// <summary>对话框允许最小化到任务栏(需设定超时/定时回调时通过 timer 自动刷新)。</summary>
        public bool CanBeMinimized { get; set; }

        /// <summary>超时自动关闭,超时结果见 <see cref="TaskDialogResult.Timeout"/>;Zero 表示不超时。</summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.Zero;

        /// <summary>对话框内容区的最小宽度,单位是对话框单位(DLU);0 表示自动计算。</summary>
        public int MinimumWidth { get; set; }
    }
}
