using Maqui.Core.Logic;
using R3;
using System.Collections.Generic;

namespace MaquiDemos.Frosted
{
    public sealed class NotificationEntry
    {
        public string Icon;      // "🔔" "⚔" "🛡" etc.
        public string Title;
        public string Body;
        public string Time;
    }

    /// <summary>
    /// Demo 3 — Frosted Glass HUD state (Overlay layer).
    /// Drives the persistent HUD panel and feeds notification/settings sub-windows.
    /// </summary>
    public sealed class FrostedHUDViewModel : ViewModel
    {
        // ── Persistent HUD data ────────────────────────────────────────────────
        public readonly ReactiveProperty<string> PlayerName     = new("Aelindra");
        public readonly ReactiveProperty<string> CurrentZone    = new("The Shattered Vale");
        public readonly ReactiveProperty<float>  XPRatio        = new(0.62f);  // 0-1
        public readonly ReactiveProperty<int>    UnreadCount    = new(3);

        // ── Notifications (shared between HUD badge and NotificationView) ──────
        public readonly ReactiveProperty<IReadOnlyList<NotificationEntry>> Notifications;

        // ── Settings (shared between SettingsView and HUD) ────────────────────
        public readonly ReactiveProperty<float>  MasterVolume   = new(0.8f);
        public readonly ReactiveProperty<float>  MusicVolume    = new(0.6f);
        public readonly ReactiveProperty<bool>   FullscreenMode = new(true);
        public readonly ReactiveProperty<bool>   ShowFPS        = new(false);

        public FrostedHUDViewModel()
        {
            Notifications = new ReactiveProperty<IReadOnlyList<NotificationEntry>>(
                new List<NotificationEntry>
                {
                    new() { Icon = "⚔", Title = "Siege begins in 10 min", Body = "Ironwall Fortress — defend!", Time = "now" },
                    new() { Icon = "🛡", Title = "Guild quest completed",  Body = "+1,200 XP earned",            Time = "2m"  },
                    new() { Icon = "🔔", Title = "Friend request",         Body = "Kira_XII wants to party",    Time = "5m"  },
                });
        }

        public void ApplySetting(string key, float value)
        {
            switch (key)
            {
                case "master": MasterVolume.Value = value; break;
                case "music":  MusicVolume.Value  = value; break;
            }
        }

        public override void Dispose()
        {
            PlayerName.Dispose();
            CurrentZone.Dispose();
            XPRatio.Dispose();
            UnreadCount.Dispose();
            Notifications.Dispose();
            MasterVolume.Dispose();
            MusicVolume.Dispose();
            FullscreenMode.Dispose();
            ShowFPS.Dispose();
            base.Dispose();
        }
    }
}
