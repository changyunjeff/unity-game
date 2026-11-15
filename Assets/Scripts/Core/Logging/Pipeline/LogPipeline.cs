using System;

namespace Game.Core.Logging.Pipeline
{
    /// <summary>
    /// 日志的管线：盖戳 → 依次过每道工序 → 交给输出。
    /// 它自己不知道有哪些工序、输出到哪里，全由装配的人从构造函数传进来。
    /// 它也是一个 <see cref="ILogSink"/>，所以可以直接装到 <see cref="Log.Output"/> 上。
    /// </summary>
    public sealed class LogPipeline : ILogSink
    {
        private readonly ILogClock _clock;
        private readonly ILogProcessor[] _processors;
        private readonly ILogSink _output;

        public LogPipeline(ILogClock clock, ILogSink output, params ILogProcessor[] processors)
        {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _processors = processors ?? Array.Empty<ILogProcessor>();

            foreach (ILogProcessor processor in _processors)
            {
                if (processor == null)
                {
                    throw new ArgumentException("工序不能是 null", nameof(processors));
                }
            }
        }

        public void Write(in LogEntry entry)
        {
            LogEntry current = entry.WithStamp(_clock.Now());

            for (int i = 0; i < _processors.Length; i++)
            {
                if (!_processors[i].Process(ref current))
                {
                    return;
                }
            }

            _output.Write(in current);
        }
    }
}
