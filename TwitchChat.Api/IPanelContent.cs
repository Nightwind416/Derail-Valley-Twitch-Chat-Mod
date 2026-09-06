using UnityEngine;

namespace TwitchChat.Api
{
    /// <summary>
    /// One plugin panel on one display. Created by <see cref="ITwitchChatPlugin.CreateContent"/> and then
    /// driven by TwitchChat for as long as that display exists.
    /// </summary>
    public interface IPanelContent
    {
        /// <summary>The panel has just been brought to the front of its display.</summary>
        void OnShow();

        /// <summary>The panel has just been hidden, because another one was shown or the display closed.</summary>
        void OnHide();

        /// <summary>
        /// Called once a frame while the panel is showing. Reading another mod's state every frame is
        /// rarely worth it; keep your own timer and refresh a few times a second instead.
        /// </summary>
        /// <param name="deltaTime">Seconds since the last call.</param>
        void Tick(float deltaTime);

        /// <summary>
        /// The display has been dragged to a new size, in canvas units. Panels have no size of their own:
        /// a display is whatever size the player has made it, so lay out against this rather than against
        /// any fixed dimensions.
        /// </summary>
        void OnResize(Vector2 size);
    }
}
