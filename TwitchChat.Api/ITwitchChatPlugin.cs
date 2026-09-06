namespace TwitchChat.Api
{
    /// <summary>
    /// A panel contributed by something other than TwitchChat itself. Implement this to put your own
    /// content on the mod's world-space displays: the cab displays in a locomotive and the VR wrist panel.
    /// </summary>
    /// <remarks>
    /// TwitchChat creates one <see cref="IPanelContent"/> per plugin per display, so a plugin instance may
    /// be asked for content several times over and must not assume there is only one of it on screen.
    /// Everything is called on Unity's main thread.
    /// </remarks>
    public interface ITwitchChatPlugin
    {
        /// <summary>
        /// Stable identifier for this panel, unique across all plugins. It is written into the player's
        /// settings as the panel a display was last showing, so changing it loses that choice. Something
        /// like "AI Traffic" or "MyMod.Speedometer".
        /// </summary>
        string Id { get; }

        /// <summary>The name shown in the panel's title bar and on its button in the Mods menu.</summary>
        string Title { get; }

        /// <summary>
        /// Whether this panel should exist at all right now, typically "is the mod I am reporting on
        /// installed". Checked when a display's panel stack is built, so it should be cheap and it should
        /// not depend on the world being loaded. Content that is merely not ready yet is better handled
        /// by the panel saying so than by disappearing from the menu.
        /// </summary>
        bool IsAvailable { get; }

        /// <summary>
        /// Builds the panel's content onto the surface it is given. Called once per display, when that
        /// display's panel stack is created, whether or not the panel is the one currently shown.
        /// </summary>
        IPanelContent CreateContent(IPanelSurface surface);
    }
}
