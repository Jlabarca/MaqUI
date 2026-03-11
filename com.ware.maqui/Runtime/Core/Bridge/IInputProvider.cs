using UnityEngine;

namespace Maqui.Core.Bridge
{
    /// <summary>
    /// Interface for providing input data to the Maqui system.
    /// Allows swapping between Legacy Input and New Input System.
    /// </summary>
    public interface IInputProvider
    {
        float GetAxis(string axisName);
        bool GetButton(string buttonName);
        bool GetButtonDown(string buttonName);
        bool GetButtonUp(string buttonName);
        Vector2 GetPointerPosition();
    }
}
