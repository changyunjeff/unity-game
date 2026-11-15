namespace Game.Core.Flow
{
    /// <summary>一次状态转移：从哪到哪。转移表里的一行、状态变化的通知，用的都是它。</summary>
    public readonly struct AppTransition
    {
        public readonly AppState From;
        public readonly AppState To;

        public AppTransition(AppState from, AppState to)
        {
            From = from;
            To = to;
        }

        public override string ToString() => From + " -> " + To;
    }

    /// <summary>一次被拒的申请：当时在哪、想去哪、为什么不行。</summary>
    public readonly struct AppFlowRejection
    {
        public readonly AppTransition Requested;
        public readonly AppFlowRejectReason Reason;

        public AppFlowRejection(AppTransition requested, AppFlowRejectReason reason)
        {
            Requested = requested;
            Reason = reason;
        }

        public override string ToString() => Requested + "（" + Reason + "）";
    }
}
