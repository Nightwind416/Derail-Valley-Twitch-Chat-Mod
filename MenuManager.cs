using UnityEngine;
using UnityEngine.UI;
using System.Reflection;
using TwitchChat.Menus;
using System.Collections.Generic;
using System;

namespace TwitchChat
{
    public class MenuManager : MonoBehaviour
    {
        private static MenuManager? instance;
        public static GameObject?[] menuCanvases = new GameObject?[6];
        private readonly bool[] attachedToStickyTape = new bool[6];
        private readonly GameObject?[] stickyTapeBases = new GameObject?[6];
        private readonly string[] licenseNames =
        [
            "LicenseTrainDriver",
            "LicenseShunting",
            "LicenseLocomotiveDE2",
            "LicenseMuseumCitySouth",
            "LicenseFreightHaul",
            "LicenseDispatcher1"
        ];
        private readonly GameObject?[] licenseObjects = new GameObject?[6];

        private MainMenu?[] mainMenus = new MainMenu?[6];
        private StatusMenu?[] statusMenus = new StatusMenu?[6];
        private NotificationMenu?[] notificationSettingsMenus = new NotificationMenu?[6];
        private LargeDisplayBoard?[] largeDisplayBoards = new LargeDisplayBoard?[6];
        private MediumDisplayBoard?[] mediumDisplayBoards = new MediumDisplayBoard?[6];
        private WideDisplayBoard?[] wideDisplayBoards = new WideDisplayBoard?[6];
        private SmallDisplayBoard?[] smallDisplayBoards = new SmallDisplayBoard?[6];
        private StandardMessagesMenu?[] StandardMessagesMenus = new StandardMessagesMenu?[6];
        private CommandMessagesMenu?[] CommandMessagesMenus = new CommandMessagesMenu?[6];
        private TimedMessagesMenu?[] TimedMessagesMenus = new TimedMessagesMenu?[6];
        private ConfigurationMenu?[] configurationMenus = new ConfigurationMenu?[6];
        private DebugMenu?[] debugMenus = new DebugMenu?[6];

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

        private void Update()
        {
            string methodName = MethodBase.GetCurrentMethod().Name;

            for (int i = 0; i < licenseObjects.Length; i++)
            {
                if (licenseObjects[i] == null)
                {
                    licenseObjects[i] = GameObject.Find(licenseNames[i]);
                    if (licenseObjects[i] != null)
                    {
                        string licenseName = licenseObjects[i]!.name;
                        Main.LogEntry(methodName, $"Attaching menu canvas {i + 1} to {licenseName}");
                        
                        if (menuCanvases[i] == null)
                        {
                            Main.LogEntry(methodName, $"Menu canvas {i + 1} was null, creating...");
                            CreateMenuCanvas(i);
                        }

                        menuCanvases[i]!.SetActive(true);
                    }
                }

                if (licenseObjects[i] != null)
                {
                    bool isLicenseActive = IsLicenseActive(licenseObjects[i]);
                    if (menuCanvases[i] != null)
                    {
                        menuCanvases[i]!.SetActive(isLicenseActive);
                    }

                    if (isLicenseActive)
                    {
                        // Update menu values
                        statusMenus[i]?.UpdateStatusMenuValues();
                        // notificationSettingsMenus[i]?.UpdateNotificationSettingsMenuValues();
                        // largeDisplayBoards[i]?.UpdateLargeDisplayBoardValues();
                        // mediumDisplayBoards[i]?.UpdateMediumDisplayBoardValues();
                        // wideDisplayBoards[i]?.UpdateWideDisplayBoardValues();
                        // smallDisplayBoards[i]?.UpdateSmallDisplayBoardValues();
                        StandardMessagesMenus[i]?.UpdateStandardMessagesMenuValues();
                        CommandMessagesMenus[i]?.UpdateCommandMessagesMenuValues();
                        // TimedMessagesMenus[i]?.UpdateTimedMessagesMenuValues();
                        // configurationMenus[i]?.UpdateConfigurationMenuValues();
                        // debugMenus[i]?.UpdateDebugMenuValues();

                        HandleLicenseAttachment(i);
                        HandlePaperVisibility(i);
                    }
                }
            }
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
        private void HandleLicenseAttachment(int index)
        {
            // Bounds checking for all arrays
            if (index < 0 || index >= licenseNames.Length || 
                index >= attachedToStickyTape.Length ||
                index >= stickyTapeBases.Length ||
                licenseObjects[index] == null)
            {
                Main.LogEntry("MenuManager.HandleLicenseAttachment", $"Invalid index or null object: {index}");
                return;
            }

            try
            {
                Transform current = licenseObjects[index]!.transform;
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

                if (currentlyAttached != attachedToStickyTape[index])
                {
                    attachedToStickyTape[index] = currentlyAttached;
                    Main.LogEntry("HandleLicenseAttachment", $"License {index + 1} attachment to sticky tape changed: {attachedToStickyTape[index]}");
                    
                    if (attachedToStickyTape[index] && newStickyTapeBase != null)
                    {
                        stickyTapeBases[index] = newStickyTapeBase;
                        stickyTapeBases[index]!.SetActive(false);
                    }
                    else if (!attachedToStickyTape[index] && stickyTapeBases[index] != null)
                    {
                        stickyTapeBases[index]!.SetActive(true);
                        stickyTapeBases[index] = null;
                    }
                }
            }
            catch (Exception e)
            {
                Main.LogEntry("MenuManager.HandleLicenseAttachment", $"Error: {e.Message}");
            }
        }

        private void HandlePaperVisibility(int index)
        {
            if (licenseObjects[index] != null)
            {
                Transform paperTransform = licenseObjects[index]!.transform.Find("Pivot/TempPaper(Clone)(Clone) 0/Paper");
                paperTransform?.gameObject.SetActive(false);
            }
        }

        private void LateUpdate()
        {
            for (int i = 0; i < licenseObjects.Length; i++)
            {
                if (licenseObjects[i] != null && menuCanvases[i] != null)
                {
                    PositionNearObject(licenseObjects[i], menuCanvases[i], i);
                }
            }
        }

        public void OnMenuButtonClicked(string menuName, int index)
        {
            Main.LogEntry("OnMenuButtonClicked", $"Menu button clicked: {menuName} for index: {index}");
            ShowPanel(menuName, index);
        }

        private void CreateMenuCanvas(int index)
        {
            string methodName = MethodBase.GetCurrentMethod().Name;
            Main.LogEntry(methodName, $"Creating menu UI elements for license {index + 1}");

            if (menuCanvases[index] != null)
            {
                Main.LogEntry(methodName, $"Menu {index + 1} already exists, destroying old instance");
                Destroy(menuCanvases[index]);
            }

            // Create canvas and setup basic components
            menuCanvases[index] = new GameObject($"MenuCanvas_{index + 1}");
            if (menuCanvases[index] == null)
                throw new InvalidOperationException($"Menu canvas {index} is null after creation");

            Canvas canvas = menuCanvases[index]!.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 1000;
            
            RectTransform canvasRect = menuCanvases[index]!.GetComponent<RectTransform>();
            canvasRect.sizeDelta = panelConfigs[PanelType.Main].CanvasSize;
            canvasRect.localScale = Vector3.one * 0.001f;

            menuCanvases[index]!.AddComponent<GraphicRaycaster>();

            // Create panel
            GameObject menuPanel = new("MenuPanel");
            menuPanel.transform.SetParent(menuCanvases[index]!.transform, false);
            RectTransform panelRect = menuPanel.AddComponent<RectTransform>();
            panelRect.sizeDelta = panelConfigs[PanelType.Main].PanelSize;
            panelRect.anchorMin = new Vector2(0f, 1f);
            panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.localPosition = panelConfigs[PanelType.Main].PanelPosition;
            panelRect.localRotation = Quaternion.Euler(panelConfigs[PanelType.Main].PanelRotationOffset);

            // Create all panels - ensure they start hidden
            mainMenus[index] = new MainMenu(menuPanel.transform, index);
            mainMenus[index]?.Hide();
            
            statusMenus[index] = new StatusMenu(menuPanel.transform);
            statusMenus[index]?.Hide();
            
            notificationSettingsMenus[index] = new NotificationMenu(menuPanel.transform);
            notificationSettingsMenus[index]?.Hide();
            
            largeDisplayBoards[index] = new LargeDisplayBoard(menuPanel.transform);
            largeDisplayBoards[index]?.Hide();
            
            mediumDisplayBoards[index] = new MediumDisplayBoard(menuPanel.transform);
            mediumDisplayBoards[index]?.Hide();
            
            wideDisplayBoards[index] = new WideDisplayBoard(menuPanel.transform);
            wideDisplayBoards[index]?.Hide();
            
            smallDisplayBoards[index] = new SmallDisplayBoard(menuPanel.transform);
            smallDisplayBoards[index]?.Hide();
            
            StandardMessagesMenus[index] = new StandardMessagesMenu(menuPanel.transform);
            StandardMessagesMenus[index]?.Hide();
            
            CommandMessagesMenus[index] = new CommandMessagesMenu(menuPanel.transform);
            CommandMessagesMenus[index]?.Hide();
            
            TimedMessagesMenus[index] = new TimedMessagesMenu(menuPanel.transform);
            TimedMessagesMenus[index]?.Hide();
            
            configurationMenus[index] = new ConfigurationMenu(menuPanel.transform);
            configurationMenus[index]?.Hide();
            
            debugMenus[index] = new DebugMenu(menuPanel.transform);
            debugMenus[index]?.Hide();

            // Wire up back button events
            if (statusMenus[index] != null) statusMenus[index]!.OnBackButtonClicked += () => ShowPanel("Main", index);
            if (notificationSettingsMenus[index] != null) notificationSettingsMenus[index]!.OnBackButtonClicked += () => ShowPanel("Main", index);
            if (largeDisplayBoards[index] != null) largeDisplayBoards[index]!.OnBackButtonClicked += () => ShowPanel("Main", index);
            if (mediumDisplayBoards[index] != null) mediumDisplayBoards[index]!.OnBackButtonClicked += () => ShowPanel("Main", index);
            if (wideDisplayBoards[index] != null) wideDisplayBoards[index]!.OnBackButtonClicked += () => ShowPanel("Main", index);
            if (smallDisplayBoards[index] != null) smallDisplayBoards[index]!.OnBackButtonClicked += () => ShowPanel("Main", index);
            if (StandardMessagesMenus[index] != null) StandardMessagesMenus[index]!.OnBackButtonClicked += () => ShowPanel("Main", index);
            if (CommandMessagesMenus[index] != null) CommandMessagesMenus[index]!.OnBackButtonClicked += () => ShowPanel("Main", index);
            if (TimedMessagesMenus[index] != null) TimedMessagesMenus[index]!.OnBackButtonClicked += () => ShowPanel("Main", index);
            if (configurationMenus[index] != null) configurationMenus[index]!.OnBackButtonClicked += () => ShowPanel("Main", index);
            if (debugMenus[index] != null) debugMenus[index]!.OnBackButtonClicked += () => ShowPanel("Main", index);

            // Show the saved panel or default to main menu
            string panelToShow = !string.IsNullOrEmpty(Settings.Instance.activePanels[index]) 
                ? Settings.Instance.activePanels[index] 
                : "Main";
            ShowPanel(panelToShow, index);

            // Initially disable the canvas until the license is found
            menuCanvases[index]!.SetActive(false);
        }

        private void HideAllPanels(int index)
        {
            mainMenus[index]?.Hide();
            statusMenus[index]?.Hide();
            notificationSettingsMenus[index]?.Hide();
            largeDisplayBoards[index]?.Hide();
            mediumDisplayBoards[index]?.Hide();
            wideDisplayBoards[index]?.Hide();
            smallDisplayBoards[index]?.Hide();
            StandardMessagesMenus[index]?.Hide();
            CommandMessagesMenus[index]?.Hide();
            TimedMessagesMenus[index]?.Hide();
            configurationMenus[index]?.Hide();
            debugMenus[index]?.Hide();
        }

        private void ShowPanel(string panelName, int index)
        {
            if (menuCanvases[index] == null || !menuCanvases[index]!.activeSelf)
                return;
            
            Main.LogEntry("ShowMenu", $"Showing panel {panelName} for license {index + 1}");

            HideAllPanels(index);

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
            var menuPanel = menuCanvases[index]!.transform.Find("MenuPanel");
            if (menuPanel != null)
            {
                RectTransform canvasRect = menuCanvases[index]!.GetComponent<RectTransform>();
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
                    mainMenus[index]?.Show();
                    break;
                case PanelType.Status:
                    statusMenus[index]?.Show();
                    break;
                case PanelType.NotificationSettings:
                    notificationSettingsMenus[index]?.Show();
                    break;
                case PanelType.LargeDisplay:
                    largeDisplayBoards[index]?.Show();
                    break;
                case PanelType.MediumDisplay:
                    mediumDisplayBoards[index]?.Show();
                    break;
                case PanelType.WideDisplay:
                    wideDisplayBoards[index]?.Show();
                    break;
                case PanelType.SmallDisplay:
                    smallDisplayBoards[index]?.Show();
                    break;
                case PanelType.StandardMessages:
                    StandardMessagesMenus[index]?.Show();
                    break;
                case PanelType.CommandMessages:
                    CommandMessagesMenus[index]?.Show();
                    break;
                case PanelType.TimedMessages:
                    TimedMessagesMenus[index]?.Show();
                    break;
                case PanelType.Configuration:
                    configurationMenus[index]?.Show();
                    break;
                case PanelType.Debug:
                    debugMenus[index]?.Show();
                    break;
            }

            // Save the active menu state
            Settings.Instance.activePanels[index] = panelName;
            Settings.Instance.Save(Main.ModEntry);
            Main.LogEntry("ShowMenu", $"Saving active menu state for license {index + 1}: {panelName}");
        }

        private void PositionNearObject(GameObject licenseObject, GameObject menuCanvas, int index)
        {
            if (menuCanvas == null)
            {
                Main.LogEntry("MenuManager.PositionNearObject", "Menu Canvas is not initialized.");
                return;
            }

            if (licenseObject == null) return;

            Vector3 targetPosition = licenseObject.transform.position;
            menuCanvas.transform.position = targetPosition;

            menuCanvas.transform.rotation = licenseObject.transform.rotation * 
                                         Quaternion.Euler(90f, 180f, 0f) * 
                                         Quaternion.Euler(panelConfigs[PanelType.Main].PanelRotationOffset);
        }

        public void AddMessageToDisplayBoards(string username, string message)
        {
            // TODO: Add message to display boards
        }
    }
}
