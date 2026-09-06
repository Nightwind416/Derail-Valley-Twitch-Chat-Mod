using System;
using TwitchChat.Api;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TwitchChat.Plugins
{
    /// <summary>
    /// Hands the mod's own widget factories to plugins through the public interface, so a plugin panel is
    /// built out of exactly the same parts as the mod's panels: same fonts, same VR poke colliders, and
    /// the same object naming the colour settings key off.
    /// </summary>
    internal sealed class WidgetFactory : IWidgetFactory
    {
        internal static readonly WidgetFactory Instance = new();

        private WidgetFactory() { }

        public Text CreateLabel(Transform parent, string text, int x, int y, Color? color = null)
            => PanelConstructor.Label.Create(parent, text, x, y, color);

        public Text CreateText(Transform parent, string text, int x, int y, Color? color = null, int lines = 1, int fontSize = 12)
            => PanelConstructor.DisplayText.Create(parent, text, x, y, color, lines, fontSize);

        public void CreateTitle(Transform parent, string text, int fontSize = 16, Color? color = null)
            => PanelConstructor.Title.Create(parent, text, fontSize, color);

        public GameObject CreateSection(Transform parent, string name, int y, int height, bool withLabel = true)
            => PanelConstructor.Section.Create(parent, name, y, height, withLabel);

        public GameObject CreateSeparator(Transform parent, int y, Color? color = null)
            => PanelConstructor.HorizontalBar.Create(parent, y, color);

        public Button CreateButton(Transform parent, string text, int x, int y, Color? color = null, Action? clicked = null, int? width = null)
            => PanelConstructor.Button.Create(parent, text, x, y, color, Wrap(clicked), width);

        public Toggle CreateToggle(Transform parent, int x, int y, string textOn, string textOff, bool initialState = true, Color? onColor = null, Color? offColor = null, Action<bool>? changed = null)
            => PanelConstructor.Toggle.Create(parent, x, y, textOn, textOff, initialState, onColor, offColor, Wrap(changed));

        public Slider CreateSlider(Transform parent, int x, int y, float min = 0f, float max = 1f, float value = 0.5f, Action<float>? changed = null)
            => PanelConstructor.Slider.Create(parent, x, y, min, max, value, Wrap(changed));

        public GameObject CreateValueBar(Transform parent, string label, int y, Color? fillColor = null)
            => PanelConstructor.ValueBar.Create(parent, label, y, fillColor);

        public void SetValueBar(GameObject bar, float fraction, string? valueText = null)
            => PanelConstructor.ValueBar.SetValue(bar, fraction, valueText);

        // The public API speaks in plain delegates so a plugin need not care that the widgets underneath
        // are uGUI; these turn them back into the UnityEvent callbacks those widgets want
        private static UnityAction? Wrap(Action? action) => action == null ? null : new UnityAction(action);

        private static UnityAction<T>? Wrap<T>(Action<T>? action) => action == null ? null : new UnityAction<T>(action);
    }
}
