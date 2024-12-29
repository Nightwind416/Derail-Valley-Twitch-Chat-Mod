using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace TwitchChat.Menus
{
    public abstract class BaseMenu
    {
        protected GameObject menuObject;
        protected RectTransform rectTransform;

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
            panelImage.color = new Color(0, 0, 0, 0.8f);
            
            rectTransform = menuObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        protected Button CreateButton(string text, int x, int y, Color? textColor = null, UnityAction onClick = null)
        {
            GameObject buttonObj = new GameObject($"{text}Button");
            buttonObj.transform.SetParent(menuObject.transform, false);
            
            // Create temporary text to measure width
            GameObject tempTextObj = new GameObject("TempText");
            Text tempText = tempTextObj.AddComponent<Text>();
            tempText.text = text;
            tempText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            tempText.fontSize = 18;
            tempText.cachedTextGenerator.Populate(text, tempText.GetGenerationSettings(Vector2.zero));
            float textWidth = tempText.cachedTextGenerator.GetPreferredWidth(text, tempText.GetGenerationSettings(Vector2.zero));
            GameObject.Destroy(tempTextObj);
            
            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            
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
            buttonText.fontSize = 18;
            buttonText.alignment = TextAnchor.MiddleCenter;
            buttonText.color = textColor ?? Color.white;
            
            RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
            buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRect.sizeDelta = new Vector2(textWidth + 20, 30); // 20 pixels padding around text
            buttonRect.anchoredPosition = new Vector2(x, y);
            
            RectTransform textRect = buttonTextObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            return button;
        }

        protected void CreateTitle(string titleText, int fontSize = 18, Color? textColor = null, TextAnchor alignment = TextAnchor.UpperCenter)
        {
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(menuObject.transform, false);
            Text title = titleObj.AddComponent<Text>();
            title.text = titleText;
            title.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            title.fontSize = fontSize;
            title.alignment = alignment;
            title.color = textColor ?? Color.white;
            
            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0.9f);
            titleRect.anchorMax = new Vector2(1, 1f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
        }

        protected Toggle CreateToggle(string label, bool initialState, UnityAction<bool> onValueChanged, Color? toggleColor = null, Color? checkmarkColor = null)
        {
            GameObject toggleObj = new GameObject($"{label}Toggle");
            toggleObj.transform.SetParent(menuObject.transform, false);
            
            Toggle toggle = toggleObj.AddComponent<Toggle>();
            toggle.isOn = initialState;
            toggle.onValueChanged.AddListener(onValueChanged);

            // Add background image for toggle
            Image toggleImage = toggleObj.AddComponent<Image>();
            toggleImage.color = toggleColor ?? new Color(0.2f, 0.2f, 0.2f);

            // Add checkmark
            GameObject checkmark = new GameObject("Checkmark");
            checkmark.transform.SetParent(toggleObj.transform, false);
            Image checkmarkImage = checkmark.AddComponent<Image>();
            checkmarkImage.color = checkmarkColor ?? Color.white;
            RectTransform checkmarkRect = checkmark.GetComponent<RectTransform>();
            checkmarkRect.anchorMin = Vector2.zero;
            checkmarkRect.anchorMax = Vector2.one;
            checkmarkRect.offsetMin = new Vector2(2, 2);
            checkmarkRect.offsetMax = new Vector2(-2, -2);
            toggle.graphic = checkmarkImage;
            
            return toggle;
        }

        protected InputField CreateInputField(string name, string initialValue, UnityAction<string> onValueChanged, int fontSize = 16, Color? textColor = null, TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            GameObject inputObj = new GameObject(name);
            inputObj.transform.SetParent(menuObject.transform, false);
            
            // Create background image
            Image backgroundImage = inputObj.AddComponent<Image>();
            backgroundImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            // Create child text area
            GameObject textArea = new GameObject("Text");
            textArea.transform.SetParent(inputObj.transform, false);
            Text inputText = textArea.AddComponent<Text>();
            inputText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            inputText.fontSize = fontSize;
            inputText.color = textColor ?? Color.white;
            inputText.alignment = alignment;
            
            // Set up the RectTransform for the text area
            RectTransform textRect = textArea.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0, 0);
            textRect.anchorMax = new Vector2(1, 1);
            textRect.offsetMin = new Vector2(5, 2);
            textRect.offsetMax = new Vector2(-5, -2);

            // Set up the InputField
            InputField input = inputObj.AddComponent<InputField>();
            input.textComponent = inputText;
            input.text = initialValue;
            input.onValueChanged.AddListener(onValueChanged);

            return input;
        }

        protected Text CreateStatusText()
        {
            GameObject textObj = new GameObject("StatusText");
            textObj.transform.SetParent(menuObject.transform, false);
            Text text = textObj.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 16;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            
            RectTransform rect = textObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.1f, 0.85f);
            rect.anchorMax = new Vector2(0.9f, 0.9f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            
            return text;
        }

        protected Text CreateLastMessageText()
        {
            GameObject textObj = new GameObject("LastMessageText");
            textObj.transform.SetParent(menuObject.transform, false);
            Text text = textObj.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 14;
            text.alignment = TextAnchor.MiddleLeft;
            text.color = Color.white;
            
            RectTransform rect = textObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.1f, 0.8f);
            rect.anchorMax = new Vector2(0.9f, 0.85f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            
            return text;
        }

        protected GameObject CreateSection(string name, float yPosition, float height)
        {
            GameObject section = new GameObject(name);
            section.transform.SetParent(menuObject.transform, false);
            
            Image sectionImage = section.AddComponent<Image>();
            sectionImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            
            RectTransform rect = section.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.1f, yPosition);
            rect.anchorMax = new Vector2(0.9f, yPosition + height);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            
            return section;
        }

        protected Text CreateLabel(Transform parent, string text, float yPosition, float width = 125f, Color? textColor = null)
        {
            GameObject labelObj = new GameObject($"{text}Label");
            labelObj.transform.SetParent(parent, false);
            
            Text label = labelObj.AddComponent<Text>();
            label.text = text;
            label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            label.fontSize = 14;
            label.alignment = TextAnchor.MiddleLeft;
            label.color = textColor ?? Color.white;
            
            RectTransform rect = labelObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, yPosition);
            rect.anchorMax = new Vector2(0, yPosition);
            rect.sizeDelta = new Vector2(width, 20);
            rect.anchoredPosition = new Vector2(10, 0);
            
            return label;
        }

        protected Text CreateStatusIndicator(Transform parent, string text, float yPosition, float xOffset = 135f)
        {
            GameObject statusObj = new GameObject($"{text}Status");
            statusObj.transform.SetParent(parent, false);
            
            Text status = statusObj.AddComponent<Text>();
            status.text = text;
            status.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            status.fontSize = 14;
            status.alignment = TextAnchor.MiddleLeft;
            
            RectTransform rect = statusObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, yPosition);
            rect.anchorMax = new Vector2(1, yPosition);
            rect.sizeDelta = new Vector2(0, 20);
            rect.anchoredPosition = new Vector2(xOffset, 0);
            
            return status;
        }

        public virtual void Show() => menuObject.SetActive(true);
        public virtual void Hide() => menuObject.SetActive(false);
    }
}
