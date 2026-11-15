using System;
using Game.Core.Handles;

namespace Game.Core.StateMachines
{
    /// <summary>
    /// 扁平状态机：一张转移表，加一个当前状态。给数量少的对象用（程序运行状态、一次加载流程、某个子流程）。
    /// 走边的规则见 <see cref="TransitionTable{TState,TContext}"/>；表可以和别的状态机共用。
    ///
    /// 不做分层状态机。需要"大状态里套小流程"时，小流程是另一台状态机（子机），它向父机汇报有两种写法：
    /// 一是在边上写明"走这条边时发什么消息"，二是随时调 <see cref="Emit"/>。
    /// 父机一般只把收到的消息记进自己的上下文，下一次 <see cref="Tick"/> 时由某条边的条件读到它再走边。
    ///
    /// 它要用到的东西全部在构造时给定，之后不能换。
    /// </summary>
    public sealed class StateMachine<TState, TContext>
        where TState : struct, Enum
        where TContext : class
    {
        private readonly TransitionTable<TState, TContext> _table;
        private readonly Action<StateMachineMessage> _parent;
        private TState _state;

        /// <param name="table">转移表。</param>
        /// <param name="initial">一开始的状态。</param>
        /// <param name="owner">这台状态机属于哪个对象：交给走边的记录者，也作为消息的来源。可以不给。</param>
        /// <param name="parent">父机接收消息的地方。表里有边要发消息的话必须给。</param>
        public StateMachine(TransitionTable<TState, TContext> table, TState initial, Handle owner = default, Action<StateMachineMessage> parent = null)
        {
            _table = table ?? throw new ArgumentNullException(nameof(table));

            // 接线错误在装配时就报，不等到运行中走到那条边才发现。
            if (table.DeclaresMessages && parent == null)
            {
                throw new ArgumentException("这张表里有边要向父机发消息，必须给出父机。", nameof(parent));
            }

            _state = initial;
            _parent = parent;
            Owner = owner;
        }

        /// <summary>当前状态。</summary>
        public TState State => _state;

        /// <summary>这台状态机属于哪个对象；没指定就是空句柄。</summary>
        public Handle Owner { get; }

        /// <summary>走一步。返回有没有走边。走的那条边上写了消息的话，走完之后发给父机。</summary>
        public bool Tick(TContext context)
        {
            if (!_table.TryStep(ref _state, context, Owner, out Transition<TState, TContext> taken))
            {
                return false;
            }

            if (taken.Message.HasValue)
            {
                _parent(taken.Message.Value.WithSource(Owner));
            }

            return true;
        }

        /// <summary>子机在走边之外的时候向父机汇报。没接父机就发消息是接线错误，直接抛异常。</summary>
        public void Emit(StateMachineMessageKind kind, int detail = 0)
        {
            if (_parent == null)
            {
                throw new InvalidOperationException("这台状态机没有接父机，不能发消息。");
            }

            _parent(new StateMachineMessage(kind, detail).WithSource(Owner));
        }
    }
}
