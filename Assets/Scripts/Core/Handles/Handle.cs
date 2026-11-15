using System;

namespace Game.Core.Handles
{
    /// <summary>
    /// 句柄：对"表里某一行"的弱引用，由槽位的下标和代数组成。
    /// 它不控制那一行的生死：行被删掉之后，旧句柄去查就查不到了，由拿着句柄的人自己处理。
    ///
    /// 代数从 1 起，0 专门留给"空句柄"。所以默认值（全零）天然就是空句柄，
    /// 而真实的行代数永远不小于 1，空句柄不可能碰巧对上某个真实的行。
    ///
    /// 这是不分类型的底层句柄。业务代码用 <see cref="Handle{T}"/>；只有分配器、走边记录、存档这类
    /// 不关心"是哪种东西"的地方才直接用它。
    /// </summary>
    public readonly struct Handle : IEquatable<Handle>
    {
        /// <summary>空句柄：不指向任何东西。等于默认值。</summary>
        public static readonly Handle Null = default;

        /// <param name="index">槽位下标，不能为负。</param>
        /// <param name="generation">槽位的代数。真实的句柄不小于 1。</param>
        public Handle(int index, int generation)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            // 畸形的句柄造不出来：代数小于 1 的只有空句柄一种，而空句柄用 Null，不用构造函数。
            if (generation < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(generation), "代数从 1 起。要空句柄请用 Handle.Null。");
            }

            Index = index;
            Generation = generation;
        }

        /// <summary>行在表里的槽位下标。</summary>
        public int Index { get; }

        /// <summary>槽位的代数。0 表示空句柄。</summary>
        public int Generation { get; }

        public bool IsNull => Generation == 0;

        public static bool operator ==(Handle left, Handle right) => left.Equals(right);

        public static bool operator !=(Handle left, Handle right) => !left.Equals(right);

        public bool Equals(Handle other) => Index == other.Index && Generation == other.Generation;

        public override bool Equals(object obj) => obj is Handle other && Equals(other);

        public override int GetHashCode() => (Index * 397) ^ Generation;

        /// <summary>写成 <c>#下标.代数</c>，例如 <c>#42.3</c>；空句柄写成 <c>#null</c>。</summary>
        public override string ToString() => IsNull ? "#null" : "#" + Index + "." + Generation;
    }

    /// <summary>
    /// 带类型的句柄：指向一种特定东西的那一行。<typeparamref name="T"/> 只是个标记（一般就是行的类型），不占空间。
    /// 不同 <typeparamref name="T"/> 的句柄是互不相干的类型：不能互相赋值、不能互相比较、不能传给别的表，写错了编译不过。
    /// </summary>
    public readonly struct Handle<T> : IEquatable<Handle<T>>
    {
        /// <summary>空句柄。等于默认值。</summary>
        public static readonly Handle<T> Null = default;

        /// <summary>从底层句柄包出带类型的句柄。只能明着写，留给表的实现和存档用。</summary>
        public Handle(Handle raw)
        {
            Raw = raw;
        }

        /// <summary>底层不分类型的句柄。</summary>
        public Handle Raw { get; }

        public bool IsNull => Raw.IsNull;

        public static bool operator ==(Handle<T> left, Handle<T> right) => left.Equals(right);

        public static bool operator !=(Handle<T> left, Handle<T> right) => !left.Equals(right);

        public bool Equals(Handle<T> other) => Raw.Equals(other.Raw);

        public override bool Equals(object obj) => obj is Handle<T> other && Equals(other);

        public override int GetHashCode() => Raw.GetHashCode();

        /// <summary>类型名加底层句柄，例如 <c>Workshop#42.3</c>。</summary>
        public override string ToString() => typeof(T).Name + Raw;
    }
}
