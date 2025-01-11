using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.PanelConstructor
{
    /// <summary>
    /// Factory class for creating panel sections with consistent styling.
    /// </summary>
    public static class Section
    {
        /// <summary>
        /// Creates a new section container with optional label.
        /// </summary>
        /// <param name="parent">Parent transform to attach the section to</param>
        /// <param name="name">Name of the section, used for label if enabled</param>
        /// <param name="yPosition">Y position relative to parent</param>
        /// <param name="height">Height of the section</param>
        /// <param name="createLabel">Whether to create a label for the section</param>
        /// <returns>Created section GameObject</returns>
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
