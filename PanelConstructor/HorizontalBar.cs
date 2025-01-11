using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.PanelConstructor
{
    /// <summary>
    /// Factory class for creating horizontal separator lines in the UI.
    /// </summary>
    public static class HorizontalBar
    {
        /// <summary>
        /// Creates a horizontal separator line with specified positioning.
        /// </summary>
        /// <param name="parent">Parent transform to attach the bar to</param>
        /// <param name="yPosition">Y position relative to parent</param>
        /// <param name="color">Optional color for the bar</param>
        /// <returns>Created GameObject containing the bar</returns>
        public static GameObject Create(Transform parent, int yPosition, Color? color = null)
        {
            GameObject horizontalBar = new("HorizontalBar");
            horizontalBar.transform.SetParent(parent, false);

            Image barImage = horizontalBar.AddComponent<Image>();
            barImage.color = color ?? Color.gray;

            RectTransform rect = horizontalBar.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.sizeDelta = new Vector2(0, 1); // Adjust height as needed
            rect.anchoredPosition = new Vector2(0, -yPosition);

            return horizontalBar;
        }
    }
}
