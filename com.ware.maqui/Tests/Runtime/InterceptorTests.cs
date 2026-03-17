using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using System.Threading.Tasks;
using UnityEngine.TestTools;
using VitalRouter;

namespace Maqui.Tests
{
    /// <summary>
    /// Tests for VitalRouter interceptor chains — cross-cutting concern pattern.
    /// Verifies ordering, async interceptors, and short-circuiting.
    /// </summary>
    public class InterceptorTests
    {
        private Router _router;

        [SetUp]
        public void SetUp()
        {
            _router = new Router();
        }

        [TearDown]
        public void TearDown()
        {
            _router.Dispose();
        }

        [Test]
        public void PublishAsync_WithNoSubscribers_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
                _router.PublishAsync(new TestCommand("hello")).AsTask().Wait());
        }

        [UnityTest]
        public IEnumerator Interceptor_ReceivesCommand_BeforeHandler()
        {
            var order = new List<string>();
            var interceptor = new OrderTrackingInterceptor("interceptor", order);
            var handler = new OrderTrackingHandler("handler", order);

            _router.AddFilter(interceptor);
            var subscription = _router.Subscribe(handler);

            _router.PublishAsync(new TestCommand("test")).AsTask().Wait();

            Assert.AreEqual(2, order.Count);
            Assert.AreEqual("interceptor:test", order[0]);
            Assert.AreEqual("handler:test", order[1]);

            subscription.Dispose();
            yield return null;
        }

        [UnityTest]
        public IEnumerator MultipleInterceptors_ExecuteInRegistrationOrder()
        {
            var order = new List<string>();
            var i1 = new OrderTrackingInterceptor("first", order);
            var i2 = new OrderTrackingInterceptor("second", order);
            var handler = new OrderTrackingHandler("handler", order);

            _router.AddFilter(i1);
            _router.AddFilter(i2);
            var subscription = _router.Subscribe(handler);

            _router.PublishAsync(new TestCommand("cmd")).AsTask().Wait();

            Assert.AreEqual(3, order.Count);
            Assert.AreEqual("first:cmd", order[0]);
            Assert.AreEqual("second:cmd", order[1]);
            Assert.AreEqual("handler:cmd", order[2]);

            subscription.Dispose();
            yield return null;
        }

        [UnityTest]
        public IEnumerator Interceptor_CanShortCircuit_ByNotCallingNext()
        {
            var order = new List<string>();
            var blocker = new BlockingInterceptor(order);
            var handler = new OrderTrackingHandler("handler", order);

            _router.AddFilter(blocker);
            var subscription = _router.Subscribe(handler);

            _router.PublishAsync(new TestCommand("blocked")).AsTask().Wait();

            Assert.AreEqual(1, order.Count);
            Assert.AreEqual("blocked:blocked", order[0]);
            // Handler should never execute

            subscription.Dispose();
            yield return null;
        }

        // ── Test command and doubles ─────────────────────────────────────────

        private readonly struct TestCommand : ICommand
        {
            public readonly string Payload;
            public TestCommand(string payload) => Payload = payload;
        }

        private class OrderTrackingInterceptor : ICommandInterceptor
        {
            private readonly string _name;
            private readonly List<string> _log;

            public OrderTrackingInterceptor(string name, List<string> log)
            {
                _name = name;
                _log = log;
            }

            public ValueTask InvokeAsync<T>(T command, PublishContext context, PublishContinuation<T> next)
                where T : ICommand
            {
                if (command is TestCommand tc)
                    _log.Add($"{_name}:{tc.Payload}");
                return next(command, context);
            }
        }

        private class BlockingInterceptor : ICommandInterceptor
        {
            private readonly List<string> _log;
            public BlockingInterceptor(List<string> log) => _log = log;

            public ValueTask InvokeAsync<T>(T command, PublishContext context, PublishContinuation<T> next)
                where T : ICommand
            {
                if (command is TestCommand tc)
                    _log.Add($"blocked:{tc.Payload}");
                // Intentionally NOT calling next — short-circuit
                return default;
            }
        }

        private class OrderTrackingHandler : ICommandSubscriber
        {
            private readonly string _name;
            private readonly List<string> _log;

            public OrderTrackingHandler(string name, List<string> log)
            {
                _name = name;
                _log = log;
            }

            public void Receive<T>(T command, PublishContext context) where T : ICommand
            {
                if (command is TestCommand tc)
                    _log.Add($"{_name}:{tc.Payload}");
            }
        }
    }
}
