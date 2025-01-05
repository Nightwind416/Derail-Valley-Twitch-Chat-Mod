using UnityEngine;
using UnityEngine.UI;
using System.Reflection;
using TwitchChat.Menus;
using System.Collections.Generic;
using System;

namespace TwitchChat
{
    public class License
    {
        public string Name { get; private set; }
        public GameObject? LicenseObject { get; set; }
        public GameObject? MenuCanvas { get; set; }
        public bool AttachedToStickyTape { get; set; }
        public GameObject? StickyTapeBase { get; set; }

        // Menu panels
        public MainMenu? MainMenu { get; set; }
        public StatusMenu? StatusMenu { get; set; }
        public NotificationMenu? NotificationMenu { get; set; }
        public LargeDisplayBoard? LargeDisplayBoard { get; set; }
        public MediumDisplayBoard? MediumDisplayBoard { get; set; }
        public WideDisplayBoard? WideDisplayBoard { get; set; }
        public SmallDisplayBoard? SmallDisplayBoard { get; set; }
        public StandardMessagesMenu? StandardMessagesMenu { get; set; }
        public CommandMessagesMenu? CommandMessagesMenu { get; set; }
        public TimedMessagesMenu? TimedMessagesMenu { get; set; }
        public ConfigurationMenu? ConfigurationMenu { get; set; }
        public DebugMenu? DebugMenu { get; set; }

        public License(string name)
        {
            Name = name;
        }
    }

    public class MenuManager : MonoBehaviour
    {
        private static MenuManager? instance;
        private readonly Dictionary<string, License> licenses = new();

        private enum PanelType
        {
            Main,
            Status,
            NotificationSettings,
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
            { PanelType.NotificationSettings, new(new Vector2(200, 300), new Vector2(200, 300), Vector2.zero, Vector3.zero) },
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

        public static MenuManager Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new("TwitchChatModMenuManager");
                    instance = go.AddComponent<MenuManager>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        public MenuManager()
        {
            // Initialize licenses
            foreach (string licenseName in new[] {
                "LicenseTrainDriver",
                "LicenseShunting",
                "LicenseLocomotiveDE2",
                "LicenseMuseumCitySouth",
                "LicenseFreightHaul",
                "LicenseDispatcher1"
            })
            {
                licenses.Add(licenseName, new License(licenseName));
            }
        }

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

                        license.MenuCanvas!.SetActive(true);
                    }
                }

                if (license.LicenseObject != null)
                {
                    bool isLicenseActive = IsLicenseActive(license.LicenseObject);
                    if (license.MenuCanvas != null)
                    {
                        license.MenuCanvas.SetActive(isLicenseActive);
                    }

                    if (isLicenseActive)
                    {
                        UpdateMenuValues(license);
                        HandleLicenseAttachment(license);
                        HandlePaperVisibility(license);
                    }
                }
            }
        }

        private void UpdateMenuValues(License license)
        {
            license.StatusMenu?.UpdateStatusMenuValues();
            license.StandardMessagesMenu?.UpdateStandardMessagesMenuValues();
            license.CommandMessagesMenu?.UpdateCommandMessagesMenuValues();
            // Add other menu updates as needed
        }

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

        public void OnMenuButtonClicked(string menuName, License license)
        {
            Main.LogEntry("OnMenuButtonClicked", $"Menu button clicked: {menuName} for license: {license.Name}");
            ShowPanel(menuName, license);
        }

        private void CreateMenuCanvas(License license)
        {
            string methodName = MethodBase.GetCurrentMethod().Name;
            Main.LogEntry(methodName, $"Creating menu UI elements for license {license.Name}");

            if (license.MenuCanvas != null)
            {
                Main.LogEntry(methodName, $"Menu for {license.Name} already exists, destroying old instance");
                Destroy(license.MenuCanvas);
            }

            // Create canvas and setup basic components
            license.MenuCanvas = new GameObject($"MenuCanvas_{license.Name}");
            if (license.MenuCanvas == null)
                throw new InvalidOperationException($"Menu canvas {license.Name} is null after creation");

            Canvas canvas = license.MenuCanvas!.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 1000;
            
            RectTransform canvasRect = license.MenuCanvas!.GetComponent<RectTransform>();
            canvasRect.sizeDelta = panelConfigs[PanelType.Main].CanvasSize;
            canvasRect.localScale = Vector3.one * 0.001f;

            license.MenuCanvas!.AddComponent<GraphicRaycaster>();

            // Create panel
            GameObject menuPanel = new("MenuPanel");
            menuPanel.transform.SetParent(license.MenuCanvas!.transform, false);
            RectTransform panelRect = menuPanel.AddComponent<RectTransform>();
            panelRect.sizeDelta = panelConfigs[PanelType.Main].PanelSize;
            panelRect.anchorMin = new Vector2(0f, 1f);
            panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.localPosition = panelConfigs[PanelType.Main].PanelPosition;
            panelRect.localRotation = Quaternion.Euler(panelConfigs[PanelType.Main].PanelRotationOffset);

            // Create all panels - ensure they start hidden
            license.MainMenu = new MainMenu(menuPanel.transform, license);
            license.MainMenu?.Hide();
            
            license.StatusMenu = new StatusMenu(menuPanel.transform);
            license.StatusMenu?.Hide();
            
            license.NotificationMenu = new NotificationMenu(menuPanel.transform);
            license.NotificationMenu?.Hide();
            
            license.LargeDisplayBoard = new LargeDisplayBoard(menuPanel.transform);
            license.LargeDisplayBoard?.Hide();
            
            license.MediumDisplayBoard = new MediumDisplayBoard(menuPanel.transform);
            license.MediumDisplayBoard?.Hide();
            
            license.WideDisplayBoard = new WideDisplayBoard(menuPanel.transform);
            license.WideDisplayBoard?.Hide();
            
            license.SmallDisplayBoard = new SmallDisplayBoard(menuPanel.transform);
            license.SmallDisplayBoard?.Hide();
            
            license.StandardMessagesMenu = new StandardMessagesMenu(menuPanel.transform);
            license.StandardMessagesMenu?.Hide();
            
            license.CommandMessagesMenu = new CommandMessagesMenu(menuPanel.transform);
            license.CommandMessagesMenu?.Hide();
            
            license.TimedMessagesMenu = new TimedMessagesMenu(menuPanel.transform);
            license.TimedMessagesMenu?.Hide();
            
            license.ConfigurationMenu = new ConfigurationMenu(menuPanel.transform);
            license.ConfigurationMenu?.Hide();
            
            license.DebugMenu = new DebugMenu(menuPanel.transform);
            license.DebugMenu?.Hide();

            // Wire up back button events
            if (license.StatusMenu != null) license.StatusMenu!.OnBackButtonClicked += () => ShowPanel("Main", license);
            if (license.NotificationMenu != null) license.NotificationMenu!.OnBackButtonClicked += () => ShowPanel("Main", license);
            if (license.LargeDisplayBoard != null) license.LargeDisplayBoard!.OnBackButtonClicked += () => ShowPanel("Main", license);
            if (license.MediumDisplayBoard != null) license.MediumDisplayBoard!.OnBackButtonClicked += () => ShowPanel("Main", license);
            if (license.WideDisplayBoard != null) license.WideDisplayBoard!.OnBackButtonClicked += () => ShowPanel("Main", license);
            if (license.SmallDisplayBoard != null) license.SmallDisplayBoard!.OnBackButtonClicked += () => ShowPanel("Main", license);
            if (license.StandardMessagesMenu != null) license.StandardMessagesMenu!.OnBackButtonClicked += () => ShowPanel("Main", license);
            if (license.CommandMessagesMenu != null) license.CommandMessagesMenu!.OnBackButtonClicked += () => ShowPanel("Main", license);
            if (license.TimedMessagesMenu != null) license.TimedMessagesMenu!.OnBackButtonClicked += () => ShowPanel("Main", license);
            if (license.ConfigurationMenu != null) license.ConfigurationMenu!.OnBackButtonClicked += () => ShowPanel("Main", license);
            if (license.DebugMenu != null) license.DebugMenu!.OnBackButtonClicked += () => ShowPanel("Main", license);

            // Show the saved panel or default to main menu
            string panelToShow = !string.IsNullOrEmpty(Settings.Instance.activePanels[0]) 
                ? Settings.Instance.activePanels[0] 
                : "Main";
            ShowPanel(panelToShow, license);

            // Initially disable the canvas until the license is found
            license.MenuCanvas!.SetActive(false);
        }

        private void HideAllPanels(License license)
        {
            license.MainMenu?.Hide();
            license.StatusMenu?.Hide();
            license.NotificationMenu?.Hide();
            license.LargeDisplayBoard?.Hide();
            license.MediumDisplayBoard?.Hide();
            license.WideDisplayBoard?.Hide();
            license.SmallDisplayBoard?.Hide();
            license.StandardMessagesMenu?.Hide();
            license.CommandMessagesMenu?.Hide();
            license.TimedMessagesMenu?.Hide();
            license.ConfigurationMenu?.Hide();
            license.DebugMenu?.Hide();
        }

        private void ShowPanel(string panelName, License license)
        {
            if (license.MenuCanvas == null || !license.MenuCanvas!.activeSelf)
                return;
            
            Main.LogEntry("ShowMenu", $"Showing panel {panelName} for license {license.Name}");

            HideAllPanels(license);

            PanelType panelType = panelName switch
            {
                "Main" => PanelType.Main,
                "Status" => PanelType.Status,
                "Notification Settings" => PanelType.NotificationSettings,
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
                    license.MainMenu?.Show();
                    break;
                case PanelType.Status:
                    license.StatusMenu?.Show();
                    break;
                case PanelType.NotificationSettings:
                    license.NotificationMenu?.Show();
                    break;
                case PanelType.LargeDisplay:
                    license.LargeDisplayBoard?.Show();
                    break;
                case PanelType.MediumDisplay:
                    license.MediumDisplayBoard?.Show();
                    break;
                case PanelType.WideDisplay:
                    license.WideDisplayBoard?.Show();
                    break;
                case PanelType.SmallDisplay:
                    license.SmallDisplayBoard?.Show();
                    break;
                case PanelType.StandardMessages:
                    license.StandardMessagesMenu?.Show();
                    break;
                case PanelType.CommandMessages:
                    license.CommandMessagesMenu?.Show();
                    break;
                case PanelType.TimedMessages:
                    license.TimedMessagesMenu?.Show();
                    break;
                case PanelType.Configuration:
                    license.ConfigurationMenu?.Show();
                    break;
                case PanelType.Debug:
                    license.DebugMenu?.Show();
                    break;
            }

            // Save the active panel state
            Settings.Instance.activePanels[0] = panelName;
            Settings.Instance.Save(Main.ModEntry);
            Main.LogEntry("ShowPanel", $"Saving active panel state for license {license.Name}: {panelName}");
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

        public void AddMessageToDisplayBoards(string username, string message)
        {
            // TODO: Add message to display boards
        }
    }
}
