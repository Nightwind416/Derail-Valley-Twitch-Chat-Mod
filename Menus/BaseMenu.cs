using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections.Generic;

namespace TwitchChat.Menus
{
    public abstract class BaseMenu
    {
        protected GameObject menuObject;
        protected RectTransform rectTransform;
        protected GameObject textInputField;

        public GameObject MenuObject => menuObject;

        protected BaseMenu(Transform parent)
        {
            CreateBaseMenu(parent);
        }

        protected virtual void CreateBaseMenu(Transform parent)
        {
            menuObject = new GameObject(GetType().Name);
            menuObject.transform.SetParent(parent, false);
            
            Image panelImage = menuObject.AddComponent<Image>();
            panelImage.color = new Color(0, 0, 0, 0.3f);
            
            rectTransform = menuObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 0);
            rectTransform.anchorMax = new Vector2(1, 1);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.pivot = new Vector2(0, 1);
            rectTransform.anchoredPosition = Vector2.zero;
        }
        protected void CreateTitle(string titleText, int fontSize = 16, Color? textColor = null, TextAnchor textAlignment = TextAnchor.UpperCenter)
        {
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(menuObject.transform, false);
            Text title = titleObj.AddComponent<Text>();
            title.text = titleText;
            title.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            title.fontSize = fontSize;
            title.alignment = textAlignment;
            title.color = textColor ?? Color.white;
            
            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.offsetMin = new Vector2(0, -40);
            titleRect.offsetMax = Vector2.zero;
        }

        protected GameObject CreateSection(string name, int yPosition, int height, bool createLabel = true)
        {
            GameObject section = new GameObject(name);
            section.transform.SetParent(menuObject.transform, false);
            
            Image sectionImage = section.AddComponent<Image>();
            sectionImage.color = new Color(0, 0, 0, 0.5f);
            
            RectTransform rect = section.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0, 1);
            rect.sizeDelta = new Vector2(-20, height); // Changed from 0 to -20 to account for 10 units on each side
            rect.anchoredPosition = new Vector2(10, -yPosition);
            rect.offsetMin = new Vector2(10, rect.offsetMin.y);
            rect.offsetMax = new Vector2(-10, rect.offsetMax.y);
            
            if (createLabel)
            {
            CreateLabel(section.transform, name, 5, 5, Color.gray);
            }
            
            return section;
        }
        protected Text CreateLabel(Transform parent, string text, int xPosition, int yPosition, Color? textColor = null)
        {
            GameObject labelObj = new GameObject($"{text}Label");
            labelObj.transform.SetParent(parent, false);
            
            // Create temporary text to measure width
            GameObject tempTextObj = new GameObject("TempText");
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
        protected Button CreateButton(Transform parent, string text, int xPosition, int yPosition, Color? textColor = null, UnityAction onClick = null, int? width = null)
        {
            GameObject buttonObj = new GameObject($"{text}Button");
            buttonObj.transform.SetParent(parent, false);
            
            // Create temporary text to measure width
            GameObject tempTextObj = new GameObject("TempText");
            Text tempText = tempTextObj.AddComponent<Text>();
            tempText.text = text;
            tempText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            tempText.fontSize = 12;
            tempText.cachedTextGenerator.Populate(text, tempText.GetGenerationSettings(Vector2.zero));
            float textWidth = width ?? tempText.cachedTextGenerator.GetPreferredWidth(text, tempText.GetGenerationSettings(Vector2.zero));
            GameObject.Destroy(tempTextObj);
            
            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = new Color(0, 0, 0, 0.75f);
            
            Button button = buttonObj.AddComponent<Button>();
            if (onClick != null)
            {
                button.onClick.AddListener(onClick);
            }
            
            GameObject buttonTextObj = new GameObject("Text");
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

            return button;
        }

        protected Toggle CreateToggle(Transform parent, int xPosition, int yPosition, string textOn, string textOff, bool initialState = true, Color? color1 = null, Color? color2 = null)
        {
            GameObject toggleObj = new GameObject($"{textOn}/{textOff}Toggle");
            toggleObj.transform.SetParent(parent, false);
            
            // Create temporary text to measure width for both strings
            GameObject tempTextObj = new GameObject("TempText");
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
            
            Toggle toggle = toggleObj.AddComponent<Toggle>();
            toggle.isOn = initialState;
            
            GameObject toggleTextObj = new GameObject("Text");
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

        protected Text CreateTextDisplay(Transform parent, string text, int xPosition, int yPosition, Color? color = null, int lines = 1)
        {
            GameObject messageDisplayObj = new GameObject($"{text}Status");
            messageDisplayObj.transform.SetParent(parent, false);
            
            Text message = messageDisplayObj.AddComponent<Text>();
            message.text = text;
            message.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            message.fontSize = 12;
            message.color = color ?? Color.white;
            message.alignment = TextAnchor.UpperLeft;
            
            RectTransform parentRect = parent.GetComponent<RectTransform>();
            RectTransform rect = messageDisplayObj.GetComponent<RectTransform>();
            rect.pivot = new Vector2(0, 1);

            float width;

            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            width = parentRect.rect.width - xPosition - 5f; // 5 units buffer from right edge
            rect.anchoredPosition = new Vector2(xPosition, -yPosition);

            message.horizontalOverflow = HorizontalWrapMode.Wrap;
            message.verticalOverflow = lines > 1 ? VerticalWrapMode.Overflow : VerticalWrapMode.Truncate;
            
            rect.sizeDelta = new Vector2(width, 20 * lines); // 20 units height per line
            
            return message;
        }

        protected Slider CreateSlider(Transform parent, int xPosition, int yPosition, float minValue = 0f, float maxValue = 1f, float value = 0.5f, UnityAction<float> onValueChanged = null)
        {
            GameObject sliderObj = new GameObject("Slider");
            sliderObj.transform.SetParent(parent, false);
            
            // Background (base layer)
            GameObject background = new GameObject("Background");
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
                GameObject tick = new GameObject($"Tick_{i}");
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
            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderObj.transform, false);
            RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0, 0.5f);
            fillAreaRect.anchorMax = new Vector2(1, 0.5f);
            fillAreaRect.sizeDelta = new Vector2(0, 5);  // 5 pixels tall
            fillAreaRect.anchoredPosition = Vector2.zero;

            // Fill
            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            Image fillImage = fill.AddComponent<Image>();
            fillImage.color = new Color(0.8f, 0.8f, 0.8f, 1f);  // Light gray
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;  // Stretches to fill area size
            fillRect.anchorMax = Vector2.one;   // Stretches to fill area size
            fillRect.sizeDelta = Vector2.zero;
            
            // Handle Slide Area
            GameObject handleSlideArea = new GameObject("Handle Slide Area");
            handleSlideArea.transform.SetParent(sliderObj.transform, false);
            RectTransform handleSlideAreaRect = handleSlideArea.AddComponent<RectTransform>();
            handleSlideAreaRect.anchorMin = new Vector2(0, 0.5f);
            handleSlideAreaRect.anchorMax = new Vector2(1, 0.5f);
            handleSlideAreaRect.sizeDelta = new Vector2(0, 10);  // Taller than the bar for easier interaction
            handleSlideAreaRect.anchoredPosition = Vector2.zero;
            
            // Handle
            GameObject handle = new GameObject("Handle");
            handle.transform.SetParent(handleSlideArea.transform, false);
            Image handleImage = handle.AddComponent<Image>();
            handleImage.color = Color.cyan;
            RectTransform handleRect = handle.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(5, 0);
            handleRect.anchorMin = new Vector2(0.25f, 0.25f);
            handleRect.anchorMax = new Vector2(0.25f, 0.75f);

            // Handle highlight effect
            GameObject handleHighlight = new GameObject("HandleHighlight");
            handleHighlight.transform.SetParent(handle.transform, false);
            Image highlightImage = handleHighlight.AddComponent<Image>();
            highlightImage.color = new Color(1f, 1f, 1f, 0.2f);
            RectTransform highlightRect = handleHighlight.GetComponent<RectTransform>();
            highlightRect.anchorMin = Vector2.zero;
            highlightRect.anchorMax = Vector2.one;
            highlightRect.sizeDelta = new Vector2(2, 2);  // Slightly larger than handle
            
            // Min Label
            GameObject minLabel = new GameObject("MinLabel");
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
            GameObject maxLabel = new GameObject("MaxLabel");
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
            GameObject valueLabel = new GameObject("ValueLabel");
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
            valueRect.anchoredPosition = new Vector2(0, 3);  // Added this line to shift up 3 pixels

            // Calculate initial normalized value position
            float normalizedValue = (value - minValue) / (maxValue - minValue);
            valueRect.anchorMin = new Vector2(normalizedValue, 1);
            valueRect.anchorMax = new Vector2(normalizedValue, 1);

            // Setup slider component
            Slider slider = sliderObj.AddComponent<Slider>();
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;
            slider.direction = Slider.Direction.LeftToRight;
            
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
                if (onValueChanged != null)
                    onValueChanged.Invoke(newValue);
            });

            return slider;
        }

        protected enum LineStyle
        {
            Solid,
            Dotted,
            Dashed
        }

        protected GameObject CreateHorizontalBar(Transform parent, int yPosition, int height = 5, Color? color = null, LineStyle style = LineStyle.Solid)
        {
            GameObject barObj = new GameObject("HorizontalBar");
            barObj.transform.SetParent(parent, false);

            Image barImage = barObj.AddComponent<Image>();
            barImage.color = color ?? Color.black;

            // Create a texture based on the line style
            Texture2D texture = new Texture2D(style == LineStyle.Solid ? 1 : 8, 1);
            texture.filterMode = FilterMode.Point;
            
            Color[] pixels = new Color[texture.width];
            switch (style)
            {
                case LineStyle.Solid:
                    pixels[0] = Color.white;
                    break;
                case LineStyle.Dotted:
                    for (int i = 0; i < pixels.Length; i++)
                    {
                        pixels[i] = i % 2 == 0 ? Color.white : Color.clear;
                    }
                    break;
                case LineStyle.Dashed:
                    for (int i = 0; i < pixels.Length; i++)
                    {
                        pixels[i] = i < pixels.Length / 2 ? Color.white : Color.clear;
                    }
                    break;
            }
            texture.SetPixels(pixels);
            texture.Apply();

            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            barImage.sprite = sprite;
            barImage.type = Image.Type.Tiled;

            RectTransform rect = barObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.offsetMin = new Vector2(5, rect.offsetMin.y);
            rect.offsetMax = new Vector2(-5, rect.offsetMax.y);
            rect.sizeDelta = new Vector2(0, height);
            rect.anchoredPosition = new Vector2(0, -yPosition);

            return barObj;
        }

        public virtual void Show() => menuObject.SetActive(true);
        public virtual void Hide() => menuObject.SetActive(false);
    }
}
