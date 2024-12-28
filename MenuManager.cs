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
        private bool paper1Visible = true;
        private bool attachedToStickyTape1 = false;
        private GameObject? stickyTapeBase1;
        private GameObject? licenseObject1;
        private GameObject? licenseObject2;
        private GameObject? licenseObject3;
        private GameObject? licenseObject4;
        private GameObject? licenseObject5;

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

        private void Update()
        {
            string methodName = MethodBase.GetCurrentMethod().Name;

            // HandleVRInput();

            if (licenseObject1 == null)
            {
                licenseObject1 = GameObject.Find("LicenseTrainDriver");
                if (licenseObject1 != null)
                {
                    Main.LogEntry(methodName, "Attaching menu to license");
            
                    if (menuCanvas == null)
                    {
                        Main.LogEntry(methodName, "Menu canvas was null, recreating...");
                        CreateCanvas();
                    }

                    menuCanvas!.SetActive(true);

                    mainMenu?.Show();
                }
            }
            
            if (licenseObject1 != null)
            {
                settingsMenu?.UpdateDisplayedValues(
                    Settings.Instance.twitchUsername,
                    Settings.Instance.messageDuration,
                    WebSocketManager.LastChatMessage
                );

                // Check if attached to sticky tape
                Transform current = licenseObject1.transform;
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

                if (currentlyAttached != attachedToStickyTape1)
                {
                    attachedToStickyTape1 = currentlyAttached;
                    Main.LogEntry(methodName, $"License attachment to sticky tape changed: {attachedToStickyTape1}");
                    
                    // Handle sticky tape visibility
                    if (attachedToStickyTape1 && newStickyTapeBase != null)
                    {
                        stickyTapeBase1 = newStickyTapeBase;
                        stickyTapeBase1.SetActive(false);
                    }
                    else if (!attachedToStickyTape1 && stickyTapeBase1 != null)
                    {
                        stickyTapeBase1.SetActive(true);
                        stickyTapeBase1 = null;
                    }
                }

                if (paper1Visible)
                {
                    Transform paperTransform = licenseObject1.transform.Find("Pivot/TempPaper(Clone)(Clone) 0/Paper");
                    if (paperTransform != null)
                    {
                        paperTransform.gameObject.SetActive(false);
                        paper1Visible = false;
                    }
                }

                // LogGameObjectHierarchy(licenseObject);
            }
        }

        private void LateUpdate()
        {
            if (licenseObject1 != null)
            {
                PositionNearObject(licenseObject1);
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

        private void PositionNearObject(GameObject licenseObject)
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

        // private void HandleVRInput()
        // {
        //     if (XRDevice.isPresent)
        //     {
        //         Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
        //         if (Physics.Raycast(ray, out RaycastHit hit))
        //         {
        //             if (hit.collider != null && Input.GetButtonDown("Fire1"))
        //             {
        //                 var button = hit.collider.GetComponent<Button>();
        //                 if (button != null)
        //                 {
        //                     button.onClick.Invoke();
        //                 }
        //             }
        //         }
        //     }
        // }
    }
}
