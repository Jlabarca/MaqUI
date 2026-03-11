using System;
using UnityEngine;

namespace Maqui.Core.Presentation
{
    /// <summary>
    /// Represents a live window managed by MaquiWindowManager.
    /// Dispose to destroy the window and release its assets.
    /// </summary>
    public interface IWindowHandle : IDisposable
    {
        /// <summary>Whether the window's root GameObject is currently active.</summary>
        bool IsVisible { get; }

        /// <summary>The layer this window lives on.</summary>
        UILayer Layer { get; }

        /// <summary>Root GameObject of the window.</summary>
        GameObject Root { get; }

        /// <summary>Activates the window's root GameObject.</summary>
        void Show();

        /// <summary>Deactivates the window's root GameObject without destroying it.</summary>
        void Hide();
    }
}
