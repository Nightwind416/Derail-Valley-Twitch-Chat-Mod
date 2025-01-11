using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace TwitchChat.PanelConstructor
{
    public static class Button
    {
        public static UnityEngine.UI.Button Create(Transform parent, string text, int xPosition, int yPosition, Color? textColor = null, UnityAction? clicked = null, int? width = null)
        {
            // Create button
            GameObject buttonObj = new($"{text}Button");
            buttonObj.transform.SetParent(parent, false);

            // Create temporary text to measure width
            GameObject tempTextObj = new("TempText");
            Text tempText = tempTextObj.AddComponent<Text>();
            tempText.text = text;
            tempText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            tempText.fontSize = 12;
            tempText.cachedTextGenerator.Populate(text, tempText.GetGenerationSettings(Vector2.zero));
            float textWidth = width ?? tempText.cachedTextGenerator.GetPreferredWidth(text, tempText.GetGenerationSettings(Vector2.zero));
            GameObject.Destroy(tempTextObj);

            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = new Color(0, 0, 0, 0.75f);

            if (VRManager.IsVREnabled())
            {
                BoxCollider boxCollider = buttonObj.AddComponent<BoxCollider>();
                boxCollider.size = new Vector3(textWidth + 10, 20, 1);
                
                WorldUiButtonVr vrButton = buttonObj.AddComponent<WorldUiButtonVr>();
                vrButton.SetAction(() => clicked?.Invoke());
                
                // Add hover handlers
                vrButton.Touched += () => {
                    buttonImage.color = new Color(0.3f, 0.3f, 0.3f, 0.75f);
                };
                vrButton.Untouched += () => {
                    buttonImage.color = new Color(0, 0, 0, 0.75f);
                };
                
                Main.LogEntry("ButtonCreation", $"VR Button '{text}' created for parent '{parent.name}'");
            }

            UnityEngine.UI.Button button = buttonObj.AddComponent<UnityEngine.UI.Button>();
            if (clicked != null)
            {
                button.onClick.AddListener(clicked);
            }

            GameObject buttonTextObj = new("Text");
            buttonTextObj.transform.SetParent(buttonObj.transform, false);
            Text buttonText = buttonTextObj.AddComponent<Text>();
            buttonText.text = text;
            buttonText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            buttonText.fontSize = 12;
            buttonText.alignment = TextAnchor.MiddleCenter;
            buttonText.color = textColor ?? Color.white;

            RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0, 1);
            buttonRect.anchorMax = new Vector2(0, 1);
            buttonRect.sizeDelta = new Vector2(textWidth + 10, 20);
            buttonRect.pivot = new Vector2(0.5f, 0.5f);
            buttonRect.anchoredPosition = new Vector2(xPosition, -yPosition);

            RectTransform textRect = buttonTextObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            Main.LogEntry("ButtonCreation", $"Button '{text}' created for '{parent.name}'");

            return button;
        }
    }
}
