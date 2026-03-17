using UnityEngine;

namespace Maqui.Core.Bridge
{
    /// <summary>
    /// Input abstraction bridge for Maqui.
    /// Proxies calls to the currently active IInputProvider.
    /// Registered into MaquiServices as IInputBridge by CoreBootstrap.
    /// </summary>
    public class InputBridge : MonoBehaviour, IInputBridge
    {
        private IInputProvider _provider;

        private void Awake()
        {
            InitializeDefaultProvider();
        }

        private void InitializeDefaultProvider()
        {
            // By default, use Legacy Provider if nothing else is set
            // In a real scenario, this would check for New Input System availability
            SetProvider(new LegacyInputProvider());
        }

        public void SetProvider(IInputProvider provider)
        {
            _provider = provider;
            Debug.Log($"[Maqui] Input Provider set to: {provider.GetType().Name}");
        }

        public float GetAxis(string axisName) => _provider?.GetAxis(axisName) ?? 0f;
        public bool GetButton(string buttonName) => _provider?.GetButton(buttonName) ?? false;
        public bool GetButtonDown(string buttonName) => _provider?.GetButtonDown(buttonName) ?? false;
        public bool GetButtonUp(string buttonName) => _provider?.GetButtonUp(buttonName) ?? false;
        public Vector2 GetPointerPosition() => _provider?.GetPointerPosition() ?? Vector2.zero;
    }

    /// <summary>
    /// Default implementation using Unity's Legacy Input Manager.
    /// Falls back gracefully if Input System package is active.
    /// </summary>
    public class LegacyInputProvider : IInputProvider
    {
        public float GetAxis(string axisName)
        {
            try { return Input.GetAxis(axisName); }
            catch (System.InvalidOperationException) { return 0f; }
        }

        public bool GetButton(string buttonName)
        {
            try { return Input.GetButton(buttonName); }
            catch (System.InvalidOperationException) { return false; }
        }

        public bool GetButtonDown(string buttonName)
        {
            try { return Input.GetButtonDown(buttonName); }
            catch (System.InvalidOperationException) { return false; }
        }

        public bool GetButtonUp(string buttonName)
        {
            try { return Input.GetButtonUp(buttonName); }
            catch (System.InvalidOperationException) { return false; }
        }

        public Vector2 GetPointerPosition()
        {
            try { return Input.mousePosition; }
            catch (System.InvalidOperationException) { return Vector2.zero; }
        }
    }
}
