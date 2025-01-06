using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Collections;

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
            
            CreateScrollView();
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
            
            // Add ScrollRect component
            scrollRect = scrollableArea.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            
            // Add viewport with padding for safety
            GameObject viewport = new("Viewport", typeof(RectTransform));
            viewport.transform.SetParent(scrollableArea.transform, false);
            viewport.AddComponent<Image>().color = new Color(0, 0, 0, 0.1f);
            viewport.AddComponent<Mask>();
            
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            viewportRect.offsetMin = new Vector2(2, 2); // Add small padding
            viewportRect.offsetMax = new Vector2(-2, -2);
            
            // Add content container with safe area
            GameObject content = new("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            
            contentRectTransform = content.GetComponent<RectTransform>();
            contentRectTransform.anchorMin = new Vector2(0, 1);
            contentRectTransform.anchorMax = new Vector2(1, 1);
            contentRectTransform.pivot = new Vector2(0.5f, 1);
            contentRectTransform.sizeDelta = new Vector2(0, 0);
            
            VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 10, 10); // Increased padding
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
        }

        protected virtual void AddMessage(string username, string message)
        {
            if (contentRectTransform == null) return;

            try
            {
                // Process message text first before creating any GameObjects
                string safeMessage = ProcessMessageText($"{username}: {message}");
                if (string.IsNullOrEmpty(safeMessage)) return;

                // Start coroutine to add message safely
                UnityMainThreadDispatcher.Instance().StartCoroutine(CreateMessageObject(safeMessage));
            }
            catch (System.Exception ex)
            {
                Main.LogEntry("BasePanel.AddMessage", $"Critical error in AddMessage: {ex.Message}");
            }
        }

        private IEnumerator CreateMessageObject(string safeMessage)
        {
            if (contentRectTransform == null) yield break;

            yield return CreateMessageObjectInternal(safeMessage);
        }

        private IEnumerator CreateMessageObjectInternal(string safeMessage)
        {
            GameObject? messageObj = null;
            try
            {
                messageObj = new GameObject($"Message_{Time.time}", typeof(RectTransform));
                messageObj.transform.SetParent(contentRectTransform, false);

                // Configure text component with safe defaults
                Text messageText = messageObj.AddComponent<Text>();
                messageText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                messageText.fontSize = 14;
                messageText.color = Color.white;
                messageText.supportRichText = false;
                messageText.horizontalOverflow = HorizontalWrapMode.Wrap;
                messageText.verticalOverflow = VerticalWrapMode.Overflow;
                messageText.raycastTarget = false;
                messageText.text = safeMessage;

                // Setup RectTransform with safe values
                RectTransform textRect = messageText.rectTransform;
                textRect.anchorMin = new Vector2(0, 0);
                textRect.anchorMax = new Vector2(1, 0);
                textRect.sizeDelta = new Vector2(0, 20); // Initial height

                UpdateMessageLayout(messageText, textRect);
            }
            catch (System.Exception ex)
            {
                Main.LogEntry("BasePanel.CreateMessageObject", $"Error creating message: {ex.Message}");
                if (messageObj != null)
                {
                    GameObject.Destroy(messageObj);
                }
            }
            yield break;
        }

        private void UpdateMessageLayout(Text messageText, RectTransform textRect)
        {
            // Wait one frame to ensure layout is ready
            UnityMainThreadDispatcher.Instance().StartCoroutine(UpdateLayoutCoroutine(messageText, textRect));
        }

        private IEnumerator UpdateLayoutCoroutine(Text messageText, RectTransform textRect)
        {
            yield return null;

            // Force layout rebuild
            LayoutRebuilder.ForceRebuildLayoutImmediate(textRect);

            yield return null; // Wait another frame

            // Set final height with safety check
            float preferredHeight = Mathf.Max(20, messageText.preferredHeight);
            textRect.sizeDelta = new Vector2(0, preferredHeight);

            // Update scroll position
            if (scrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                scrollRect.verticalNormalizedPosition = 0f;
            }

            // Force content size update
            if (contentRectTransform != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentRectTransform);
            }
        }

        private string ProcessMessageText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";

            try
            {
                // Limit total length
                const int maxLength = 200;
                if (text.Length > maxLength)
                {
                    text = text.Substring(0, maxLength) + "...";
                }

                // Remove problematic characters
                text = text.Replace('\u200B', ' ')  // Zero-width space
                         .Replace('\u200C', ' ')  // Zero-width non-joiner
                         .Replace('\u200D', ' ')  // Zero-width joiner
                         .Replace('\u2028', ' ')  // Line separator
                         .Replace('\u2029', ' ')  // Paragraph separator
                         .Replace('\t', ' ')      // Tab
                         .Replace('\r', ' ')      // Carriage return
                         .Replace('\n', ' ');     // New line

                // Remove any other control characters and non-printable characters
                text = new string(text.Where(c => !char.IsControl(c) && 
                                                (char.IsLetterOrDigit(c) || 
                                                 char.IsPunctuation(c) || 
                                                 char.IsWhiteSpace(c) ||
                                                 char.IsSymbol(c))).ToArray());

                // Collapse multiple spaces
                while (text.Contains("  "))
                {
                    text = text.Replace("  ", " ");
                }

                // Add extra safety trim
                text = text.Trim();
                
                // Ensure minimum length
                if (text.Length < 1)
                    return "[Empty Message]";

                return text;
            }
            catch (System.Exception ex)
            {
                Main.LogEntry("BasePanel.ProcessMessageText", $"Error processing message text: {ex.Message}");
                return "[Message Processing Error]";
            }
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
