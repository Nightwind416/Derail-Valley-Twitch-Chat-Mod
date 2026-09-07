using System;
using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.Api
{
    /// <summary>
    /// Builds the same widgets the mod's own panels are made of. Using these rather than raw uGUI keeps a
    /// plugin panel looking like the rest of the mod, gets it the VR poke colliders on anything clickable,
    /// and gets it recoloured along with everything else when the player changes the colour settings.
    /// </summary>
    /// <remarks>
    /// Positions are in canvas units measured down and right from the top left of the parent. Widgets
    /// parented to <see cref="IPanelSurface.Content"/> are laid out by the scroll area instead, so their
    /// positions are ignored there.
    /// </remarks>
    public interface IWidgetFactory
    {
        /// <summary>A short one-line caption.</summary>
        Text CreateLabel(Transform parent, string text, int x, int y, Color? color = null);

        /// <summary>A block of text that can wrap over several lines.</summary>
        Text CreateText(Transform parent, string text, int x, int y, Color? color = null, int lines = 1, int fontSize = 12);

        /// <summary>A heading across the top of an area.</summary>
        void CreateTitle(Transform parent, string text, int fontSize = 16, Color? color = null);

        /// <summary>
        /// A shaded box to group related widgets in, optionally labelled with its name. Parent your rows
        /// to the returned object to keep them together.
        /// </summary>
        GameObject CreateSection(Transform parent, string name, int y, int height, bool withLabel = true);

        /// <summary>A thin horizontal rule.</summary>
        GameObject CreateSeparator(Transform parent, int y, Color? color = null);

        /// <summary>A button, pokeable in VR and clickable outside it.</summary>
        Button CreateButton(Transform parent, string text, int x, int y, Color? color = null, Action? clicked = null, int? width = null);

        /// <summary>A two-state toggle showing one of two captions.</summary>
        Toggle CreateToggle(Transform parent, int x, int y, string textOn, string textOff, bool initialState = true, Color? onColor = null, Color? offColor = null, Action<bool>? changed = null);

        /// <summary>A slider. In VR each press advances it about a tenth and then wraps.</summary>
        Slider CreateSlider(Transform parent, int x, int y, float min = 0f, float max = 1f, float value = 0.5f, Action<float>? changed = null);

        /// <summary>
        /// A labelled bar for showing a fraction of something, such as a throttle position. Set the value
        /// afterwards with <see cref="SetValueBar"/>.
        /// </summary>
        GameObject CreateValueBar(Transform parent, string label, int y, Color? fillColor = null);

        /// <summary>
        /// Updates a bar made by <see cref="CreateValueBar"/>.
        /// </summary>
        /// <param name="bar">The object returned by <see cref="CreateValueBar"/>.</param>
        /// <param name="fraction">How full the bar is, from 0 to 1. Values outside that range are clamped.</param>
        /// <param name="valueText">Text shown at the right of the bar, or null to show a percentage.</param>
        void SetValueBar(GameObject bar, float fraction, string? valueText = null);
    }
}
