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
    /// - Cab displays: up to <see cref="Settings.MaxDisplaysPerCar"/> canvases parented to the current locomotive
    ///   interior, each placed where the player is looking. Each one remembers its own pose, size, panel and locks
    ///   against the locomotive type, and the whole set is restored when the player boards that type again.
    /// - Wrist panel: one canvas parented to a VR controller so it can be glanced at like a watch.
    /// </remarks>
    public class MenuManager : MonoBehaviour
    {
        private const float WristSearchInterval = 2f;
        private const float BaseCanvasScale = 0.001f;

        /// <summary>
        /// How big a display starts out, in canvas units, before anyone drags it. Panels have no sizes of their
        /// own any more: a display is whatever size the player has made it, whichever panel it is showing.
        /// </summary>
        public static readonly Vector2 DefaultPanelSize = new(240f, 380f);

        /// <summary>Smallest a display can be dragged, in canvas units: still wide enough for the title row buttons.</summary>
        public static readonly Vector2 MinPanelSize = new(140f, 120f);

        /// <summary>Largest a display can be dragged, in canvas units. At scale 1 that is 2 by 1.4 metres.</summary>
        public static readonly Vector2 MaxPanelSize = new(2000f, 1400f);

        private static MenuManager? instance;

        /// <summary>The displays live in the locomotive the player is in, one per saved slot for its type.</summary>
        private readonly List<CabDisplayHost> cabDisplays = new();
        private readonly WristPanelHost wristPanel = new();
        private GameObject? templateCanvas;

        private TrainCar? cabDisplayCar;
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
                foreach (CabDisplayHost display in cabDisplays)
                {
                    yield return display;
                }
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
            Chat,
            StandardMessages,
            CommandMessages,
            TimedMessages,
            Config1,
            Config2,
            Displays,
            Debug
        }

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
            canvasRect.sizeDelta = DefaultPanelSize;
            canvasRect.localScale = Vector3.one * BaseCanvasScale;

            // Add GraphicRaycaster for non-VR clicks
            templateCanvas.AddComponent<GraphicRaycaster>();

            // Create template panel
            GameObject menuPanel = new("MenuPanel");
            menuPanel.transform.SetParent(templateCanvas.transform, false);
            RectTransform panelRect = menuPanel.AddComponent<RectTransform>();
            panelRect.sizeDelta = DefaultPanelSize;
            panelRect.anchorMin = new Vector2(0f, 1f);
            panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.localPosition = Vector3.zero;
            panelRect.localRotation = Quaternion.identity;

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
            _ = new ChatPanel(parent);
            _ = new StandardMessagesPanel(parent);
            _ = new CommandMessagesPanel(parent);
            _ = new TimedMessagesPanel(parent);
            _ = new Config1Panel(parent);
            _ = new Config2Panel(parent);
            _ = new DisplaysPanel(parent);
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

            UpdateCabDisplays(inSession);
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
        // Cab displays
        // ------------------------------------------------------------------

        /// <summary>
        /// Keeps the live displays matching the locomotive the player is in, then shows and feeds each one.
        /// </summary>
        private void UpdateCabDisplays(bool inSession)
        {
            if (!inSession)
            {
                ClearCabDisplays();
                cabDisplayCar = null;
                return;
            }

            TrainCar? car = PlayerManager.Car;

            // Only follow the player into an actual car. Stepping outside leaves the displays where they are,
            // riding along with the locomotive they belong to.
            if (car != null && (car != cabDisplayCar || AnyCanvasLost()))
            {
                if (!AnyDisplayHeld())
                {
                    SyncDisplaysToCar(car);
                }
            }

            bool visible = Settings.Instance.cabDisplayVisible;

            foreach (CabDisplayHost display in cabDisplays)
            {
                if (display.MenuCanvas == null)
                {
                    continue;
                }

                bool shouldShow = display.Placed && visible;
                if (display.MenuCanvas.activeSelf != shouldShow)
                {
                    display.MenuCanvas.SetActive(shouldShow);
                    if (shouldShow)
                    {
                        ShowPanel(display.ActivePanel, display);
                    }
                }

                if (shouldShow)
                {
                    display.MenuCanvas.transform.localScale = Vector3.one * BaseCanvasScale * Settings.Instance.cabDisplayScale;
                    UpdatePanelValues(display);
                }
            }
        }

        /// <summary>True if a canvas went away with the car it was parented to.</summary>
        private bool AnyCanvasLost()
        {
            foreach (CabDisplayHost display in cabDisplays)
            {
                if (display.MenuCanvas == null)
                {
                    return true;
                }
            }
            return false;
        }

        private bool AnyDisplayHeld()
        {
            foreach (CabDisplayHost display in cabDisplays)
            {
                if (display.Handles != null && display.Handles.IsBusy)
                {
                    return true;
                }
            }
            return false;
        }

        private void OnPlayerCarChanged(TrainCar car)
        {
            if (car == null || car == cabDisplayCar || AnyDisplayHeld())
            {
                return;
            }
            SyncDisplaysToCar(car);
        }

        /// <summary>
        /// Rebuilds the live displays from the slots saved for the given car's locomotive type, dropping
        /// whatever was built for the car before it.
        /// </summary>
        private void SyncDisplaysToCar(TrainCar car)
        {
            ClearCabDisplays();
            cabDisplayCar = car;

            string key = CarKey(car);
            List<CabDisplayPose> slots = SlotsFor(key);

            foreach (CabDisplayPose slot in slots)
            {
                CabDisplayHost display = CreateCabDisplay(slot);
                AttachCabDisplay(display, car, slot.localPosition, Quaternion.Euler(slot.localEuler));
            }

            if (slots.Count > 0)
            {
                Main.LogEntry("CabDisplay", $"Restored {slots.Count} display(s) saved for {key}.");
            }
            RefreshDisplaysPanels();
        }

        private static List<CabDisplayPose> SlotsFor(string carId)
        {
            return Settings.Instance.cabDisplayPoses
                .Where(p => p.carId == carId)
                .Take(Settings.MaxDisplaysPerCar)
                .ToList();
        }

        private CabDisplayHost CreateCabDisplay(CabDisplayPose slot)
        {
            CabDisplayHost display = new(slot, cabDisplays.Count + 1);
            cabDisplays.Add(display);
            CreateMenuCanvas(display);
            return display;
        }

        private void ClearCabDisplays()
        {
            foreach (CabDisplayHost display in cabDisplays)
            {
                if (display.MenuCanvas != null)
                {
                    Destroy(display.MenuCanvas);
                }
            }
            cabDisplays.Clear();
        }

        private void AttachCabDisplay(CabDisplayHost display, TrainCar car, Vector3 localPosition, Quaternion localRotation)
        {
            if (display.MenuCanvas == null)
            {
                return;
            }

            Transform parent = car.interior != null ? car.interior : car.transform;
            Transform canvas = display.MenuCanvas.transform;
            canvas.SetParent(parent, false);
            canvas.localPosition = localPosition;
            canvas.localRotation = localRotation;
            canvas.localScale = Vector3.one * BaseCanvasScale * Settings.Instance.cabDisplayScale;
            display.Placed = true;
        }

        /// <summary>
        /// Adds a display where the player is looking and saves it as a new slot for this locomotive type,
        /// up to <see cref="Settings.MaxDisplaysPerCar"/> of them.
        /// </summary>
        public void PlaceCabDisplay()
        {
            string methodName = "CabDisplay";

            Camera? camera = PlayerManager.PlayerCamera != null ? PlayerManager.PlayerCamera : Camera.main;
            if (camera == null || PlayerManager.PlayerTransform == null)
            {
                Main.LogEntry(methodName, "Cannot place a display outside of a game session.");
                NotificationManager.SetVariable("alertMessage", "Displays can only be placed while in a game session.");
                return;
            }

            TrainCar? car = PlayerManager.Car;
            if (car == null)
            {
                Main.LogEntry(methodName, "Cannot place a display outside of a car; there would be no locomotive to save it against.");
                NotificationManager.SetVariable("alertMessage", "Stand in a locomotive to place a display. Displays are saved per locomotive type.");
                return;
            }

            if (car != cabDisplayCar)
            {
                SyncDisplaysToCar(car);
            }

            string key = CarKey(car);
            if (cabDisplays.Count >= Settings.MaxDisplaysPerCar)
            {
                Main.LogEntry(methodName, $"{key} already has the maximum of {Settings.MaxDisplaysPerCar} displays.");
                NotificationManager.SetVariable("alertMessage", $"This locomotive already has {Settings.MaxDisplaysPerCar} displays. Close one from the Displays panel first.");
                return;
            }

            CabDisplayPose slot = new() { carId = key };
            Settings.Instance.cabDisplayPoses.Add(slot);
            CabDisplayHost display = CreateCabDisplay(slot);

            if (display.MenuCanvas == null)
            {
                return;
            }

            Transform canvas = display.MenuCanvas.transform;
            Vector3 position = camera.transform.position + camera.transform.forward * Settings.Instance.cabDisplayDistance;
            Quaternion rotation = Quaternion.LookRotation(position - camera.transform.position, Vector3.up);
            Transform parent = car.interior != null ? car.interior : car.transform;

            canvas.SetParent(parent, true);
            canvas.SetPositionAndRotation(position, rotation);
            canvas.localScale = Vector3.one * BaseCanvasScale * Settings.Instance.cabDisplayScale;

            display.Placed = true;
            Settings.Instance.cabDisplayVisible = true;
            SaveSlotPose(display);

            canvas.gameObject.SetActive(true);
            ShowPanel(display.ActivePanel, display);
            RefreshDisplaysPanels();

            Main.LogEntry(methodName, $"Display {cabDisplays.Count} placed on {key} at {position}.");
            NotificationManager.SetVariable("alertMessage", $"Display {cabDisplays.Count} of {Settings.MaxDisplaysPerCar} placed and saved for {key}.");
        }

        /// <summary>
        /// Removes one display and forgets its saved slot.
        /// </summary>
        public void CloseCabDisplay(CabDisplayHost display)
        {
            if (!cabDisplays.Remove(display))
            {
                return;
            }

            Settings.Instance.cabDisplayPoses.Remove(display.Slot);
            Settings.Instance.RequestSave();

            if (display.MenuCanvas != null)
            {
                Destroy(display.MenuCanvas);
            }

            Main.LogEntry("CabDisplay", $"Display closed; {cabDisplays.Count} left in {display.Slot.carId}.");
            NotificationManager.SetVariable("alertMessage", $"Display closed. {cabDisplays.Count} of {Settings.MaxDisplaysPerCar} left in this locomotive.");
            RefreshDisplaysPanels();
        }

        /// <summary>
        /// Shows or hides every display in the current locomotive, without moving any of them.
        /// </summary>
        public void ToggleCabDisplay()
        {
            Settings.Instance.cabDisplayVisible = !Settings.Instance.cabDisplayVisible;
            Settings.Instance.RequestSave();
            Main.LogEntry("CabDisplay", $"Displays visible: {Settings.Instance.cabDisplayVisible}");

            if (Settings.Instance.cabDisplayVisible && cabDisplays.Count == 0)
            {
                NotificationManager.SetVariable("alertMessage", "No displays have been placed in this locomotive yet. Use Place Display first.");
            }
        }

        /// <summary>
        /// Called by <see cref="PanelGrabHandles"/> when a hand lets go of a display, or finishes resizing one.
        /// </summary>
        private void OnCabDisplayChanged(CabDisplayHost display)
        {
            SaveSlotPose(display);
        }

        /// <summary>
        /// Writes a display's current pose and size back to its slot.
        /// </summary>
        private static void SaveSlotPose(CabDisplayHost display)
        {
            if (display.MenuCanvas == null)
            {
                return;
            }

            Transform canvas = display.MenuCanvas.transform;
            display.Slot.localPosition = canvas.localPosition;
            display.Slot.localEuler = canvas.localRotation.eulerAngles;
            Settings.Instance.RequestSave();
        }

        /// <summary>
        /// Redraws the Displays panel on every host, since it lists all the displays in the locomotive.
        /// </summary>
        private void RefreshDisplaysPanels()
        {
            foreach (PanelHost host in AllHosts)
            {
                host.DisplaysPanel?.Rebuild(cabDisplays, host);
            }
        }

        private static string CarKey(TrainCar car)
        {
            return car.carLivery != null ? car.carLivery.id : car.carType.ToString();
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
            host.ChatPanel = new ChatPanel(menuPanel);
            host.StandardMessagesPanel = new StandardMessagesPanel(menuPanel);
            host.CommandMessagesPanel = new CommandMessagesPanel(menuPanel);
            host.TimedMessagesPanel = new TimedMessagesPanel(menuPanel);
            host.Config1Panel = new Config1Panel(menuPanel);
            host.Config2Panel = new Config2Panel(menuPanel);
            host.DisplaysPanel = new DisplaysPanel(menuPanel);
            host.DebugPanel = new DebugPanel(menuPanel);

            // Explicitly hide all panels immediately after creation
            HideAllPanels(host);

            // Wire up back button events
            host.AuthenticationPanel.OnBackButtonClicked += () => ShowPanel("Main", host);
            host.StatusPanel.OnBackButtonClicked += () => ShowPanel("Main", host);
            host.NotificationsPanel.OnBackButtonClicked += () => ShowPanel("Main", host);
            host.ChatPanel.OnBackButtonClicked += () => ShowPanel("Main", host);
            host.StandardMessagesPanel.OnBackButtonClicked += () => ShowPanel("Main", host);
            host.CommandMessagesPanel.OnBackButtonClicked += () => ShowPanel("Main", host);
            host.TimedMessagesPanel.OnBackButtonClicked += () => ShowPanel("Main", host);
            host.Config1Panel.OnBackButtonClicked += () => ShowPanel("Main", host);
            host.Config2Panel.OnBackButtonClicked += () => ShowPanel("Main", host);
            host.DisplaysPanel.OnBackButtonClicked += () => ShowPanel("Main", host);
            host.DebugPanel.OnBackButtonClicked += () => ShowPanel("Main", host);

            // Cab displays are the hosts the player moves and resizes, so they are the ones that get grab bars
            if (host is CabDisplayHost display && menuPanel != null)
            {
                display.Handles = host.MenuCanvas.AddComponent<PanelGrabHandles>();
                display.Handles.Initialize(menuPanel, display, () => OnCabDisplayChanged(display));
                host.SetCloseAction(() => CloseCabDisplay(display));
            }

            Main.LogEntry("CreateMenuCanvas", $"Created panel stack for host {host.Name}.");
        }

        private static void HideAllPanels(PanelHost host)
        {
            host.MainPanel?.Hide();
            host.AuthenticationPanel?.Hide();
            host.StatusPanel?.Hide();
            host.NotificationsPanel?.Hide();
            host.ChatPanel?.Hide();
            host.StandardMessagesPanel?.Hide();
            host.CommandMessagesPanel?.Hide();
            host.TimedMessagesPanel?.Hide();
            host.Config1Panel?.Hide();
            host.Config2Panel?.Hide();
            host.DisplaysPanel?.Hide();
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
                "Chat" => PanelType.Chat,

                // Settings written before the sized display panels were merged into one
                "Large Display" or "Medium Display" or "Wide Display" or "Small Display" => PanelType.Chat,

                "Standard Messages" => PanelType.StandardMessages,
                "Command Messages" => PanelType.CommandMessages,
                "Timed Messages" => PanelType.TimedMessages,
                "Config1" => PanelType.Config1,
                "Config2" => PanelType.Config2,
                "Displays" => PanelType.Displays,
                "Debug" => PanelType.Debug,
                _ => PanelType.Main
            };

            // Panels have no size of their own: a display keeps whatever size it has been dragged to, whichever
            // panel it shows. A display that has never been sized starts at the default.
            if (host is CabDisplayHost sizedDisplay)
            {
                Vector2 size = sizedDisplay.Slot.panelSize;
                if (size.x < MinPanelSize.x || size.y < MinPanelSize.y)
                {
                    size = DefaultPanelSize;
                    sizedDisplay.Slot.panelSize = size;
                }

                Transform menuPanel = host.MenuCanvas.transform.Find("MenuPanel");
                if (menuPanel != null)
                {
                    host.MenuCanvas.GetComponent<RectTransform>().sizeDelta = size;
                    menuPanel.GetComponent<RectTransform>().sizeDelta = size;
                }
            }

            if (panelType == PanelType.Displays)
            {
                host.DisplaysPanel?.Rebuild(cabDisplays, host);
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
                case PanelType.Chat:
                    host.ChatPanel?.Show();
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
                case PanelType.Displays:
                    host.DisplaysPanel?.Show();
                    break;
                case PanelType.Debug:
                    host.DebugPanel?.Show();
                    break;
            }

            // Remember the active panel for this host, under its current name so the legacy sized-display
            // names are not written back out again
            host.ActivePanel = panelType == PanelType.Chat ? "Chat" : panelName;
            Settings.Instance.RequestSave();
        }

        /// <summary>
        /// Adds a chat message to the Chat panel of every host that exists, so a message costs one entry per
        /// display rather than one per display size as it used to.
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
                    // Kept up to date even while another panel is showing, so the history is there when Chat is opened
                    host.ChatPanel?.AddChatMessage(username, message);
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
