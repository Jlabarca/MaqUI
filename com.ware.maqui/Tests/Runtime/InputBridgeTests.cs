using System.Collections;
using NUnit.Framework;
using Maqui.Core;
using Maqui.Core.Bridge;
using UnityEngine;
using UnityEngine.TestTools;

namespace Maqui.Tests
{
    public class InputBridgeTests
    {
        private GameObject _go;

        [SetUp]
        public void SetUp()
        {
            MaquiServices.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
            MaquiServices.Reset();
        }

        [UnityTest]
        public IEnumerator Awake_InitializesDefaultProvider()
        {
            _go = new GameObject("TestInputBridge");
            var bridge = _go.AddComponent<InputBridge>();
            yield return null;

            // Default LegacyInputProvider is set in Awake
            Assert.IsNotNull(bridge);
        }

        [UnityTest]
        public IEnumerator SetProvider_SwapsProvider()
        {
            _go = new GameObject("TestInputBridge");
            var bridge = _go.AddComponent<InputBridge>();
            yield return null;

            var mockProvider = new MockInputProvider();
            mockProvider.AxisValue = 0.75f;

            bridge.SetProvider(mockProvider);

            Assert.AreEqual(0.75f, bridge.GetAxis("Horizontal"));
        }

        [UnityTest]
        public IEnumerator GetAxis_ProxiesToProvider()
        {
            _go = new GameObject("TestInputBridge");
            var bridge = _go.AddComponent<InputBridge>();
            yield return null;

            var mock = new MockInputProvider { AxisValue = -0.5f };
            bridge.SetProvider(mock);

            Assert.AreEqual(-0.5f, bridge.GetAxis("any"));
            Assert.AreEqual("any", mock.LastAxisName);
        }

        [UnityTest]
        public IEnumerator GetButton_ProxiesToProvider()
        {
            _go = new GameObject("TestInputBridge");
            var bridge = _go.AddComponent<InputBridge>();
            yield return null;

            var mock = new MockInputProvider { ButtonValue = true };
            bridge.SetProvider(mock);

            Assert.IsTrue(bridge.GetButton("Fire1"));
            Assert.AreEqual("Fire1", mock.LastButtonName);
        }

        [UnityTest]
        public IEnumerator GetButtonDown_ProxiesToProvider()
        {
            _go = new GameObject("TestInputBridge");
            var bridge = _go.AddComponent<InputBridge>();
            yield return null;

            var mock = new MockInputProvider { ButtonDownValue = true };
            bridge.SetProvider(mock);

            Assert.IsTrue(bridge.GetButtonDown("Jump"));
        }

        [UnityTest]
        public IEnumerator GetButtonUp_ProxiesToProvider()
        {
            _go = new GameObject("TestInputBridge");
            var bridge = _go.AddComponent<InputBridge>();
            yield return null;

            var mock = new MockInputProvider { ButtonUpValue = true };
            bridge.SetProvider(mock);

            Assert.IsTrue(bridge.GetButtonUp("Jump"));
        }

        [UnityTest]
        public IEnumerator GetPointerPosition_ProxiesToProvider()
        {
            _go = new GameObject("TestInputBridge");
            var bridge = _go.AddComponent<InputBridge>();
            yield return null;

            var mock = new MockInputProvider { PointerValue = new Vector2(100f, 200f) };
            bridge.SetProvider(mock);

            Assert.AreEqual(new Vector2(100f, 200f), bridge.GetPointerPosition());
        }

        // ── Test doubles ──────────────────────────────────────────────────────

        private class MockInputProvider : IInputProvider
        {
            public float AxisValue;
            public bool ButtonValue;
            public bool ButtonDownValue;
            public bool ButtonUpValue;
            public Vector2 PointerValue;

            public string LastAxisName { get; private set; }
            public string LastButtonName { get; private set; }

            public float GetAxis(string axisName) { LastAxisName = axisName; return AxisValue; }
            public bool GetButton(string buttonName) { LastButtonName = buttonName; return ButtonValue; }
            public bool GetButtonDown(string buttonName) => ButtonDownValue;
            public bool GetButtonUp(string buttonName) => ButtonUpValue;
            public Vector2 GetPointerPosition() => PointerValue;
        }
    }
}
