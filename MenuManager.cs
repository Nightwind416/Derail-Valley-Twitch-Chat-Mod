using System;
using System.Collections.Generic;
using System.Linq;
using TwitchChat.PanelDisplays;
using TwitchChat.PanelMenus;
using UnityEngine;
using UnityEngine.UI;
using VRTK;

namespace TwitchChat
{
    /// <summary>
    /// Manages the creation, positioning, and interaction of UI menus and panels for the Twitch Chat mod.
    /// </summary>
    /// <remarks>
    /// Two kinds of host carry the panel stack:
    /// - Cab display: one canvas parented to the current locomotive interior, placed where the player is looking.
    ///   Its pose is remembered per locomotive type and restored when the player enters that type again.
    /// - Wrist panel: one canvas parented to a VR controller so it can be glanced at like a watch.
    /// </remarks>
    public class MenuManager : MonoBehaviour
    {
        private const float WristSearchInterval = 2f;
        private const float BaseCanvasScale = 0.001f;

        private static MenuManager? instance;
        private readonly CabDisplayHost cabDisplay = new();
        private readonly WristPanelHost wristPanel = new();
        private GameObject? templateCanvas;

        private TrainCar? cabDisplayCar;
        private PanelGrabHandles? cabDisplayHandles;
        private Transform? wristAnchor;
        private float nextWristSearchTime;

        private KeyCode placeKey = KeyCode.None;
        private KeyCode toggleKey = KeyCode.None;
        private string parsedPlaceKey = string.Empty;
        private string parsedToggleKey = string.Empty;

        /// <summary>Every host, whether or not its canvas currently exists.</summary>
        private IEnumerable<PanelHost> AllHosts
        {
            get
            {
                yield return cabDisplay;
                yield return wristPanel;
            }
        }

        /// <summary>
        /// Defines the types of panels available in the mod interface.
        /// </summary>
        private enum PanelType
        {
            Main,
            Authentication,
            Status,
            Notifications,
            LargeDisplay,
            MediumDisplay,
            WideDisplay,
            SmallDisplay,
            StandardMessages,
            CommandMessages,
            TimedMessages,
            Config1,
            Config2,
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
            { PanelType.Main, new(new Vector2(200, 360), new Vector2(200, 360), Vector2.zero, Vector3.zero) },
            { PanelType.Authentication, new(new Vector2(230, 440), new Vector2(230, 440), Vector2.zero, Vector3.zero) },
            { PanelType.Status, new(new Vector2(200, 300), new Vector2(200, 300), Vector2.zero, Vector3.zero) },
            { PanelType.Notifications, new(new Vector2(200, 300), new Vector2(200, 300), Vector2.zero, Vector3.zero) },
            { PanelType.LargeDisplay, new(new Vector2(1200, 650), new Vector2(1200, 650), Vector2.zero, Vector3.zero) },
            { PanelType.MediumDisplay, new(new Vector2(500, 500), new Vector2(500, 500), Vector2.zero, Vector3.zero) },
            { PanelType.WideDisplay, new(new Vector2(900, 220), new Vector2(900, 220), Vector2.zero, Vector3.zero) },
            { PanelType.SmallDisplay, new(new Vector2(200, 300), new Vector2(200, 300), Vector2.zero, Vector3.zero) },
            { PanelType.StandardMessages, new(new Vector2(200, 300), new Vector2(200, 300), Vector2.zero, Vector3.zero) },
            { PanelType.CommandMessages, new(new Vector2(200, 300), new Vector2(200, 300), Vector2.zero, Vector3.zero) },
            { PanelType.TimedMessages, new(new Vector2(200, 300), new Vector2(200, 300), Vector2.zero, Vector3.zero) },
            { PanelType.Config1, new(new Vector2(200, 300), new Vector2(200, 300), Vector2.zero, Vector3.zero) },
            { PanelType.Config2, new(new Vector2(200, 300), new Vector2(200, 300), Vector2.zero, Vector3.zero) },
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

        private void Awake()
        {
            CreateTemplateCanvas();
            PlayerManager.CarChanged += OnPlayerCarChanged;
        }

        private void OnDestroy()
        {
            PlayerManager.CarChanged -= OnPlayerCarChanged;
        }

        private void OnApplicationQuit()
        {
            Settings.Instance.FlushPendingSave(force: true);
        }

        /// <summary>
        /// Creates a template canvas that serves as the base for all UI panels.
        /// </summary>
        private void CreateTemplateCanvas()
        {
            string methodName = "CreateTemplateCanvas";

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
            canvasRect.localScale = Vector3.one * BaseCanvasScale;

            // Add GraphicRaycaster for non-VR clicks
            templateCanvas.AddComponent<GraphicRaycaster>();

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
            _ = new MainPanel(parent, null);
            _ = new StatusPanel(parent);
            _ = new NotificationsPanel(parent);
            _ = new LargeDisplayPanel(parent);
            _ = new MediumDisplayPanel(parent);
            _ = new WideDisplayPanel(parent);
            _ = new SmallDisplayPanel(parent);
            _ = new StandardMessagesPanel(parent);
            _ = new CommandMessagesPanel(parent);
            _ = new TimedMessagesPanel(parent);
            _ = new Config1Panel(parent);
            _ = new Config2Panel(parent);
            _ = new DebugPanel(parent);

            // Hide all template panels
            foreach (Transform child in parent)
            {
                child.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Drives all hosts once per frame.
        /// </summary>
        private void Update()
        {
            // Write any coalesced settings changes from the panels once they have settled
            Settings.Instance.FlushPendingSave();
            HandleHotkeys();

            bool inSession = PlayerManager.PlayerTransform != null;

            UpdateCabDisplay(inSession);
            UpdateWristPanel(inSession);
        }

        private void LateUpdate()
        {
            if (wristAnchor != null && wristPanel.MenuCanvas != null && wristPanel.MenuCanvas.activeSelf)
            {
                ApplyWristPose();
            }
        }

        // ------------------------------------------------------------------
        // Hotkeys (flat mode, or VR with a keyboard in reach)
        // ------------------------------------------------------------------

        private void HandleHotkeys()
        {
            placeKey = ParseKey(Settings.Instance.placeDisplayKey, ref parsedPlaceKey, placeKey);
            toggleKey = ParseKey(Settings.Instance.toggleDisplayKey, ref parsedToggleKey, toggleKey);

            if (placeKey != KeyCode.None && Input.GetKeyDown(placeKey))
            {
                PlaceCabDisplay();
            }
            if (toggleKey != KeyCode.None && Input.GetKeyDown(toggleKey))
            {
                ToggleCabDisplay();
            }
        }

        private static KeyCode ParseKey(string name, ref string cachedName, KeyCode cached)
        {
            if (name == cachedName)
            {
                return cached;
            }
            cachedName = name;
            return Enum.TryParse(name, true, out KeyCode key) ? key : KeyCode.None;
        }

        // ------------------------------------------------------------------
        // Cab display
        // ------------------------------------------------------------------

        private void UpdateCabDisplay(bool inSession)
        {
            if (!inSession)
            {
                cabDisplay.Placed = false;
                cabDisplayCar = null;
                return;
            }

            if (cabDisplay.MenuCanvas == null)
            {
                // First frame of a session, or the canvas was destroyed along with the car it was parented to
                CreateMenuCanvas(cabDisplay);
                cabDisplay.Placed = false;
                cabDisplayCar = null;
                TryRestoreCabDisplay(PlayerManager.Car);
            }

            GameObject canvas = cabDisplay.MenuCanvas!;
            bool shouldShow = cabDisplay.Placed && Settings.Instance.cabDisplayVisible;
            if (canvas.activeSelf != shouldShow)
            {
                canvas.SetActive(shouldShow);
                if (shouldShow)
                {
                    ShowPanel(cabDisplay.ActivePanel, cabDisplay);
                }
            }

            if (shouldShow)
            {
                canvas.transform.localScale = Vector3.one * BaseCanvasScale * Settings.Instance.cabDisplayScale;
                UpdatePanelValues(cabDisplay);
            }
        }

        private void OnPlayerCarChanged(TrainCar car)
        {
            if (car == null || cabDisplay.MenuCanvas == null || cabDisplayCar == car)
            {
                return;
            }
            if (cabDisplayHandles != null && cabDisplayHandles.IsHeld)
            {
                return; // the player is carrying it between cars; where they let go decides where it lands
            }
            TryRestoreCabDisplay(car);
        }

        /// <summary>
        /// Moves the cab display onto the given car if a pose was saved for that locomotive type.
        /// </summary>
        private void TryRestoreCabDisplay(TrainCar? car)
        {
            if (car == null || cabDisplay.MenuCanvas == null)
            {
                return;
            }

            string key = CarKey(car);
            CabDisplayPose? pose = FindPose(key);
            if (pose == null)
            {
                return;
            }

            AttachCabDisplay(car, pose.localPosition, Quaternion.Euler(pose.localEuler));
            Main.LogEntry("CabDisplay", $"Restored cab display pose for {key}.");
        }

        private void AttachCabDisplay(TrainCar car, Vector3 localPosition, Quaternion localRotation)
        {
            Transform parent = car.interior != null ? car.interior : car.transform;
            Transform canvas = cabDisplay.MenuCanvas!.transform;
            canvas.SetParent(parent, false);
            canvas.localPosition = localPosition;
            canvas.localRotation = localRotation;
            canvas.localScale = Vector3.one * BaseCanvasScale * Settings.Instance.cabDisplayScale;
            cabDisplayCar = car;
            cabDisplay.Placed = true;
        }

        /// <summary>
        /// Places the cab display in front of the player's view, parents it to the current car, and remembers the pose for that locomotive type.
        /// </summary>
        public void PlaceCabDisplay()
        {
            string methodName = "CabDisplay";

            Camera? camera = PlayerManager.PlayerCamera != null ? PlayerManager.PlayerCamera : Camera.main;
            if (camera == null || PlayerManager.PlayerTransform == null)
            {
                Main.LogEntry(methodName, "Cannot place the cab display outside of a game session.");
                NotificationManager.SetVariable("alertMessage", "The cab display can only be placed while in a game session.");
                return;
            }

            if (cabDisplay.MenuCanvas == null)
            {
                CreateMenuCanvas(cabDisplay);
            }

            Transform canvas = cabDisplay.MenuCanvas!.transform;
            Vector3 position = camera.transform.position + camera.transform.forward * Settings.Instance.cabDisplayDistance;
            Quaternion rotation = Quaternion.LookRotation(position - camera.transform.position, Vector3.up);

            TrainCar? car = PlayerManager.Car;
            Transform? parent = car != null
                ? (car.interior != null ? car.interior : car.transform)
                : WorldMover.OriginShiftParent;

            canvas.SetParent(parent, true);
            canvas.SetPositionAndRotation(position, rotation);
            canvas.localScale = Vector3.one * BaseCanvasScale * Settings.Instance.cabDisplayScale;

            cabDisplayCar = car;
            cabDisplay.Placed = true;
            Settings.Instance.cabDisplayVisible = true;

            string location = "the world";
            if (car != null)
            {
                location = CarKey(car);
                SavePose(location, canvas.localPosition, canvas.localRotation.eulerAngles);
            }
            Settings.Instance.RequestSave();

            canvas.gameObject.SetActive(true);
            ShowPanel(cabDisplay.ActivePanel, cabDisplay);

            Main.LogEntry(methodName, $"Cab display placed on {location} at {position}.");
            NotificationManager.SetVariable("alertMessage", car != null
                ? $"Cab display placed. Position saved for {location}."
                : "Cab display placed. Not in a car, so this position is not saved.");
        }

        /// <summary>
        /// Shows or hides the cab display without moving it.
        /// </summary>
        public void ToggleCabDisplay()
        {
            Settings.Instance.cabDisplayVisible = !Settings.Instance.cabDisplayVisible;
            Settings.Instance.RequestSave();
            Main.LogEntry("CabDisplay", $"Cab display visible: {Settings.Instance.cabDisplayVisible}");

            if (Settings.Instance.cabDisplayVisible && !cabDisplay.Placed)
            {
                NotificationManager.SetVariable("alertMessage", "The cab display has not been placed in this locomotive yet. Use Place Display first.");
            }
        }

        /// <summary>
        /// Called by <see cref="PanelGrabHandles"/> when a hand lets go of the display. Re-parents it to whichever
        /// car the player is in, so it rides along from wherever it was left, and saves the new pose for that
        /// locomotive type.
        /// </summary>
        private void OnCabDisplayReleased()
        {
            if (cabDisplay.MenuCanvas == null)
            {
                return;
            }

            Transform canvas = cabDisplay.MenuCanvas.transform;
            TrainCar? car = PlayerManager.Car;
            Transform? parent = car != null
                ? (car.interior != null ? car.interior : car.transform)
                : WorldMover.OriginShiftParent;

            if (parent != null && canvas.parent != parent)
            {
                canvas.SetParent(parent, true);
            }

            cabDisplayCar = car;
            cabDisplay.Placed = true;

            string location = "the world";
            if (car != null)
            {
                location = CarKey(car);
                SavePose(location, canvas.localPosition, canvas.localRotation.eulerAngles);
            }
            Settings.Instance.RequestSave();

            Main.LogEntry("CabDisplay", $"Cab display released on {location}.");
        }

        private static string CarKey(TrainCar car)
        {
            return car.carLivery != null ? car.carLivery.id : car.carType.ToString();
        }

        private static CabDisplayPose? FindPose(string key)
        {
            return Settings.Instance.cabDisplayPoses.FirstOrDefault(p => p.carId == key);
        }

        private static void SavePose(string key, Vector3 localPosition, Vector3 localEuler)
        {
            CabDisplayPose? pose = FindPose(key);
            if (pose == null)
            {
                pose = new CabDisplayPose { carId = key };
                Settings.Instance.cabDisplayPoses.Add(pose);
            }
            pose.localPosition = localPosition;
            pose.localEuler = localEuler;
        }

        // ------------------------------------------------------------------
        // Wrist panel
        // ------------------------------------------------------------------

        private void UpdateWristPanel(bool inSession)
        {
            bool wanted = inSession && Settings.Instance.wristPanelEnabled && VRManager.IsVREnabled();
            if (!wanted)
            {
                if (wristPanel.MenuCanvas != null && wristPanel.MenuCanvas.activeSelf)
                {
                    wristPanel.MenuCanvas.SetActive(false);
                }
                if (!inSession)
                {
                    wristAnchor = null;
                }
                return;
            }

            if (wristPanel.MenuCanvas == null)
            {
                CreateMenuCanvas(wristPanel);
                wristAnchor = null;
            }

            bool wantLeft = Settings.Instance.wristPanelOnLeftHand;
            if (wristAnchor != null && wristPanel.AttachedToLeftHand != wantLeft)
            {
                wristAnchor = null; // hand preference changed, re-attach
            }

            if (wristAnchor == null && Time.unscaledTime >= nextWristSearchTime)
            {
                nextWristSearchTime = Time.unscaledTime + WristSearchInterval;
                GameObject? hand = wantLeft ? VRTK_DeviceFinder.GetControllerLeftHand(true) : VRTK_DeviceFinder.GetControllerRightHand(true);
                if (hand != null)
                {
                    wristAnchor = hand.transform;
                    wristPanel.AttachedToLeftHand = wantLeft;
                    wristPanel.MenuCanvas!.transform.SetParent(wristAnchor, false);
                    ApplyWristPose();
                    Main.LogEntry("WristPanel", $"Wrist panel attached to the {(wantLeft ? "left" : "right")} controller ('{hand.name}').");
                }
            }

            GameObject canvas = wristPanel.MenuCanvas!;
            bool shouldShow = wristAnchor != null;
            if (canvas.activeSelf != shouldShow)
            {
                canvas.SetActive(shouldShow);
                if (shouldShow)
                {
                    ShowPanel(wristPanel.ActivePanel, wristPanel);
                }
            }

            if (shouldShow)
            {
                UpdatePanelValues(wristPanel);
            }
        }

        private void ApplyWristPose()
        {
            if (wristPanel.MenuCanvas == null)
            {
                return;
            }
            Transform canvas = wristPanel.MenuCanvas.transform;
            canvas.localPosition = Settings.Instance.wristPanelOffset;
            canvas.localRotation = Quaternion.Euler(Settings.Instance.wristPanelRotation);
            canvas.localScale = Vector3.one * BaseCanvasScale * Settings.Instance.wristPanelScale;
        }

        // ------------------------------------------------------------------
        // Shared host plumbing
        // ------------------------------------------------------------------

        /// <summary>
        /// Updates values displayed on active panels.
        /// </summary>
        private void UpdatePanelValues(PanelHost host)
        {
            host.AuthenticationPanel?.UpdateAuthenticationPanelValues();
            host.StatusPanel?.UpdateStatusPanelValues();
            host.StandardMessagesPanel?.UpdateStandardMessagesPanelValues();
            host.CommandMessagesPanel?.UpdateCommandMessagesPanelValues();
        }

        public void OnPanelButtonClicked(string panelName, PanelHost host)
        {
            Main.LogEntry("OnPanelButtonClicked", $"Panel button clicked: {panelName} for host: {host.Name}");
            ShowPanel(panelName, host);
        }

        private void CreateMenuCanvas(PanelHost host)
        {
            if (templateCanvas == null)
            {
                Main.LogEntry("CreateMenuCanvas", "Template canvas not found!");
                return;
            }

            // Clone the (inactive) template
            host.MenuCanvas = Instantiate(templateCanvas);
            host.MenuCanvas.name = $"MenuCanvas_{host.Name}";

            Transform menuPanel = host.MenuCanvas.transform.Find("MenuPanel");

            // Create and wire up all panels from the templates
            host.MainPanel = new MainPanel(menuPanel, host);
            host.AuthenticationPanel = new AuthenticationPanel(menuPanel);
            host.StatusPanel = new StatusPanel(menuPanel);
            host.NotificationsPanel = new NotificationsPanel(menuPanel);
            host.LargeDisplayPanel = new LargeDisplayPanel(menuPanel);
            host.MediumDisplayPanel = new MediumDisplayPanel(menuPanel);
            host.WideDisplayPanel = new WideDisplayPanel(menuPanel);
            host.SmallDisplayPanel = new SmallDisplayPanel(menuPanel);
            host.StandardMessagesPanel = new StandardMessagesPanel(menuPanel);
            host.CommandMessagesPanel = new CommandMessagesPanel(menuPanel);
            host.TimedMessagesPanel = new TimedMessagesPanel(menuPanel);
            host.Config1Panel = new Config1Panel(menuPanel);
            host.Config2Panel = new Config2Panel(menuPanel);
            host.DebugPanel = new DebugPanel(menuPanel);

            // Explicitly hide all panels immediately after creation
            HideAllPanels(host);

            // Wire up back button events
            host.AuthenticationPanel.OnBackButtonClicked += () => ShowPanel("Main", host);
            host.StatusPanel.OnBackButtonClicked += () => ShowPanel("Main", host);
            host.NotificationsPanel.OnBackButtonClicked += () => ShowPanel("Main", host);
            host.LargeDisplayPanel.OnBackButtonClicked += () => ShowPanel("Main", host);
            host.MediumDisplayPanel.OnBackButtonClicked += () => ShowPanel("Main", host);
            host.WideDisplayPanel.OnBackButtonClicked += () => ShowPanel("Main", host);
            host.SmallDisplayPanel.OnBackButtonClicked += () => ShowPanel("Main", host);
            host.StandardMessagesPanel.OnBackButtonClicked += () => ShowPanel("Main", host);
            host.CommandMessagesPanel.OnBackButtonClicked += () => ShowPanel("Main", host);
            host.TimedMessagesPanel.OnBackButtonClicked += () => ShowPanel("Main", host);
            host.Config1Panel.OnBackButtonClicked += () => ShowPanel("Main", host);
            host.Config2Panel.OnBackButtonClicked += () => ShowPanel("Main", host);
            host.DebugPanel.OnBackButtonClicked += () => ShowPanel("Main", host);

            // The cab display is the only host the player moves around, so it is the only one that gets grab bars
            if (host is CabDisplayHost && menuPanel != null)
            {
                cabDisplayHandles = host.MenuCanvas.AddComponent<PanelGrabHandles>();
                cabDisplayHandles.Initialize(menuPanel, OnCabDisplayReleased);
            }

            Main.LogEntry("CreateMenuCanvas", $"Created panel stack for host {host.Name}.");
        }

        private static void HideAllPanels(PanelHost host)
        {
            host.MainPanel?.Hide();
            host.AuthenticationPanel?.Hide();
            host.StatusPanel?.Hide();
            host.NotificationsPanel?.Hide();
            host.LargeDisplayPanel?.Hide();
            host.MediumDisplayPanel?.Hide();
            host.WideDisplayPanel?.Hide();
            host.SmallDisplayPanel?.Hide();
            host.StandardMessagesPanel?.Hide();
            host.CommandMessagesPanel?.Hide();
            host.TimedMessagesPanel?.Hide();
            host.Config1Panel?.Hide();
            host.Config2Panel?.Hide();
            host.DebugPanel?.Hide();
        }

        private void ShowPanel(string panelName, PanelHost host)
        {
            if (host.MenuCanvas == null || !host.MenuCanvas.activeSelf)
            {
                return;
            }

            Main.LogEntry("ShowPanel", $"Showing panel {panelName} on host {host.Name}");

            HideAllPanels(host);

            PanelType panelType = panelName switch
            {
                "Main" => PanelType.Main,
                "Authentication" => PanelType.Authentication,
                "Status" => PanelType.Status,
                "Notifications" => PanelType.Notifications,
                "Large Display" => PanelType.LargeDisplay,
                "Medium Display" => PanelType.MediumDisplay,
                "Wide Display" => PanelType.WideDisplay,
                "Small Display" => PanelType.SmallDisplay,
                "Standard Messages" => PanelType.StandardMessages,
                "Command Messages" => PanelType.CommandMessages,
                "Timed Messages" => PanelType.TimedMessages,
                "Config1" => PanelType.Config1,
                "Config2" => PanelType.Config2,
                "Debug" => PanelType.Debug,
                _ => PanelType.Main
            };

            // Apply the configuration for this panel type
            PanelConfig config = panelConfigs[panelType];
            Transform menuPanel = host.MenuCanvas.transform.Find("MenuPanel");
            if (menuPanel != null)
            {
                RectTransform canvasRect = host.MenuCanvas.GetComponent<RectTransform>();
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
                    host.MainPanel?.Show();
                    break;
                case PanelType.Authentication:
                    host.AuthenticationPanel?.Show();
                    break;
                case PanelType.Status:
                    host.StatusPanel?.Show();
                    break;
                case PanelType.Notifications:
                    host.NotificationsPanel?.Show();
                    break;
                case PanelType.LargeDisplay:
                    host.LargeDisplayPanel?.Show();
                    break;
                case PanelType.MediumDisplay:
                    host.MediumDisplayPanel?.Show();
                    break;
                case PanelType.WideDisplay:
                    host.WideDisplayPanel?.Show();
                    break;
                case PanelType.SmallDisplay:
                    host.SmallDisplayPanel?.Show();
                    break;
                case PanelType.StandardMessages:
                    host.StandardMessagesPanel?.Show();
                    break;
                case PanelType.CommandMessages:
                    host.CommandMessagesPanel?.Show();
                    break;
                case PanelType.TimedMessages:
                    host.TimedMessagesPanel?.Show();
                    break;
                case PanelType.Config1:
                    host.Config1Panel?.Show();
                    break;
                case PanelType.Config2:
                    host.Config2Panel?.Show();
                    break;
                case PanelType.Debug:
                    host.DebugPanel?.Show();
                    break;
            }

            // Remember the active panel for this host
            host.ActivePanel = panelName;
            Settings.Instance.RequestSave();
        }

        /// <summary>
        /// Adds a chat message to the display panels of every host that has been created.
        /// </summary>
        /// <param name="username">The username of the message sender.</param>
        /// <param name="message">The chat message content.</param>
        public void AddMessageToPanelDisplays(string username, string message)
        {
            foreach (PanelHost host in AllHosts)
            {
                if (host.MenuCanvas == null)
                {
                    continue;
                }

                try
                {
                    // Add message to all display panels regardless of which one is showing
                    host.LargeDisplayPanel?.AddChatMessage(username, message);
                    host.MediumDisplayPanel?.AddChatMessage(username, message);
                    host.WideDisplayPanel?.AddChatMessage(username, message);
                    host.SmallDisplayPanel?.AddChatMessage(username, message);
                }
                catch (Exception ex)
                {
                    Main.LogEntry("MenuManager.AddMessageToPanelDisplays", $"Error adding message to host {host.Name}: {ex.Message}");
                }
            }
        }

        public void UpdateAllNotificationToggles(bool value)
        {
            foreach (PanelHost host in AllHosts)
            {
                host.NotificationsPanel?.UpdateNotificationsEnabled(value);
            }
        }

        public void UpdateAllNotificationDurations(float value)
        {
            foreach (PanelHost host in AllHosts)
            {
                host.NotificationsPanel?.UpdateNotificationDuration(value);
            }
        }

        public void UpdateAllProcessOwnToggles(bool value)
        {
            foreach (PanelHost host in AllHosts)
            {
                host.DebugPanel?.UpdateProcessOwn(value);
            }
        }

        public void UpdateAllProcessDuplicatesToggles(bool value)
        {
            foreach (PanelHost host in AllHosts)
            {
                host.DebugPanel?.UpdateProcessDuplicates(value);
            }
        }

        public void UpdateAllConnectMessageEnabledToggles(bool value)
        {
            foreach (PanelHost host in AllHosts)
            {
                host.StandardMessagesPanel?.UpdateConnectMessageEnabled(value);
            }
        }

        public void UpdateAllDisconnectMessageEnabledToggles(bool value)
        {
            foreach (PanelHost host in AllHosts)
            {
                host.StandardMessagesPanel?.UpdateDisconnectMessageEnabled(value);
            }
        }

        public void UpdateCommandsMessageToggles(bool value)
        {
            foreach (PanelHost host in AllHosts)
            {
                host.CommandMessagesPanel?.UpdateCommandsMessageEnabled(value);
            }
        }

        public void UpdateInfoMessageToggles(bool value)
        {
            foreach (PanelHost host in AllHosts)
            {
                host.CommandMessagesPanel?.UpdateInfoMessageEnabled(value);
            }
        }

        public void UpdateAllTimedMessageToggles(bool value)
        {
            foreach (PanelHost host in AllHosts)
            {
                host.TimedMessagesPanel?.UpdateTimedMessagesEnabled(value);
            }
        }

        public void UpdateAllPanelBackgrounds()
        {
            foreach (PanelHost host in AllHosts)
            {
                if (host.MenuCanvas == null)
                {
                    continue;
                }

                Transform menuPanel = host.MenuCanvas.transform.Find("MenuPanel");
                if (menuPanel == null)
                {
                    continue;
                }

                Image panelImage = menuPanel.GetComponent<Image>();
                if (panelImage != null)
                {
                    panelImage.color = Settings.Instance.panelColor;
                }

                // Direct children of MenuPanel whose name contains "Panel" are the panel backgrounds
                foreach (Transform child in menuPanel)
                {
                    if (child.name.Contains("Panel"))
                    {
                        Image childImage = child.GetComponent<Image>();
                        if (childImage != null)
                        {
                            childImage.color = Settings.Instance.panelColor;
                        }
                    }
                }
            }
        }

        public void UpdateAllSectionBackgrounds()
        {
            foreach (PanelHost host in AllHosts)
            {
                if (host.MenuCanvas == null)
                {
                    continue;
                }

                Transform menuPanel = host.MenuCanvas.transform.Find("MenuPanel");
                if (menuPanel == null)
                {
                    continue;
                }

                foreach (Image image in menuPanel.GetComponentsInChildren<Image>(true))
                {
                    if (image.gameObject.name.EndsWith("Section"))
                    {
                        image.color = Settings.Instance.sectionColor;
                    }
                }
            }
        }

        public void UpdateAllButtonColors()
        {
            foreach (PanelHost host in AllHosts)
            {
                if (host.MenuCanvas == null)
                {
                    continue;
                }

                Transform menuPanel = host.MenuCanvas.transform.Find("MenuPanel");
                if (menuPanel == null)
                {
                    continue;
                }

                foreach (Image image in menuPanel.GetComponentsInChildren<Image>(true))
                {
                    if (image.GetComponent<Button>() != null)
                    {
                        image.color = Settings.Instance.buttonColor;
                    }
                }
            }
        }
    }
}
