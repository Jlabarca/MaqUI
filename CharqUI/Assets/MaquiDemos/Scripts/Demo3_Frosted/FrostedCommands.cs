using VitalRouter;

namespace MaquiDemos.Frosted
{
    /// <summary>Opens the notification tray (Modal layer, slides from top).</summary>
    public readonly record struct OpenNotificationsCommand : ICommand;

    /// <summary>Closes the notification tray.</summary>
    public readonly record struct CloseNotificationsCommand : ICommand;

    /// <summary>Opens the settings sheet (Modal layer, slides from bottom).</summary>
    public readonly record struct OpenSettingsCommand : ICommand;

    /// <summary>Closes the settings sheet.</summary>
    public readonly record struct CloseSettingsCommand : ICommand;

    /// <summary>Published when the user changes a setting value.</summary>
    public readonly record struct SettingChangedCommand(string Key, float Value) : ICommand;
}
