using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Game.Core.Logging.Sinks
{
    /// <summary>
    /// 去处：把日志写进一个文件。文件叫什么、目录里留几个，是 <see cref="LogFileDirectory"/> 的事；
    /// 一行写成什么样，是 <see cref="ILogFormatter"/> 的事；它只管写。
    /// Info 及以上的每一行写完立刻落盘，紧跟着崩溃也不会丢；Verbose 以下攒着一起写。
    /// 文件写到上限就停，并在末尾注明。
    /// </summary>
    public sealed class FileLogSink : ILogSink, IDisposable
    {
        private readonly object _gate = new object();
        private readonly ILogFormatter _formatter;
        private readonly long _maxBytes;
        private StreamWriter _writer;
        private long _bytes;

        /// <param name="filePath">要新建的文件。已经存在会抛异常。</param>
        /// <param name="headerLines">写在文件开头的内容，例如这次运行的版本和机器信息。</param>
        public FileLogSink(string filePath, ILogFormatter formatter, IEnumerable<string> headerLines = null, long maxBytes = 10L * 1024 * 1024)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException("日志文件的路径不能为空", nameof(filePath));
            }

            _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
            _maxBytes = maxBytes;
            FilePath = filePath;

            // 别的程序（崩溃包、文本编辑器）可以同时读这个文件。
            var stream = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
            _writer = new StreamWriter(stream, new UTF8Encoding(false));

            if (headerLines != null)
            {
                foreach (string line in headerLines)
                {
                    WriteLine(line);
                }
            }

            _writer.Flush();
        }

        public string FilePath { get; }

        public void Write(in LogEntry entry)
        {
            lock (_gate)
            {
                if (_writer == null)
                {
                    return;
                }

                WriteLine(_formatter.Format(in entry));

                if (_bytes > _maxBytes)
                {
                    WriteLine("日志文件已到上限，这次运行后面的日志不再写入。");
                    Close();
                    return;
                }

                if (entry.Level <= LogLevel.Info)
                {
                    _writer.Flush();
                }
            }
        }

        /// <summary>把攒着的内容写到磁盘上。</summary>
        public void Flush()
        {
            lock (_gate)
            {
                _writer?.Flush();
            }
        }

        public void Dispose()
        {
            lock (_gate)
            {
                Close();
            }
        }

        private void WriteLine(string line)
        {
            _writer.Write(line);
            _writer.Write('\n');
            _bytes += Encoding.UTF8.GetByteCount(line) + 1;
        }

        private void Close()
        {
            _writer?.Dispose();
            _writer = null;
        }
    }
}
