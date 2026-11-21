using System;
using System.Collections.Generic;
using System.IO;

namespace Game.Core.Logging.CrashReports
{
    /// <summary>
    /// 玩家同意发送的崩溃包。同意时在包的目录里放一个记号文件，上传的那一步只发带记号的包。
    /// 记号跟着包走：包被当作旧包清掉，记号也一起没了。
    /// </summary>
    public sealed class CrashUploadQueue
    {
        /// <summary>放在崩溃包目录里、表示玩家同意发送的记号文件。</summary>
        public const string ApprovedFileName = "upload-approved";

        public CrashUploadQueue(string rootDirectory)
        {
            if (string.IsNullOrEmpty(rootDirectory))
            {
                throw new ArgumentException("崩溃包目录不能为空", nameof(rootDirectory));
            }

            RootDirectory = rootDirectory;
        }

        public string RootDirectory { get; }

        /// <summary>记下玩家同意发送这个崩溃包。</summary>
        public void Approve(string reportDirectory)
        {
            if (string.IsNullOrEmpty(reportDirectory) || !Directory.Exists(reportDirectory))
            {
                throw new ArgumentException("崩溃包目录不存在：" + reportDirectory, nameof(reportDirectory));
            }

            File.WriteAllText(Path.Combine(reportDirectory, ApprovedFileName), "");
        }

        /// <summary>玩家同意过发送这个崩溃包没有。</summary>
        public bool IsApproved(string reportDirectory) => File.Exists(Path.Combine(reportDirectory, ApprovedFileName));

        /// <summary>所有玩家同意发送的崩溃包的目录，旧的在前。</summary>
        public IReadOnlyList<string> Approved()
        {
            if (!Directory.Exists(RootDirectory))
            {
                return Array.Empty<string>();
            }

            string[] directories = Directory.GetDirectories(RootDirectory);
            Array.Sort(directories, StringComparer.Ordinal);

            var approved = new List<string>();
            foreach (string directory in directories)
            {
                if (IsApproved(directory))
                {
                    approved.Add(directory);
                }
            }

            return approved;
        }
    }
}
