using System;
using System.Collections.Generic;
using Game.Core.Handles;
using NUnit.Framework;

namespace Game.Core.Tests.Handles
{
    public class HandleAllocatorTests
    {
        // 按槽位下标的先后，列出现在占着的句柄。
        private static List<Handle> AliveHandles(HandleAllocator allocator)
        {
            var handles = new List<Handle>();

            for (int i = 0; i < allocator.SlotCount; i++)
            {
                if (allocator.TryGetHandleAt(i, out Handle handle))
                {
                    handles.Add(handle);
                }
            }

            return handles;
        }

        [Test]
        public void 新发的句柄下标依次递增_代数从1起_都有效()
        {
            var allocator = new HandleAllocator();

            Handle first = allocator.Allocate();
            Handle second = allocator.Allocate();

            Assert.That(first, Is.EqualTo(new Handle(0, 1)));
            Assert.That(second, Is.EqualTo(new Handle(1, 1)));
            Assert.That(allocator.IsAlive(first), Is.True);
            Assert.That(allocator.IsAlive(second), Is.True);
            Assert.That(allocator.SlotCount, Is.EqualTo(2));
            Assert.That(allocator.AliveCount, Is.EqualTo(2));
        }

        [Test]
        public void 收回之后_旧句柄当场失效_别的句柄不受影响()
        {
            var allocator = new HandleAllocator();
            Handle first = allocator.Allocate();
            Handle second = allocator.Allocate();

            Assert.That(allocator.Release(first), Is.True);

            Assert.That(allocator.IsAlive(first), Is.False);
            Assert.That(allocator.IsAlive(second), Is.True);
            Assert.That(allocator.AliveCount, Is.EqualTo(1));
            Assert.That(allocator.SlotCount, Is.EqualTo(2), "槽位还在，只是空着");
        }

        [Test]
        public void 空出来的槽位被复用_代数加1_旧句柄仍然无效()
        {
            var allocator = new HandleAllocator();
            Handle old = allocator.Allocate();
            allocator.Release(old);

            Handle reused = allocator.Allocate();

            Assert.That(reused, Is.EqualTo(new Handle(0, 2)));
            Assert.That(allocator.IsAlive(reused), Is.True);
            Assert.That(allocator.IsAlive(old), Is.False, "同一个槽位，但已经是另一个东西了");
            Assert.That(allocator.SlotCount, Is.EqualTo(1));
        }

        [Test]
        public void 空着的槽位_后收回的先复用()
        {
            var allocator = new HandleAllocator();
            Handle a = allocator.Allocate();
            Handle b = allocator.Allocate();
            Handle c = allocator.Allocate();
            allocator.Release(a);
            allocator.Release(c);
            allocator.Release(b);

            Assert.That(allocator.Allocate().Index, Is.EqualTo(b.Index));
            Assert.That(allocator.Allocate().Index, Is.EqualTo(c.Index));
            Assert.That(allocator.Allocate().Index, Is.EqualTo(a.Index));
            Assert.That(allocator.Allocate().Index, Is.EqualTo(3), "空槽用完了才开新的");
        }

        [Test]
        public void 同样的操作永远发出同样的句柄()
        {
            List<Handle> Run()
            {
                var allocator = new HandleAllocator();
                var issued = new List<Handle>();

                for (int i = 0; i < 5; i++)
                {
                    issued.Add(allocator.Allocate());
                }

                allocator.Release(issued[1]);
                allocator.Release(issued[3]);
                issued.Add(allocator.Allocate());
                issued.Add(allocator.Allocate());
                issued.Add(allocator.Allocate());
                return issued;
            }

            Assert.That(Run(), Is.EqualTo(Run()));
        }

        [Test]
        public void 空句柄和悬挂句柄_查是无效_收回返回false_不出错()
        {
            var allocator = new HandleAllocator();
            Handle handle = allocator.Allocate();
            allocator.Release(handle);

            Assert.That(allocator.IsAlive(Handle.Null), Is.False);
            Assert.That(allocator.Release(Handle.Null), Is.False);
            Assert.That(allocator.Release(handle), Is.False, "已经收回过了");
            Assert.That(allocator.AliveCount, Is.Zero);
        }

        [Test]
        public void 下标超出槽位数的句柄不是这里发的_抛异常()
        {
            var allocator = new HandleAllocator();
            allocator.Allocate();
            var foreign = new Handle(7, 1);

            Assert.Throws<ArgumentOutOfRangeException>(() => allocator.IsAlive(foreign));
            Assert.Throws<ArgumentOutOfRangeException>(() => allocator.Release(foreign));
        }

        [Test]
        public void 遍历只给出占着的槽位_按下标的先后()
        {
            var allocator = new HandleAllocator();
            Handle a = allocator.Allocate();
            Handle b = allocator.Allocate();
            Handle c = allocator.Allocate();
            allocator.Release(b);

            Assert.That(AliveHandles(allocator), Is.EqualTo(new[] { a, c }));
            Assert.That(allocator.TryGetHandleAt(b.Index, out Handle empty), Is.False);
            Assert.That(empty, Is.EqualTo(Handle.Null));
            Assert.Throws<ArgumentOutOfRangeException>(() => allocator.TryGetHandleAt(3, out _));
            Assert.Throws<ArgumentOutOfRangeException>(() => allocator.TryGetHandleAt(-1, out _));
        }

        [Test]
        public void 代数加到头就回到1_永远跳过留给空句柄的0()
        {
            Assert.That(HandleAllocator.NextGeneration(1), Is.EqualTo(2));
            Assert.That(HandleAllocator.NextGeneration(int.MaxValue - 1), Is.EqualTo(int.MaxValue));
            Assert.That(HandleAllocator.NextGeneration(int.MaxValue), Is.EqualTo(1));
        }
    }
}
