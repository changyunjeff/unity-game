using System;
using System.Globalization;
using System.IO;
using System.Text;
using Game.Core.Logging.CrashReports;

namespace Game.Core.Logging.CrashUpload
{
    /// <summary>
    /// 把一个崩溃包写成一个 Sentry envelope（一次请求要发的全部内容）：
    /// 一个事件（原因、info.txt 的内容），一个附件（整个包压成的 zip），有内存转储时再加一个 minidump 附件。
    /// Sentry 一个事件只收一个 minidump；包里有好几个时发最新的那个，其余的留在本地。
    /// 时间和事件编号由调用的人给，这样同样的输入得到同样的结果。
    /// </summary>
    public sealed class SentryCrashEnvelope
    {
        private readonly SentryDsn _dsn;
        private readonly string _release;
        private readonly string _environment;

        /// <param name="release">游戏版本，Sentry 按它把崩溃分到各个版本下。</param>
        /// <param name="environment">例如 production、development。</param>
        public SentryCrashEnvelope(SentryDsn dsn, string release, string environment)
        {
            _dsn = dsn ?? throw new ArgumentNullException(nameof(dsn));
            _release = release;
            _environment = environment;
        }

        public byte[] Build(string reportDirectory, Guid eventId, DateTime utcTime)
        {
            if (string.IsNullOrEmpty(reportDirectory) || !Directory.Exists(reportDirectory))
            {
                throw new ArgumentException("崩溃包目录不存在：" + reportDirectory, nameof(reportDirectory));
            }

            string name = Path.GetFileName(reportDirectory.TrimEnd('\\', '/'));
            string infoPath = Path.Combine(reportDirectory, CrashReportStore.InfoFileName);
            string info = File.Exists(infoPath) ? File.ReadAllText(infoPath) : "";
            string minidump = NewestMinidump(reportDirectory);
            string id = eventId.ToString("N");

            using (var stream = new MemoryStream())
            {
                WriteText(stream, "{\"event_id\":" + JsonText.Quote(id) + ",\"dsn\":" + JsonText.Quote(_dsn.Original) + "}\n");

                byte[] eventJson = Encoding.UTF8.GetBytes(EventJson(id, utcTime, name, info, minidump != null));
                WriteItem(stream, "{\"type\":\"event\",\"length\":" + eventJson.Length + "}", eventJson);

                byte[] zip = CrashReportArchive.Zip(reportDirectory);
                WriteItem(stream, "{\"type\":\"attachment\",\"length\":" + zip.Length + ",\"filename\":" + JsonText.Quote(name + ".zip")
                    + ",\"content_type\":\"application/zip\"}", zip);

                if (minidump != null)
                {
                    byte[] dump = File.ReadAllBytes(minidump);
                    WriteItem(stream, "{\"type\":\"attachment\",\"length\":" + dump.Length + ",\"filename\":" + JsonText.Quote(Path.GetFileName(minidump))
                        + ",\"attachment_type\":\"event.minidump\"}", dump);
                }

                return stream.ToArray();
            }
        }

        /// <summary>崩溃包目录名是 <c>年月日-时分秒-原因</c>，有时后面还带序号；取出原因。</summary>
        public static string ReasonOf(string reportName)
        {
            string[] parts = (reportName ?? "").Split('-');
            return parts.Length >= 3 ? parts[2] : "Unknown";
        }

        private string EventJson(string id, DateTime utcTime, string name, string info, bool hasMinidump)
        {
            string reason = ReasonOf(name);
            string message = FirstLine(info);

            return "{"
                + "\"event_id\":" + JsonText.Quote(id)
                + ",\"timestamp\":" + JsonText.Quote(utcTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture))
                + ",\"platform\":" + JsonText.Quote(hasMinidump ? "native" : "other")
                + ",\"level\":\"fatal\""
                + ",\"logger\":\"crash-report\""
                + ",\"release\":" + JsonText.Quote(_release)
                + ",\"environment\":" + JsonText.Quote(_environment)
                + ",\"message\":{\"formatted\":" + JsonText.Quote(message.Length == 0 ? "崩溃包 " + name : message) + "}"
                + ",\"tags\":{\"reason\":" + JsonText.Quote(reason) + "}"
                + ",\"extra\":{\"report\":" + JsonText.Quote(name) + ",\"info\":" + JsonText.Quote(info) + "}"
                + "}";
        }

        private static string FirstLine(string text)
        {
            foreach (string line in text.Split('\n'))
            {
                if (line.Trim().Length > 0)
                {
                    return line.Trim();
                }
            }

            return "";
        }

        private static string NewestMinidump(string reportDirectory)
        {
            string[] dumps = Directory.GetFiles(reportDirectory, "*.dmp", SearchOption.AllDirectories);
            if (dumps.Length == 0)
            {
                return null;
            }

            // Unity 的崩溃目录名里带着时间，按字典序最后一个就是最新的。
            Array.Sort(dumps, StringComparer.Ordinal);
            return dumps[dumps.Length - 1];
        }

        private static void WriteItem(Stream stream, string header, byte[] payload)
        {
            WriteText(stream, header + "\n");
            stream.Write(payload, 0, payload.Length);
            WriteText(stream, "\n");
        }

        private static void WriteText(Stream stream, string text)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            stream.Write(bytes, 0, bytes.Length);
        }
    }
}
