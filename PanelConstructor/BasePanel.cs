using UnityEngine;
using UnityEngine.UI;
using System.Linq;

namespace TwitchChat.PanelConstructor
{
    /// <summary>
    /// Base class for all panel implementations providing common functionality for UI panels.
    /// Handles panel creation, scrolling, message display, and minimization features.
    /// </summary>
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
            
            CreateScrollView();
        }

        /// <summary>
        /// Creates a base panel with standard UI elements including title, back button, and minimize button.
        /// </summary>
        /// <param name="parent">Parent transform to attach the panel to</param>
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

        /// <summary>
        /// Creates a scrollable view area within the panel for content display.
        /// Configures viewport, content area, and scroll behavior.
        /// </summary>
        protected virtual void CreateScrollView()
        {
            // Create scroll view container
            scrollableArea = new GameObject("ScrollView", typeof(RectTransform));
            scrollableArea.transform.SetParent(panelObject.transform, false);
            
            RectTransform scrollViewRect = scrollableArea.GetComponent<RectTransform>();
            scrollViewRect.anchorMin = new Vector2(0, 0);
            scrollViewRect.anchorMax = new Vector2(1, 1);
            scrollViewRect.offsetMin = new Vector2(5, 5);
            scrollViewRect.offsetMax = new Vector2(-5, -35); // Leave space for title and buttons
            scrollViewRect.sizeDelta = Vector2.zero;
            
            // Add ScrollRect component
            scrollRect = scrollableArea.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.inertia = false;
            
            // Add viewport with padding for safety
            GameObject viewport = new("Viewport", typeof(RectTransform));
            viewport.transform.SetParent(scrollableArea.transform, false);
            viewport.AddComponent<Image>().color = new Color(0, 0, 0, 0.1f);
            viewport.AddComponent<Mask>();
            
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            viewportRect.offsetMin = new Vector2(2, 2);
            viewportRect.offsetMax = new Vector2(-2, -2);
            
            // Add content container
            GameObject content = new("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            
            contentRectTransform = content.GetComponent<RectTransform>();
            contentRectTransform.anchorMin = new Vector2(0, 1);
            contentRectTransform.anchorMax = new Vector2(1, 1);
            contentRectTransform.pivot = new Vector2(0.5f, 1);
            contentRectTransform.sizeDelta = new Vector2(0, 0);
            
            VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.spacing = 5;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            ContentSizeFitter sizeFitter = content.AddComponent<ContentSizeFitter>();
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            
            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRectTransform;

            // Add a mask to the viewport
            RectMask2D rectMask = viewport.AddComponent<RectMask2D>();
            rectMask.padding = Vector4.zero;
        }

        /// <summary>
        /// Adds a chat message to the panel's content area.
        /// </summary>
        /// <param name="username">Username of the message sender</param>
        /// <param name="message">Content of the message</param>
        protected virtual void AddMessage(string username, string message)
        {
            if (contentRectTransform == null) return;

            try
            {
                // Process message text first before creating any GameObjects
                string safeMessage = ProcessMessageText(message);
                if (string.IsNullOrEmpty(safeMessage)) return;

                // Force message to be created on the main thread
                UnityMainThreadDispatcher.Instance().Enqueue(() => CreateMessageObjectSafe(username, safeMessage));
            }
            catch (System.Exception ex)
            {
                Main.LogEntry("BasePanel.AddMessage", $"Critical error in AddMessage: {ex.Message}");
            }
        }

        /// <summary>
        /// Creates a message object in the UI safely on the main thread.
        /// </summary>
        /// <param name="username">Username of the message sender</param>
        /// <param name="safeMessage">Processed and sanitized message content</param>
        private void CreateMessageObjectSafe(string username, string safeMessage)
        {
            try
            {
                if (contentRectTransform == null) return;

                GameObject messageObj = new GameObject($"Message_{Time.time}", typeof(RectTransform));
                messageObj.transform.SetParent(contentRectTransform, false);

                // Configure text component with safe defaults
                Text messageText = messageObj.AddComponent<Text>();
                messageText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                messageText.fontSize = 14;
                messageText.supportRichText = true;
                messageText.horizontalOverflow = HorizontalWrapMode.Wrap;
                messageText.verticalOverflow = VerticalWrapMode.Truncate;
                messageText.raycastTarget = false;
                messageText.color = Color.white;
                messageText.text = $"<color=cyan>{username}</color>: {safeMessage}";

                // Setup RectTransform with safe values
                RectTransform textRect = messageText.rectTransform;
                textRect.anchorMin = new Vector2(0, 0);
                textRect.anchorMax = new Vector2(1, 0);
                textRect.sizeDelta = new Vector2(0, 20);

                // Force layout rebuild
                LayoutRebuilder.ForceRebuildLayoutImmediate(textRect);
                Canvas.ForceUpdateCanvases();

                // Adjust final height
                float preferredHeight = Mathf.Max(20, messageText.preferredHeight);
                textRect.sizeDelta = new Vector2(0, preferredHeight);

                // Update scroll position
                if (scrollRect != null)
                {
                    scrollRect.verticalNormalizedPosition = 0f;
                }

                // Force content update
                if (contentRectTransform != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(contentRectTransform);
                }
            }
            catch (System.Exception ex)
            {
                Main.LogEntry("BasePanel.CreateMessageObjectSafe", $"Error creating message: {ex.Message}");
            }
        }

        /// <summary>
        /// Processes and sanitizes message text to ensure safe display in UI.
        /// Removes invalid characters and applies length limits.
        /// </summary>
        /// <param name="text">Raw message text to process</param>
        /// <returns>Sanitized message text</returns>
        private string ProcessMessageText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";

            try
            {
                // Hard limit on length to prevent layout issues
                // text = text.Length > 500 ? text.Substring(0, 500) + "..." : text;

                // Convert to char array and filter
                var filtered = text.ToCharArray().Where(c => 
                    char.IsLetterOrDigit(c) ||
                    char.IsPunctuation(c) ||
                    char.IsWhiteSpace(c) ||
                    c == ':' ||  // Preserve colons for username separator
                    (c >= 0x20 && c <= 0x7E) // Basic ASCII range
                ).ToArray();

                // Convert back to string and clean up
                text = new string(filtered)
                    .Replace("\n", " ")
                    .Replace("\r", " ")
                    .Replace("\t", " ");

                // Collapse multiple spaces
                while (text.Contains("  "))
                {
                    text = text.Replace("  ", " ");
                }

                return text.Trim();
            }
            catch (System.Exception ex)
            {
                Main.LogEntry("BasePanel.ProcessMessageText", $"Error processing text: {ex.Message}");
                return "[Message Error]";
            }
        }

        /// <summary>
        /// Handles the minimize/maximize button click event.
        /// Toggles between collapsed and expanded panel states.
        /// </summary>
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
