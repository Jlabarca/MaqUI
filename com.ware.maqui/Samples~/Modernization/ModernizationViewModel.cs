using Maqui.Core.Logic;
using R3;

namespace Maqui.Samples.Modernization
{
    /// <summary>
    /// ViewModel for the Visual Modernization sample.
    /// Manages reactive properties that drive procedural visuals.
    /// </summary>
    public class ModernizationViewModel : ViewModel
    {
        public readonly ReactiveProperty<bool> IsHighPerformance = new(true);
        public readonly ReactiveProperty<float> BlurIntensity = new(10f);
        public readonly ReactiveProperty<ThemeColorType> SelectedColorType = new(ThemeColorType.Primary);

        public void TogglePerformance()
        {
            IsHighPerformance.Value = !IsHighPerformance.Value;
        }

        public void SetColor(ThemeColorType type)
        {
            SelectedColorType.Value = type;
        }
    }
}
