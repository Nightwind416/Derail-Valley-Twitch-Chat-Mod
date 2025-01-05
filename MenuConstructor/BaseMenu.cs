using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.MenuConstructor
{
    public abstract class BaseMenu
    {
        protected GameObject menuObject;
        public GameObject MenuObject => menuObject;
        protected RectTransform rectTransform;
        protected GameObject textInputField;
        protected GameObject scrollableArea;
        protected RectTransform contentRectTransform;
        protected ScrollRect scrollRect;

        // Add fields for minimize functionality
        protected bool isMinimized = false;
        protected Vector2 originalSize;
        protected UnityEngine.UI.Button minimizeButton;

        // Add delegate and event for back button
        public delegate void OnBackButtonClickedHandler();
        public event OnBackButtonClickedHandler OnBackButtonClicked;

        protected bool showBackButton = true;
        protected bool showMinimizeButton = true;

        public virtual void Show() => menuObject.SetActive(true);
        public virtual void Hide() => menuObject.SetActive(false);

        protected BaseMenu(Transform parent)
        {
            CreateBaseMenu(parent);
            // Get references from the scrollable area when created
            if (scrollableArea != null)
            {
                scrollRect = scrollableArea.GetComponent<ScrollRect>();
                if (scrollRect != null)
                {
                    contentRectTransform = scrollRect.content;
                }
            }
        }

        protected virtual void CreateBaseMenu(Transform parent)
        {
            menuObject = new GameObject(GetType().Name);
            menuObject.transform.SetParent(parent, false);
            
            Image panelImage = menuObject.AddComponent<Image>();
            panelImage.color = new Color(0, 0, 0, 0.3f);
            
            rectTransform = menuObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 0);
            rectTransform.anchorMax = new Vector2(1, 1);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.pivot = new Vector2(0, 1);
            rectTransform.anchoredPosition = Vector2.zero;

            // Create title using Title factory
            Title.Create(menuObject.transform, GetType().Name.Replace("Menu", ""), 18);

            // Create back button
            UnityEngine.UI.Button backButton = Button.Create(menuObject.transform, " < ", 0, 0, Color.white, () => OnBackButtonClicked?.Invoke());
            RectTransform backRect = backButton.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(1, 1);
            backRect.anchorMax = new Vector2(1, 1);
            backRect.pivot = new Vector2(1, 1);

            // Create minimize button
            minimizeButton = Button.Create(menuObject.transform, " − ", 0, 0, Color.white, OnMinimizeClick);
            RectTransform minimizeRect = minimizeButton.GetComponent<RectTransform>();
            minimizeRect.anchorMin = new Vector2(0, 1);
            minimizeRect.anchorMax = new Vector2(0, 1);
            minimizeRect.pivot = new Vector2(0, 1);
        }

        protected virtual void OnMinimizeClick()
        {
            if (!isMinimized)
            {
                // Store original size and anchors before minimizing
                originalSize = rectTransform.sizeDelta;

                // Collapse the menu, but keep title and buttons visible
                foreach (Transform child in menuObject.transform)
                {
                    // Skip the minimize button, title, and back button
                    if (child.gameObject != minimizeButton.gameObject && 
                        !child.name.Equals("Title") && 
                        !child.name.Equals(" < Button"))
                    {
                        child.gameObject.SetActive(false);
                    }
                }
                
                // Set the height to exactly 30 pixels from the top
                rectTransform.anchorMin = new Vector2(0, 1);
                rectTransform.anchorMax = new Vector2(1, 1);
                rectTransform.offsetMin = new Vector2(rectTransform.offsetMin.x, -30);
                rectTransform.offsetMax = new Vector2(rectTransform.offsetMax.x, 0);
                
                minimizeButton.GetComponentInChildren<Text>().text = " + ";
            }
            else
            {
                foreach (Transform child in menuObject.transform)
                {
                    child.gameObject.SetActive(true);
                }
                
                // Restore original anchors and position
                rectTransform.anchorMin = new Vector2(0, 0);
                rectTransform.anchorMax = new Vector2(1, 1);
                rectTransform.offsetMin = Vector2.zero;
                rectTransform.offsetMax = Vector2.zero;
                
                minimizeButton.GetComponentInChildren<Text>().text = " − ";
            }
            
            isMinimized = !isMinimized;
        }
    }
}
