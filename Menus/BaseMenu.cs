using UnityEngine;
using UnityEngine.UI;

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

        protected Button CreateButton(string name, string text, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject buttonObj = new GameObject(name);
            buttonObj.transform.SetParent(menuObject.transform, false);
            
            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            
            Button button = buttonObj.AddComponent<Button>();
            
            GameObject buttonTextObj = new GameObject("Text");
            buttonTextObj.transform.SetParent(buttonObj.transform, false);
            Text buttonText = buttonTextObj.AddComponent<Text>();
            buttonText.text = text;
            buttonText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            buttonText.fontSize = 18;
            buttonText.alignment = TextAnchor.MiddleCenter;
            buttonText.color = Color.white;
            
            RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
            buttonRect.anchorMin = anchorMin;
            buttonRect.anchorMax = anchorMax;
            buttonRect.offsetMin = Vector2.zero;
            buttonRect.offsetMax = Vector2.zero;
            
            RectTransform textRect = buttonTextObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            return button;
        }

        public virtual void Show() => menuObject.SetActive(true);
        public virtual void Hide() => menuObject.SetActive(false);
    }
}
