using System;
using System.Threading;
using Game.Core.Logging;
using Game.Core.Logging.Pipeline;
using UnityEngine;
using UnityEngine.LowLevel;

namespace Game.Engine.Logging
{
    /// <summary>
    /// 时钟：给每行日志提供时间和帧号。
    /// 帧号只能在主线程上问 Unity，而日志可能在任何线程上打，所以每帧开头在主线程上记一次，别的线程读记下的那个数。
    /// </summary>
    internal sealed class UnityLogClock : ILogClock
    {
        // 帧号对整个进程只有一个，所以是静态的。
        private static int _frame;

        public LogStamp Now() => new LogStamp(DateTime.Now, Volatile.Read(ref _frame));

        /// <summary>往 Unity 每帧要跑的步骤的最前面加一步：记下帧号。重复调用只加一次。</summary>
        internal static void TrackFrames()
        {
            PlayerLoopSystem loop = PlayerLoop.GetCurrentPlayerLoop();
            PlayerLoopSystem[] systems = loop.subSystemList ?? Array.Empty<PlayerLoopSystem>();

            foreach (PlayerLoopSystem system in systems)
            {
                if (system.type == typeof(UnityLogClock))
                {
                    return;
                }
            }

            var withOurs = new PlayerLoopSystem[systems.Length + 1];
            withOurs[0] = new PlayerLoopSystem { type = typeof(UnityLogClock), updateDelegate = RememberFrame };
            Array.Copy(systems, 0, withOurs, 1, systems.Length);

            loop.subSystemList = withOurs;
            PlayerLoop.SetPlayerLoop(loop);
        }

        private static void RememberFrame() => Volatile.Write(ref _frame, Time.frameCount);
    }
}
