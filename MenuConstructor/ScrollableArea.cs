using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.MenuConstructor
{
    public static class ScrollableArea
    {
        public static GameObject Create(Transform parent, float width, float height)
        {
            GameObject scrollView = new("ScrollView");
            scrollView.transform.SetParent(parent, false);

            RectTransform scrollViewRect = scrollView.AddComponent<RectTransform>();
            scrollViewRect.anchorMin = new Vector2(0.5f, 0.5f);
            scrollViewRect.anchorMax = new Vector2(0.5f, 0.5f);
            scrollViewRect.pivot = new Vector2(0.5f, 0.5f);
            scrollViewRect.sizeDelta = new Vector2(width, height);
            scrollViewRect.anchoredPosition = new Vector2(0, -50); // Offset for title

            // Add mask
            Image maskImage = scrollView.AddComponent<Image>();
            maskImage.color = new Color(0, 0, 0, 0.1f);
            Mask mask = scrollView.AddComponent<Mask>();
            mask.showMaskGraphic = true;

            // Create viewport
            GameObject viewport = new("Viewport");
            viewport.transform.SetParent(scrollView.transform, false);
            RectTransform viewportRect = viewport.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            viewportRect.pivot = new Vector2(0, 1);

            // Create content
            GameObject content = new("Content");
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.anchoredPosition = Vector2.zero;

            // Add ScrollRect component
            ScrollRect scrollRect = scrollView.AddComponent<ScrollRect>();
            scrollRect.content = contentRect;
            scrollRect.viewport = viewportRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.scrollSensitivity = 5;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
            scrollRect.elasticity = 0.1f;
            scrollRect.inertia = true;
            scrollRect.decelerationRate = 0.135f;

            return scrollView;
        }
    }
}
