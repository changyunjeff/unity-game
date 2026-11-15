using Game.Core.Handles;

namespace Game.Core.StateMachines
{
    /// <summary>子机发给父机的消息只有这三类，不再加。</summary>
    public enum StateMachineMessageKind : byte
    {
        /// <summary>子机的活干完了。Detail 可以带结果码。</summary>
        Finished = 0,

        /// <summary>子机失败了。Detail 是失败原因码。</summary>
        Failed = 1,

        /// <summary>子机需要父机做点什么。Detail 是请求码，含义由父子双方约定。</summary>
        Request = 2,
    }

    /// <summary>
    /// 子机发给父机的一条消息。
    /// 写在边上时只填种类和附带的数；发出去的时候状态机会补上"是谁发的"。
    /// </summary>
    public readonly struct StateMachineMessage
    {
        /// <summary>发消息的子机属于哪个对象；没指定就是空句柄。</summary>
        public readonly Handle Source;

        public readonly StateMachineMessageKind Kind;

        /// <summary>附带的小整数：结果码、失败原因码或请求码。</summary>
        public readonly int Detail;

        public StateMachineMessage(StateMachineMessageKind kind, int detail = 0)
            : this(Handle.Null, kind, detail)
        {
        }

        private StateMachineMessage(Handle source, StateMachineMessageKind kind, int detail)
        {
            Source = source;
            Kind = kind;
            Detail = detail;
        }

        /// <summary>同一条消息，标明是谁发的。</summary>
        public StateMachineMessage WithSource(Handle source) => new StateMachineMessage(source, Kind, Detail);
    }
}
