namespace Game.Core.Flow
{
    /// <summary>程序现在在哪一步。扁平，不嵌套。</summary>
    public enum AppState : byte
    {
        /// <summary>刚启动，什么都还没做。</summary>
        Boot,

        /// <summary>放制作方 LOGO。</summary>
        Splash,

        /// <summary>主菜单。</summary>
        MainMenu,

        /// <summary>正在开一局：建游戏数据、读档。</summary>
        Loading,

        /// <summary>游戏在跑。</summary>
        Running,

        /// <summary>游戏暂停。</summary>
        Paused,

        /// <summary>程序正在退出。终点，去不了别处。</summary>
        Shutdown,
    }

    /// <summary>一次状态申请为什么被拒。</summary>
    public enum AppFlowRejectReason : byte
    {
        /// <summary>转移表里没有"当前状态到申请的状态"这条边。</summary>
        NotInTable,

        /// <summary>上一次转移的通知还没发完，又来申请（在 Transitioned 的回调里申请）。</summary>
        Reentrant,
    }
}
