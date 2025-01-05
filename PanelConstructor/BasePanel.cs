using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.PanelConstructor
{
    public abstract class BasePanel
    {
        protected GameObject panelObject;
        public GameObject PanelObject => panelObject;
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

        public virtual void Show() => panelObject.SetActive(true);
        public virtual void Hide() => panelObject.SetActive(false);

        protected BasePanel(Transform parent)
        {
            CreateBasePanel(parent);
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

        protected virtual void CreateBasePanel(Transform parent)
        {
            panelObject = new GameObject(GetType().Name);
            panelObject.transform.SetParent(parent, false);
            
            Image panelImage = panelObject.AddComponent<Image>();
            panelImage.color = new Color(0, 0, 0, 0.3f);
            
            rectTransform = panelObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 0);
            rectTransform.anchorMax = new Vector2(1, 1);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.pivot = new Vector2(0, 1);
            rectTransform.anchoredPosition = Vector2.zero;

            // Create title using Title factory
            Title.Create(panelObject.transform, GetType().Name.Replace("Panel", ""), 18);

            // Create back button
            UnityEngine.UI.Button backButton = Button.Create(panelObject.transform, " < ", 0, 0, Color.white, () => OnBackButtonClicked?.Invoke());
            RectTransform backRect = backButton.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(1, 1);
            backRect.anchorMax = new Vector2(1, 1);
            backRect.pivot = new Vector2(1, 1);

            // Create minimize button
            minimizeButton = Button.Create(panelObject.transform, " − ", 0, 0, Color.white, OnMinimizeClick);
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

                // Collapse the panel, but keep title and buttons visible
                foreach (Transform child in panelObject.transform)
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
                foreach (Transform child in panelObject.transform)
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
