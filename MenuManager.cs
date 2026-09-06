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
    /// - Wrist panel: one canvas on the pad on the back of a VR hand, resting as a small button until the player
    ///   presses it, so it can be glanced at like a watch.
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

        /// <summary>
        /// How big the collapsed wrist button is, in canvas units. Small enough to sit on the back of a hand
        /// without covering it, but still a comfortable target for a VR fingertip.
        /// </summary>
        private static readonly Vector2 WristButtonSize = new(96f, 40f);

        /// <summary>How far the panel floats off the back of the hand, in metres, so it does not clip the glove.</summary>
        private const float WristSurfaceLift = 0.005f;

        /// <summary>
        /// Where the two wrist surfaces sit on the game's hand pad, in the pad's own space: metres from it, and
        /// how they are turned against it. Measured by placing them by hand in VR on a Quest 3, and good for any
        /// controller, since the pad is the same object on every hand however the game fits that hand to the
        /// controller. The rotations are kept as the fit they were measured against times the turn that was
        /// made from it, which is exactly what produced them, rather than a single number worked out by hand.
        /// The player's own placement is applied on top of these.
        /// </summary>
        private static readonly Vector3 PadMenuPosition = new(-0.0614f, 0.0813f, 0.0658f);
        private static readonly Quaternion PadMenuRotation = Quaternion.Euler(270f, 180f, 0f) * Quaternion.Euler(38.4911f, -177.0492f, 19.1316f);
        private static readonly Vector3 PadButtonPosition = new(-0.0963f, 0.0804f, -0.0863f);
        private static readonly Quaternion PadButtonRotation = Quaternion.Euler(270f, 180f, 0f) * Quaternion.Euler(10.2575f, -83.4423f, 67.3378f);

        /// <summary>How many searches to give the game's hand pad before settling for the bare controller pose.</summary>
        private const int WristPadAttempts = 5;

        /// <summary>
        /// Gap in canvas units between the anchor on the back of the hand and the near edge of the open menus,
        /// which unfold back down the forearm rather than sitting on top of the hand.
        /// </summary>
        private const float WristMenuGap = 10f;

        private static MenuManager? instance;

        /// <summary>The displays live in the locomotive the player is in, one per saved slot for its type.</summary>
        private readonly List<CabDisplayHost> cabDisplays = new();
        private readonly WristPanelHost wristPanel = new();
        private GameObject? templateCanvas;

        private TrainCar? cabDisplayCar;

        /// <summary>What the wrist canvas is parented to: the pad on the back of the hand, or the controller.</summary>
        private Transform? wristAnchor;
        private float nextWristSearchTime;
        private int wristAnchorAttempts;

        /// <summary>True while the canvas is on the hand pad rather than making do with the controller pose.</summary>
        private bool wristOnPad;

        /// <summary>Carries the open menus around while the player is placing them, on the canvas that holds them.</summary>
        private WristPanelGrab? wristGrab;

        /// <summary>The same for the button the menus fold down to, which is placed on the hand separately.</summary>
        private WristPanelGrab? wristButtonGrab;

        private string wristAnchorSummary = "nothing yet";

        /// <summary>What the wrist panel is hanging off, in a few words, for the panel that places it.</summary>
        public string WristAnchorSummary => wristAnchorSummary;

        /// <summary>How each surface sits on the anchor before the player's own placement is applied.</summary>
        private WristFit menuFit;
        private WristFit buttonFit;

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

        /// <summary>How a wrist surface sits on its anchor, in the anchor's own space, before any placement.</summary>
        private struct WristFit
        {
            public Vector3 Position;
            public Quaternion Rotation;
        }

        /// <summary>
        /// Panel names written into settings before the sized chat displays were merged into one. A
        /// display saved under one of these still opens, on the single Chat panel that replaced them.
        /// </summary>
        private static readonly Dictionary<string, string> RenamedPanels = new()
        {
            ["Large Display"] = "Chat",
            ["Medium Display"] = "Chat",
            ["Wide Display"] = "Chat",
            ["Small Display"] = "Chat"
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
            // Normally already done during Load; harmless to repeat, and it means a display can still be
            // built if the manager is reached before the plugins have been looked for
            Plugins.PanelRegistry.RegisterBuiltIns();

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
        private static void CreatePanelTemplates(Transform parent)
        {
            foreach (Plugins.PanelDescriptor descriptor in Plugins.PanelRegistry.Available)
            {
                _ = descriptor.Create(parent, null);
            }

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
            // Whichever surface a hand is carrying is left where that hand has it; ApplyWristPose sees to that
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
                host.Get<DisplaysPanel>()?.Rebuild(cabDisplays, host);
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
                if (wristPanel.ButtonCanvas != null && wristPanel.ButtonCanvas.activeSelf)
                {
                    wristPanel.ButtonCanvas.SetActive(false);
                }
                if (!inSession)
                {
                    ClearWristAnchor();
                }
                return;
            }

            if (wristPanel.MenuCanvas == null)
            {
                CreateMenuCanvas(wristPanel);
                ClearWristAnchor();
            }

            bool wantLeft = Settings.Instance.wristPanelOnLeftHand;
            if (wristAnchor != null && wristPanel.AttachedToLeftHand != wantLeft)
            {
                ClearWristAnchor(); // hand preference changed, re-attach
            }

            // Keep looking while there is no anchor at all, and while making do with the controller, so the
            // panel moves onto the hand pad as soon as the game has built the hands
            if ((wristAnchor == null || !wristOnPad) && Time.unscaledTime >= nextWristSearchTime)
            {
                nextWristSearchTime = Time.unscaledTime + WristSearchInterval;
                TryAttachWristPanel(wantLeft);
            }

            GameObject canvas = wristPanel.MenuCanvas!;
            bool shouldShow = wristAnchor != null;
            if (canvas.activeSelf != shouldShow)
            {
                canvas.SetActive(shouldShow);
                if (shouldShow)
                {
                    ApplyWristExpansion();
                }
            }

            UpdateWristPlacing(shouldShow);

            // Only the open menus need feeding; while collapsed there is nothing on show but the button
            if (shouldShow && wristPanel.Expanded)
            {
                UpdatePanelValues(wristPanel);
            }
        }

        /// <summary>
        /// Decides which of the two surfaces, if either, is being placed at the moment, and shows the button
        /// alongside the menus while it is the one being placed so the player can see where it is going.
        /// </summary>
        private void UpdateWristPlacing(bool attached)
        {
            bool adjusting = attached && WristAdjustShowing();
            bool placingButton = adjusting && PlacingWristButton;

            if (wristGrab != null)
            {
                // Only ever carry what can be seen: the menus are only on show while the panel is open
                wristGrab.AdjustMode = adjusting && !PlacingWristButton && wristPanel.Expanded;
            }
            if (wristButtonGrab != null)
            {
                wristButtonGrab.AdjustMode = placingButton;
            }

            if (wristPanel.ButtonCanvas == null)
            {
                return;
            }

            bool showButton = attached && (!wristPanel.Expanded || placingButton);
            if (wristPanel.ButtonCanvas.activeSelf != showButton)
            {
                wristPanel.ButtonCanvas.SetActive(showButton);
            }
        }

        /// <summary>True while some host has the panel that places the wrist panel on show.</summary>
        private bool WristAdjustShowing()
        {
            foreach (PanelHost host in AllHosts)
            {
                if (host.MenuCanvas == null || !host.MenuCanvas.activeSelf || host.ActivePanel != "Wrist Adjust")
                {
                    continue;
                }
                if (host is WristPanelHost wrist && !wrist.Expanded)
                {
                    continue; // folded away, so nothing is on show to place from
                }
                return true;
            }

            return false;
        }

        /// <summary>Which of the wrist panel's two surfaces the adjust panel is placing.</summary>
        public bool PlacingWristButton { get; private set; }

        /// <summary>Switches the adjust panel between placing the open menus and placing the button.</summary>
        public void SetPlacingWristButton(bool button)
        {
            PlacingWristButton = button;
            Main.LogEntry("WristPanel", button ? "Now placing the hand button." : "Now placing the floating menus.");
        }

        /// <summary>
        /// Opens the wrist menus, or folds them back down into the button.
        /// </summary>
        private void SetWristExpanded(bool expanded)
        {
            wristPanel.Expanded = expanded;
            ApplyWristExpansion();
            Main.LogEntry("WristPanel", expanded ? "Wrist panel opened." : "Wrist panel collapsed back to its button.");
        }

        /// <summary>
        /// Shows either the panel stack or the collapsed button, whichever state the wrist panel is in.
        /// </summary>
        private void ApplyWristExpansion()
        {
            if (wristPanel.MenuCanvas == null)
            {
                return;
            }

            Transform? menuPanel = wristPanel.MenuCanvas.transform.Find("MenuPanel");
            if (menuPanel != null)
            {
                menuPanel.gameObject.SetActive(wristPanel.Expanded);
            }

            if (wristPanel.Expanded)
            {
                ShowPanel(wristPanel.ActivePanel, wristPanel);
            }
        }

        /// <summary>
        /// Builds the button the wrist panel rests as, on a canvas of its own so it can be placed on the hand
        /// separately from the menus. Pressing it opens them.
        /// </summary>
        private void CreateWristButton(WristPanelHost host)
        {
            if (host.MenuCanvas == null)
            {
                return;
            }

            if (host.ButtonCanvas != null)
            {
                Destroy(host.ButtonCanvas); // the menus have been rebuilt, so the button is rebuilt with them
                host.ButtonCanvas = null;
            }

            GameObject buttonCanvas = new("MenuCanvas_WristButton");
            Canvas canvasComponent = buttonCanvas.AddComponent<Canvas>();
            canvasComponent.renderMode = RenderMode.WorldSpace;
            canvasComponent.sortingOrder = 1000;
            buttonCanvas.AddComponent<GraphicRaycaster>();

            RectTransform canvasRect = buttonCanvas.GetComponent<RectTransform>();
            canvasRect.sizeDelta = WristButtonSize;

            Button button = PanelConstructor.Button.Create(
                buttonCanvas.transform,
                "TwitchChat",
                0,
                0,
                Color.white,
                () => SetWristExpanded(true));

            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = WristButtonSize;
            rect.anchoredPosition = Vector2.zero;

            // The collider is sized from the button's text, so widen it to the button we have just made
            BoxCollider? collider = button.GetComponent<BoxCollider>();
            if (collider != null)
            {
                collider.size = new Vector3(WristButtonSize.x, WristButtonSize.y, 1f);
            }

            Text? label = button.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.fontSize = 16;
            }

            wristButtonGrab = buttonCanvas.AddComponent<WristPanelGrab>();
            wristButtonGrab.Initialize(() => OnWristSurfacePlaced(true));
            wristButtonGrab.WornOnLeft = host.AttachedToLeftHand;

            host.ButtonCanvas = buttonCanvas;
            buttonCanvas.SetActive(!host.Expanded);
        }

        /// <summary>
        /// Puts the menus and the button where their own settings say, unless a hand is busy carrying one.
        /// </summary>
        private void ApplyWristPose()
        {
            if (wristAnchor == null)
            {
                return;
            }

            if (wristPanel.MenuCanvas != null && (wristGrab == null || !wristGrab.IsHeld))
            {
                PoseWristSurface(wristPanel.MenuCanvas.transform, menuFit, Settings.Instance.wristMenuOffset, Settings.Instance.wristMenuAngle);
            }

            if (wristPanel.ButtonCanvas != null && (wristButtonGrab == null || !wristButtonGrab.IsHeld))
            {
                PoseWristSurface(wristPanel.ButtonCanvas.transform, buttonFit, Settings.Instance.wristButtonOffset, Settings.Instance.wristButtonAngle);
            }
        }

        /// <summary>
        /// Sits one canvas on the hand: where its measured fit puts it, then moved and turned by its settings.
        /// </summary>
        private void PoseWristSurface(Transform canvas, WristFit fit, Vector3 nudge, Vector3 tilt)
        {
            // The anchor belongs to the game, so it may well be scaled, and the hand pad is built at a tenth of
            // a unit. Divide that back out, or the panel comes out a tenth of the size it should be.
            float anchorScale = Mathf.Max(0.0001f, wristAnchor!.lossyScale.x);

            // The nudge is in the surface's own directions: x across it, y along it, z out of the hand
            Vector3 offset = fit.Position + (fit.Rotation * new Vector3(nudge.x, nudge.y, -nudge.z));

            canvas.localPosition = offset / anchorScale;
            canvas.localRotation = fit.Rotation * Quaternion.Euler(tilt);
            canvas.localScale = Vector3.one * (BaseCanvasScale * Settings.Instance.wristPanelScale / anchorScale);
        }

        /// <summary>
        /// Parents the canvas to the sticky pad on the back of the chosen hand, which is where the game itself
        /// puts anything the player is carrying, and squares the panel up with it. Falls back to the bare
        /// controller pose if the pad never turns up.
        /// </summary>
        private void TryAttachWristPanel(bool wantLeft)
        {
            GameObject? controller = wantLeft
                ? VRTK_DeviceFinder.GetControllerLeftHand(true)
                : VRTK_DeviceFinder.GetControllerRightHand(true);
            if (controller == null || wristPanel.MenuCanvas == null)
            {
                return;
            }

            wristAnchorAttempts++;
            Transform? pad = FindHandPad(wantLeft, controller);
            bool onPad = pad != null;

            // The hands are built a moment after the controllers appear, so give the pad a few tries to show up.
            // Once we have settled for the controller there is nothing to redo until the pad finally appears.
            if (!onPad && (wristAnchor != null || wristAnchorAttempts < WristPadAttempts))
            {
                return;
            }

            Transform anchor = onPad ? pad! : controller.transform;
            AlignWristPanel(anchor, controller.transform, onPad);

            wristAnchor = anchor;
            wristOnPad = onPad;
            wristPanel.AttachedToLeftHand = wantLeft;
            wristPanel.MenuCanvas.transform.SetParent(anchor, false);
            wristPanel.ButtonCanvas?.transform.SetParent(anchor, false);
            ApplyWristPose();

            string hand = wantLeft ? "left" : "right";
            wristAnchorSummary = onPad ? $"{hand} hand pad" : $"{hand} controller, no pad";
            if (wristGrab != null)
            {
                wristGrab.WornOnLeft = wantLeft;
            }
            if (wristButtonGrab != null)
            {
                wristButtonGrab.WornOnLeft = wantLeft;
            }

            Main.LogEntry("WristPanel", onPad
                ? $"Wrist panel attached to the {hand} hand pad ('{anchor.name}'), scale {anchor.lossyScale}."
                : $"No {hand} hand pad found, so the wrist panel fell back to the controller ('{anchor.name}').");
        }

        /// <summary>
        /// Finds the pad on the back of a hand. The game's own lookup is the first choice, but it answers from
        /// a cache that is only filled once it has built the hands, so failing that the pad is looked for by
        /// name under the controller.
        /// </summary>
        private static Transform? FindHandPad(bool wantLeft, GameObject controller)
        {
            try
            {
                Transform? pad = PipaUtils.PipaTransform(wantLeft
                    ? SDK_BaseController.ControllerHand.Left
                    : SDK_BaseController.ControllerHand.Right);
                if (pad != null)
                {
                    return pad;
                }
            }
            catch (Exception ex)
            {
                Main.LogEntry("WristPanel", $"The game could not name its hand pad: {ex.Message}");
            }

            string exact = wantLeft ? "[pipa l]" : "[pipa r]";
            Transform? loose = null;

            foreach (Transform child in controller.GetComponentsInChildren<Transform>(true))
            {
                if (!child.gameObject.activeInHierarchy)
                {
                    continue;
                }

                string name = child.name.ToLowerInvariant();
                if (name == exact || name == "[pipa]")
                {
                    return child;
                }
                if (loose == null && name.Contains("pipa") && !name.Contains("attach"))
                {
                    loose = child;
                }
            }

            return loose;
        }

        /// <summary>
        /// Works out how the panel should sit on its anchor: flat against the back of the hand, facing out of
        /// it, with the top of the panel towards the fingers. The anchor is the game's, so rather than assume
        /// which way it was built, each direction is matched to whichever of the anchor's own axes fits it.
        /// </summary>
        private void AlignWristPanel(Transform anchor, Transform controller, bool onPad)
        {
            if (onPad)
            {
                // The pad is the same object on every hand, so how a surface should sit on it is a measurement
                // rather than a guess
                menuFit = new WristFit { Position = PadMenuPosition, Rotation = PadMenuRotation };
                buttonFit = new WristFit { Position = PadButtonPosition, Rotation = PadButtonRotation };
                return;
            }

            // Without the pad there is nothing measured to go on, so both surfaces are squared up with the
            // controller as best it can be guessed: out of its top, with the top of the panel where it points
            Vector3 normal = NearestLocalAxis(anchor, anchor.up, Vector3.zero);
            Vector3 up = NearestLocalAxis(anchor, controller.forward, normal);

            // A canvas is read from the side its forward points away from, so aim forward into the hand
            WristFit guess = new()
            {
                Position = normal * WristSurfaceLift,
                Rotation = Quaternion.LookRotation(-normal, up)
            };

            menuFit = guess;
            buttonFit = guess;

            Main.LogEntry("WristPanel", $"No pad to measure against on '{anchor.name}', so both surfaces face out " +
                $"along its {normal} with {up} towards the fingers, scale {anchor.lossyScale}.");
        }

        /// <summary>
        /// Picks whichever of the anchor's own six axis directions points most nearly the way we want, so the
        /// panel lands square on the anchor however the game happened to build it.
        /// </summary>
        /// <param name="anchor">The transform whose axes are being chosen between.</param>
        /// <param name="worldTarget">The direction being matched, in world space.</param>
        /// <param name="skip">An axis already spoken for, which rules out that axis and its opposite.</param>
        /// <returns>The chosen axis, in the anchor's local space.</returns>
        private static Vector3 NearestLocalAxis(Transform anchor, Vector3 worldTarget, Vector3 skip)
        {
            Vector3[] candidates = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
            Vector3 target = worldTarget.normalized;
            Vector3 best = Vector3.up;
            float bestDot = float.NegativeInfinity;

            foreach (Vector3 candidate in candidates)
            {
                if (skip != Vector3.zero && Mathf.Abs(Vector3.Dot(candidate, skip)) > 0.5f)
                {
                    continue;
                }

                float dot = Vector3.Dot(anchor.TransformDirection(candidate).normalized, target);
                if (dot > bestDot)
                {
                    bestDot = dot;
                    best = candidate;
                }
            }

            return best;
        }

        /// <summary>
        /// Takes where a hand has just put one of the wrist surfaces and works it back into that surface's own
        /// settings, so the numbers on the adjust panel, what is saved, and where it actually is all agree.
        /// </summary>
        /// <param name="isButton">True for the button, false for the open menus.</param>
        private void OnWristSurfacePlaced(bool isButton)
        {
            GameObject? surface = isButton ? wristPanel.ButtonCanvas : wristPanel.MenuCanvas;
            if (surface == null || wristAnchor == null)
            {
                return;
            }

            Transform canvas = surface.transform;
            WristFit fit = isButton ? buttonFit : menuFit;
            float anchorScale = Mathf.Max(0.0001f, wristAnchor.lossyScale.x);

            // Undo the surface's fit, which leaves exactly the player's own nudge and tilt on top of it
            Vector3 offset = (canvas.localPosition * anchorScale) - fit.Position;
            Vector3 inPanel = Quaternion.Inverse(fit.Rotation) * offset;
            Vector3 nudge = new(inPanel.x, inPanel.y, -inPanel.z);

            Vector3 tilt = (Quaternion.Inverse(fit.Rotation) * canvas.localRotation).eulerAngles;
            tilt = new Vector3(Mathf.DeltaAngle(0f, tilt.x), Mathf.DeltaAngle(0f, tilt.y), Mathf.DeltaAngle(0f, tilt.z));

            float limit = WristAdjustPanel.MoveLimit;
            nudge = new Vector3(
                Mathf.Clamp(nudge.x, -limit, limit),
                Mathf.Clamp(nudge.y, -limit, limit),
                Mathf.Clamp(nudge.z, -limit, limit));

            if (isButton)
            {
                Settings.Instance.wristButtonOffset = nudge;
                Settings.Instance.wristButtonAngle = tilt;
            }
            else
            {
                Settings.Instance.wristMenuOffset = nudge;
                Settings.Instance.wristMenuAngle = tilt;
            }
            Settings.Instance.RequestSave();

            // Snap to what was actually saved, so a surface put out of reach comes back to the edge of its range
            ApplyWristPose();

            // The whole pose against the anchor as well as the settings, since that is what a new built-in
            // default would have to be: it does not depend on the placement it was reached from
            string what = isButton ? "Hand button" : "Floating menus";
            Main.LogEntry("WristPanel", $"{what} placed by hand: nudge {nudge.ToString("F4")}, tilt {tilt.ToString("F2")}. " +
                $"On the anchor that is position {(canvas.localPosition * anchorScale).ToString("F4")} m, rotation {canvas.localRotation.eulerAngles.ToString("F2")}.");
        }

        private void ClearWristAnchor()
        {
            wristAnchor = null;
            wristOnPad = false;
            wristAnchorAttempts = 0;
        }

        // ------------------------------------------------------------------
        // Shared host plumbing
        // ------------------------------------------------------------------

        /// <summary>
        /// Refreshes whatever the host is currently showing. Only the visible panel is asked: the rest are
        /// hidden behind it, and a plugin panel reading another mod's state has no business doing so
        /// thirteen times over for panels nobody is looking at.
        /// </summary>
        private static void UpdatePanelValues(PanelHost host)
        {
            foreach (PanelConstructor.BasePanel panel in host.Panels)
            {
                if (panel.IsVisible)
                {
                    panel.Tick(Time.deltaTime);
                }
            }
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

            // Build every panel the registry currently offers, the mod's own and any from plugins alike.
            // Back on any of them returns to the main menu; back on the main menu is the one exception,
            // wired up below by whichever kind of host this is.
            host.ClearPanels();
            foreach (Plugins.PanelDescriptor descriptor in Plugins.PanelRegistry.Available)
            {
                PanelConstructor.BasePanel? panel = descriptor.Create(menuPanel, host);
                if (panel == null)
                {
                    continue;
                }

                host.AddPanel(descriptor.Id, panel);

                if (descriptor.Id != "Main")
                {
                    panel.OnBackButtonClicked += () => ShowPanel("Main", host);
                }
            }

            // Explicitly hide all panels immediately after creation
            HideAllPanels(host);

            // Cab displays are the hosts the player moves and resizes, so they are the ones that get grab bars
            if (host is CabDisplayHost display && menuPanel != null)
            {
                display.Handles = host.MenuCanvas.AddComponent<PanelGrabHandles>();
                display.Handles.Initialize(menuPanel, display, () => OnCabDisplayChanged(display));
                host.SetCloseAction(() => CloseCabDisplay(display));
            }

            // The wrist panel rests as a button: going back from its main menu folds the menus away again
            if (host is WristPanelHost wrist)
            {
                CreateWristButton(wrist);

                PanelConstructor.BasePanel? mainPanel = host.GetPanel("Main");
                if (mainPanel != null)
                {
                    mainPanel.OnBackButtonClicked += () => SetWristExpanded(false);
                }

                wristGrab = host.MenuCanvas.AddComponent<WristPanelGrab>();
                wristGrab.Initialize(() => OnWristSurfacePlaced(false));
                wristGrab.Reference = menuPanel;
                wristGrab.WornOnLeft = wrist.AttachedToLeftHand;

                if (menuPanel != null)
                {
                    // The button sits on the back of the hand, so the open menus start there and run back down
                    // the forearm instead of hanging half inside it
                    menuPanel.localPosition = new Vector3(0f, -((DefaultPanelSize.y / 2f) + WristMenuGap), 0f);
                    menuPanel.gameObject.SetActive(wrist.Expanded);
                }
            }

            Main.LogEntry("CreateMenuCanvas", $"Created panel stack for host {host.Name}.");
        }

        private static void HideAllPanels(PanelHost host)
        {
            foreach (PanelConstructor.BasePanel panel in host.Panels)
            {
                panel.Hide();
            }
        }

        private void ShowPanel(string panelName, PanelHost host)
        {
            if (host.MenuCanvas == null || !host.MenuCanvas.activeSelf)
            {
                return;
            }

            Main.LogEntry("ShowPanel", $"Showing panel {panelName} on host {host.Name}");

            HideAllPanels(host);

            string panelId = ResolvePanelId(panelName, host);

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

            // The two panels that list something the player has changed elsewhere are rebuilt on the way in
            if (panelId == "Displays")
            {
                host.Get<DisplaysPanel>()?.Rebuild(cabDisplays, host);
            }
            else if (panelId == "Mods")
            {
                host.Get<ModsPanel>()?.Rebuild();
            }

            host.GetPanel(panelId)?.Show();

            // Remember the active panel for this host, under the id it resolved to, so a name that has
            // since been renamed or removed is not written straight back out again
            host.ActivePanel = panelId;
            Settings.Instance.RequestSave();
        }

        /// <summary>
        /// Works out which panel a saved or clicked name means. A name that has been renamed since it was
        /// saved is followed to its replacement, and one this host has no panel for - a plugin that has
        /// been uninstalled, or switched off - falls back to the main menu rather than leaving the display
        /// blank.
        /// </summary>
        private static string ResolvePanelId(string panelName, PanelHost host)
        {
            if (RenamedPanels.TryGetValue(panelName, out string? renamed))
            {
                panelName = renamed;
            }

            if (host.GetPanel(panelName) != null)
            {
                return panelName;
            }

            Main.LogEntry("ShowPanel", $"Host {host.Name} has no panel called '{panelName}'; showing Main instead.");
            return "Main";
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
                host.Get<NotificationsPanel>()?.UpdateNotificationsEnabled(value);
            }
        }

        public void UpdateAllNotificationDurations(float value)
        {
            foreach (PanelHost host in AllHosts)
            {
                host.Get<NotificationsPanel>()?.UpdateNotificationDuration(value);
            }
        }

        public void UpdateAllProcessOwnToggles(bool value)
        {
            foreach (PanelHost host in AllHosts)
            {
                host.Get<DebugPanel>()?.UpdateProcessOwn(value);
            }
        }

        public void UpdateAllProcessDuplicatesToggles(bool value)
        {
            foreach (PanelHost host in AllHosts)
            {
                host.Get<DebugPanel>()?.UpdateProcessDuplicates(value);
            }
        }

        public void UpdateAllConnectMessageEnabledToggles(bool value)
        {
            foreach (PanelHost host in AllHosts)
            {
                host.Get<StandardMessagesPanel>()?.UpdateConnectMessageEnabled(value);
            }
        }

        public void UpdateAllDisconnectMessageEnabledToggles(bool value)
        {
            foreach (PanelHost host in AllHosts)
            {
                host.Get<StandardMessagesPanel>()?.UpdateDisconnectMessageEnabled(value);
            }
        }

        public void UpdateCommandsMessageToggles(bool value)
        {
            foreach (PanelHost host in AllHosts)
            {
                host.Get<CommandMessagesPanel>()?.UpdateCommandsMessageEnabled(value);
            }
        }

        public void UpdateInfoMessageToggles(bool value)
        {
            foreach (PanelHost host in AllHosts)
            {
                host.Get<CommandMessagesPanel>()?.UpdateInfoMessageEnabled(value);
            }
        }

        public void UpdateAllTimedMessageToggles(bool value)
        {
            foreach (PanelHost host in AllHosts)
            {
                host.Get<TimedMessagesPanel>()?.UpdateTimedMessagesEnabled(value);
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
