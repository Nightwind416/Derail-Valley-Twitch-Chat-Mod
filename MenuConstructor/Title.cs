using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.MenuConstructor
{
    public static class Title
    {
        public static void Create(Transform parent, string titleText, int fontSize = 16, Color? textColor = null, TextAnchor textAlignment = TextAnchor.UpperCenter)
        {
            GameObject titleObj = new("Title");
            titleObj.transform.SetParent(parent, false);
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
    }
}
