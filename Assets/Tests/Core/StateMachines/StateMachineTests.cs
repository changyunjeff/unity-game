using System;
using System.Collections.Generic;
using Game.Core.StateMachines;
using NUnit.Framework;

namespace Game.Core.Tests.StateMachines
{
    /// <summary>一张表加一个当前状态，以及子机向父机发消息。</summary>
    public class StateMachineTests
    {
        private static TransitionTableBuilder<Door, DoorContext> Builder() => new TransitionTableBuilder<Door, DoorContext>();

        [Test]
        public void 从给定的状态开始_每次Tick照表走一步()
        {
            TransitionTable<Door, DoorContext> table = Builder()
                .Add(Door.Closed, Door.Opening, c => c.Pushed)
                .Add(Door.Opening, Door.Open, c => true)
                .Build();
            var machine = new StateMachine<Door, DoorContext>(table, Door.Closed);
            var context = new DoorContext();

            Assert.That(machine.State, Is.EqualTo(Door.Closed));
            Assert.That(machine.Tick(context), Is.False);

            context.Pushed = true;
            Assert.That(machine.Tick(context), Is.True);
            Assert.That(machine.State, Is.EqualTo(Door.Opening));

            machine.Tick(context);
            Assert.That(machine.State, Is.EqualTo(Door.Open));
        }

        [Test]
        public void 两台状态机可以共用一张表_互不影响()
        {
            TransitionTable<Door, DoorContext> table = Builder().Add(Door.Closed, Door.Open, c => c.Pushed).Build();
            var first = new StateMachine<Door, DoorContext>(table, Door.Closed);
            var second = new StateMachine<Door, DoorContext>(table, Door.Closed);

            first.Tick(new DoorContext { Pushed = true });

            Assert.That(first.State, Is.EqualTo(Door.Open));
            Assert.That(second.State, Is.EqualTo(Door.Closed));
        }

        [Test]
        public void 走边时把自己是谁告诉记录者()
        {
            var trace = new ListTrace();
            TransitionTable<Door, DoorContext> table = Builder().Add(Door.Closed, Door.Open, c => true, null, 3).Build(trace);
            var machine = new StateMachine<Door, DoorContext>(table, Door.Closed, owner: TestHandles.First);

            machine.Tick(new DoorContext());

            Assert.That(machine.Owner, Is.EqualTo(TestHandles.First));
            Assert.That(trace.Edges, Is.EqualTo(new[] { (TestHandles.First, (object)Door.Closed, (object)Door.Open, (byte)3) }));
        }

        // ---- 子机向父机发消息 ----

        [Test]
        public void 边上写了消息_走这条边之后发给父机_带着种类_附带的数和来源()
        {
            var received = new List<StateMachineMessage>();
            TransitionTable<Door, DoorContext> table = Builder()
                .Add(Door.Closed, Door.Opening, c => c.Pushed)
                .Add(Door.Opening, Door.Open, c => true, message: new StateMachineMessage(StateMachineMessageKind.Finished, 42))
                .Build();
            var child = new StateMachine<Door, DoorContext>(table, Door.Closed, owner: TestHandles.Second, parent: received.Add);
            var context = new DoorContext { Pushed = true };

            child.Tick(context);
            Assert.That(received, Is.Empty, "这条边上没写消息");

            child.Tick(context);
            Assert.That(received, Has.Count.EqualTo(1));
            Assert.That(received[0].Kind, Is.EqualTo(StateMachineMessageKind.Finished));
            Assert.That(received[0].Detail, Is.EqualTo(42));
            Assert.That(received[0].Source, Is.EqualTo(TestHandles.Second));
        }

        [Test]
        public void 父机收到消息时_子机的状态已经改完_动作也已经执行()
        {
            StateMachine<Door, DoorContext> child = null;
            var context = new DoorContext();
            Door stateSeenByParent = Door.Closed;
            int callsSeenByParent = 0;
            TransitionTable<Door, DoorContext> table = Builder()
                .Add(Door.Closed, Door.Open, c => true, c => c.Calls.Add("opened"), message: new StateMachineMessage(StateMachineMessageKind.Finished))
                .Build();
            child = new StateMachine<Door, DoorContext>(table, Door.Closed, parent: message =>
            {
                stateSeenByParent = child.State;
                callsSeenByParent = context.Calls.Count;
            });

            child.Tick(context);

            Assert.That(stateSeenByParent, Is.EqualTo(Door.Open));
            Assert.That(callsSeenByParent, Is.EqualTo(1));
        }

        [Test]
        public void 子机干完活_父机下一步读到消息才走边()
        {
            // 父机只把子机的消息记进自己的上下文，由自己的边的条件去读。
            var parentContext = new DoorContext();
            TransitionTable<Door, DoorContext> parentTable = Builder().Add(Door.Closed, Door.Open, c => c.Pushed).Build();
            var parent = new StateMachine<Door, DoorContext>(parentTable, Door.Closed);

            TransitionTable<Door, DoorContext> childTable = Builder()
                .Add(Door.Closed, Door.Open, c => c.Pushed, message: new StateMachineMessage(StateMachineMessageKind.Finished))
                .Build();
            var child = new StateMachine<Door, DoorContext>(
                childTable, Door.Closed,
                parent: message => parentContext.Pushed = message.Kind == StateMachineMessageKind.Finished);

            Assert.That(parent.Tick(parentContext), Is.False, "子机还没干完，父机不动");

            child.Tick(new DoorContext { Pushed = true });

            Assert.That(parent.Tick(parentContext), Is.True);
            Assert.That(parent.State, Is.EqualTo(Door.Open));
        }

        [Test]
        public void 走边之外也可以随时发消息_三类都能发()
        {
            var received = new List<StateMachineMessage>();
            var child = new StateMachine<Door, DoorContext>(Builder().Build(), Door.Closed, owner: TestHandles.Second, parent: received.Add);

            child.Emit(StateMachineMessageKind.Finished);
            child.Emit(StateMachineMessageKind.Failed, 3);
            child.Emit(StateMachineMessageKind.Request, 42);

            Assert.That(received.ConvertAll(message => message.Kind), Is.EqualTo(new[]
            {
                StateMachineMessageKind.Finished, StateMachineMessageKind.Failed, StateMachineMessageKind.Request,
            }));
            Assert.That(received[1].Detail, Is.EqualTo(3));
            Assert.That(received[2].Source, Is.EqualTo(TestHandles.Second));
        }

        // ---- 接线错误在装配时就报 ----

        [Test]
        public void 表里有边要发消息却没给父机_建状态机时就抛异常()
        {
            TransitionTable<Door, DoorContext> table = Builder()
                .Add(Door.Closed, Door.Open, c => true, message: new StateMachineMessage(StateMachineMessageKind.Finished))
                .Build();

            Assert.Throws<ArgumentException>(() => new StateMachine<Door, DoorContext>(table, Door.Closed));
        }

        [Test]
        public void 没接父机就调Emit_抛异常()
        {
            var machine = new StateMachine<Door, DoorContext>(Builder().Build(), Door.Closed);

            Assert.Throws<InvalidOperationException>(() => machine.Emit(StateMachineMessageKind.Finished));
        }

        [Test]
        public void 必须给转移表()
        {
            Assert.Throws<ArgumentNullException>(() => new StateMachine<Door, DoorContext>(null, Door.Closed));
        }
    }
}
