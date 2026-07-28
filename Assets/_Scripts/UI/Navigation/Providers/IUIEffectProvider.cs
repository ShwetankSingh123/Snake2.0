using UnityEngine.UI;

namespace CustomUI.Navigation.Providers
{
    /// <summary>
    /// Adapter interface for optional UI effect integrations (third-party visual effects).
    /// Keep implementations minimal: methods may be no-ops when no effect package is present.
    /// </summary>
    public interface IUIEffectProvider
    {
        /// <summary>Enable a UI effect on the specified target (outline, shadow, etc.).</summary>
        void EnableEffect(Graphic target);

        /// <summary>Disable any UI effect previously enabled on the target.</summary>
        void DisableEffect(Graphic target);
    }
}
