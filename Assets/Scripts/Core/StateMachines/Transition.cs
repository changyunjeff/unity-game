using System;

namespace Game.Core.StateMachines
{
    /// <summary>
    /// 一条边：处在 <see cref="From"/> 且 <see cref="Condition"/> 为真时，转到 <see cref="To"/>。
    /// 走这条边时依次：改状态、执行 <see cref="Action"/>、通知走边的记录者、向父机发 <see cref="Message"/>。
    /// 它是不可变的数据。
    /// </summary>
    public readonly struct Transition<TState, TContext>
        where TState : struct, Enum
        where TContext : class
    {
        public readonly TState From;
        public readonly TState To;

        /// <summary>必须有。只读上下文，不能有副作用。</summary>
        public readonly Func<TContext, bool> Condition;

        /// <summary>可以没有（只换状态）。在状态已经改成 To 之后才调用。</summary>
        public readonly Action<TContext> Action;

        /// <summary>给走边记录用的小整数原因码，含义由用的人自己定。</summary>
        public readonly byte Reason;

        /// <summary>走这条边时要向父机发的消息；不发就是 null。</summary>
        public readonly StateMachineMessage? Message;

        public Transition(
            TState from,
            TState to,
            Func<TContext, bool> condition,
            Action<TContext> action = null,
            byte reason = 0,
            StateMachineMessage? message = null)
        {
            // 没有条件的边不允许；要"无条件"就明着写 _ => true。
            From = from;
            To = to;
            Condition = condition ?? throw new ArgumentNullException(nameof(condition));
            Action = action;
            Reason = reason;
            Message = message;
        }
    }
}
