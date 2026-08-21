// SPDX-License-Identifier: MIT
// MaqUI v2 — GuiDriver MonoBehaviour. Bridges UIDocument → Gui → UIToolkitBackend.
//
// =====================================================================
// MAQUIV2.2 — BLIND DRAFT. Operator must compile in Unity to validate.
// =====================================================================
//
// Attach this component alongside a UIDocument on the same GameObject (or
// assign the UIDocument via the inspector field). At runtime it:
//   1. Constructs a Gui + UIToolkitBackend bound to UIDocument.rootVisualElement
//   2. Each LateUpdate calls: BeginFrame → Component(_gui) → EndFrame → Render(backend)
//
// Operator wires their UI by either:
//   - Setting `ComponentMethod` from code (test/headless flow), or
//   - Subclassing GuiDriver and overriding `BuildUI(Gui gui)`

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Maqui
{
    /// <summary>
    /// MonoBehaviour driver that renders a code-only Maqui component into a
    /// UI Toolkit document each frame.
    ///
    /// <para><b>Status: blind draft.</b> Not validated outside Unity Editor.</para>
    /// </summary>
    [DisallowMultipleComponent]
    public class GuiDriver : MonoBehaviour
    {
        [SerializeField] private UIDocument _uiDocument;

        private Gui _gui;
        private UIToolkitBackend _backend;

        /// <summary>
        /// User-supplied component method. Receives the Gui to record into.
        /// Replaces a `BuildUI` override for callers who prefer composition over inheritance.
        /// </summary>
        public Action<Gui> ComponentMethod { get; set; }

        protected virtual void Awake()
        {
            if (_uiDocument == null) _uiDocument = GetComponent<UIDocument>();
            if (_uiDocument == null)
            {
                Debug.LogError("Maqui.GuiDriver: no UIDocument assigned or found on this GameObject.", this);
                enabled = false;
                return;
            }

            _gui = new Gui();
            _backend = new UIToolkitBackend(_uiDocument.rootVisualElement);
        }

        protected virtual void LateUpdate()
        {
            if (_gui == null || _backend == null) return;

            _gui.BeginFrame();
            BuildUI(_gui);
            _gui.EndFrame();
            _gui.Render(_backend);
        }

        /// <summary>
        /// Override to author your UI directly. Default impl dispatches to
        /// <see cref="ComponentMethod"/> if assigned.
        /// </summary>
        protected virtual void BuildUI(Gui gui)
        {
            ComponentMethod?.Invoke(gui);
        }
    }
}
