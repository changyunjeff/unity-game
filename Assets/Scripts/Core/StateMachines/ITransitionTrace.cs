using System;
using Game.Core.Handles;

namespace Game.Core.StateMachines
{
    /// <summary>
    /// 走边的记录者。状态机每走一条边通知它一次，以后的黑匣子就接在这里。
    /// 实现的人不能在这里改状态机，也不能打日志（它会在遍历大量对象的循环里被调用）。
    /// </summary>
    public interface ITransitionTrace
    {
        /// <param name="owner">走边的是哪个对象；没指定就是空句柄。</param>
        void OnTransition<TState>(Handle owner, TState from, TState to, byte reason) where TState : struct, Enum;
    }

    /// <summary>什么都不记的记录者。不指定记录者时的默认值。</summary>
    public sealed class NullTransitionTrace : ITransitionTrace
    {
        public static readonly NullTransitionTrace Instance = new NullTransitionTrace();

        private NullTransitionTrace()
        {
        }

        public void OnTransition<TState>(Handle owner, TState from, TState to, byte reason) where TState : struct, Enum
        {
        }
    }
}
