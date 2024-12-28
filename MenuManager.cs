using UnityEngine;
using UnityEngine.UI;
using System.Reflection;
using TwitchChat.Menus;
using System.Collections.Generic;

namespace TwitchChat
{
    public class MenuManager : MonoBehaviour
    {
        private static MenuManager? instance;
        private static GameObject?[] menuCanvases = new GameObject?[5];
        private readonly bool[] paperVisible = [true, true, true, true, true];
        private readonly bool[] attachedToStickyTape = [false, false, false, false, false];
        private readonly GameObject?[] stickyTapeBases = new GameObject?[5];
        private readonly string[] licenseNames =
        [
            "LicenseTrainDriver",
            "LicenseShunting",
            "LicenseLocomotiveDE2",
            "LicenseMuseumCitySouth",
            "LicenseFreightHaul"
        ];
        private readonly GameObject?[] licenseObjects = new GameObject?[5];

        private MainMenu?[] mainMenus = new MainMenu?[5];
        private StatusMenu?[] statusMenus = new StatusMenu?[5];
        private SettingsMenu?[] settingsMenus = new SettingsMenu?[5];
        private StandardMessagesMenu?[] standardMessagesMenus = new StandardMessagesMenu?[5];
        private CommandMessagesMenu?[] commandMessagesMenus = new CommandMessagesMenu?[5];
        private CustomCommandsMenu?[] customCommandsMenus = new CustomCommandsMenu?[5];
        private TimedMessagesMenu?[] timedMessagesMenus = new TimedMessagesMenu?[5];
        private DispatcherMessagesMenu?[] dispatcherMessagesMenus = new DispatcherMessagesMenu?[5];
        private DebugMenu?[] debugMenus = new DebugMenu?[5];

        private enum MenuType
        {
            Main,
            Status,
            Settings,
            StandardMessages,
            CommandMessages,
            CustomCommands,
            TimedMessages,
            DispatcherMessages,
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

        private readonly Dictionary<MenuType, PanelConfig> menuConfigs = new()
        {
            { MenuType.Main, new(new Vector2(200, 300), new Vector2(200, 300), Vector2.zero, Vector3.zero) },
            { MenuType.Status, new(new Vector2(200, 300), new Vector2(200, 300), Vector2.zero, Vector3.zero) },
            { MenuType.Settings, new(new Vector2(200, 300), new Vector2(200, 300), Vector2.zero, Vector3.zero) },
            { MenuType.StandardMessages, new(new Vector2(200, 300), new Vector2(200, 300), Vector2.zero, Vector3.zero) },
            { MenuType.CommandMessages, new(new Vector2(200, 300), new Vector2(200, 300), Vector2.zero, Vector3.zero) },
            { MenuType.CustomCommands, new(new Vector2(200, 300), new Vector2(200, 300), Vector2.zero, Vector3.zero) },
            { MenuType.TimedMessages, new(new Vector2(200, 300), new Vector2(200, 300), Vector2.zero, Vector3.zero) },
            { MenuType.DispatcherMessages, new(new Vector2(200, 300), new Vector2(200, 300), Vector2.zero, Vector3.zero) },
            { MenuType.Debug, new(new Vector2(200, 300), new Vector2(200, 300), Vector2.zero, Vector3.zero) }
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
                        Main.LogEntry(methodName, $"Attaching menu to license {i + 1}");
                        
                        if (menuCanvases[i] == null)
                        {
                            Main.LogEntry(methodName, $"Menu canvas {i + 1} was null, creating...");
                            CreateCanvas(i);
                        }

                        menuCanvases[i]!.SetActive(true);
                    }
                }

                if (licenseObjects[i] != null)
                {
                    settingsMenus[i]?.UpdateDisplayedValues(
                        Settings.Instance.twitchUsername,
                        Settings.Instance.messageDuration,
                        WebSocketManager.LastChatMessage
                    );

                    HandleLicenseAttachment(i);
                    HandlePaperVisibility(i);
                }
            }
        }

        private void HandleLicenseAttachment(int index)
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

        private void HandlePaperVisibility(int index)
        {
            if (paperVisible[index])
            {
                Transform paperTransform = licenseObjects[index]!.transform.Find("Pivot/TempPaper(Clone)(Clone) 0/Paper");
                if (paperTransform != null)
                {
                    paperTransform.gameObject.SetActive(false);
                    paperVisible[index] = false;
                }
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

        private void CreateCanvas(int index)
        {
            string methodName = MethodBase.GetCurrentMethod().Name;
            Main.LogEntry(methodName, $"Creating menu UI elements for license {index + 1}");

            if (menuCanvases[index] != null)
            {
                Main.LogEntry(methodName, $"Menu {index + 1} already exists, destroying old instance");
                Destroy(menuCanvases[index]);
            }

            menuCanvases[index] = new GameObject($"MenuCanvas_{index + 1}");
            if (menuCanvases[index] == null)
                throw new System.InvalidOperationException($"Menu canvas {index} is null after creation");

            Canvas canvas = menuCanvases[index]!.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 1000;
            
            RectTransform canvasRect = menuCanvases[index]!.GetComponent<RectTransform>();
            canvasRect.sizeDelta = menuConfigs[MenuType.Main].CanvasSize; // Start with main menu size
            canvasRect.localScale = Vector3.one * 0.001f;

            menuCanvases[index]!.AddComponent<GraphicRaycaster>();

            // Create a panel to contain all menus
            GameObject menuPanel = new GameObject("MenuPanel");
            menuPanel.transform.SetParent(menuCanvases[index]!.transform, false);
            RectTransform panelRect = menuPanel.AddComponent<RectTransform>();
            panelRect.sizeDelta = menuConfigs[MenuType.Main].PanelSize;
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.localPosition = menuConfigs[MenuType.Main].PanelPosition;
            panelRect.localRotation = Quaternion.Euler(menuConfigs[MenuType.Main].PanelRotationOffset);

            // Create all menus for this instance - now parenting to panel instead of canvas
            mainMenus[index] = new MainMenu(menuPanel.transform);
            statusMenus[index] = new StatusMenu(menuPanel.transform);
            settingsMenus[index] = new SettingsMenu(menuPanel.transform);
            standardMessagesMenus[index] = new StandardMessagesMenu(menuPanel.transform);
            commandMessagesMenus[index] = new CommandMessagesMenu(menuPanel.transform);
            customCommandsMenus[index] = new CustomCommandsMenu(menuPanel.transform);
            timedMessagesMenus[index] = new TimedMessagesMenu(menuPanel.transform);
            dispatcherMessagesMenus[index] = new DispatcherMessagesMenu(menuPanel.transform);
            debugMenus[index] = new DebugMenu(menuPanel.transform);

            // Hide all menus first
            HideAllMenus(index);
            
            // Show only the main menu
            mainMenus[index]?.Show();

            // Setup menu navigation events
            mainMenus[index]!.OnMenuButtonClicked += (menuName) => ShowMenu(menuName, index);
            statusMenus[index]!.OnBackButtonClicked += () => ShowMenu("Main", index);
            settingsMenus[index]!.OnBackButtonClicked += () => ShowMenu("Main", index);
            standardMessagesMenus[index]!.OnBackButtonClicked += () => ShowMenu("Main", index);
            commandMessagesMenus[index]!.OnBackButtonClicked += () => ShowMenu("Main", index);
            customCommandsMenus[index]!.OnBackButtonClicked += () => ShowMenu("Main", index);
            timedMessagesMenus[index]!.OnBackButtonClicked += () => ShowMenu("Main", index);
            dispatcherMessagesMenus[index]!.OnBackButtonClicked += () => ShowMenu("Main", index);
            debugMenus[index]!.OnBackButtonClicked += () => ShowMenu("Main", index);

            // Initially disable the canvas until the license is found
            menuCanvases[index]!.SetActive(false);
        }

        private void HideAllMenus(int index)
        {
            mainMenus[index]?.Hide();
            statusMenus[index]?.Hide();
            settingsMenus[index]?.Hide();
            standardMessagesMenus[index]?.Hide();
            commandMessagesMenus[index]?.Hide();
            customCommandsMenus[index]?.Hide();
            timedMessagesMenus[index]?.Hide();
            dispatcherMessagesMenus[index]?.Hide();
            debugMenus[index]?.Hide();
        }

        private void ShowMenu(string menuName, int index)
        {
            if (menuCanvases[index] == null || !menuCanvases[index]!.activeSelf)
                return;

            HideAllMenus(index);

            MenuType menuType = menuName switch
            {
                "Main" => MenuType.Main,
                "Status" => MenuType.Status,
                "Settings" => MenuType.Settings,
                "Standard Messages" => MenuType.StandardMessages,
                "Command Messages" => MenuType.CommandMessages,
                "Custom Commands" => MenuType.CustomCommands,
                "Timed Messages" => MenuType.TimedMessages,
                "Dispatcher Messages" => MenuType.DispatcherMessages,
                "Debug" => MenuType.Debug,
                _ => MenuType.Main
            };

            // Apply the configuration for this menu type
            var config = menuConfigs[menuType];
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

            // Show the selected menu
            switch (menuType)
            {
                case MenuType.Main:
                    mainMenus[index]?.Show();
                    break;
                case MenuType.Status:
                    statusMenus[index]?.Show();
                    break;
                case MenuType.Settings:
                    settingsMenus[index]?.Show();
                    break;
                case MenuType.StandardMessages:
                    standardMessagesMenus[index]?.Show();
                    break;
                case MenuType.CommandMessages:
                    commandMessagesMenus[index]?.Show();
                    break;
                case MenuType.CustomCommands:
                    customCommandsMenus[index]?.Show();
                    break;
                case MenuType.TimedMessages:
                    timedMessagesMenus[index]?.Show();
                    break;
                case MenuType.DispatcherMessages:
                    dispatcherMessagesMenus[index]?.Show();
                    break;
                case MenuType.Debug:
                    debugMenus[index]?.Show();
                    break;
            }
        }

        private void PositionNearObject(GameObject licenseObject, GameObject menuCanvas, int index)
        {
            if (menuCanvas == null)
            {
                Debug.LogWarning("Menu Canvas is not initialized.");
                return;
            }

            if (licenseObject == null) return;

            Vector3 targetPosition = licenseObject.transform.position;
            menuCanvas.transform.position = targetPosition;

            menuCanvas.transform.rotation = licenseObject.transform.rotation * 
                                         Quaternion.Euler(90f, 180f, 0f) * 
                                         Quaternion.Euler(menuConfigs[MenuType.Main].PanelRotationOffset);
        }
    }
}
