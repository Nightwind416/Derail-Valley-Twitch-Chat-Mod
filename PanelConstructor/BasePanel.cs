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

        /// <summary>Folds the whole display away to its title strip. Only cab displays can be folded.</summary>
        protected UnityEngine.UI.Button minimizeButton;

        /// <summary>Opens the colours of this panel on this display. Hidden on the template copy.</summary>
        protected UnityEngine.UI.Button? gearButton;

        // Add delegate and event for back button
        public delegate void OnBackButtonClickedHandler();
        public event OnBackButtonClickedHandler OnBackButtonClicked;

        /// <summary>The display this panel belongs to, or null for the hidden template copy.</summary>
        public PanelHost? Host { get; private set; }

        /// <summary>The id the registry knows this panel by, which its saved colours are filed under.</summary>
        public string PanelId { get; private set; } = string.Empty;

        /// <summary>
        /// Tells a panel which display it is on and what it is called. Everything that has to know one panel
        /// from another - which display's colours to wear, which display to fold away - waits on this, so it
        /// happens as soon as the display has taken the panel and before anything is shown.
        /// </summary>
        public virtual void Bind(PanelHost host, string id)
        {
            Host = host;
            PanelId = id;

            // Only a cab display has anything to fold away to; the wrist panel already folds to its button
            minimizeButton.gameObject.SetActive(host is CabDisplayHost);
            gearButton?.gameObject.SetActive(true);

            ApplyAppearance();
        }

        /// <summary>
        /// Paints this panel in the colours saved for it on this display, or the shared defaults if it has
        /// none of its own.
        /// </summary>
        public void ApplyAppearance()
        {
            PanelTheme? theme = panelObject.GetComponent<PanelTheme>();
            if (theme == null)
            {
                return;
            }

            PanelAppearance appearance = Host == null
                ? Settings.Instance.EffectiveAppearance(string.Empty, PanelId)
                : Settings.Instance.EffectiveAppearance(Host.AppearanceKey, PanelId);

            theme.PanelColor = appearance.panelColor;
            theme.SectionColor = appearance.sectionColor;
            theme.ButtonColor = appearance.buttonColor;
            theme.Repaint(panelObject);
        }

        /// <summary>
        /// What the title row reads. Panels that are a class of their own leave this null and are named
        /// after their type; panels built for someone else's content, where one class serves many names,
        /// pass their own.
        /// </summary>
        private readonly string? titleOverride;

        /// <summary>
        /// Closes the display this panel is on. Created hidden and only revealed for hosts that can be
        /// closed, which is the cab displays; the wrist panel never shows one.
        /// </summary>
        protected UnityEngine.UI.Button? closeButton;
        private System.Action? closeAction;

        /// <summary>
        /// Gives the panel something to do when its close button is pressed, and shows the button.
        /// Passing null hides it again.
        /// </summary>
        public void SetCloseAction(System.Action? action)
        {
            closeAction = action;
            if (closeButton != null)
            {
                closeButton.gameObject.SetActive(action != null);
            }
        }

        /// <summary>
        /// Whether this panel is the one its display is currently showing. Tracked rather than read back
        /// off the object, so that hiding every panel when a display is built - which happens before any
        /// of them has been shown - does not tell them all they have just been hidden.
        /// </summary>
        public bool IsVisible { get; private set; }

        public virtual void Show()
        {
            bool wasVisible = IsVisible;
            IsVisible = true;
            panelObject.SetActive(true);

            if (!wasVisible)
            {
                OnShown();
            }
        }

        public virtual void Hide()
        {
            bool wasVisible = IsVisible;
            IsVisible = false;
            panelObject.SetActive(false);

            if (wasVisible)
            {
                OnHidden();
            }
        }

        /// <summary>Called when the panel becomes the one on show, but not again while it stays there.</summary>
        protected virtual void OnShown() { }

        /// <summary>Called when the panel stops being the one on show.</summary>
        protected virtual void OnHidden() { }

        /// <summary>
        /// Called once a frame while the panel is the one on show, to refresh whatever it is displaying.
        /// </summary>
        /// <param name="deltaTime">Seconds since the last call.</param>
        public virtual void Tick(float deltaTime) { }

        /// <summary>
        /// Called when the display this panel is on has been dragged to a new size, in canvas units.
        /// Panels anchored to their display need do nothing; those that lay themselves out by hand, or
        /// draw into a texture, want to know.
        /// </summary>
        public virtual void OnResize(Vector2 size) { }

        /// <param name="parent">Parent transform to attach the panel to.</param>
        /// <param name="title">Title row text, or null to name the panel after its type.</param>
        protected BasePanel(Transform parent, string? title = null)
        {
            titleOverride = title;
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
            panelObject = new GameObject(titleOverride == null ? GetType().Name : $"{titleOverride}Panel");
            panelObject.transform.SetParent(parent, false);

            // Before anything is built on it: the widget factories walk up to this to find out what colour
            // to be, so a panel without one would build itself in the defaults and stay that way
            PanelTheme theme = panelObject.AddComponent<PanelTheme>();
            theme.PanelColor = Settings.Instance.panelColor;
            theme.SectionColor = Settings.Instance.sectionColor;
            theme.ButtonColor = Settings.Instance.buttonColor;

            Image panelImage = panelObject.AddComponent<Image>();
            panelImage.color = theme.PanelColor;

            rectTransform = panelObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 0);
            rectTransform.anchorMax = new Vector2(1, 1);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.pivot = new Vector2(0, 1);
            rectTransform.anchoredPosition = Vector2.zero;

            // Create title using Title factory
            Title.Create(panelObject.transform, titleOverride ?? GetType().Name.Replace("Panel", ""), 18);

            // Create close button, in the top right corner with the back button beside it
            closeButton = Button.Create(panelObject.transform, " x ", 0, 0, Color.white, () => closeAction?.Invoke());
            RectTransform closeRect = closeButton.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1, 1);
            closeRect.anchorMax = new Vector2(1, 1);
            closeRect.pivot = new Vector2(1, 1);
            closeButton.gameObject.SetActive(false); // shown only once a host gives it something to close

            // Create back button
            UnityEngine.UI.Button backButton = Button.Create(panelObject.transform, " < ", -22, 0, Color.white, () => OnBackButtonClicked?.Invoke());
            RectTransform backRect = backButton.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(1, 1);
            backRect.anchorMax = new Vector2(1, 1);
            backRect.pivot = new Vector2(1, 1);

            // Create minimize button, shown by Bind only on the hosts that can be folded away
            minimizeButton = Button.Create(panelObject.transform, " − ", 0, 0, Color.white, OnMinimizeClick);
            RectTransform minimizeRect = minimizeButton.GetComponent<RectTransform>();
            minimizeRect.anchorMin = new Vector2(0, 1);
            minimizeRect.anchorMax = new Vector2(0, 1);
            minimizeRect.pivot = new Vector2(0, 1);
            minimizeButton.gameObject.SetActive(false);

            // Colours for this panel on this display, beside the minimize button. Three bars rather than a
            // cogwheel because the built-in font has no cogwheel and would draw nothing at all
            gearButton = Button.Create(panelObject.transform, " ≡ ", 26, 0, Color.white, OnGearClick, 18);
            RectTransform gearRect = gearButton.GetComponent<RectTransform>();
            gearRect.anchorMin = new Vector2(0, 1);
            gearRect.anchorMax = new Vector2(0, 1);
            gearRect.pivot = new Vector2(0, 1);
            gearButton.gameObject.SetActive(false); // shown by Bind; the template copy has no display to edit
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
            viewport.AddComponent<Image>().color = new Color(0, 0, 0, 0.0f);
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

                GameObject messageObj = new($"Message_{Time.time}", typeof(RectTransform));
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
        /// Folds the whole display away to its title strip.
        /// </summary>
        /// <remarks>
        /// The panel does not fold itself. Minimizing is something the display does - its size, its grab
        /// bars and every panel on it are all involved - so the panel only asks, and the one place that
        /// knows what a folded display looks like does the rest.
        /// </remarks>
        protected virtual void OnMinimizeClick()
        {
            if (Host is CabDisplayHost display)
            {
                MenuManager.Instance.SetDisplayMinimized(display, true);
            }
        }

        /// <summary>Opens the colours of this panel on this display.</summary>
        protected virtual void OnGearClick()
        {
            if (Host != null)
            {
                MenuManager.Instance.OpenAppearance(Host, PanelId);
            }
        }
    }
}
