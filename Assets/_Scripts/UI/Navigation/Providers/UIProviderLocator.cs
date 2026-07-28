using System;

namespace CustomUI.Navigation.Providers
{
    /// <summary>
    /// Simple locator for UI provider adapters. Projects can replace providers at runtime
    /// by assigning to these static properties (e.g., from a bootstrap script).
    /// Default providers are safe no-ops if the game's audio/effect systems are absent.
    /// </summary>
    public static class UIProviderLocator
    {
        private static IUIAudioProvider _audio = null;
        private static IUIEffectProvider _effect = null;

        static UIProviderLocator()
        {
            // Initialize default providers
            _audio = new DefaultUIAudioProvider();
            _effect = new DefaultUIEffectProvider();
        }

        public static IUIAudioProvider Audio
        {
            get => _audio;
            set => _audio = value ?? new DefaultUIAudioProvider();
        }

        public static IUIEffectProvider Effect
        {
            get => _effect;
            set => _effect = value ?? new DefaultUIEffectProvider();
        }
    }
}
