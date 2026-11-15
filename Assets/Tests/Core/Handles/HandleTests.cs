using System;
using System.Collections.Generic;
using Game.Core.Handles;
using NUnit.Framework;

namespace Game.Core.Tests.Handles
{
    public class HandleTests
    {
        // 只当类型标记用。
        private sealed class Workshop
        {
        }

        private sealed class Citizen
        {
        }

        // ---- 不分类型的句柄 ----

        [Test]
        public void 默认值就是空句柄()
        {
            Handle handle = default;

            Assert.That(handle.IsNull, Is.True);
            Assert.That(handle, Is.EqualTo(Handle.Null));
            Assert.That(handle.ToString(), Is.EqualTo("#null"));
        }

        [Test]
        public void 下标和代数都相同才相等()
        {
            var handle = new Handle(42, 3);

            Assert.That(handle == new Handle(42, 3), Is.True);
            Assert.That(handle != new Handle(42, 4), Is.True, "同一个槽位、不同的代数，指的是先后两个不同的东西");
            Assert.That(handle != new Handle(41, 3), Is.True);
            Assert.That(handle.IsNull, Is.False);
            Assert.That(handle.ToString(), Is.EqualTo("#42.3"));
        }

        [Test]
        public void 可以当字典的键()
        {
            var names = new Dictionary<Handle, string>
            {
                [new Handle(1, 1)] = "甲",
                [new Handle(1, 2)] = "乙",
            };

            Assert.That(names[new Handle(1, 1)], Is.EqualTo("甲"));
            Assert.That(names[new Handle(1, 2)], Is.EqualTo("乙"));
            Assert.That(names.ContainsKey(new Handle(2, 1)), Is.False);
        }

        [Test]
        public void 畸形的句柄造不出来()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Handle(-1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Handle(5, 0), "代数 0 只属于空句柄");
            Assert.Throws<ArgumentOutOfRangeException>(() => new Handle(0, -1));
        }

        // ---- 带类型的句柄 ----

        [Test]
        public void 带类型的句柄包着一个底层句柄_相等与否看底层()
        {
            var raw = new Handle(42, 3);
            var workshop = new Handle<Workshop>(raw);

            Assert.That(workshop.Raw, Is.EqualTo(raw));
            Assert.That(workshop == new Handle<Workshop>(raw), Is.True);
            Assert.That(workshop != new Handle<Workshop>(new Handle(42, 4)), Is.True);
            Assert.That(workshop.ToString(), Is.EqualTo("Workshop#42.3"));
        }

        [Test]
        public void 带类型的句柄默认值也是空句柄()
        {
            Handle<Workshop> handle = default;

            Assert.That(handle.IsNull, Is.True);
            Assert.That(handle, Is.EqualTo(Handle<Workshop>.Null));
            Assert.That(handle.Raw, Is.EqualTo(Handle.Null));
        }

        [Test]
        public void 不同类型的句柄是不同的类型_即使底层一样也不相等()
        {
            var raw = new Handle(42, 3);
            object workshop = new Handle<Workshop>(raw);
            object citizen = new Handle<Citizen>(raw);

            // 直接写 workshop == citizen 编译不过，这正是要的效果；这里只能绕到 object 上比。
            Assert.That(workshop.Equals(citizen), Is.False);
            Assert.That(workshop.GetType(), Is.Not.EqualTo(citizen.GetType()));
        }
    }
}
