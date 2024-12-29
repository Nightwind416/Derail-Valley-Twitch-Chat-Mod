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
        private LargeDisplayBoard?[] largeDisplayBoards = new LargeDisplayBoard?[5];
        private MediumDisplayBoard?[] mediumDisplayBoards = new MediumDisplayBoard?[5];
        private WideDisplayBoard?[] wideDisplayBoards = new WideDisplayBoard?[5];
        private SmallDisplayBoard?[] smallDisplayBoards = new SmallDisplayBoard?[5];
        private DebugMenu?[] debugMenus = new DebugMenu?[5];

        private enum MenuType
        {
            Main,
            Status,
            Settings,
            LargeDisplay,
            MediumDisplay,
            WideDisplay,
            SmallDisplay,
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
            { MenuType.LargeDisplay, new(new Vector2(1200, 650), new Vector2(1200, 650), Vector2.zero, Vector3.zero) },
            { MenuType.MediumDisplay, new(new Vector2(500, 500), new Vector2(500, 500), Vector2.zero, Vector3.zero) },
            { MenuType.WideDisplay, new(new Vector2(900, 200), new Vector2(900, 200), Vector2.zero, Vector3.zero) },
            { MenuType.SmallDisplay, new(new Vector2(200, 300), new Vector2(200, 300), Vector2.zero, Vector3.zero) },
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
                    
                    // Add this line to update the status menu
                    statusMenus[i]?.Update();

                    HandleLicenseAttachment(i);
                    HandlePaperVisibility(i);
                }
            }
        }
        // BUG: Sticky tape returns after away from loco, detact/reattach fixes it
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
            if (licenseObjects[index] != null)
            {
                Transform paperTransform = licenseObjects[index]!.transform.Find("Pivot/TempPaper(Clone)(Clone) 0/Paper");
                if (paperTransform != null)
                {
                    paperTransform.gameObject.SetActive(false);
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

        public void OnMenuButtonClicked(string menuName, int index)
        {
            Main.LogEntry("OnMenuButtonClicked", $"Menu button clicked: {menuName} for index: {index}");
            ShowMenu(menuName, index);
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
            // Change anchor points to top-left (0,1)
            panelRect.anchorMin = new Vector2(0f, 1f);
            panelRect.anchorMax = new Vector2(0f, 1f);
            // Keep pivot in center (0.5,0.5) for rotation around center
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.localPosition = menuConfigs[MenuType.Main].PanelPosition;
            panelRect.localRotation = Quaternion.Euler(menuConfigs[MenuType.Main].PanelRotationOffset);

            // Create all menus for this instance - now parenting to panel instead of canvas
            mainMenus[index] = new MainMenu(menuPanel.transform, index);
            statusMenus[index] = new StatusMenu(menuPanel.transform);
            settingsMenus[index] = new SettingsMenu(menuPanel.transform);
            largeDisplayBoards[index] = new LargeDisplayBoard(menuPanel.transform);
            mediumDisplayBoards[index] = new MediumDisplayBoard(menuPanel.transform);
            wideDisplayBoards[index] = new WideDisplayBoard(menuPanel.transform);
            smallDisplayBoards[index] = new SmallDisplayBoard(menuPanel.transform);
            debugMenus[index] = new DebugMenu(menuPanel.transform);

            // Wire up back button events
            if (statusMenus[index] != null) statusMenus[index]!.OnBackButtonClicked += () => ShowMenu("Main", index);
            if (settingsMenus[index] != null) settingsMenus[index]!.OnBackButtonClicked += () => ShowMenu("Main", index);
            if (largeDisplayBoards[index] != null) largeDisplayBoards[index]!.OnBackButtonClicked += () => ShowMenu("Main", index);
            if (mediumDisplayBoards[index] != null) mediumDisplayBoards[index]!.OnBackButtonClicked += () => ShowMenu("Main", index);
            if (wideDisplayBoards[index] != null) wideDisplayBoards[index]!.OnBackButtonClicked += () => ShowMenu("Main", index);
            if (smallDisplayBoards[index] != null) smallDisplayBoards[index]!.OnBackButtonClicked += () => ShowMenu("Main", index);
            if (debugMenus[index] != null) debugMenus[index]!.OnBackButtonClicked += () => ShowMenu("Main", index);

            // Hide all menus first
            HideAllMenus(index);
            
            // Show the saved menu instead of defaulting to main menu
            ShowMenu(Settings.Instance.activeMenus[index], index);

            // Initially disable the canvas until the license is found
            menuCanvases[index]!.SetActive(false);
        }

        private void HideAllMenus(int index)
        {
            mainMenus[index]?.Hide();
            statusMenus[index]?.Hide();
            settingsMenus[index]?.Hide();
            largeDisplayBoards[index]?.Hide();
            mediumDisplayBoards[index]?.Hide();
            wideDisplayBoards[index]?.Hide();
            smallDisplayBoards[index]?.Hide();
            debugMenus[index]?.Hide();
        }

        private void ShowMenu(string menuName, int index)
        {
            if (menuCanvases[index] == null || !menuCanvases[index]!.activeSelf)
                return;
            
            Main.LogEntry("ShowMenu", $"Showing menu {menuName} for license {index + 1}");

            HideAllMenus(index);

            MenuType menuType = menuName switch
            {
                "Main" => MenuType.Main,
                "Status" => MenuType.Status,
                "Settings" => MenuType.Settings,
                "Large Display" => MenuType.LargeDisplay,
                "Medium Display" => MenuType.MediumDisplay,
                "Wide Display" => MenuType.WideDisplay,
                "Small Display" => MenuType.SmallDisplay,
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
                case MenuType.LargeDisplay:
                    largeDisplayBoards[index]?.Show();
                    break;
                case MenuType.MediumDisplay:
                    mediumDisplayBoards[index]?.Show();
                    break;
                case MenuType.WideDisplay:
                    wideDisplayBoards[index]?.Show();
                    break;
                case MenuType.SmallDisplay:
                    smallDisplayBoards[index]?.Show();
                    break;
                case MenuType.Debug:
                    debugMenus[index]?.Show();
                    break;
            }

            // Save the active menu state
            Settings.Instance.activeMenus[index] = menuName;
            Settings.Instance.Save(Main.ModEntry);
            Main.LogEntry("ShowMenu", $"Saving active menu state for license {index + 1}: {menuName}");
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
