using System;
using System.Collections.Generic;
using Game.Core.Handles;
using Game.Core.StateMachines;

namespace Game.Core.Tests.StateMachines
{
    /// <summary>测试用的例子：一扇门。</summary>
    public enum Door : byte
    {
        Closed,
        Opening,
        Open,
        Broken,
    }

    /// <summary>门的上下文：边的条件读它，动作写它。</summary>
    public sealed class DoorContext
    {
        public bool Pushed { get; set; }

        public bool Kicked { get; set; }

        /// <summary>动作按执行的先后在这里留名。</summary>
        public List<string> Calls { get; } = new List<string>();
    }

    /// <summary>测试用的两个句柄：代表两扇不同的门。</summary>
    public static class TestHandles
    {
        public static readonly Handle First = new Handle(1, 1);
        public static readonly Handle Second = new Handle(2, 1);
    }

    /// <summary>测试用的走边记录者：把每条边记下来。</summary>
    public sealed class ListTrace : ITransitionTrace
    {
        public List<(Handle owner, object from, object to, byte reason)> Edges { get; } =
            new List<(Handle, object, object, byte)>();

        public void OnTransition<TState>(Handle owner, TState from, TState to, byte reason) where TState : struct, Enum
        {
            Edges.Add((owner, from, to, reason));
        }
    }
}
