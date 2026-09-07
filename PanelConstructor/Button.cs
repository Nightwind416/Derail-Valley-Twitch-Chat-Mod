using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace TwitchChat.PanelConstructor
{
    /// <summary>
    /// Factory class for creating UI buttons with consistent styling and behavior.
    /// Supports both standard and VR button implementations.
    /// </summary>
    public static class Button
    {
        /// <summary>
        /// Creates a new button with specified text and positioning.
        /// </summary>
        /// <param name="parent">Parent transform to attach the button to</param>
        /// <param name="text">Button label text</param>
        /// <param name="xPosition">X position relative to parent</param>
        /// <param name="yPosition">Y position relative to parent</param>
        /// <param name="textColor">Optional text color</param>
        /// <param name="clicked">Optional click event handler</param>
        /// <param name="width">Optional fixed width for the button</param>
        /// <returns>Created button component</returns>
        public static UnityEngine.UI.Button Create(Transform parent, string text, int xPosition, int yPosition, Color? textColor = null, UnityAction? clicked = null, int? width = null)
        {
            // Create button
            GameObject buttonObj = new($"{text}Button");
            buttonObj.transform.SetParent(parent, false);

            // Create temporary text to measure width
            GameObject tempTextObj = new("TempText");
            Text tempText = tempTextObj.AddComponent<Text>();
            tempText.text = text;
            tempText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            tempText.fontSize = 12;
            tempText.cachedTextGenerator.Populate(text, tempText.GetGenerationSettings(Vector2.zero));
            float textWidth = width ?? tempText.cachedTextGenerator.GetPreferredWidth(text, tempText.GetGenerationSettings(Vector2.zero));
            GameObject.Destroy(tempTextObj);

            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = PanelTheme.ButtonColorFor(parent);

            if (VRManager.IsVREnabled())
            {
                BoxCollider boxCollider = buttonObj.AddComponent<BoxCollider>();
                boxCollider.size = new Vector3(textWidth + 10, 20, 1);
                // A trigger, so the collider stays out of the physics of whatever the panel is parented to
                boxCollider.isTrigger = true;
                
                WorldUiButtonVr vrButton = buttonObj.AddComponent<WorldUiButtonVr>();
                vrButton.SetAction(() => clicked?.Invoke());
                
                // Hover handlers. Both work out the colour when they fire rather than remembering one from
                // now, so a button touched after the player has changed the colours goes back to the colour
                // they chose instead of to the one it happened to be built with
                vrButton.Touched += () => {
                    buttonImage.color = PanelTheme.Highlight(PanelTheme.ButtonColorFor(buttonObj.transform));
                };
                vrButton.Untouched += () => {
                    buttonImage.color = PanelTheme.ButtonColorFor(buttonObj.transform);
                };

                Main.LogEntry("ButtonCreation", $"VR Button '{text}' created for parent '{parent.name}'");
            }

            UnityEngine.UI.Button button = buttonObj.AddComponent<UnityEngine.UI.Button>();
            if (clicked != null)
            {
                button.onClick.AddListener(clicked);
            }

            GameObject buttonTextObj = new("Text");
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

            Main.LogEntry("ButtonCreation", $"Button '{text}' created for '{parent.name}'");

            return button;
        }
    }
}
