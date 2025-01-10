using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using VRTK;

namespace TwitchChat.PanelConstructor
{
    public static class Slider
    {
        public static UnityEngine.UI.Slider Create(Transform parent, int xPosition, int yPosition, float minValue = 0f, float maxValue = 1f, float value = 0.5f, UnityAction<float>? onValueChanged = null)
        {
            GameObject sliderObj = new("Slider");
            sliderObj.transform.SetParent(parent, false);

            // Background (base layer)
            GameObject background = new("Background");
            background.transform.SetParent(sliderObj.transform, false);
            Image bgImage = background.AddComponent<Image>();
            bgImage.color = new Color(0.15f, 0.15f, 0.15f, 1f);  // Darker gray for better contrast
            RectTransform bgRect = background.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0, 0.5f);
            bgRect.anchorMax = new Vector2(1, 0.5f);
            bgRect.sizeDelta = new Vector2(0, 6);  // Slightly taller
            bgRect.anchoredPosition = Vector2.zero;

            // Add tick marks
            int tickCount = Mathf.Min(10, Mathf.RoundToInt(maxValue - minValue));
            for (int i = 0; i <= tickCount; i++)
            {
                GameObject tick = new($"Tick_{i}");
                tick.transform.SetParent(background.transform, false);
                Image tickImage = tick.AddComponent<Image>();
                tickImage.color = new Color(0.4f, 0.4f, 0.4f, 0.8f);

                RectTransform tickRect = tick.GetComponent<RectTransform>();
                tickRect.anchorMin = new Vector2((float)i / tickCount, 0.5f);
                tickRect.anchorMax = new Vector2((float)i / tickCount, 0.5f);
                tickRect.sizeDelta = new Vector2(1, 10);
                tickRect.anchoredPosition = Vector2.zero;
            }

            // Fill Area
            GameObject fillArea = new("Fill Area");
            fillArea.transform.SetParent(sliderObj.transform, false);
            RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0, 0.5f);
            fillAreaRect.anchorMax = new Vector2(1, 0.5f);
            fillAreaRect.sizeDelta = new Vector2(0, 5);  // 5 pixels tall
            fillAreaRect.anchoredPosition = Vector2.zero;

            // Fill
            GameObject fill = new("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            Image fillImage = fill.AddComponent<Image>();
            fillImage.color = new Color(0.8f, 0.8f, 0.8f, 1f);  // Light gray
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;  // Stretches to fill area size
            fillRect.anchorMax = Vector2.one;   // Stretches to fill area size
            fillRect.sizeDelta = Vector2.zero;

            // Handle Slide Area
            GameObject handleSlideArea = new("Handle Slide Area");
            handleSlideArea.transform.SetParent(sliderObj.transform, false);
            RectTransform handleSlideAreaRect = handleSlideArea.AddComponent<RectTransform>();
            handleSlideAreaRect.anchorMin = new Vector2(0, 0.5f);
            handleSlideAreaRect.anchorMax = new Vector2(1, 0.5f);
            handleSlideAreaRect.sizeDelta = new Vector2(0, 10);  // Taller than the bar for easier interaction
            handleSlideAreaRect.anchoredPosition = Vector2.zero;

            // Handle
            GameObject handle = new("Handle");
            handle.transform.SetParent(handleSlideArea.transform, false);
            Image handleImage = handle.AddComponent<Image>();
            handleImage.color = Color.cyan;
            RectTransform handleRect = handle.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(5, 0);
            handleRect.anchorMin = new Vector2(0.25f, 0.25f);
            handleRect.anchorMax = new Vector2(0.25f, 0.75f);

            // Handle highlight effect
            GameObject handleHighlight = new("HandleHighlight");
            handleHighlight.transform.SetParent(handle.transform, false);
            Image highlightImage = handleHighlight.AddComponent<Image>();
            highlightImage.color = new Color(1f, 1f, 1f, 0.2f);
            RectTransform highlightRect = handleHighlight.GetComponent<RectTransform>();
            highlightRect.anchorMin = Vector2.zero;
            highlightRect.anchorMax = Vector2.one;
            highlightRect.sizeDelta = new Vector2(2, 2);  // Slightly larger than handle

            // Min Label
            GameObject minLabel = new("MinLabel");
            minLabel.transform.SetParent(sliderObj.transform, false);
            Text minText = minLabel.AddComponent<Text>();
            minText.text = minValue.ToString("0.##");
            minText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            minText.fontSize = 10;
            minText.alignment = TextAnchor.MiddleLeft;
            minText.color = Color.white;
            RectTransform minRect = minLabel.GetComponent<RectTransform>();
            minRect.anchorMin = new Vector2(0, 0.5f);
            minRect.anchorMax = new Vector2(0, 0.5f);
            minRect.pivot = new Vector2(1, 0.5f);
            minRect.sizeDelta = new Vector2(40, 20);
            minRect.anchoredPosition = new Vector2(-30, 0);

            // Max Label
            GameObject maxLabel = new("MaxLabel");
            maxLabel.transform.SetParent(sliderObj.transform, false);
            Text maxText = maxLabel.AddComponent<Text>();
            maxText.text = maxValue.ToString("0.##");
            maxText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            maxText.fontSize = 10;
            maxText.alignment = TextAnchor.MiddleLeft;
            maxText.color = Color.white;
            RectTransform maxRect = maxLabel.GetComponent<RectTransform>();
            maxRect.anchorMin = new Vector2(1, 0.5f);
            maxRect.anchorMax = new Vector2(1, 0.5f);
            maxRect.pivot = new Vector2(0, 0.5f);
            maxRect.sizeDelta = new Vector2(40, 20);
            maxRect.anchoredPosition = new Vector2(5, 0);

            // Value Label
            GameObject valueLabel = new("ValueLabel");
            valueLabel.transform.SetParent(handleSlideArea.transform, false);
            Text valueText = valueLabel.AddComponent<Text>();
            valueText.text = ((int)value).ToString();
            valueText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            valueText.fontSize = 10;
            valueText.alignment = TextAnchor.LowerCenter;
            valueText.color = Color.cyan;
            RectTransform valueRect = valueLabel.GetComponent<RectTransform>();
            valueRect.anchorMin = new Vector2(0, 1);
            valueRect.anchorMax = new Vector2(0, 1);
            valueRect.pivot = new Vector2(0.5f, 0);
            valueRect.sizeDelta = new Vector2(30, 15);
            valueRect.anchoredPosition = new Vector2(0, 3);

            // Calculate initial normalized value position
            float normalizedValue = (value - minValue) / (maxValue - minValue);
            valueRect.anchorMin = new Vector2(normalizedValue, 1);
            valueRect.anchorMax = new Vector2(normalizedValue, 1);

            // Setup slider component
            UnityEngine.UI.Slider slider = sliderObj.AddComponent<UnityEngine.UI.Slider>();
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;
            slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;

            slider.minValue = minValue;
            slider.maxValue = maxValue;
            slider.value = value;

            float width;
            RectTransform parentRect = parent.GetComponent<RectTransform>();
            width = parentRect.rect.width - 70;

            // Setup RectTransform
            RectTransform sliderRect = sliderObj.GetComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0.5f, 1);
            sliderRect.anchorMax = new Vector2(0.5f, 1);
            sliderRect.pivot = new Vector2(0.5f, 1);
            sliderRect.sizeDelta = new Vector2(width, 20);
            sliderRect.anchoredPosition = new Vector2(xPosition, -yPosition);

            if (onValueChanged != null)
            {
                slider.onValueChanged.AddListener(onValueChanged);
            }

            // Update value label position and text when slider changes
            slider.onValueChanged.AddListener((newValue) => {
                valueText.text = ((int)newValue).ToString();
                float normalizedValue = (newValue - slider.minValue) / (slider.maxValue - slider.minValue);
                valueRect.anchorMin = new Vector2(normalizedValue, 1);
                valueRect.anchorMax = new Vector2(normalizedValue, 1);
                onValueChanged?.Invoke(newValue);
            });

            // Only add VRTK components if in VR mode
            if (VRManager.IsVREnabled())
            {
                sliderObj.AddComponent<VRTK_BasePointerRenderer>();
            }

            return slider;
        }
    }
}
