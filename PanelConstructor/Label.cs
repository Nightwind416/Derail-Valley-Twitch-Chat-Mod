using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.PanelConstructor
{
    /// <summary>
    /// Factory class for creating text labels with automatic sizing.
    /// </summary>
    public static class Label
    {
        /// <summary>
        /// Creates a new text label with specified text and positioning.
        /// </summary>
        /// <param name="parent">Parent transform to attach the label to</param>
        /// <param name="text">Label text content</param>
        /// <param name="xPosition">X position relative to parent</param>
        /// <param name="yPosition">Y position relative to parent</param>
        /// <param name="textColor">Optional text color</param>
        /// <returns>Created Text component</returns>
        public static Text Create(Transform parent, string text, int xPosition, int yPosition, Color? textColor = null)
        {
            GameObject labelObj = new($"{text}Label");
            labelObj.transform.SetParent(parent, false);

            // Create temporary text to measure width
            GameObject tempTextObj = new("TempText");
            Text tempText = tempTextObj.AddComponent<Text>();
            tempText.text = text;
            tempText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            tempText.fontSize = 12;
            tempText.cachedTextGenerator.Populate(text, tempText.GetGenerationSettings(Vector2.zero));
            float textWidth = tempText.cachedTextGenerator.GetPreferredWidth(text, tempText.GetGenerationSettings(Vector2.zero));
            GameObject.Destroy(tempTextObj);

            Text label = labelObj.AddComponent<Text>();
            label.text = text;
            label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            label.fontSize = 12;
            label.alignment = TextAnchor.UpperLeft;
            label.color = textColor ?? Color.white;

            RectTransform rect = labelObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.sizeDelta = new Vector2(textWidth + 10, 20);
            rect.anchoredPosition = new Vector2(xPosition, -yPosition);

            return label;
        }
    }
}
