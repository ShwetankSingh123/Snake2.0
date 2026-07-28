namespace CustomUI.Navigation
{
    /// <summary>
    /// Implement this on a component that needs custom behavior when the UI back/cancel action occurs.
    /// Return true from OnBackHandled to indicate the back action was handled and no further automatic closing
    /// should be performed by the navigation manager.
    /// </summary>
    public interface IBackHandler
    {
        /// <summary>
        /// Called when a back/cancel (Escape) is requested for the owning panel.
        /// Return true to indicate the event was handled (navigation manager won't auto-close the panel).
        /// </summary>
        bool OnBackHandled();
    }
}
