using VitalRouter;

namespace MaquiDemos.Gallery
{
    public enum GalleryTab { Buttons = 0, Cards = 1, Fields = 2 }

    /// <summary>Published when the user clicks a tab button.</summary>
    public readonly record struct SwitchTabCommand(GalleryTab Tab) : ICommand;

    /// <summary>Published when the user clicks the theme toggle.</summary>
    public readonly record struct ToggleThemeCommand : ICommand;
}
