using System;
using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.PanelConstructor
{
    /// <summary>
    /// The strip a display folds down to. It stands in for the panels while a display is minimized, rather
    /// than the panels being made small, so that nothing is left half on show.
    /// </summary>
    /// <remarks>
    /// Hiding a panel's contents piece by piece was what the old minimize did, and it could not work: a
    /// panel that rebuilds its own rows, which several do and any plugin may, simply builds them again a
    /// moment later, and a hidden button in VR is still something a finger can press, since the presses go
    /// through colliders rather than through anything a mask would clip. A separate object that is either
    /// there or not has neither problem.
    /// </remarks>
    public static class MinimizedBar
    {
        /// <summary>
        /// How tall the strip is, in canvas units. The same room a panel's title row takes, so the title
        /// reads exactly where it did before the display was folded away.
        /// </summary>
        public const float Height = 40f;

        /// <summary>
        /// Builds the strip on a display's MenuPanel, inactive until it is wanted.
        /// </summary>
        /// <param name="menuPanel">The display's MenuPanel, which the strip fills while it is shown.</param>
        /// <param name="restore">Opens the display back up. Sits where the minimize button was.</param>
        /// <param name="close">Closes the display. Sits where the close button was.</param>
        public static GameObject Create(Transform menuPanel, Action restore, Action close)
        {
            // The name ends in "Panel" so the colour settings paint it along with the panels it stands in for
            GameObject bar = new("MinimizedPanel");
            bar.transform.SetParent(menuPanel, false);

            Image background = bar.AddComponent<Image>();
            background.color = Settings.Instance.panelColor;

            RectTransform rect = bar.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0, 1);

            Title.Create(bar.transform, string.Empty, 18);

            UnityEngine.UI.Button restoreButton = Button.Create(bar.transform, " + ", 0, 0, Color.white, () => restore());
            RectTransform restoreRect = restoreButton.GetComponent<RectTransform>();
            restoreRect.anchorMin = new Vector2(0, 1);
            restoreRect.anchorMax = new Vector2(0, 1);
            restoreRect.pivot = new Vector2(0, 1);

            UnityEngine.UI.Button closeButton = Button.Create(bar.transform, " x ", 0, 0, Color.white, () => close());
            RectTransform closeRect = closeButton.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1, 1);
            closeRect.anchorMax = new Vector2(1, 1);
            closeRect.pivot = new Vector2(1, 1);

            bar.SetActive(false);
            return bar;
        }

        /// <summary>Says which panel the display will come back to, so a folded display is still identifiable.</summary>
        public static void SetTitle(GameObject bar, string title)
        {
            Transform? titleObject = bar.transform.Find("Title");
            Text? text = titleObject != null ? titleObject.GetComponent<Text>() : null;
            if (text != null)
            {
                text.text = title;
            }
        }
    }
}
