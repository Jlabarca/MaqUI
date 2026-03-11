using R3;
using System;

namespace Maqui.Core.Logic
{
    /// <summary>
    /// Base class for all ViewModels in Maqui.
    /// Provides lifecycle management for R3 disposables.
    /// </summary>
    public abstract class ViewModel : IDisposable
    {
        protected readonly CompositeDisposable Disposables = new();

        /// <summary>
        /// Called when the ViewModel is initialized.
        /// Use this to setup bindings and initial state.
        /// </summary>
        public virtual void Initialize() { }

        public virtual void Dispose()
        {
            Disposables.Dispose();
        }
    }
}
