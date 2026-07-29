using MobileCore;

namespace Game
{
    /// <summary>
    /// A panel that only opens and closes. Used by the store (deliberately empty,
    /// GDD 5.7) and the profile panel (visual only, GDD 5.8). BasePanel is
    /// abstract, so these need a concrete component to attach.
    /// </summary>
    /// <remarks>
    /// Set the GameObject inactive in the scene — UIManager activates it on
    /// request. Nothing here deactivates itself, because Awake/Start first run at
    /// that moment and would close the panel as it opened.
    /// </remarks>
    public class OverlayPanel : BasePanel
    {
        /// <summary>Hook this to the close button's OnClick.</summary>
        public void OnCloseButtonClicked() => Hide();
    }
}
