using System.Globalization;
using System.Text;

namespace Game.Core.Logging.Sinks
{
    /// <summary>一行日志写成文字时长什么样。文件和崩溃包都用它，想换格式就换一个实现。</summary>
    public interface ILogFormatter
    {
        string Format(in LogEntry entry);
    }

    /// <summary>
    /// 固定的纯文本格式：
    /// <c>12:03:45.120 F1234 E [Save] 读档失败</c>，依次是时间、帧号、级别字母（E、W、I、V、VV）、分类、内容。
    /// 调用栈缩进四格写在下面几行。搜 <c> E [</c> 就是所有错误。
    /// </summary>
    public sealed class PlainTextLogFormatter : ILogFormatter
    {
        public static readonly PlainTextLogFormatter Instance = new PlainTextLogFormatter();

        private PlainTextLogFormatter()
        {
        }

        public string Format(in LogEntry entry)
        {
            var builder = new StringBuilder(128);
            builder.Append(entry.Stamp.Time.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture));
            builder.Append(" F").Append(entry.Stamp.Frame.ToString(CultureInfo.InvariantCulture));
            builder.Append(' ').Append(LevelLetter(entry.Level));
            builder.Append(" [").Append(entry.Category).Append("] ");
            builder.Append(entry.Message);

            if (!string.IsNullOrEmpty(entry.StackTrace))
            {
                foreach (string line in entry.StackTrace.Split('\n'))
                {
                    string trimmed = line.Trim();
                    if (trimmed.Length > 0)
                    {
                        builder.Append("\n    ").Append(trimmed);
                    }
                }
            }

            return builder.ToString();
        }

        private static string LevelLetter(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Error:
                    return "E";
                case LogLevel.Warning:
                    return "W";
                case LogLevel.Info:
                    return "I";
                case LogLevel.Verbose:
                    return "V";
                default:
                    return "VV";
            }
        }
    }
}
