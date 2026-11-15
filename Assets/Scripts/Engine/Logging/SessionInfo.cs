using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Engine.Logging
{
    /// <summary>这次运行的信息：写在日志文件开头，也写进每个崩溃包。不含机器名和用户名。</summary>
    public static class SessionInfo
    {
        /// <summary>要在主线程上调用。</summary>
        public static IReadOnlyList<string> Collect(DateTime start)
        {
            return new[]
            {
                "游戏：" + Application.productName + " " + Application.version,
                "Unity：" + Application.unityVersion,
                "平台：" + Application.platform,
                "系统：" + SystemInfo.operatingSystem,
                "处理器：" + SystemInfo.processorType + "（" + SystemInfo.processorCount + " 线程）",
                "内存：" + SystemInfo.systemMemorySize + " MB",
                "显卡：" + SystemInfo.graphicsDeviceName + "（显存 " + SystemInfo.graphicsMemorySize + " MB）",
                "运行编号：" + Guid.NewGuid().ToString("N"),
                "启动时间：" + start.ToString("yyyy-MM-dd HH:mm:ss"),
            };
        }
    }
}
