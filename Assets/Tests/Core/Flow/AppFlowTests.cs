using System;
using System.Collections.Generic;
using Game.Core.Flow;
using NUnit.Framework;

namespace Game.Core.Tests.Flow
{
    public class AppFlowTests : CoreTestBase
    {
        [Test]
        public void 一开始停在Boot()
        {
            var flow = new AppFlow();

            Assert.That(flow.State, Is.EqualTo(AppState.Boot));
            Assert.That(flow.RejectedCount, Is.Zero);
        }

        [Test]
        public void 表里有的边_申请成功_状态改变_通知带着从和到()
        {
            var flow = new AppFlow();
            var transitions = new List<AppTransition>();
            AppState stateWhenNotified = AppState.Boot;
            flow.Transitioned += transition =>
            {
                transitions.Add(transition);
                stateWhenNotified = flow.State;
            };

            bool moved = flow.Request(AppState.MainMenu);

            Assert.That(moved, Is.True);
            Assert.That(flow.State, Is.EqualTo(AppState.MainMenu));
            Assert.That(transitions, Has.Count.EqualTo(1));
            Assert.That(transitions[0].From, Is.EqualTo(AppState.Boot));
            Assert.That(transitions[0].To, Is.EqualTo(AppState.MainMenu));
            Assert.That(stateWhenNotified, Is.EqualTo(AppState.MainMenu), "收到通知时状态已经是新的");
        }

        [Test]
        public void 表外的申请被拒_状态不动_通知带着当时在哪_想去哪_为什么()
        {
            var flow = new AppFlow();
            var rejections = new List<AppFlowRejection>();
            int transitions = 0;
            flow.Rejected += rejections.Add;
            flow.Transitioned += transition => transitions++;

            bool moved = flow.Request(AppState.Running);

            Assert.That(moved, Is.False);
            Assert.That(flow.State, Is.EqualTo(AppState.Boot));
            Assert.That(flow.RejectedCount, Is.EqualTo(1));
            Assert.That(transitions, Is.Zero);
            Assert.That(rejections, Has.Count.EqualTo(1));
            Assert.That(rejections[0].Requested.From, Is.EqualTo(AppState.Boot));
            Assert.That(rejections[0].Requested.To, Is.EqualTo(AppState.Running));
            Assert.That(rejections[0].Reason, Is.EqualTo(AppFlowRejectReason.NotInTable));
        }

        [Test]
        public void 被拒之后_下一次合法的申请照常走()
        {
            var flow = new AppFlow();

            flow.Request(AppState.Running);

            Assert.That(flow.Request(AppState.MainMenu), Is.True);
            Assert.That(flow.State, Is.EqualTo(AppState.MainMenu));
        }

        [Test]
        public void 在状态变化的通知里再申请_被拒_原因是重入()
        {
            var flow = new AppFlow();
            var reasons = new List<AppFlowRejectReason>();
            bool innerResult = true;
            flow.Rejected += rejection => reasons.Add(rejection.Reason);
            flow.Transitioned += transition => innerResult = flow.Request(AppState.Loading);

            flow.Request(AppState.MainMenu);

            Assert.That(innerResult, Is.False);
            Assert.That(flow.State, Is.EqualTo(AppState.MainMenu));
            Assert.That(reasons, Is.EqualTo(new[] { AppFlowRejectReason.Reentrant }));
        }

        [Test]
        public void LOGO夹在启动和主菜单之间_放LOGO时不能直接开局()
        {
            var flow = new AppFlow();

            Assert.That(flow.Request(AppState.Splash), Is.True);
            Assert.That(flow.Request(AppState.Loading), Is.False);
            Assert.That(flow.Request(AppState.MainMenu), Is.True);
        }

        [Test]
        public void 开局_暂停_恢复_回主菜单_退出_一路走通()
        {
            var flow = new AppFlow();
            var path = new List<AppState>();
            flow.Transitioned += transition => path.Add(transition.To);

            flow.Request(AppState.MainMenu);
            flow.Request(AppState.Loading);
            flow.Request(AppState.Running);
            flow.Request(AppState.Paused);
            flow.Request(AppState.Running);
            flow.Request(AppState.MainMenu);
            flow.Request(AppState.Shutdown);

            Assert.That(path, Is.EqualTo(new[]
            {
                AppState.MainMenu, AppState.Loading, AppState.Running, AppState.Paused,
                AppState.Running, AppState.MainMenu, AppState.Shutdown,
            }));
            Assert.That(flow.RejectedCount, Is.Zero);
        }

        [Test]
        public void 读档时不能暂停()
        {
            var flow = new AppFlow();
            flow.Request(AppState.Loading);

            Assert.That(flow.Request(AppState.Paused), Is.False);
            Assert.That(flow.State, Is.EqualTo(AppState.Loading));
        }

        [Test]
        public void 退出之后哪里都去不了()
        {
            var flow = new AppFlow();
            flow.Request(AppState.Shutdown);

            foreach (AppState target in new[] { AppState.Boot, AppState.MainMenu, AppState.Running, AppState.Shutdown })
            {
                Assert.That(flow.Request(target), Is.False, target.ToString());
            }

            Assert.That(flow.State, Is.EqualTo(AppState.Shutdown));
            Assert.That(flow.RejectedCount, Is.EqualTo(4));
        }

        [Test]
        public void 默认的表里_每条边的起点终点不同_没有重复_Shutdown没有出边()
        {
            var seen = new HashSet<(AppState, AppState)>();

            foreach (AppTransition edge in AppFlow.DefaultTable)
            {
                Assert.That(edge.From, Is.Not.EqualTo(edge.To));
                Assert.That(edge.From, Is.Not.EqualTo(AppState.Shutdown), "Shutdown 是终点，不该有出边");
                Assert.That(seen.Add((edge.From, edge.To)), Is.True, "重复的边：" + edge);
            }
        }

        [Test]
        public void 流程规则是数据_换一张表和起点就是另一套流程()
        {
            var table = new[] { new AppTransition(AppState.MainMenu, AppState.Running) };
            var flow = new AppFlow(table, AppState.MainMenu);

            Assert.That(flow.Request(AppState.Loading), Is.False, "默认表里有这条边，这张表里没有");
            Assert.That(flow.Request(AppState.Running), Is.True);
            Assert.Throws<ArgumentNullException>(() => new AppFlow(null));
        }

        [Test]
        public void 每次转移和每次拒绝各留一行日志()
        {
            var flow = new AppFlow();

            flow.Request(AppState.MainMenu);
            flow.Request(AppState.Paused);

            Assert.That(Logs.Messages, Is.EqualTo(new[] { "Boot -> MainMenu", "拒绝 MainMenu -> Paused（NotInTable）" }));
        }
    }
}
