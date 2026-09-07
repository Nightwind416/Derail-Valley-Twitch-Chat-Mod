using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.PanelConstructor
{
    /// <summary>
    /// The colours one panel is painted in, carried on the panel itself so that anything built on it can
    /// ask. Every panel has one; a panel the player has given colours of its own carries those, and the
    /// rest carry the shared defaults.
    /// </summary>
    /// <remarks>
    /// The widget factories read this as they build, rather than everything being repainted afterwards.
    /// That is the only way to keep up: the Displays and Mods panels rebuild their rows whenever they are
    /// opened, and a plugin can rebuild its content at any moment, so widgets born the wrong colour would
    /// stay the wrong colour until something happened to sweep them.
    /// </remarks>
    public class PanelTheme : MonoBehaviour
    {
        public Color PanelColor = Settings.DefaultPanelColor;
        public Color SectionColor = Settings.DefaultSectionColor;
        public Color ButtonColor = Settings.DefaultButtonColor;

        /// <summary>
        /// The theme of the panel this transform sits on, or null if it is not on one - which is the case
        /// for the grab bars and for anything built before a panel exists.
        /// </summary>
        /// <remarks>
        /// The parents are walked by hand rather than with GetComponentInParent, which skips inactive
        /// objects: the template canvas is switched off before its panels are built, so every widget on a
        /// template would otherwise find nothing.
        /// </remarks>
        public static PanelTheme? For(Transform? start)
        {
            for (Transform? at = start; at != null; at = at.parent)
            {
                PanelTheme theme = at.GetComponent<PanelTheme>();
                if (theme != null)
                {
                    return theme;
                }
            }

            return null;
        }

        /// <summary>The section colour to build with under this parent, falling back to the default.</summary>
        public static Color SectionColorFor(Transform? parent) =>
            For(parent) is PanelTheme theme ? theme.SectionColor : Settings.Instance.sectionColor;

        /// <summary>The button colour to build with under this parent, falling back to the default.</summary>
        public static Color ButtonColorFor(Transform? parent) =>
            For(parent) is PanelTheme theme ? theme.ButtonColor : Settings.Instance.buttonColor;

        /// <summary>
        /// A button's colour while a hand is touching it: the colour it already has, lifted, so the
        /// highlight follows whatever the player has chosen instead of replacing it with a fixed grey.
        /// Its transparency is left alone, or a touch would make a see-through panel solid.
        /// </summary>
        public static Color Highlight(Color color) => new(
            Mathf.Min(1f, color.r + 0.3f),
            Mathf.Min(1f, color.g + 0.3f),
            Mathf.Min(1f, color.b + 0.3f),
            color.a);

        /// <summary>
        /// Repaints everything on one panel that follows the colours: the panel's own background, its
        /// sections, and its buttons and toggles. Used when the colours change, rather than on every build.
        /// </summary>
        public void Repaint(GameObject panelObject)
        {
            Image own = panelObject.GetComponent<Image>();
            if (own != null)
            {
                own.color = PanelColor;
            }

            foreach (Image image in panelObject.GetComponentsInChildren<Image>(true))
            {
                if (image.gameObject == panelObject)
                {
                    continue;
                }

                if (image.gameObject.name.EndsWith("Section"))
                {
                    image.color = SectionColor;
                }
                else if (image.GetComponent<UnityEngine.UI.Button>() != null
                    || image.GetComponent<UnityEngine.UI.Toggle>() != null)
                {
                    image.color = ButtonColor;
                }
            }
        }
    }
}
