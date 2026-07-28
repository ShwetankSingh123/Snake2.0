namespace CustomUI.Navigation.Providers
{
    /// <summary>
    /// Lightweight adapter interface for UI audio hooks.
    /// Implement this to bridge your project's audio system into the navigation package.
    /// </summary>
    public interface IUIAudioProvider
    {
        /// <summary>Play a standard UI button click sound.</summary>
        void PlayUIButtonClick();

        /// <summary>Play a subtle navigation / focus change sound.</summary>
        void PlayNavigationFocus();

        /// <summary>Play a generic submit sound (alias for click in many projects).</summary>
        void PlaySubmit();
    }
}
