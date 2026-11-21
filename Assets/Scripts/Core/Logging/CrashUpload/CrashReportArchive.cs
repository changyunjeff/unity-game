using System;
using System.IO;
using System.IO.Compression;
using Game.Core.Logging.CrashReports;

namespace Game.Core.Logging.CrashUpload
{
    /// <summary>
    /// 把一个崩溃包目录压成一个 zip。内存转储（.dmp）不放进去：它另外作为 minidump 发，Sentry 才认得出来、解得开；
    /// 上传用的记号文件也不放。
    /// </summary>
    public static class CrashReportArchive
    {
        public static byte[] Zip(string reportDirectory)
        {
            if (string.IsNullOrEmpty(reportDirectory) || !Directory.Exists(reportDirectory))
            {
                throw new ArgumentException("崩溃包目录不存在：" + reportDirectory, nameof(reportDirectory));
            }

            using (var stream = new MemoryStream())
            {
                using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, true))
                {
                    foreach (string relative in CrashReportStore.ListFiles(reportDirectory))
                    {
                        if (!Belongs(relative))
                        {
                            continue;
                        }

                        ZipArchiveEntry entry = zip.CreateEntry(relative.Replace('\\', '/'), CompressionLevel.Optimal);
                        using (Stream output = entry.Open())
                        using (FileStream input = File.OpenRead(Path.Combine(reportDirectory, relative)))
                        {
                            input.CopyTo(output);
                        }
                    }
                }

                return stream.ToArray();
            }
        }

        private static bool Belongs(string relative)
        {
            string name = Path.GetFileName(relative);
            return name != CrashUploadQueue.ApprovedFileName
                && name != CrashUploadQueue.UploadedFileName
                && !string.Equals(Path.GetExtension(name), ".dmp", StringComparison.OrdinalIgnoreCase);
        }
    }
}
