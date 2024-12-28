using UnityEngine;
using UnityEngine.UI;
using System.Reflection;
using TwitchChat.Menus;

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
                    PositionNearObject(licenseObjects[i], menuCanvases[i]);
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
            canvasRect.sizeDelta = new Vector2(200, 300);
            canvasRect.localScale = Vector3.one * 0.001f;

            menuCanvases[index]!.AddComponent<GraphicRaycaster>();

            // Create all menus for this instance
            mainMenus[index] = new MainMenu(menuCanvases[index]!.transform);
            statusMenus[index] = new StatusMenu(menuCanvases[index]!.transform);
            settingsMenus[index] = new SettingsMenu(menuCanvases[index]!.transform);
            standardMessagesMenus[index] = new StandardMessagesMenu(menuCanvases[index]!.transform);
            commandMessagesMenus[index] = new CommandMessagesMenu(menuCanvases[index]!.transform);
            customCommandsMenus[index] = new CustomCommandsMenu(menuCanvases[index]!.transform);
            timedMessagesMenus[index] = new TimedMessagesMenu(menuCanvases[index]!.transform);
            dispatcherMessagesMenus[index] = new DispatcherMessagesMenu(menuCanvases[index]!.transform);
            debugMenus[index] = new DebugMenu(menuCanvases[index]!.transform);

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

            switch (menuName)
            {
                case "Main":
                    mainMenus[index]?.Show();
                    break;
                case "Status":
                    statusMenus[index]?.Show();
                    break;
                case "Settings":
                    settingsMenus[index]?.Show();
                    break;
                case "Standard Messages":
                    standardMessagesMenus[index]?.Show();
                    break;
                case "Command Messages":
                    commandMessagesMenus[index]?.Show();
                    break;
                case "Custom Commands":
                    customCommandsMenus[index]?.Show();
                    break;
                case "Timed Messages":
                    timedMessagesMenus[index]?.Show();
                    break;
                case "Dispatcher Messages":
                    dispatcherMessagesMenus[index]?.Show();
                    break;
                case "Debug":
                    debugMenus[index]?.Show();
                    break;
            }
        }

        private void PositionNearObject(GameObject licenseObject, GameObject menuCanvas)
        {
            if (menuCanvas == null)
            {
                Debug.LogWarning("Menu Canvas is not initialized.");
                return;
            }

            if (licenseObject == null) return;

            Vector3 targetPosition = licenseObject.transform.position;
            menuCanvas.transform.position = targetPosition;

            menuCanvas.transform.rotation = licenseObject.transform.rotation * Quaternion.Euler(90f, 180f, 0f);
        }
    }
}
