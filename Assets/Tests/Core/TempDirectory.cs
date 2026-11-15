using System;
using System.IO;
using System.Text;

namespace Game.Core.Tests
{
    /// <summary>测试用的临时目录。用完 <see cref="Dispose"/> 连同里面的东西一起删掉。</summary>
    public sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "game-core-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public string Combine(string name) => System.IO.Path.Combine(Path, name);

        /// <summary>读一个可能还开着在写的文件。</summary>
        public static string ReadAll(string path)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                return reader.ReadToEnd();
            }
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, true);
            }
        }
    }
}
