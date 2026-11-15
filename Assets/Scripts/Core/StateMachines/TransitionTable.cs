using System;
using System.Collections.Generic;
using Game.Core.Handles;

namespace Game.Core.StateMachines
{
    /// <summary>
    /// 扁平状态机的转移表。建好之后不可变，所以可以给任意多个对象共用：
    /// 同一类对象只建一张表，每个对象自己只存一个枚举状态，每一步对每个对象调一次 <see cref="Step"/>。
    /// 数量少的对象用 <see cref="StateMachine{TState,TContext}"/>，它就是"一张表加一个当前状态"，规则完全一样。
    /// 用 <see cref="TransitionTableBuilder{TState,TContext}"/> 来建。
    ///
    /// 走边的规则：
    /// 1. 只看当前状态的出边，按添加的先后求条件，第一条为真的执行。
    /// 2. 执行的先后：先改状态，再调动作，再通知走边的记录者。
    /// 3. 没有边满足：状态不变，不调任何动作，不通知记录者，返回 false。
    /// 4. 一步最多走一条边，不连跳；要连跳就下一步再走。
    /// </summary>
    public sealed class TransitionTable<TState, TContext>
        where TState : struct, Enum
        where TContext : class
    {
        private readonly Dictionary<TState, Transition<TState, TContext>[]> _edgesByFrom;
        private readonly ITransitionTrace _trace;

        // 只由 TransitionTableBuilder 来建。
        internal TransitionTable(
            Dictionary<TState, Transition<TState, TContext>[]> edgesByFrom,
            int transitionCount,
            bool declaresMessages,
            ITransitionTrace trace)
        {
            _edgesByFrom = edgesByFrom;
            _trace = trace;
            TransitionCount = transitionCount;
            DeclaresMessages = declaresMessages;
        }

        /// <summary>一共有多少条边。</summary>
        public int TransitionCount { get; }

        /// <summary>表里有没有哪条边要向父机发消息。有的话，用这张表的状态机必须接了父机。</summary>
        public bool DeclaresMessages { get; }

        /// <summary>
        /// 让一个对象走一步。<paramref name="state"/> 是这个对象自己存的状态，走了边就被改掉。
        /// </summary>
        /// <param name="owner">这个对象的句柄，只交给走边的记录者用；可以不给。</param>
        /// <returns>有没有走边。</returns>
        public bool Step(ref TState state, TContext context, Handle owner = default)
        {
            return TryStep(ref state, context, owner, out _);
        }

        /// <summary>同 <see cref="Step"/>，另外告诉调用的人走的是哪条边。</summary>
        public bool TryStep(ref TState state, TContext context, Handle owner, out Transition<TState, TContext> taken)
        {
            if (_edgesByFrom.TryGetValue(state, out Transition<TState, TContext>[] edges))
            {
                for (int i = 0; i < edges.Length; i++)
                {
                    if (!edges[i].Condition(context))
                    {
                        continue;
                    }

                    taken = edges[i];
                    state = taken.To;
                    taken.Action?.Invoke(context);
                    _trace.OnTransition(owner, taken.From, taken.To, taken.Reason);
                    return true;
                }
            }

            taken = default;
            return false;
        }
    }
}
