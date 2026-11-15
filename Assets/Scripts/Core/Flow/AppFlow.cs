using System;
using System.Collections.Generic;
using Game.Core.Logging;
using Game.Core.StateMachines;

namespace Game.Core.Flow
{
    /// <summary>
    /// 程序运行状态：一块"现在在哪一步"的牌子，加一张"从哪步能到哪步"的表。
    ///
    /// 别的模块不直接改状态，只调 <see cref="Request"/> 申请；表里有这条边就走，没有就拒绝。
    /// 想知道状态变了，订阅 <see cref="Transitioned"/>。
    ///
    /// 流程规则全在转移表里，是数据不是代码：默认用 <see cref="DefaultTable"/>，也可以从构造函数换一张。
    /// 走边交给扁平状态机：申请先记进上下文，每条边的条件是"有人申请去我的终点"。
    /// </summary>
    public sealed class AppFlow
    {
        /// <summary>默认的转移表。Shutdown 没有出边，是终点。</summary>
        public static readonly IReadOnlyList<AppTransition> DefaultTable = new[]
        {
            new AppTransition(AppState.Boot, AppState.Splash),       // 启动完成，放 LOGO
            new AppTransition(AppState.Boot, AppState.MainMenu),     // 跳过 LOGO 直接进主菜单
            new AppTransition(AppState.Boot, AppState.Loading),      // 跳过主菜单直接开局（调试、测试）
            new AppTransition(AppState.Boot, AppState.Shutdown),
            new AppTransition(AppState.Splash, AppState.MainMenu),   // LOGO 放完或被跳过
            new AppTransition(AppState.Splash, AppState.Shutdown),
            new AppTransition(AppState.MainMenu, AppState.Loading),  // 继续游戏、新游戏
            new AppTransition(AppState.MainMenu, AppState.Shutdown),
            new AppTransition(AppState.Loading, AppState.Running),   // 读完了，开始玩
            new AppTransition(AppState.Loading, AppState.Shutdown),
            new AppTransition(AppState.Running, AppState.Paused),
            new AppTransition(AppState.Running, AppState.MainMenu),
            new AppTransition(AppState.Running, AppState.Shutdown),
            new AppTransition(AppState.Paused, AppState.Running),
            new AppTransition(AppState.Paused, AppState.MainMenu),
            new AppTransition(AppState.Paused, AppState.Shutdown),
        };

        private static readonly LogCategory LogFlow = new LogCategory("Flow");

        private readonly StateMachine<AppState, PendingRequest> _machine;
        private readonly PendingRequest _pending = new PendingRequest();
        private bool _notifying;

        public AppFlow()
            : this(DefaultTable)
        {
        }

        public AppFlow(IReadOnlyList<AppTransition> table, AppState initial = AppState.Boot, ITransitionTrace trace = null)
        {
            if (table == null)
            {
                throw new ArgumentNullException(nameof(table));
            }

            var builder = new TransitionTableBuilder<AppState, PendingRequest>();

            foreach (AppTransition edge in table)
            {
                AppState target = edge.To;
                builder.Add(edge.From, target, pending => pending.Target == target);
            }

            _machine = new StateMachine<AppState, PendingRequest>(builder.Build(trace), initial);
        }

        /// <summary>状态变了。收到时 <see cref="State"/> 已经是新状态。</summary>
        public event Action<AppTransition> Transitioned;

        /// <summary>一次申请被拒了。</summary>
        public event Action<AppFlowRejection> Rejected;

        /// <summary>当前状态。没人申请就一直停在初始状态。</summary>
        public AppState State => _machine.State;

        /// <summary>到现在为止拒绝过多少次申请。</summary>
        public int RejectedCount { get; private set; }

        /// <summary>
        /// 申请转到 <paramref name="to"/>。走成了返回 true；被拒返回 false，状态不动，并发出 <see cref="Rejected"/>。
        /// </summary>
        public bool Request(AppState to)
        {
            var requested = new AppTransition(_machine.State, to);

            // 上一次转移的通知还没发完就又来申请：订阅的人看到的先后会乱，所以不接。
            if (_notifying)
            {
                return Reject(requested, AppFlowRejectReason.Reentrant);
            }

            _pending.Target = to;
            bool moved = _machine.Tick(_pending);
            _pending.Target = null;

            if (!moved)
            {
                return Reject(requested, AppFlowRejectReason.NotInTable);
            }

            Log.Info(LogFlow, requested.ToString());
            Notify(requested);
            return true;
        }

        private void Notify(AppTransition transition)
        {
            _notifying = true;
            try
            {
                Transitioned?.Invoke(transition);
            }
            finally
            {
                _notifying = false;
            }
        }

        private bool Reject(AppTransition requested, AppFlowRejectReason reason)
        {
            var rejection = new AppFlowRejection(requested, reason);
            RejectedCount++;
            Log.Info(LogFlow, "拒绝 " + rejection);
            Rejected?.Invoke(rejection);
            return false;
        }

        // 状态机的上下文：现在有没有人申请、申请去哪。
        private sealed class PendingRequest
        {
            public AppState? Target { get; set; }
        }
    }
}
