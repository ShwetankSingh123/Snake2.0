using UnityEngine.UI;

namespace CustomUI.Navigation.Providers
{
    /// <summary>
    /// Default implementation of IUIEffectProvider. This implementation is a safe no-op so
    /// projects without third-party UI effect packages will not fail. Replace with an
    /// implementation that hooks into your preferred visual effect package if desired.
    /// </summary>
    public class DefaultUIEffectProvider : IUIEffectProvider
    {
        public void EnableEffect(Graphic target)
        {
            // No-op by default. Example integrations (e.g. Coffee.UIEffect) can enable/disable
            // the effect component on the target here.
        }

        public void DisableEffect(Graphic target)
        {
            // No-op
        }
    }
}
