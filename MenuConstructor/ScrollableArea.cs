using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.MenuConstructor
{
    public static class ScrollableArea
    {
        public static GameObject Create(Transform parent, int width = 180, int height = 250)
        {
            GameObject scrollableArea = new("ScrollableArea");
            scrollableArea.transform.SetParent(parent, false);

            // Add visible background to scrollable area
            Image scrollAreaImage = scrollableArea.AddComponent<Image>();
            scrollAreaImage.color = new Color(0, 0, 0, 0.3f); // Semi-transparent black

            ScrollRect scrollRect = scrollableArea.AddComponent<ScrollRect>();
            scrollRect.vertical = true;
            scrollRect.horizontal = false;

            // Set up viewport
            GameObject viewport = new("Viewport");
            viewport.transform.SetParent(scrollableArea.transform, false);
            RectTransform viewportRect = viewport.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            viewportRect.anchoredPosition = Vector2.zero;

            // Add mask to viewport
            Image viewportImage = viewport.AddComponent<Image>();
            viewportImage.color = new Color(0, 0, 0, 0.1f);
            viewport.AddComponent<Mask>().showMaskGraphic = true;

            // Set up content
            GameObject content = new("Content");
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRectTransform = content.AddComponent<RectTransform>();
            contentRectTransform.anchorMin = new Vector2(0, 1);
            contentRectTransform.anchorMax = new Vector2(1, 1);
            contentRectTransform.pivot = new Vector2(0.5f, 1);
            contentRectTransform.sizeDelta = new Vector2(0, 0);

            // Configure scrollable area size and position
            RectTransform scrollAreaRect = scrollableArea.GetComponent<RectTransform>();
            scrollAreaRect.anchorMin = new Vector2(0, 1);
            scrollAreaRect.anchorMax = new Vector2(1, 1);
            scrollAreaRect.pivot = new Vector2(0.5f, 1);
            scrollAreaRect.sizeDelta = new Vector2(width, height);
            scrollAreaRect.anchoredPosition = new Vector2(0, -25);
            scrollAreaRect.offsetMin = new Vector2(10, scrollAreaRect.offsetMin.y);
            scrollAreaRect.offsetMax = new Vector2(-10, scrollAreaRect.offsetMax.y);

            scrollRect.content = contentRectTransform;
            scrollRect.viewport = viewportRect;

            return scrollableArea;
        }
    }
}
