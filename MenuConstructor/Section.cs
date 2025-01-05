using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.MenuConstructor
{
    public static class Section
    {
        public static GameObject Create(Transform parent, string name, int yPosition, int height, bool createLabel = true)
        {
            GameObject section = new(name);
            section.transform.SetParent(parent, false);

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
                Label.Create(section.transform, name, 5, 5, Color.gray);
            }

            return section;
        }
    }
}
