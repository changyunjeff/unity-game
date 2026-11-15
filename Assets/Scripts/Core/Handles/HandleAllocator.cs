using System;
using System.Collections.Generic;

namespace Game.Core.Handles
{
    /// <summary>
    /// 句柄的分配器：管一排槽位，发句柄、收句柄、判断一个句柄现在还指不指得到东西。
    /// 它只管槽位和代数，不存任何数据；以后的表在它旁边按下标放自己的行。
    ///
    /// 规则：
    /// 1. 新槽位的代数从 1 起。
    /// 2. 收回一个句柄时，那个槽位的代数立刻加 1。于是所有指向它的旧句柄当场失效。
    /// 3. 空出来的槽位优先复用，后收回的先用（先后固定，同样的操作永远得到同样的句柄）。
    /// 4. 行已经没了的句柄（悬挂句柄）是正常情况：<see cref="IsAlive"/> 返回 false，<see cref="Release"/> 返回 false。
    /// 5. 下标超出这排槽位的句柄是程序错误（多半是把别处的句柄拿到这里来了）：抛异常。
    /// </summary>
    public sealed class HandleAllocator
    {
        // 第 i 个槽位现在的代数，不小于 1。
        private readonly List<int> _generations = new List<int>();

        // 第 i 个槽位现在有没有被占着。
        private readonly List<bool> _alive = new List<bool>();

        // 空着的槽位，当栈用：后放进去的先拿出来。
        private readonly List<int> _freeSlots = new List<int>();

        /// <summary>一共开过多少个槽位（占着的和空着的都算）。遍历时的下标上限。</summary>
        public int SlotCount => _generations.Count;

        /// <summary>现在有多少个句柄是有效的。</summary>
        public int AliveCount => _generations.Count - _freeSlots.Count;

        /// <summary>发一个新句柄。</summary>
        public Handle Allocate()
        {
            int index;

            if (_freeSlots.Count > 0)
            {
                index = _freeSlots[_freeSlots.Count - 1];
                _freeSlots.RemoveAt(_freeSlots.Count - 1);
                _alive[index] = true;
            }
            else
            {
                index = _generations.Count;
                _generations.Add(1);
                _alive.Add(true);
            }

            return new Handle(index, _generations[index]);
        }

        /// <summary>收回一个句柄。它原本有效就返回 true；它是空句柄或早已失效就返回 false，什么都不做。</summary>
        public bool Release(Handle handle)
        {
            if (!IsAlive(handle))
            {
                return false;
            }

            _alive[handle.Index] = false;
            _generations[handle.Index] = NextGeneration(handle.Generation);
            _freeSlots.Add(handle.Index);
            return true;
        }

        /// <summary>这个句柄现在还指不指得到东西。空句柄和悬挂句柄都返回 false。</summary>
        public bool IsAlive(Handle handle)
        {
            if (handle.IsNull)
            {
                return false;
            }

            if (handle.Index >= _generations.Count)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(handle), "句柄 " + handle + " 的下标超出了这个分配器的槽位数 " + _generations.Count + "，它不是这里发出去的。");
            }

            return _alive[handle.Index] && _generations[handle.Index] == handle.Generation;
        }

        /// <summary>
        /// 遍历用：第 <paramref name="index"/> 个槽位现在占着的话，给出它的句柄。
        /// 用法是 <c>for (int i = 0; i &lt; SlotCount; i++) if (TryGetHandleAt(i, out handle)) …</c>。
        /// 遍历的过程中不要分配或收回。
        /// </summary>
        public bool TryGetHandleAt(int index, out Handle handle)
        {
            if (index < 0 || index >= _generations.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            if (!_alive[index])
            {
                handle = Handle.Null;
                return false;
            }

            handle = new Handle(index, _generations[index]);
            return true;
        }

        /// <summary>一个槽位被收回之后的下一个代数：加 1；加到头了就回到 1，永远跳过留给空句柄的 0。</summary>
        public static int NextGeneration(int generation)
        {
            return generation == int.MaxValue ? 1 : generation + 1;
        }
    }
}
