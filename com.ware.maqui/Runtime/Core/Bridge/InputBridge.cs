using UnityEngine;

namespace Maqui.Core.Bridge
{
    /// <summary>
    /// Static bridge for accessing input.
    /// Proxies calls to the currently active IInputProvider.
    /// </summary>
    public class InputBridge : MonoBehaviour
    {
        public static InputBridge Instance { get; private set; }

        private IInputProvider _provider;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeDefaultProvider();
            }
            else
            {
                Destroy(gameObject);
            }
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
    /// </summary>
    public class LegacyInputProvider : IInputProvider
    {
        public float GetAxis(string axisName) => Input.GetAxis(axisName);
        public bool GetButton(string buttonName) => Input.GetButton(buttonName);
        public bool GetButtonDown(string buttonName) => Input.GetButtonDown(buttonName);
        public bool GetButtonUp(string buttonName) => Input.GetButtonUp(buttonName);
        public Vector2 GetPointerPosition() => Input.mousePosition;
    }
}
