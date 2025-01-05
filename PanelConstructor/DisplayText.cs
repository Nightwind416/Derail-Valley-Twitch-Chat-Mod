using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.PanelConstructor
{
    public static class DisplayText
    {
        public static Text Create(Transform parent, string text, int xPosition, int yPosition, Color? textColor = null, int fontSize = 12)
        {
            GameObject textObj = new($"{text}TextDisplay");
            textObj.transform.SetParent(parent, false);

            Text displayText = textObj.AddComponent<Text>();
            displayText.text = text;
            displayText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            displayText.fontSize = fontSize;
            displayText.alignment = TextAnchor.UpperLeft;
            displayText.color = textColor ?? Color.white;

            RectTransform rect = textObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.sizeDelta = new Vector2(200, 20); // Adjust size as needed
            rect.anchoredPosition = new Vector2(xPosition, -yPosition);

            return displayText;
        }
    }
}
