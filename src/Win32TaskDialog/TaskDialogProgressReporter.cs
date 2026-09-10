using System;

namespace Win32TaskDialog
{
    /// <summary>
    /// 进度对话框的进度报告器,由工作委托在后台线程调用。
    /// 实现 <see cref="IProgress{T}"/>,线程安全,可跨线程任意调用。
    /// </summary>
    public sealed class TaskDialogProgressReporter : IProgress<double>
    {
        private readonly object _lock = new object();
        private double _value;
        private string? _latestText;
        private Exception? _exception;
        private bool _completed;

        /// <summary>报告进度(0-100 的百分比),越界值自动截断。</summary>
        public void Report(double value)
        {
            lock (_lock)
            {
                _value = value < 0 ? 0 : value > 100 ? 100 : value;
            }
        }

        /// <summary>更新对话框内容文本(替换 <see cref="TaskDialogOptions.Text"/>)。</summary>
        public void SetText(string? text)
        {
            lock (_lock)
            {
                _latestText = text;
            }
        }

        /// <summary>工作委托正常结束且无异常时调用(通常由库在 work 返回后自动标记)。</summary>
        public void Complete() => SignalCompleted(null);

        internal void SignalCompleted(Exception? exception)
        {
            lock (_lock)
            {
                _exception = exception;
                _completed = true;
            }
        }

        internal double CurrentValue
        {
            get { lock (_lock) return _value; }
        }

        internal string? ConsumeText()
        {
            lock (_lock)
            {
                string? t = _latestText;
                _latestText = null;
                return t;
            }
        }

        /// <summary>取回完成状态与异常(一次性)。</summary>
        internal bool TryGetOutcome(out Exception? exception)
        {
            lock (_lock)
            {
                if (!_completed)
                {
                    exception = null;
                    return false;
                }
                exception = _exception;
                return true;
            }
        }
    }
}
