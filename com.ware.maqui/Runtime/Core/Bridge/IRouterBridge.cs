using VitalRouter;

namespace Maqui.Core.Bridge
{
    /// <summary>
    /// Contract for the VitalRouter bridge.
    /// Provides access to the default Router instance.
    /// </summary>
    public interface IRouterBridge
    {
        Router Router { get; }
    }
}
