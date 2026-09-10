using System;

namespace Win32TaskDialog
{
    /// <summary>
    /// 任务对话框中的一个按钮。Win32 的任务对话框没有"标准按钮标志",
    /// 所有按钮都以 <see cref="Text"/> + <see cref="Id"/> 的形式传入;
    /// 预定义实例(如 <see cref="TaskDialogButton.Ok"/>)使用英文文字,
    /// 需要本地化时请通过 <see cref="TaskDialogOptions.CustomButtons"/> 自定义。
    /// </summary>
    public sealed class TaskDialogButton
    {
        /// <summary>
        /// 创建按钮。Win32 以按钮 ID 作为返回码,用户自定义 ID 建议从 100 开始,
        /// 避免与标准 ID(1-11)冲突。
        /// </summary>
        /// <param name="id">按钮 ID,同时作为对话框的返回结果。</param>
        /// <param name="text">按钮文字;为 null 时不显示文字。</param>
        /// <param name="isDefault">是否为默认按钮。</param>
        public TaskDialogButton(int id, string? text, bool isDefault = false)
        {
            if (id == 0)
                throw new ArgumentOutOfRangeException(nameof(id), "按钮 ID 不能为 0。");

            Id = id;
            Text = text;
            IsDefault = isDefault;
        }

        /// <summary>按钮 ID,同时也是对话框的返回结果。</summary>
        public int Id { get; }

        /// <summary>按钮文字(为 null 时不显示)。</summary>
        public string? Text { get; }

        /// <summary>是否为默认按钮。</summary>
        public bool IsDefault { get; }

        /// <summary>OK 按钮(ID 1,Win32 IDOK)。</summary>
        public static TaskDialogButton Ok { get; } = new TaskDialogButton(1, "OK");

        /// <summary>Cancel 按钮(ID 2,Win32 IDCANCEL)。</summary>
        public static TaskDialogButton Cancel { get; } = new TaskDialogButton(2, "Cancel");

        /// <summary>Abort 按钮(ID 3,Win32 IDABORT)。</summary>
        public static TaskDialogButton Abort { get; } = new TaskDialogButton(3, "Abort");

        /// <summary>Retry 按钮(ID 4,Win32 IDRETRY)。</summary>
        public static TaskDialogButton Retry { get; } = new TaskDialogButton(4, "Retry");

        /// <summary>Ignore 按钮(ID 5,Win32 IDIGNORE)。</summary>
        public static TaskDialogButton Ignore { get; } = new TaskDialogButton(5, "Ignore");

        /// <summary>Yes 按钮(ID 6,Win32 IDYES)。</summary>
        public static TaskDialogButton Yes { get; } = new TaskDialogButton(6, "Yes");

        /// <summary>No 按钮(ID 7,Win32 IDNO)。</summary>
        public static TaskDialogButton No { get; } = new TaskDialogButton(7, "No");

        /// <summary>Close 按钮(ID 8,Win32 IDCLOSE)。</summary>
        public static TaskDialogButton Close { get; } = new TaskDialogButton(8, "Close");

        /// <summary>Try Again 按钮(ID 10,Win32 IDTRYAGAIN)。</summary>
        public static TaskDialogButton TryAgain { get; } = new TaskDialogButton(10, "Try Again");

        /// <summary>Continue 按钮(ID 11,Win32 IDCONTINUE)。</summary>
        public static TaskDialogButton Continue { get; } = new TaskDialogButton(11, "Continue");
    }
}
