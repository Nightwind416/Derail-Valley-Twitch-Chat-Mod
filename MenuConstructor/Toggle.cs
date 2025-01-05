using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.MenuConstructor
{
    public static class Toggle
    {
        public static UnityEngine.UI.Toggle Create(Transform parent, int xPosition, int yPosition, string textOn, string textOff, bool initialState = true, Color? color1 = null, Color? color2 = null)
        {
            GameObject toggleObj = new($"{textOn}/{textOff}Toggle");
            toggleObj.transform.SetParent(parent, false);

            // Create temporary text to measure width for both strings
            GameObject tempTextObj = new("TempText");
            Text tempText = tempTextObj.AddComponent<Text>();
            tempText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            tempText.fontSize = 12;

            tempText.text = textOn;
            tempText.cachedTextGenerator.Populate(textOn, tempText.GetGenerationSettings(Vector2.zero));
            float textWidthOn = tempText.cachedTextGenerator.GetPreferredWidth(textOn, tempText.GetGenerationSettings(Vector2.zero));

            tempText.text = textOff;
            tempText.cachedTextGenerator.Populate(textOff, tempText.GetGenerationSettings(Vector2.zero));
            float textWidthOff = tempText.cachedTextGenerator.GetPreferredWidth(textOff, tempText.GetGenerationSettings(Vector2.zero));

            float textWidth = Mathf.Max(textWidthOn, textWidthOff);
            GameObject.Destroy(tempTextObj);

            Image toggleImage = toggleObj.AddComponent<Image>();
            toggleImage.color = new Color(0, 0, 0, 0.75f);

            UnityEngine.UI.Toggle toggle = toggleObj.AddComponent<UnityEngine.UI.Toggle>();
            toggle.isOn = initialState;

            GameObject toggleTextObj = new("Text");
            toggleTextObj.transform.SetParent(toggleObj.transform, false);
            Text toggleText = toggleTextObj.AddComponent<Text>();
            toggleText.text = initialState ? textOn : textOff;
            toggleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            toggleText.fontSize = 12;
            toggleText.alignment = TextAnchor.MiddleCenter;
            toggleText.color = initialState ? (color1 ?? Color.white) : (color2 ?? Color.gray);

            // Add listener to update text and color when toggle changes
            toggle.onValueChanged.AddListener((isOn) => {
                toggleText.text = isOn ? textOn : textOff;
                toggleText.color = isOn ? (color1 ?? Color.white) : (color2 ?? Color.gray);
            });

            RectTransform toggleRect = toggleObj.GetComponent<RectTransform>();
            toggleRect.anchorMin = new Vector2(0, 1);
            toggleRect.anchorMax = new Vector2(0, 1);
            toggleRect.sizeDelta = new Vector2(textWidth + 10, 20);
            toggleRect.pivot = new Vector2(0.5f, 0.5f);
            toggleRect.anchoredPosition = new Vector2(xPosition, -yPosition);

            RectTransform textRect = toggleTextObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            return toggle;
        }
    }
}
