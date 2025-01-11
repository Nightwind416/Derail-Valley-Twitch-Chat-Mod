using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.PanelConstructor
{
    /// <summary>
    /// Factory class for creating text display elements with configurable size and wrapping.
    /// Handles text truncation and multi-line display.
    /// </summary>
    public static class DisplayText
    {
        /// <summary>
        /// Creates a new text display element with specified formatting and positioning.
        /// </summary>
        /// <param name="parent">Parent transform to attach the text to</param>
        /// <param name="text">Content to display</param>
        /// <param name="xPosition">X position relative to parent</param>
        /// <param name="yPosition">Y position relative to parent</param>
        /// <param name="textColor">Optional text color</param>
        /// <param name="lines">Number of lines to display</param>
        /// <param name="fontSize">Font size in pixels</param>
        /// <returns>Created Text component</returns>
        public static Text Create(Transform parent, string text, int xPosition, int yPosition, Color? textColor = null, int lines = 1, int fontSize = 12)
        {
            GameObject textObj = new($"{text}TextDisplay");
            textObj.transform.SetParent(parent, false);

            // Get parent width
            float parentWidth = parent.GetComponent<RectTransform>().rect.width;

            Text displayText = textObj.AddComponent<Text>();
            displayText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            displayText.fontSize = fontSize;
            displayText.alignment = TextAnchor.UpperLeft;
            displayText.color = textColor ?? Color.white;

            // Calculate line height and total height
            float lineHeight = fontSize * 1.2f; // Approximate line height with some spacing
            float totalHeight = lineHeight * lines;

            // Set up rect transform
            RectTransform rect = textObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.sizeDelta = new Vector2(parentWidth - xPosition - 10, totalHeight); // Subtract xPosition and add small margin
            rect.anchoredPosition = new Vector2(xPosition, -yPosition);

            // Handle text overflow
            displayText.horizontalOverflow = HorizontalWrapMode.Wrap;
            displayText.verticalOverflow = VerticalWrapMode.Truncate;

            // Truncate text if it exceeds the line limit
            string processedText = text;
            if (GetApproximateLineCount(text, displayText) > lines)
            {
                processedText = TruncateText(text, displayText, lines);
            }
            displayText.text = processedText;

            return displayText;
        }

        /// <summary>
        /// Calculates the approximate number of lines needed for the text.
        /// </summary>
        private static int GetApproximateLineCount(string text, Text textComponent)
        {
            float width = textComponent.GetComponent<RectTransform>().sizeDelta.x;
            float charWidth = textComponent.fontSize * 0.6f; // Approximate character width
            int charsPerLine = Mathf.FloorToInt(width / charWidth);
            return Mathf.CeilToInt((float)text.Length / charsPerLine);
        }

        /// <summary>
        /// Truncates text to fit within specified number of lines.
        /// </summary>
        private static string TruncateText(string text, Text textComponent, int maxLines)
        {
            float width = textComponent.GetComponent<RectTransform>().sizeDelta.x;
            float charWidth = textComponent.fontSize * 0.6f;
            int charsPerLine = Mathf.FloorToInt(width / charWidth);
            int maxChars = charsPerLine * maxLines - 3; // Leave room for "..."

            if (text.Length <= maxChars)
                return text;

            return text.Substring(0, maxChars) + "...";
        }
    }
}
