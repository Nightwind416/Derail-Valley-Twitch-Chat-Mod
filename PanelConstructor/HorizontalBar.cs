using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.PanelConstructor
{
    public static class HorizontalBar
    {
        public static GameObject Create(Transform parent, int yPosition, Color? color = null)
        {
            GameObject horizontalBar = new("HorizontalBar");
            horizontalBar.transform.SetParent(parent, false);

            Image barImage = horizontalBar.AddComponent<Image>();
            barImage.color = color ?? Color.gray;

            RectTransform rect = horizontalBar.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.sizeDelta = new Vector2(0, 1); // Adjust height as needed
            rect.anchoredPosition = new Vector2(0, -yPosition);

            return horizontalBar;
        }
    }
}
