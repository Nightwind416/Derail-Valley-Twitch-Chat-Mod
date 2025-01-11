using UnityEngine;
using UnityEngine.UI;
using System.Reflection;
using TwitchChat.PanelMenus;
using TwitchChat.PanelDisplays;
using System.Collections.Generic;
using System;

namespace TwitchChat
{
    /// <summary>
    /// Represents a license instance with associated UI panels and game objects.
    /// </summary>
    /// <remarks>
    /// Manages the relationship between in-game license objects and their corresponding UI elements,
    /// including menu canvases, panel displays, and sticky tape attachments.
    /// </remarks>
    public class License
    {
        public string Name { get; private set; }
        public int LicenseIndex { get; private set; }  // Add this property
        public GameObject? LicenseObject { get; set; }
        public GameObject? MenuCanvas { get; set; }
        public bool AttachedToStickyTape { get; set; }
        public GameObject? StickyTapeBase { get; set; }

        // Panel Menus
        public MainPanel? MainPanel { get; set; }
        public StatusPanel? StatusPanel { get; set; }
        public NotificationsPanel? NotificationsPanel { get; set; }
        public StandardMessagesPanel? StandardMessagesPanel { get; set; }
        public CommandMessagesPanel? CommandMessagesPanel { get; set; }
        public TimedMessagesPanel? TimedMessagesPanel { get; set; }
        public ConfigurationPanel? ConfigurationPanel { get; set; }
        public DebugPanel? DebugPanel { get; set; }

        // Panel Displays
        public LargeDisplayPanel? LargeDisplayPanel { get; set; }
        public MediumDisplayPanel? MediumDisplayPanel { get; set; }
        public SmallDisplayPanel? SmallDisplayPanel { get; set; }
        public WideDisplayPanel? WideDisplayPanel { get; set; }

        public License(string name, int index)  // Update constructor
        {
            Name = name;
            LicenseIndex = index;
        }
    }

    /// <summary>
    /// Manages the creation, positioning, and interaction of UI menus and panels for the Twitch Chat mod.
    /// </summary>
    /// <remarks>
    /// This class is responsible for:
    /// - Creating and managing UI canvases for each license
    /// - Handling panel visibility and transitions
    /// - Positioning menus relative to license objects
    /// - Managing sticky tape attachments and paper visibility
    /// - Coordinating message displays across all active panels
    /// </remarks>
    public class MenuManager : MonoBehaviour
    {
        private static MenuManager? instance;
        private readonly Dictionary<string, License> licenses = new();
        private GameObject? templateCanvas;

        /// <summary>
        /// Defines the types of panels available in the mod interface.
        /// </summary>
        private enum PanelType
        {
            Main,
            Status,
            Notifications,
            LargeDisplay,
            MediumDisplay,
            WideDisplay,
            SmallDisplay,
            StandardMessages,
            CommandMessages,
            TimedMessages,
            Configuration,
            Debug
        }

        /// <summary>
        /// Defines the configuration parameters for panel positioning and sizing.
        /// </summary>
        private struct PanelConfig
        {
            public Vector2 CanvasSize;
            public Vector2 PanelSize;
            public Vector2 PanelPosition;
            public Vector3 PanelRotationOffset;

            public PanelConfig(Vector2 canvasSize, Vector2 panelSize, Vector2 panelPosition, Vector3 panelRotationOffset)
            {
                CanvasSize = canvasSize;
                PanelSize = panelSize;
                PanelPosition = panelPosition;
                PanelRotationOffset = panelRotationOffset;
            }
        }

        private readonly Dictionary<PanelType, PanelConfig> panelConfigs = new()
        {
            { PanelType.Main, new(new Vector2(200, 300), new Vector2(200, 300), Vector2.zero, Vector3.zero) },
            { PanelType.Status, new(new Vector2(200, 300), new Vector2(200, 300), Vector2.zero, Vector3.zero) },
            { PanelType.Notifications, new(new Vector2(200, 300), new Vector2(200, 300), Vector2.zero, Vector3.zero) },
            { PanelType.LargeDisplay, new(new Vector2(1200, 650), new Vector2(1200, 650), Vector2.zero, Vector3.zero) },
            { PanelType.MediumDisplay, new(new Vector2(500, 500), new Vector2(500, 500), Vector2.zero, Vector3.zero) },
            { PanelType.WideDisplay, new(new Vector2(900, 220), new Vector2(900, 220), Vector2.zero, Vector3.zero) },
            { PanelType.SmallDisplay, new(new Vector2(200, 300), new Vector2(200, 300), Vector2.zero, Vector3.zero) },
            { PanelType.StandardMessages, new(new Vector2(200, 300), new Vector2(200, 300), Vector2.zero, Vector3.zero) },
            { PanelType.CommandMessages, new(new Vector2(200, 300), new Vector2(200, 300), Vector2.zero, Vector3.zero) },
            { PanelType.TimedMessages, new(new Vector2(200, 300), new Vector2(200, 300), Vector2.zero, Vector3.zero) },
            { PanelType.Configuration, new(new Vector2(200, 300), new Vector2(200, 300), Vector2.zero, Vector3.zero) },
            { PanelType.Debug, new(new Vector2(200, 300), new Vector2(200, 300), Vector2.zero, Vector3.zero) }
        };

        /// <summary>
        /// Gets the singleton instance of the MenuManager.
        /// Creates a new instance if one doesn't exist.
        /// </summary>
        public static MenuManager Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new("TwitchChatMenuManager");
                    instance = go.AddComponent<MenuManager>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        /// <summary>
        /// Initializes a new instance of MenuManager with predefined license configurations.
        /// </summary>
        public MenuManager()
        {
            // Initialize licenses with their index
            string[] licenseNames = {
                "LicenseTrainDriver",
                "LicenseShunting",
                "LicenseLocomotiveDE2",
                "LicenseMuseumCitySouth",
                "LicenseFreightHaul",
                "LicenseDispatcher1"
            };

            for (int i = 0; i < licenseNames.Length; i++)
            {
                licenses.Add(licenseNames[i], new License(licenseNames[i], i));
            }
        }

        private void Awake()
        {
            CreateTemplateCanvas();
        }

        /// <summary>
        /// Creates a template canvas that serves as the base for all UI panels.
        /// </summary>
        private void CreateTemplateCanvas()
        {
            string methodName = MethodBase.GetCurrentMethod().Name;

            Main.LogEntry(methodName, "Creating template canvas - VR Mode: " + VRManager.IsVREnabled());
            
            templateCanvas = new GameObject("TemplateCanvas");
            templateCanvas.SetActive(false);
            DontDestroyOnLoad(templateCanvas);

            // Setup basic canvas components
            Canvas canvas = templateCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 1000;
            
            RectTransform canvasRect = templateCanvas.GetComponent<RectTransform>();
            canvasRect.sizeDelta = panelConfigs[PanelType.Main].CanvasSize;
            canvasRect.localScale = Vector3.one * 0.001f;

            // Add GraphicRaycaster with proper VR settings
            var raycaster = templateCanvas.AddComponent<GraphicRaycaster>();

            // Create template panel
            GameObject menuPanel = new("MenuPanel");
            menuPanel.transform.SetParent(templateCanvas.transform, false);
            RectTransform panelRect = menuPanel.AddComponent<RectTransform>();
            panelRect.sizeDelta = panelConfigs[PanelType.Main].PanelSize;
            panelRect.anchorMin = new Vector2(0f, 1f);
            panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.localPosition = panelConfigs[PanelType.Main].PanelPosition;
            panelRect.localRotation = Quaternion.Euler(panelConfigs[PanelType.Main].PanelRotationOffset);

            // Create all panel templates
            CreatePanelTemplates(menuPanel.transform);
        }

        /// <summary>
        /// Creates template instances of all panel types.
        /// </summary>
        /// <param name="parent">Parent transform to attach panel templates to.</param>
        private void CreatePanelTemplates(Transform parent)
        {
            // Create one of each panel type as templates
            var mainPanel = new MainPanel(parent, null);
            var statusPanel = new StatusPanel(parent);
            var notificationsPanel = new NotificationsPanel(parent);
            var largeDisplayPanel = new LargeDisplayPanel(parent);
            var mediumDisplayPanel = new MediumDisplayPanel(parent);
            var wideDisplayPanel = new WideDisplayPanel(parent);
            var smallDisplayPanel = new SmallDisplayPanel(parent);
            var standardMessagesPanel = new StandardMessagesPanel(parent);
            var commandMessagesPanel = new CommandMessagesPanel(parent);
            var timedMessagesPanel = new TimedMessagesPanel(parent);
            var configurationPanel = new ConfigurationPanel(parent);
            var debugPanel = new DebugPanel(parent);

            // Hide all template panels
            foreach (Transform child in parent)
            {
                child.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Updates the state and visibility of all license objects and their associated UI elements.
        /// </summary>
        private void Update()
        {
            string methodName = MethodBase.GetCurrentMethod().Name;

            foreach (var license in licenses.Values)
            {
                if (license.LicenseObject == null)
                {
                    license.LicenseObject = GameObject.Find(license.Name);
                    if (license.LicenseObject != null)
                    {
                        Main.LogEntry(methodName, $"Attaching menu canvas to {license.Name}");
                        
                        if (license.MenuCanvas == null)
                        {
                            Main.LogEntry(methodName, $"Menu canvas for {license.Name} was null, creating...");
                            CreateMenuCanvas(license);
                        }
                    }
                }

                if (license.LicenseObject != null)
                {
                    bool isLicenseActive = IsLicenseActive(license.LicenseObject);
                    if (license.MenuCanvas != null)
                    {
                        // Only update canvas visibility if it's changed
                        if (license.MenuCanvas.activeSelf != isLicenseActive)
                        {
                            license.MenuCanvas.SetActive(isLicenseActive);
                            if (isLicenseActive)
                            {
                                // Re-show the active panel when canvas becomes visible
                                string panelToShow = !string.IsNullOrEmpty(Settings.Instance.activePanels[license.LicenseIndex]) 
                                    ? Settings.Instance.activePanels[license.LicenseIndex] 
                                    : "Main";
                                ShowPanel(panelToShow, license);
                            }
                        }
                    }

                    if (isLicenseActive)
                    {
                        UpdatePanelValues(license);
                        HandleLicenseAttachment(license);
                        HandlePaperVisibility(license);
                    }
                }
            }
        }

        /// <summary>
        /// Updates values displayed on active panels.
        /// </summary>
        /// <param name="license">The license containing panels to update.</param>
        private void UpdatePanelValues(License license)
        {
            license.StatusPanel?.UpdateStatusPanelValues();
            license.StandardMessagesPanel?.UpdateStandardMessagesPanelValues();
            license.CommandMessagesPanel?.UpdateCommandMessagesPanelValues();
        }

        /// <summary>
        /// Determines if a license object is currently active in the game world.
        /// </summary>
        /// <param name="license">The license GameObject to check.</param>
        /// <returns>True if the license is active and not in inventory.</returns>
        private bool IsLicenseActive(GameObject license)
        {
            if (!license.activeInHierarchy)
                return false;

            // Check if the license is in inventory by looking at its parent hierarchy
            Transform current = license.transform;
            while (current.parent != null)
            {
                // Check for common inventory container names
                if (current.parent.name.Contains("Inventory") || 
                    current.parent.name.Contains("Storage") ||
                    current.parent.name.Contains("Container"))
                {
                    return false;
                }
                current = current.parent;
            }

            return true;
        }

        // BUG: Sticky tape returns after away from loco, detact/reattach fixes it
        private void HandleLicenseAttachment(License license)
        {
            // Bounds checking for all arrays
            if (license.LicenseObject == null)
            {
                Main.LogEntry("MenuManager.HandleLicenseAttachment", $"Invalid index or null object: {license.Name}");
                return;
            }

            try
            {
                Transform current = license.LicenseObject!.transform;
                bool currentlyAttached = false;
                GameObject? newStickyTapeBase = null;
                
                while (current.parent != null)
                {
                    if (current.parent.name.Contains("StickyTape_Gadget"))
                    {
                        currentlyAttached = true;
                        Transform baseTransform = current.parent.Find("LOD gadget_sticker_base");
                        if (baseTransform != null)
                        {
                            newStickyTapeBase = baseTransform.gameObject;
                        }
                        break;
                    }
                    current = current.parent;
                }

                if (currentlyAttached != license.AttachedToStickyTape)
                {
                    license.AttachedToStickyTape = currentlyAttached;
                    Main.LogEntry("HandleLicenseAttachment", $"License {license.Name} attachment to sticky tape changed: {license.AttachedToStickyTape}");
                    
                    if (license.AttachedToStickyTape && newStickyTapeBase != null)
                    {
                        license.StickyTapeBase = newStickyTapeBase;
                        license.StickyTapeBase!.SetActive(false);
                    }
                    else if (!license.AttachedToStickyTape && license.StickyTapeBase != null)
                    {
                        license.StickyTapeBase!.SetActive(true);
                        license.StickyTapeBase = null;
                    }
                }
            }
            catch (Exception e)
            {
                Main.LogEntry("MenuManager.HandleLicenseAttachment", $"Error: {e.Message}");
            }
        }

        private void HandlePaperVisibility(License license)
        {
            if (license.LicenseObject != null)
            {
                Transform paperTransform = license.LicenseObject!.transform.Find("Pivot/TempPaper(Clone)(Clone) 0/Paper");
                paperTransform?.gameObject.SetActive(false);
            }
        }

        private void LateUpdate()
        {
            foreach (var license in licenses.Values)
            {
                if (license.LicenseObject != null && license.MenuCanvas != null)
                {
                    PositionNearObject(license);
                }
            }
        }

        public void OnPanelButtonClicked(string panelName, License license)
        {
            Main.LogEntry("OnPanelButtonClicked", $"Panel button clicked: {panelName} for license: {license.Name}");
            ShowPanel(panelName, license);
        }

        private void CreateMenuCanvas(License license)
        {
            if (templateCanvas == null)
            {
                Main.LogEntry("CreateMenuCanvas", "Template canvas not found!");
                return;
            }

            // Clone the template
            license.MenuCanvas = Instantiate(templateCanvas);
            license.MenuCanvas.name = $"MenuCanvas_{license.Name}";
            
            Transform menuPanel = license.MenuCanvas.transform.Find("MenuPanel");
            
            // Create and wire up all panels from the templates
            license.MainPanel = new MainPanel(menuPanel, license);
            license.StatusPanel = new StatusPanel(menuPanel);
            license.NotificationsPanel = new NotificationsPanel(menuPanel);
            license.LargeDisplayPanel = new LargeDisplayPanel(menuPanel);
            license.MediumDisplayPanel = new MediumDisplayPanel(menuPanel);
            license.WideDisplayPanel = new WideDisplayPanel(menuPanel);
            license.SmallDisplayPanel = new SmallDisplayPanel(menuPanel);
            license.StandardMessagesPanel = new StandardMessagesPanel(menuPanel);
            license.CommandMessagesPanel = new CommandMessagesPanel(menuPanel);
            license.TimedMessagesPanel = new TimedMessagesPanel(menuPanel);
            license.ConfigurationPanel = new ConfigurationPanel(menuPanel);
            license.DebugPanel = new DebugPanel(menuPanel);

            // Explicitly hide all panels immediately after creation
            HideAllPanels(license);

            // Wire up back button events
            if (license.StatusPanel != null) license.StatusPanel.OnBackButtonClicked += () => ShowPanel("Main", license);
            if (license.NotificationsPanel != null) license.NotificationsPanel.OnBackButtonClicked += () => ShowPanel("Main", license);
            if (license.LargeDisplayPanel != null) license.LargeDisplayPanel.OnBackButtonClicked += () => ShowPanel("Main", license);
            if (license.MediumDisplayPanel != null) license.MediumDisplayPanel.OnBackButtonClicked += () => ShowPanel("Main", license);
            if (license.WideDisplayPanel != null) license.WideDisplayPanel.OnBackButtonClicked += () => ShowPanel("Main", license);
            if (license.SmallDisplayPanel != null) license.SmallDisplayPanel.OnBackButtonClicked += () => ShowPanel("Main", license);
            if (license.StandardMessagesPanel != null) license.StandardMessagesPanel.OnBackButtonClicked += () => ShowPanel("Main", license);
            if (license.CommandMessagesPanel != null) license.CommandMessagesPanel.OnBackButtonClicked += () => ShowPanel("Main", license);
            if (license.TimedMessagesPanel != null) license.TimedMessagesPanel.OnBackButtonClicked += () => ShowPanel("Main", license);
            if (license.ConfigurationPanel != null) license.ConfigurationPanel.OnBackButtonClicked += () => ShowPanel("Main", license);
            if (license.DebugPanel != null) license.DebugPanel.OnBackButtonClicked += () => ShowPanel("Main", license);

            // Show initial panel
            string panelToShow = !string.IsNullOrEmpty(Settings.Instance.activePanels[license.LicenseIndex]) 
                ? Settings.Instance.activePanels[license.LicenseIndex] 
                : "Main";
            ShowPanel(panelToShow, license);
        }

        private void HideAllPanels(License license)
        {
            license.MainPanel?.Hide();
            license.StatusPanel?.Hide();
            license.NotificationsPanel?.Hide();
            license.LargeDisplayPanel?.Hide();
            license.MediumDisplayPanel?.Hide();
            license.WideDisplayPanel?.Hide();
            license.SmallDisplayPanel?.Hide();
            license.StandardMessagesPanel?.Hide();
            license.CommandMessagesPanel?.Hide();
            license.TimedMessagesPanel?.Hide();
            license.ConfigurationPanel?.Hide();
            license.DebugPanel?.Hide();
        }

        private void ShowPanel(string panelName, License license)
        {
            if (license.MenuCanvas == null || !license.MenuCanvas!.activeSelf)
                return;
            
            Main.LogEntry("ShowPanel", $"Showing panel {panelName} for license {license.Name} (index: {license.LicenseIndex})");

            HideAllPanels(license);

            PanelType panelType = panelName switch
            {
                "Main" => PanelType.Main,
                "Status" => PanelType.Status,
                "Notifications" => PanelType.Notifications,
                "Large Display" => PanelType.LargeDisplay,
                "Medium Display" => PanelType.MediumDisplay,
                "Wide Display" => PanelType.WideDisplay,
                "Small Display" => PanelType.SmallDisplay,
                "Standard Messages" => PanelType.StandardMessages,
                "Command Messages" => PanelType.CommandMessages,
                "Timed Messages" => PanelType.TimedMessages,
                "Configuration" => PanelType.Configuration,
                "Debug" => PanelType.Debug,
                _ => PanelType.Main
            };

            // Apply the configuration for this panel type
            var config = panelConfigs[panelType];
            var menuPanel = license.MenuCanvas!.transform.Find("MenuPanel");
            if (menuPanel != null)
            {
                RectTransform canvasRect = license.MenuCanvas!.GetComponent<RectTransform>();
                RectTransform panelRect = menuPanel.GetComponent<RectTransform>();
                
                canvasRect.sizeDelta = config.CanvasSize;
                panelRect.sizeDelta = config.PanelSize;
                panelRect.localPosition = config.PanelPosition;
                panelRect.localRotation = Quaternion.Euler(config.PanelRotationOffset);
            }

            // Show the selected panel
            switch (panelType)
            {
                case PanelType.Main:
                    license.MainPanel?.Show();
                    break;
                case PanelType.Status:
                    license.StatusPanel?.Show();
                    break;
                case PanelType.Notifications:
                    license.NotificationsPanel?.Show();
                    break;
                case PanelType.LargeDisplay:
                    license.LargeDisplayPanel?.Show();
                    break;
                case PanelType.MediumDisplay:
                    license.MediumDisplayPanel?.Show();
                    break;
                case PanelType.WideDisplay:
                    license.WideDisplayPanel?.Show();
                    break;
                case PanelType.SmallDisplay:
                    license.SmallDisplayPanel?.Show();
                    break;
                case PanelType.StandardMessages:
                    license.StandardMessagesPanel?.Show();
                    break;
                case PanelType.CommandMessages:
                    license.CommandMessagesPanel?.Show();
                    break;
                case PanelType.TimedMessages:
                    license.TimedMessagesPanel?.Show();
                    break;
                case PanelType.Configuration:
                    license.ConfigurationPanel?.Show();
                    break;
                case PanelType.Debug:
                    license.DebugPanel?.Show();
                    break;
            }

            // Save the active panel state to the correct index
            Settings.Instance.activePanels[license.LicenseIndex] = panelName;
            Settings.Instance.Save(Main.ModEntry);
            Main.LogEntry("ShowPanel", $"Saving active panel state for license {license.Name} at index {license.LicenseIndex}: {panelName}");
        }

        private void PositionNearObject(License license)
        {
            if (license.MenuCanvas == null)
            {
                Main.LogEntry("MenuManager.PositionNearObject", "Menu Canvas is not initialized.");
                return;
            }

            if (license.LicenseObject == null) return;

            Vector3 targetPosition = license.LicenseObject.transform.position;
            license.MenuCanvas.transform.position = targetPosition;

            license.MenuCanvas.transform.rotation = license.LicenseObject.transform.rotation * 
                                         Quaternion.Euler(90f, 180f, 0f) * 
                                         Quaternion.Euler(panelConfigs[PanelType.Main].PanelRotationOffset);
        }

        /// <summary>
        /// Adds a chat message to all active display panels.
        /// </summary>
        /// <param name="username">The username of the message sender.</param>
        /// <param name="message">The chat message content.</param>
        public void AddMessageToPanelDisplays(string username, string message)
        {
            try
            {
                foreach (var license in licenses.Values)
                {
                    if (license.MenuCanvas != null && license.MenuCanvas.activeSelf)
                    {
                        try
                        {
                            // Add message to all display panels regardless of visibility
                            license.LargeDisplayPanel?.AddChatMessage(username, message);
                            license.MediumDisplayPanel?.AddChatMessage(username, message);
                            license.WideDisplayPanel?.AddChatMessage(username, message);
                            license.SmallDisplayPanel?.AddChatMessage(username, message);
                        }
                        catch (System.Exception ex)
                        {
                            Main.LogEntry("MenuManager.AddMessageToPanelDisplays", 
                                $"Error adding message to license {license.Name}: {ex.Message}");
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Main.LogEntry("MenuManager.AddMessageToPanelDisplays", 
                    $"Error in AddMessageToPanelDisplays: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates the notification toggle state across all licenses.
        /// </summary>
        /// <param name="value">The new toggle state to apply.</param>
        public void UpdateAllNotificationToggles(bool value)
        {
            foreach (var license in licenses.Values)
            {
                if (license.NotificationsPanel != null)
                {
                    license.NotificationsPanel.UpdateNotificationsEnabled(value);
                }
            }
        }

        public void UpdateAllNotificationDurations(float value)
        {
            foreach (var license in licenses.Values)
            {
                if (license.NotificationsPanel != null)
                {
                    license.NotificationsPanel.UpdateNotificationDuration(value);
                }
            }
        }
        public void UpdateAllProcessOwnToggles(bool value)
        {
            foreach (var license in licenses.Values)
            {
                if (license.DebugPanel != null)
                {
                    license.DebugPanel.UpdateProcessOwn(value);
                }
            }
        }
        public void UpdateAllProcessDuplicatesToggles(bool value)
        {
            foreach (var license in licenses.Values)
            {
                if (license.DebugPanel != null)
                {
                    license.DebugPanel.UpdateProcessDuplicates(value);
                }
            }
        }

        public void UpdateAllConnectMessageEnabledToggles(bool value)
        {
            foreach (var license in licenses.Values)
            {
                if (license.StandardMessagesPanel != null)
                {
                    license.StandardMessagesPanel.UpdateConnectMessageEnabled(value);
                }
            }
        }

        // public void UpdateAllNewFollowerMessageEnabledToggles(bool value)
        // {
        //     foreach (var license in licenses.Values)
        //     {
        //         if (license.StandardMessagesPanel != null)
        //         {
        //             license.StandardMessagesPanel.UpdateNewFollowerMessageEnabled(value);
        //         }
        //     }
        // }

        // public void UpdateAllNewSubscriberMessageEnabledToggles(bool value)
        // {
        //     foreach (var license in licenses.Values)
        //     {
        //         if (license.StandardMessagesPanel != null)
        //         {
        //             license.StandardMessagesPanel.UpdateNewSubscriberMessageEnabled(value);
        //         }
        //     }
        // }

        public void UpdateAllDisconnectMessageEnabledToggles(bool value)
        {
            foreach (var license in licenses.Values)
            {
                if (license.StandardMessagesPanel != null)
                {
                    license.StandardMessagesPanel.UpdateDisconnectMessageEnabled(value);
                }
            }
        }
        public void UpdateCommandsMessageToggles(bool value)
        {
            foreach (var license in licenses.Values)
            {
                if (license.CommandMessagesPanel != null)
                {
                    license.CommandMessagesPanel.UpdateCommandsMessageEnabled(value);
                }
            }
        }
        public void UpdateInfoMessageToggles(bool value)
        {
            foreach (var license in licenses.Values)
            {
                if (license.CommandMessagesPanel != null)
                {
                    license.CommandMessagesPanel.UpdateInfoMessageEnabled(value);
                }
            }
        }
        public void UpdateAllTimedMessageToggles(bool value)
        {
            foreach (var license in licenses.Values)
            {
                if (license.TimedMessagesPanel != null)
                {
                    license.TimedMessagesPanel.UpdateTimedMessagesEnabled(value);
                }
            }
        }
    }
}
