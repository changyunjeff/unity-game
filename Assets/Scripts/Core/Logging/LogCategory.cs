using System;

namespace Game.Core.Logging
{
    /// <summary>
    /// 日志分类。一个模块一个，名字和模块名一致，各自能单独调级别。
    /// 用法：在模块里放一个静态字段
    /// <c>private static readonly LogCategory LogSave = new LogCategory("Save");</c>
    /// </summary>
    public sealed class LogCategory
    {
        public LogCategory(string name, LogLevel maxLevel = LogLevel.Info)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("日志分类必须有名字", nameof(name));
            }

            Name = name;
            MaxLevel = maxLevel;
        }

        /// <summary>分类名，会出现在每行日志的开头。</summary>
        public string Name { get; }

        /// <summary>放行到哪一级为止。比它更啰嗦的不输出。运行时可以改。</summary>
        public LogLevel MaxLevel { get; set; }

        /// <summary>这一级的日志现在会不会输出。</summary>
        public bool IsEnabled(LogLevel level) => level <= MaxLevel;
    }
}
