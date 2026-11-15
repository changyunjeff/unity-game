using System;
using System.Collections.Generic;

namespace Game.Core.StateMachines
{
    /// <summary>
    /// 装配阶段用它一条一条加边，加完 <see cref="Build"/> 出一张不可变的 <see cref="TransitionTable{TState,TContext}"/>。
    /// 装配和运行是两个类型：建好的表上没有"加边"这个方法，所以运行中改表这种错在编译时就写不出来。
    /// </summary>
    public sealed class TransitionTableBuilder<TState, TContext>
        where TState : struct, Enum
        where TContext : class
    {
        // 每个状态的出边，各自保持添加的先后。
        private readonly Dictionary<TState, List<Transition<TState, TContext>>> _edgesByFrom =
            new Dictionary<TState, List<Transition<TState, TContext>>>();

        /// <summary>加一条边。同一个出发状态的边，运行时按添加的先后求条件。可以连着写。</summary>
        public TransitionTableBuilder<TState, TContext> Add(Transition<TState, TContext> transition)
        {
            if (transition.Condition == null)
            {
                throw new ArgumentException("边必须有条件。", nameof(transition));
            }

            if (!_edgesByFrom.TryGetValue(transition.From, out List<Transition<TState, TContext>> edges))
            {
                edges = new List<Transition<TState, TContext>>();
                _edgesByFrom.Add(transition.From, edges);
            }

            edges.Add(transition);
            return this;
        }

        /// <summary>加一条边。</summary>
        /// <param name="condition">什么时候走这条边。必须有。</param>
        /// <param name="action">走的时候做什么。可以没有。</param>
        /// <param name="reason">给走边记录用的原因码。</param>
        /// <param name="message">走的时候向父机发什么消息。不发就不填。</param>
        public TransitionTableBuilder<TState, TContext> Add(
            TState from,
            TState to,
            Func<TContext, bool> condition,
            Action<TContext> action = null,
            byte reason = 0,
            StateMachineMessage? message = null)
        {
            return Add(new Transition<TState, TContext>(from, to, condition, action, reason, message));
        }

        /// <summary>
        /// 建出不可变的转移表。之后再往这个 builder 里加边，不会影响已经建出来的表。
        /// </summary>
        /// <param name="trace">走边的记录者；不给就不记。</param>
        public TransitionTable<TState, TContext> Build(ITransitionTrace trace = null)
        {
            var edgesByFrom = new Dictionary<TState, Transition<TState, TContext>[]>(_edgesByFrom.Count);
            int count = 0;
            bool declaresMessages = false;

            foreach (KeyValuePair<TState, List<Transition<TState, TContext>>> pair in _edgesByFrom)
            {
                Transition<TState, TContext>[] edges = pair.Value.ToArray();
                edgesByFrom.Add(pair.Key, edges);
                count += edges.Length;

                foreach (Transition<TState, TContext> edge in edges)
                {
                    declaresMessages |= edge.Message.HasValue;
                }
            }

            return new TransitionTable<TState, TContext>(edgesByFrom, count, declaresMessages, trace ?? NullTransitionTrace.Instance);
        }
    }
}
