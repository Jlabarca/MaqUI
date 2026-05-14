// SPDX-License-Identifier: MIT
// MaqUI v2 — ReconcilerParityTests (NUnit PlayMode).
//
// =====================================================================
// MAQUIV2.2 — BLIND DRAFT. Operator must run in Unity Test Runner.
// =====================================================================
//
// P2.6 parity test. Loads the HelloWorld v2 component, drives one frame,
// and asserts the resulting VisualElement tree matches the hand-coded
// UIToolkit reference (structurally + style key props).

using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Maqui.V2;

namespace Maqui.V2.Tests.Runtime
{
    public class ReconcilerParityTests
    {
        private VisualElement _root;
        private Gui _gui;
        private UIToolkitBackend _backend;

        [SetUp]
        public void SetUp()
        {
            _root = new VisualElement { name = "test-root" };
            _gui = new Gui();
            _backend = new UIToolkitBackend(_root);
        }

        [TearDown]
        public void TearDown()
        {
            _root?.RemoveFromHierarchy();
        }

        [UnityTest]
        public IEnumerator HelloWorld_Reconciles_ToColumnRectLabel_Structure()
        {
            _gui.BeginFrame();
            _gui.Column();
                _gui.DrawRect(new Color32(180, 32, 32, 255), Size.Pixels(200), Size.Pixels(80));
                _gui.DrawText("Hello, world!", new Color32(255, 255, 255, 255), fontSize: 20f);
            _gui.EndColumn();
            _gui.EndFrame();
            _gui.Render(_backend);

            yield return null; // allow one layout pass

            // Root should have one child (the Column).
            Assert.AreEqual(1, _root.childCount, "Root should contain exactly one Column container.");

            var column = _root[0];
            Assert.AreEqual(FlexDirection.Column, column.resolvedStyle.flexDirection, "Container should be column-flex.");
            Assert.AreEqual(2, column.childCount, "Column should hold rect + label.");

            // Rect props.
            var rect = column[0];
            Assert.IsNotInstanceOf<Label>(rect, "First child should be a plain rect, not a Label.");
            Assert.AreEqual(200f, rect.resolvedStyle.width, 0.5f, "Rect width should be 200px.");
            Assert.AreEqual(80f, rect.resolvedStyle.height, 0.5f, "Rect height should be 80px.");

            // Label props.
            var label = column[1] as Label;
            Assert.NotNull(label, "Second child should be a Label.");
            Assert.AreEqual("Hello, world!", label.text);
            Assert.AreEqual(20f, label.resolvedStyle.fontSize, 0.5f);
        }

        [UnityTest]
        public IEnumerator Render_Twice_DoesNotRecreateElements()
        {
            void Build()
            {
                _gui.BeginFrame();
                _gui.Column();
                    _gui.DrawRect(new Color32(180, 32, 32, 255), Size.Pixels(200), Size.Pixels(80));
                    _gui.DrawText("Hello, world!", new Color32(255, 255, 255, 255), fontSize: 20f);
                _gui.EndColumn();
                _gui.EndFrame();
                _gui.Render(_backend);
            }

            Build();
            yield return null;
            int firstColumnId = _root[0].GetHashCode();
            int firstRectId = _root[0][0].GetHashCode();

            Build();
            yield return null;

            Assert.AreEqual(firstColumnId, _root[0].GetHashCode(), "Column element should be reused, not recreated.");
            Assert.AreEqual(firstRectId, _root[0][0].GetHashCode(), "Rect element should be reused, not recreated.");
        }
    }
}
