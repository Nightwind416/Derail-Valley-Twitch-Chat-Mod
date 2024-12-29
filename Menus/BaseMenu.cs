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
            panelImage.color = new Color(0, 0, 0, 0.3f);
            
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
            tempText.fontSize = 12;
            tempText.cachedTextGenerator.Populate(text, tempText.GetGenerationSettings(Vector2.zero));
            float textWidth = tempText.cachedTextGenerator.GetPreferredWidth(text, tempText.GetGenerationSettings(Vector2.zero));
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
            buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
            buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRect.sizeDelta = new Vector2(textWidth + 10, 20);
            buttonRect.anchoredPosition = new Vector2(x, y);
            
            RectTransform textRect = buttonTextObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            return button;
        }

        protected void CreateTitle(string titleText, int fontSize = 16, Color? textColor = null, TextAnchor alignment = TextAnchor.UpperCenter)
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
            titleRect.anchorMin = new Vector2(0, 0.85f);
            titleRect.anchorMax = new Vector2(1, 1f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
        }
        protected GameObject CreateSection(string name, int yFromTop, int heightInPixels)
        {
            GameObject section = new GameObject(name);
            section.transform.SetParent(menuObject.transform, false);
            
            Image sectionImage = section.AddComponent<Image>();
            sectionImage.color = new Color(0, 0, 0, 0.5f);
            
            RectTransform rect = section.GetComponent<RectTransform>();
            // rect.anchorMin = Vector2.zero;
            // rect.anchorMax = Vector2.one;
            // rect.offsetMin = new Vector2(20, -yFromTop);
            // rect.offsetMax = new Vector2(-20, -(yFromTop + heightInPixels));

            float parentHeight = menuObject.GetComponent<RectTransform>().rect.height;
            if (parentHeight <= 0) parentHeight = 200f; // Fallback value if parent height is invalid
            
            float topAnchor = 1f - (yFromTop / parentHeight);
            float bottomAnchor = 1f - ((yFromTop + heightInPixels) / parentHeight);
            
            rect.anchorMin = new Vector2(0, bottomAnchor);
            rect.anchorMax = new Vector2(1, topAnchor);
            rect.offsetMin = new Vector2(20, 0);
            rect.offsetMax = new Vector2(-20, 0);
            
            CreateLabel(section.transform, name, 0, 0, Color.gray);
            
            return section;
        }

        protected Text CreateLabel(Transform parent, string text, int x, int y, Color? textColor = null)
        {
            GameObject labelObj = new GameObject($"{text}Label");
            labelObj.transform.SetParent(parent, false);
            
            // Create temporary text to measure width
            GameObject tempTextObj = new GameObject("TempText");
            Text tempText = tempTextObj.AddComponent<Text>();
            tempText.text = text;
            tempText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            tempText.fontSize = 14;
            tempText.cachedTextGenerator.Populate(text, tempText.GetGenerationSettings(Vector2.zero));
            float textWidth = tempText.cachedTextGenerator.GetPreferredWidth(text, tempText.GetGenerationSettings(Vector2.zero));
            GameObject.Destroy(tempTextObj);
            
            Text label = labelObj.AddComponent<Text>();
            label.text = text;
            label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            label.fontSize = 14;
            label.alignment = TextAnchor.MiddleLeft;
            label.color = textColor ?? Color.white;
            
            RectTransform rect = labelObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.sizeDelta = new Vector2(textWidth + 10, 20);
            rect.anchoredPosition = new Vector2(x, y);
            
            return label;
        }

        protected Text CreateStatusIndicator(Transform parent, string text, float yPosition, float xOffset = 135f)
        {
            GameObject statusObj = new GameObject($"{text}Status");
            statusObj.transform.SetParent(parent, false);
            
            Text status = statusObj.AddComponent<Text>();
            status.text = text;
            status.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            status.fontSize = 12;
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
