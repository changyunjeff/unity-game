using System;

namespace Game.Core.Logging.Sinks
{
    /// <summary>
    /// 把多个去处合成一个：收到一行，就交给里面的每一个。
    /// 它还负责隔离故障：哪个去处写的时候抛了异常，就把它拿掉，并在剩下的去处留一条错误。
    /// 一个去处坏了，既不连累别的去处，也不连累打日志的游戏代码。
    /// 运行中可以增减去处；增减和写入可以在不同线程上同时发生。
    /// </summary>
    public sealed class CompositeLogSink : ILogSink
    {
        private const string OwnCategory = "Log";

        private readonly object _gate = new object();

        // 增减时整个换成一个新数组，写入的一方拿到的永远是一份完整的旧数组或新数组。
        private ILogSink[] _sinks = Array.Empty<ILogSink>();

        public CompositeLogSink(params ILogSink[] sinks)
        {
            if (sinks == null)
            {
                return;
            }

            foreach (ILogSink sink in sinks)
            {
                Add(sink);
            }
        }

        /// <summary>现在有几个去处。</summary>
        public int Count => _sinks.Length;

        /// <summary>加一个去处。同一个去处加两次会抛异常。</summary>
        public void Add(ILogSink sink)
        {
            if (sink == null)
            {
                throw new ArgumentNullException(nameof(sink));
            }

            lock (_gate)
            {
                if (Array.IndexOf(_sinks, sink) >= 0)
                {
                    throw new InvalidOperationException("这个去处已经加过了。");
                }

                var sinks = new ILogSink[_sinks.Length + 1];
                Array.Copy(_sinks, sinks, _sinks.Length);
                sinks[_sinks.Length] = sink;
                _sinks = sinks;
            }
        }

        /// <summary>拿掉一个去处。返回它原先在不在。</summary>
        public bool Remove(ILogSink sink)
        {
            lock (_gate)
            {
                int index = Array.IndexOf(_sinks, sink);
                if (index < 0)
                {
                    return false;
                }

                var sinks = new ILogSink[_sinks.Length - 1];
                Array.Copy(_sinks, 0, sinks, 0, index);
                Array.Copy(_sinks, index + 1, sinks, index, sinks.Length - index);
                _sinks = sinks;
                return true;
            }
        }

        public void Write(in LogEntry entry)
        {
            ILogSink[] sinks = _sinks;

            for (int i = 0; i < sinks.Length; i++)
            {
                try
                {
                    sinks[i].Write(in entry);
                }
                catch (Exception exception)
                {
                    Isolate(sinks[i], exception, entry.Stamp);
                }
            }
        }

        private void Isolate(ILogSink failed, Exception exception, LogStamp stamp)
        {
            if (!Remove(failed))
            {
                return; // 别的线程已经把它拿掉并报告过了
            }

            var report = new LogEntry(
                stamp, LogLevel.Error, OwnCategory,
                "去处 " + failed.GetType().Name + " 写入失败，已停用：" + exception.Message,
                exception.ToString(), LogSource.Game, false);
            Write(in report);
        }
    }
}
