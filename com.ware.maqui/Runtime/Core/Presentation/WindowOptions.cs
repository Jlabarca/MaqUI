namespace Maqui.Core.Presentation
{
    /// <summary>
    /// Options for window creation via <see cref="IUIService.ShowWindowAsync{TView,TViewModel}"/>.
    /// Use <see cref="Pooled"/> to enable object pooling for frequently opened windows.
    /// </summary>
    public struct WindowOptions
    {
        /// <summary>
        /// When true, Dispose() returns the window to an internal pool instead of destroying it.
        /// The next ShowWindowAsync call for the same assetKey will reuse the pooled instance.
        /// </summary>
        public bool Pooled;

        /// <summary>
        /// Maximum number of instances to keep in the pool per asset key.
        /// 0 means use the default (4). Excess instances are destroyed normally.
        /// </summary>
        public int MaxPoolSize;

        public static readonly WindowOptions Default = new WindowOptions();
    }
}
