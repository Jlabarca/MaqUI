using UnityEngine;

namespace Maqui.Core.Bridge
{
    /// <summary>
    /// Contract for the input abstraction layer.
    /// Proxies calls to the currently active IInputProvider.
    /// </summary>
    public interface IInputBridge
    {
        void SetProvider(IInputProvider provider);
        float GetAxis(string axisName);
        bool GetButton(string buttonName);
        bool GetButtonDown(string buttonName);
        bool GetButtonUp(string buttonName);
        Vector2 GetPointerPosition();
    }
}
