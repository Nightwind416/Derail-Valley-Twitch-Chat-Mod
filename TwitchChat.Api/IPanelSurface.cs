using UnityEngine;

namespace TwitchChat.Api
{
    /// <summary>
    /// The piece of a display a plugin panel draws on, handed to
    /// <see cref="ITwitchChatPlugin.CreateContent"/>.
    /// </summary>
    /// <remarks>
    /// There are two places to put things, matching how the mod's own panels are built:
    /// <see cref="Root"/> for anything placed at a fixed spot, such as a header or a row of buttons, and
    /// <see cref="Content"/> for a list that grows, which scrolls and lays itself out top to bottom.
    /// </remarks>
    public interface IPanelSurface
    {
        /// <summary>
        /// The panel itself. Widgets parented here are positioned by hand, in canvas units measured down
        /// from the top, and are not scrolled. The title row occupies roughly the first 30 units.
        /// </summary>
        Transform Root { get; }

        /// <summary>
        /// The scrolling area's content. Children are laid out vertically and the area scrolls when they
        /// no longer fit, so this is where a list of rows belongs. Widgets from
        /// <see cref="IWidgetFactory"/> that take a position ignore it here; the layout decides.
        /// </summary>
        Transform Content { get; }

        /// <summary>The display's current size in canvas units. Changes when the player drags an edge.</summary>
        Vector2 Size { get; }

        /// <summary>
        /// Builds widgets that match the mod's own, including the object naming its colour settings key
        /// off, so a plugin panel is themed along with everything else.
        /// </summary>
        IWidgetFactory Widgets { get; }

        /// <summary>
        /// Keeps the top of the panel clear of the scrolling area, so a header placed on
        /// <see cref="Root"/> is not sat on by whatever is in <see cref="Content"/>.
        /// </summary>
        /// <param name="height">
        /// How much room the header needs below the title row, in canvas units. Zero puts the scrolling
        /// area back where it started.
        /// </param>
        void ReserveHeader(float height);

        /// <summary>Writes a line to the mod's log, tagged with the plugin's id.</summary>
        void Log(string message);
    }
}
