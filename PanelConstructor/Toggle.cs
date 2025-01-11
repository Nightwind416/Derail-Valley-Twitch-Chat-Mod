using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace TwitchChat.PanelConstructor
{
    public static class Toggle
    {
        public static UnityEngine.UI.Toggle Create(Transform parent, int xPosition, int yPosition, string textOn, string textOff, bool initialState = true, Color? color1 = null, Color? color2 = null, UnityAction<bool>? onValueChanged = null)
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

            if (VRManager.IsVREnabled())
            {
                BoxCollider boxCollider = toggleObj.AddComponent<BoxCollider>();
                boxCollider.size = new Vector3(textWidth + 10, 20, 1);
                
                WorldUiButtonVr vrButton = toggleObj.AddComponent<WorldUiButtonVr>();
                vrButton.SetAction(() => {
                    toggle.isOn = !toggle.isOn;
                    toggle.onValueChanged?.Invoke(toggle.isOn);
                });

                // Add tooltip
                GameObject tooltipObj = new GameObject("Tooltip");
                tooltipObj.transform.SetParent(toggleObj.transform, false);
                TMPro.TextMeshPro tooltipText = tooltipObj.AddComponent<TMPro.TextMeshPro>();
                tooltipText.text = "Click to toggle";
                tooltipText.fontSize = 12;
                tooltipText.alignment = TMPro.TextAlignmentOptions.Center;
                vrButton.tooltipLabel = tooltipText;
                
                // Add hover handlers
                vrButton.Touched += () => {
                    toggleImage.color = new Color(0.3f, 0.3f, 0.3f, 0.75f);
                };
                vrButton.Untouched += () => {
                    toggleImage.color = new Color(0, 0, 0, 0.75f);
                };
                
                Main.LogEntry("ToggleCreation", $"VR Toggle '{textOn}/{textOff}' created for parent '{parent.name}'");
            }

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

            if (onValueChanged != null)
            {
                toggle.onValueChanged.AddListener(onValueChanged);
            }

            // update text and color when toggle changes
            toggle.onValueChanged.AddListener((isOn) => {
                toggleText.text = isOn ? textOn : textOff;
                toggleText.color = isOn ? (color1 ?? Color.white) : (color2 ?? Color.gray);
            });

            Main.LogEntry("ToggleCreation", $"Toggle '{textOn}/{textOff}' created for '{parent.name}'");

            return toggle;
        }
    }
}
