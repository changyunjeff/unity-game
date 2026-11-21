using System;

namespace Game.Core.Logging.CrashReports
{
    /// <summary>
    /// 决定要不要问玩家"发不发崩溃包"，并记下玩家的回答。界面只负责画，画什么、什么时候画都问它。
    ///
    /// 规矩：
    /// 只为没人接住的异常和上次没正常退出这两种包问，手动写的不问；
    /// 一次运行最多问一次；玩家选过"以后都不问"就不再问；
    /// 不弹的环境（编辑器）里什么都不问。
    ///
    /// 崩溃包可能在任何线程上写完，界面在主线程上读，所以内部加锁。
    /// </summary>
    public sealed class CrashPrompt
    {
        private static readonly LogCategory LogCrash = new LogCategory("Crash");

        private readonly object _lock = new object();
        private readonly CrashPromptOptOut _optOut;
        private readonly CrashUploadQueue _uploads;
        private bool _offered;
        private bool _hasPending;
        private CrashReportWritten _pending;

        /// <param name="enabled">这个环境里要不要弹。编辑器里不弹。</param>
        public CrashPrompt(CrashPromptOptOut optOut, CrashUploadQueue uploads, bool enabled)
        {
            _optOut = optOut ?? throw new ArgumentNullException(nameof(optOut));
            _uploads = uploads ?? throw new ArgumentNullException(nameof(uploads));
            Enabled = enabled;
        }

        /// <summary>这个环境里会不会弹。</summary>
        public bool Enabled { get; }

        /// <summary>一个崩溃包写完了。要问玩家就记下来等界面来取，返回 true；不问返回 false，并在日志里说明为什么。</summary>
        public bool Offer(CrashReportWritten report)
        {
            if (!Enabled)
            {
                return false;
            }

            if (report.Reason != CrashReasons.Exception && report.Reason != CrashReasons.UncleanExit)
            {
                return false;
            }

            lock (_lock)
            {
                if (_offered)
                {
                    Log.Info(LogCrash, "这次运行已经问过玩家，不再弹崩溃界面");
                    return false;
                }

                if (_optOut.IsOptedOut)
                {
                    Log.Info(LogCrash, "玩家选过以后都不问，不弹崩溃界面");
                    return false;
                }

                _offered = true;
                _hasPending = true;
                _pending = report;
            }

            return true;
        }

        /// <summary>有没有一个正等着玩家回答的崩溃包。</summary>
        public bool TryGetPending(out CrashReportWritten report)
        {
            lock (_lock)
            {
                report = _pending;
                return _hasPending;
            }
        }

        /// <summary>记下玩家的回答。之后界面收起，游戏接着跑。</summary>
        public void Answer(CrashPromptChoice choice)
        {
            CrashReportWritten report;
            lock (_lock)
            {
                if (!_hasPending)
                {
                    throw new InvalidOperationException("没有正等着回答的崩溃包");
                }

                report = _pending;
                _hasPending = false;
                _pending = default;
            }

            switch (choice)
            {
                case CrashPromptChoice.Send:
                    _uploads.Approve(report.Directory);
                    Log.Info(LogCrash, "玩家同意发送崩溃包 " + report.Directory);
                    break;
                case CrashPromptChoice.DontSend:
                    Log.Info(LogCrash, "玩家这次不发送崩溃包");
                    break;
                case CrashPromptChoice.NeverAsk:
                    _optOut.OptOut();
                    Log.Info(LogCrash, "玩家选了以后都不问");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(choice), choice, null);
            }
        }
    }
}
