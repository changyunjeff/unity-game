using System;
using Game.Core.Handles;
using Game.Core.StateMachines;
using NUnit.Framework;

namespace Game.Core.Tests.StateMachines
{
    /// <summary>走边的规则，以及一张表给多个对象共用。</summary>
    public class TransitionTableTests
    {
        private static TransitionTableBuilder<Door, DoorContext> Builder() => new TransitionTableBuilder<Door, DoorContext>();

        // ---- 走边的规则 ----

        [Test]
        public void 两条边都满足时_走先加的那条()
        {
            TransitionTable<Door, DoorContext> table = Builder()
                .Add(Door.Closed, Door.Opening, c => true, c => c.Calls.Add("first"))
                .Add(Door.Closed, Door.Broken, c => true, c => c.Calls.Add("second"))
                .Build();
            var context = new DoorContext();
            Door state = Door.Closed;

            bool moved = table.Step(ref state, context);

            Assert.That(moved, Is.True);
            Assert.That(state, Is.EqualTo(Door.Opening));
            Assert.That(context.Calls, Is.EqualTo(new[] { "first" }));
        }

        [Test]
        public void 别的状态的边穿插着加_同一状态的边仍按添加的先后()
        {
            TransitionTable<Door, DoorContext> table = Builder()
                .Add(Door.Closed, Door.Opening, c => c.Pushed)
                .Add(Door.Opening, Door.Open, c => true)
                .Add(Door.Closed, Door.Broken, c => c.Kicked)
                .Add(Door.Closed, Door.Open, c => true)
                .Build();
            Door state = Door.Closed;

            table.Step(ref state, new DoorContext { Kicked = true });

            // 第一条不满足，第三条（Closed 的第二条出边）满足，轮不到第四条。
            Assert.That(state, Is.EqualTo(Door.Broken));
        }

        [Test]
        public void 没有边满足_状态不变_不调动作_返回false()
        {
            TransitionTable<Door, DoorContext> table = Builder()
                .Add(Door.Closed, Door.Opening, c => c.Pushed, c => c.Calls.Add("open"))
                .Build();
            var context = new DoorContext();
            Door state = Door.Closed;

            Assert.That(table.Step(ref state, context), Is.False);
            Assert.That(state, Is.EqualTo(Door.Closed));
            Assert.That(context.Calls, Is.Empty);
        }

        [Test]
        public void 没有出边的状态_返回false()
        {
            TransitionTable<Door, DoorContext> table = Builder().Add(Door.Closed, Door.Opening, c => true).Build();
            Door state = Door.Broken;

            Assert.That(table.Step(ref state, new DoorContext()), Is.False);
            Assert.That(state, Is.EqualTo(Door.Broken));
        }

        [Test]
        public void 一步最多走一条边_不连跳()
        {
            TransitionTable<Door, DoorContext> table = Builder()
                .Add(Door.Closed, Door.Opening, c => true)
                .Add(Door.Opening, Door.Open, c => true)
                .Build();
            Door state = Door.Closed;

            table.Step(ref state, new DoorContext());
            Assert.That(state, Is.EqualTo(Door.Opening), "Opening 的出边也满足，但这一步不该连跳");

            table.Step(ref state, new DoorContext());
            Assert.That(state, Is.EqualTo(Door.Open));
        }

        [Test]
        public void 走了哪条边可以问出来_没走时是空的()
        {
            TransitionTable<Door, DoorContext> table = Builder()
                .Add(Door.Closed, Door.Opening, c => c.Pushed, null, 9)
                .Build();
            Door state = Door.Closed;

            Assert.That(table.TryStep(ref state, new DoorContext(), Handle.Null, out Transition<Door, DoorContext> none), Is.False);
            Assert.That(none.Condition, Is.Null);

            Assert.That(table.TryStep(ref state, new DoorContext { Pushed = true }, Handle.Null, out Transition<Door, DoorContext> taken), Is.True);
            Assert.That(taken.From, Is.EqualTo(Door.Closed));
            Assert.That(taken.To, Is.EqualTo(Door.Opening));
            Assert.That(taken.Reason, Is.EqualTo(9));
        }

        // ---- 走边的记录 ----

        [Test]
        public void 每走一条边都通知记录者_带着谁_从_到_原因_没走就不通知()
        {
            var trace = new ListTrace();
            TransitionTable<Door, DoorContext> table = Builder()
                .Add(Door.Closed, Door.Opening, c => c.Pushed, null, 9)
                .Build(trace);
            Door state = Door.Closed;

            table.Step(ref state, new DoorContext(), TestHandles.First);
            Assert.That(trace.Edges, Is.Empty);

            table.Step(ref state, new DoorContext { Pushed = true }, TestHandles.First);
            Assert.That(trace.Edges, Is.EqualTo(new[] { (TestHandles.First, (object)Door.Closed, (object)Door.Opening, (byte)9) }));
        }

        [Test]
        public void 先改状态_再调动作_再通知记录者()
        {
            var trace = new ListTrace();
            Door state = Door.Closed;
            Door stateWhenActionRan = Door.Closed;
            int tracedWhenActionRan = -1;
            TransitionTable<Door, DoorContext> table = Builder()
                .Add(Door.Closed, Door.Opening, c => true, c =>
                {
                    stateWhenActionRan = state;
                    tracedWhenActionRan = trace.Edges.Count;
                })
                .Build(trace);

            table.Step(ref state, new DoorContext());

            Assert.That(stateWhenActionRan, Is.EqualTo(Door.Opening), "动作执行时状态已经改完");
            Assert.That(tracedWhenActionRan, Is.Zero, "动作执行时记录者还没被通知");
            Assert.That(trace.Edges, Has.Count.EqualTo(1));
        }

        // ---- 共用 ----

        [Test]
        public void 多个对象共用一张表_各存各的状态()
        {
            var trace = new ListTrace();
            TransitionTable<Door, DoorContext> table = Builder()
                .Add(Door.Closed, Door.Opening, c => c.Pushed)
                .Add(Door.Opening, Door.Open, c => true)
                .Build(trace);
            var pushed = new DoorContext { Pushed = true };
            var idle = new DoorContext();
            Door first = Door.Closed;
            Door second = Door.Closed;

            Assert.That(table.Step(ref first, pushed, TestHandles.First), Is.True);
            Assert.That(table.Step(ref second, idle, TestHandles.Second), Is.False);
            Assert.That(table.Step(ref first, pushed, TestHandles.First), Is.True);

            Assert.That(first, Is.EqualTo(Door.Open));
            Assert.That(second, Is.EqualTo(Door.Closed));
            Assert.That(trace.Edges, Has.Count.EqualTo(2));
            Assert.That(trace.Edges[0].owner, Is.EqualTo(TestHandles.First));
        }

        // ---- 装配 ----

        [Test]
        public void 表记着一共有几条边_有没有边要发消息()
        {
            TransitionTable<Door, DoorContext> silent = Builder()
                .Add(Door.Closed, Door.Opening, c => true)
                .Add(Door.Opening, Door.Open, c => true)
                .Build();
            TransitionTable<Door, DoorContext> talking = Builder()
                .Add(Door.Closed, Door.Open, c => true, message: new StateMachineMessage(StateMachineMessageKind.Finished))
                .Build();

            Assert.That(silent.TransitionCount, Is.EqualTo(2));
            Assert.That(silent.DeclaresMessages, Is.False);
            Assert.That(talking.DeclaresMessages, Is.True);
        }

        [Test]
        public void 建出来的表不可变_之后再往builder里加边不影响它()
        {
            TransitionTableBuilder<Door, DoorContext> builder = Builder().Add(Door.Closed, Door.Opening, c => false);
            TransitionTable<Door, DoorContext> table = builder.Build();

            builder.Add(Door.Closed, Door.Broken, c => true);
            Door state = Door.Closed;

            Assert.That(table.Step(ref state, new DoorContext()), Is.False, "后加的边不在已经建好的表里");
            Assert.That(table.TransitionCount, Is.EqualTo(1));
            Assert.That(builder.Build().TransitionCount, Is.EqualTo(2));
        }

        [Test]
        public void 边没有条件_抛异常()
        {
            Assert.Throws<ArgumentNullException>(() => Builder().Add(Door.Closed, Door.Opening, null));
            Assert.Throws<ArgumentException>(() => Builder().Add(default(Transition<Door, DoorContext>)));
        }
    }
}
