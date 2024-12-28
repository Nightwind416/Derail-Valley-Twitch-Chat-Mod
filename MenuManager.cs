using UnityEngine;
using UnityEngine.UI;
using System.Reflection;
using UnityEngine.EventSystems;
using UnityEngine.XR;
using TwitchChat.Menus;

namespace TwitchChat
{
    public class MenuManager : MonoBehaviour
    {
        private static MenuManager? instance;
        private static GameObject? menuCanvas;
        private bool isMenuVisible = false;
        private bool isPaperVisible = true;
        private bool isAttachedToStickyTape = false;
        private bool isSettingsPanelVisible = false;
        private bool isWaitingForLicense = true;
        private GameObject? licenseObject;
        private GameObject? stickyTapeBase;

        private MainMenu? mainMenu;
        private SettingsMenu? settingsMenu;
        private StandardMessagesMenu? standardMessagesMenu;
        private CommandMessagesMenu? commandMessagesMenu;
        private CustomCommandsMenu? customCommandsMenu;
        private TimedMessagesMenu? timedMessagesMenu;
        private DispatcherMessagesMenu? dispatcherMessagesMenu;
        private DebugMenu? debugMenu;

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

        private void Start()
        {
            string methodName = MethodBase.GetCurrentMethod().Name;
            Main.LogEntry(methodName, "Initializing MenuManager");
            CreateCanvas();
        }

        private void Update()
        {
            string methodName = MethodBase.GetCurrentMethod().Name;

            HandleVRInput();

            if (isWaitingForLicense)
            {
                licenseObject = GameObject.Find("LicenseTrainDriver");
                if (licenseObject != null)
                {
                    isWaitingForLicense = false;

                    isMenuVisible = true;

                    Main.LogEntry(methodName, "Attaching menu to license");
            
                    if (menuCanvas == null)
                    {
                        Main.LogEntry(methodName, "Menu canvas was null, recreating...");
                        CreateCanvas();
                    }

                    menuCanvas!.SetActive(true);

                    mainMenu?.Show();
                    settingsMenu?.Hide();
                    isSettingsPanelVisible = false;
                }
            }
            
            if (isMenuVisible && licenseObject != null && settingsMenu != null)
            {
                settingsMenu.UpdateDisplayedValues(
                    Settings.Instance.twitchUsername,
                    Settings.Instance.messageDuration,
                    WebSocketManager.LastChatMessage
                );

                // Check if attached to sticky tape
                Transform current = licenseObject.transform;
                bool currentlyAttached = false;
                GameObject? newStickyTapeBase = null;
                
                while (current.parent != null)
                {
                    if (current.parent.name.Contains("StickyTape_Gadget"))
                    {
                        currentlyAttached = true;
                        // Find the base object when attached
                        Transform baseTransform = current.parent.Find("LOD gadget_sticker_base");
                        if (baseTransform != null)
                        {
                            newStickyTapeBase = baseTransform.gameObject;
                        }
                        break;
                    }
                    current = current.parent;
                }

                if (currentlyAttached != isAttachedToStickyTape)
                {
                    isAttachedToStickyTape = currentlyAttached;
                    Main.LogEntry(methodName, $"License attachment to sticky tape changed: {isAttachedToStickyTape}");
                    
                    // Handle sticky tape visibility
                    if (isAttachedToStickyTape && newStickyTapeBase != null)
                    {
                        stickyTapeBase = newStickyTapeBase;
                        stickyTapeBase.SetActive(false);
                    }
                    else if (!isAttachedToStickyTape && stickyTapeBase != null)
                    {
                        stickyTapeBase.SetActive(true);
                        stickyTapeBase = null;
                    }
                }

                // LogGameObjectHierarchy(licenseObject);
            }

            if (isPaperVisible)
            {
                if (licenseObject != null)
                {
                    Transform paperTransform = licenseObject.transform.Find("Pivot/TempPaper(Clone)(Clone) 0/Paper");
                    if (paperTransform != null)
                    {
                        paperTransform.gameObject.SetActive(false);
                        isPaperVisible = false;
                    }
                }
            }
        }

        private void LateUpdate()
        {
            if (isMenuVisible)
            {
                PositionNearObject();
            }
        }

        private void CreateCanvas()
        {
            string methodName = MethodBase.GetCurrentMethod().Name;
            Main.LogEntry(methodName, "Creating menu UI elements");

            if (menuCanvas != null)
            {
                Main.LogEntry(methodName, "Menu already exists, destroying old instance");
                Destroy(menuCanvas);
            }

            menuCanvas = new GameObject("MenuCanvas");
            Canvas canvas = menuCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 1000;
            
            RectTransform canvasRect = menuCanvas.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(200, 300);
            canvasRect.localScale = Vector3.one * 0.001f;

            menuCanvas.AddComponent<GraphicRaycaster>();

            // Create all menus
            mainMenu = new MainMenu(menuCanvas.transform);
            settingsMenu = new SettingsMenu(menuCanvas.transform);
            standardMessagesMenu = new StandardMessagesMenu(menuCanvas.transform);
            commandMessagesMenu = new CommandMessagesMenu(menuCanvas.transform);
            customCommandsMenu = new CustomCommandsMenu(menuCanvas.transform);
            timedMessagesMenu = new TimedMessagesMenu(menuCanvas.transform);
            dispatcherMessagesMenu = new DispatcherMessagesMenu(menuCanvas.transform);
            debugMenu = new DebugMenu(menuCanvas.transform);

            // Hide all menus immediately after creation before setting up any events
            HideAllMenus();

            // Setup navigation events
            mainMenu.OnMenuButtonClicked += HandleMenuNavigation;
            settingsMenu.OnBackButtonClicked += () => ShowMenu("Main");
            standardMessagesMenu.OnBackButtonClicked += () => ShowMenu("Main");
            commandMessagesMenu.OnBackButtonClicked += () => ShowMenu("Main");
            customCommandsMenu.OnBackButtonClicked += () => ShowMenu("Main");
            timedMessagesMenu.OnBackButtonClicked += () => ShowMenu("Main");
            dispatcherMessagesMenu.OnBackButtonClicked += () => ShowMenu("Main");
            debugMenu.OnBackButtonClicked += () => ShowMenu("Main");

            // Show main menu last
            mainMenu?.Show();
        }

        private void HandleMenuNavigation(string menuName)
        {
            ShowMenu(menuName);
        }

        private void HideAllMenus()
        {
            mainMenu?.Hide();
            settingsMenu?.Hide();
            standardMessagesMenu?.Hide();
            commandMessagesMenu?.Hide();
            customCommandsMenu?.Hide();
            timedMessagesMenu?.Hide();
            dispatcherMessagesMenu?.Hide();
            debugMenu?.Hide();
        }

        private void ShowMenu(string menuName)
        {
            HideAllMenus();

            switch (menuName)
            {
                case "Main":
                    mainMenu?.Show();
                    break;
                case "Settings":
                    settingsMenu?.Show();
                    break;
                case "Standard Messages":
                    standardMessagesMenu?.Show();
                    break;
                case "Command Messages":
                    commandMessagesMenu?.Show();
                    break;
                case "Custom Commands":
                    customCommandsMenu?.Show();
                    break;
                case "Timed Messages":
                    timedMessagesMenu?.Show();
                    break;
                case "Dispatcher Messages":
                    dispatcherMessagesMenu?.Show();
                    break;
                case "Debug":
                    debugMenu?.Show();
                    break;
            }
        }

        private void ToggleSettingsPanel()
        {
            isSettingsPanelVisible = !isSettingsPanelVisible;
            if (isSettingsPanelVisible)
            {
                mainMenu?.Hide();
                settingsMenu?.Show();
                settingsMenu?.UpdateDisplayedValues(
                    Settings.Instance.twitchUsername,
                    Settings.Instance.messageDuration,
                    WebSocketManager.LastChatMessage
                );
            }
            else
            {
                settingsMenu?.Hide();
                mainMenu?.Show();
            }
        }

        private void PositionNearObject()
        {
            if (menuCanvas == null)
            {
                Debug.LogWarning("Menu Canvas is not initialized.");
                return;
            }

            // Position the menu to hover in front of the target object
            if (licenseObject == null) return;
            Vector3 targetPosition = licenseObject.transform.position;
            Vector3 offset = Vector3.forward * 0.001f;
            menuCanvas.transform.position = targetPosition;
            menuCanvas.transform.position += offset;

            // Match the rotation of the target object and add 90 degrees X rotation
            menuCanvas.transform.rotation = licenseObject.transform.rotation * Quaternion.Euler(90f, 180f, 0f);
        }

        private void HandleVRInput()
        {
            if (XRDevice.isPresent)
            {
                Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    if (hit.collider != null && Input.GetButtonDown("Fire1"))
                    {
                        var button = hit.collider.GetComponent<Button>();
                        if (button != null)
                        {
                            button.onClick.Invoke();
                        }
                    }
                }
            }
        }
    }
}
