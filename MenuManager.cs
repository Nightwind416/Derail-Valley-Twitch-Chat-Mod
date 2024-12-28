using UnityEngine;
using UnityEngine.UI;
using System.Reflection;
using UnityEngine.EventSystems;
using UnityEngine.XR;

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
        private GameObject? mainPanel;
        private GameObject? settingsPanel;
        private GameObject? licenseObject;
        private GameObject? stickyTapeBase;

        private Text? usernameText;
        private Text? durationText;
        private Text? messageText;

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
            Canvas();
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
                        Canvas();
                    }

                    menuCanvas!.SetActive(true);

                    // Show main panel and hide settings panel
                    mainPanel!.SetActive(true);
                    settingsPanel!.SetActive(false);
                    isSettingsPanelVisible = false;
                }
            }
            
            if (isMenuVisible && licenseObject != null)
            {
                UpdateDisplayedValues();

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

        private void Canvas()
        {
            string methodName = MethodBase.GetCurrentMethod().Name;
            Main.LogEntry(methodName, "Creating menu UI elements");

            if (menuCanvas != null)
            {
                Main.LogEntry(methodName, "Menu already exists, destroying old instance");
                Destroy(menuCanvas);
            }

            // Create canvas and basic setup
            menuCanvas = new GameObject("MenuCanvas");
            Canvas canvas = menuCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 1000;
            
            RectTransform canvasRect = menuCanvas.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(200, 300);
            canvasRect.localScale = Vector3.one * 0.001f;

            menuCanvas.AddComponent<GraphicRaycaster>();
            
            // Create main panel and settings panel
            mainPanel = MainMenu();
            settingsPanel = SettingsPanel();

            // Set initial panel states
            mainPanel.SetActive(true);
            settingsPanel.SetActive(false);
            isSettingsPanelVisible = false;

            menuCanvas.SetActive(isMenuVisible);
            Main.LogEntry(methodName, "Menu creation completed");
        }

        private GameObject MainMenu()
        {
            GameObject panel = new("MainPanel");
            panel.transform.SetParent(menuCanvas!.transform, false);
            
            Image panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0, 0, 0, 0.8f);
            
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            panelRect.localScale = Vector3.one;

            // Create UI container
            GameObject uiContainer = new("UIContainer");
            uiContainer.transform.SetParent(panel.transform, false);
            RectTransform containerRect = uiContainer.AddComponent<RectTransform>();
            containerRect.anchorMin = Vector2.zero;
            containerRect.anchorMax = Vector2.one;
            containerRect.offsetMin = Vector2.zero;
            containerRect.offsetMax = Vector2.zero;

            // Add title
            GameObject titleObj = new("Title");
            titleObj.transform.SetParent(uiContainer.transform, false);
            Text titleText = titleObj.AddComponent<Text>();
            titleText.text = "TwitchChatMod Main Menu";
            titleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            titleText.fontSize = 24;
            titleText.alignment = TextAnchor.UpperCenter;
            titleText.color = Color.white;
            
            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0.9f);
            titleRect.anchorMax = new Vector2(1, 1f);
            titleRect.offsetMin = new Vector2(10, 0);
            titleRect.offsetMax = new Vector2(-10, 0);

            // Add settings button
            GameObject buttonObj = new("SettingsButton");
            buttonObj.transform.SetParent(uiContainer.transform, false);
            
            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            
            Button settingsButton = buttonObj.AddComponent<Button>();
            settingsButton.onClick.AddListener(ToggleSettingsPanel);
            
            GameObject buttonTextObj = new("ButtonText");
            buttonTextObj.transform.SetParent(buttonObj.transform, false);
            Text buttonText = buttonTextObj.AddComponent<Text>();
            buttonText.text = "Settings";
            buttonText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            buttonText.fontSize = 18;
            buttonText.alignment = TextAnchor.MiddleCenter;
            buttonText.color = Color.white;
            
            RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.3f, 0.8f);
            buttonRect.anchorMax = new Vector2(0.7f, 0.87f);
            buttonRect.offsetMin = Vector2.zero;
            buttonRect.offsetMax = Vector2.zero;
            
            RectTransform buttonTextRect = buttonTextObj.GetComponent<RectTransform>();
            buttonTextRect.anchorMin = Vector2.zero;
            buttonTextRect.anchorMax = Vector2.one;
            buttonTextRect.offsetMin = Vector2.zero;
            buttonTextRect.offsetMax = Vector2.zero;

            return panel;
        }

        private void ToggleSettingsPanel()
        {
            isSettingsPanelVisible = !isSettingsPanelVisible;
            mainPanel!.SetActive(!isSettingsPanelVisible);
            settingsPanel!.SetActive(isSettingsPanelVisible);
            
            RectTransform rect = settingsPanel!.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200, 300);
            rect.localPosition = Vector3.zero;
            rect.localScale = Vector3.one;
        }

        private GameObject SettingsPanel()
        {
            GameObject panel = new("SettingsPanel");
            panel.transform.SetParent(menuCanvas!.transform, false);
            
            Image panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0, 0, 0, 0.8f);
            
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.sizeDelta = new Vector2(200, 300);
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.localPosition = Vector3.zero;
            panelRect.localScale = Vector3.one;

            // Add back button
            GameObject backButton = new("BackButton");
            backButton.transform.SetParent(panel.transform, false);
            Button button = backButton.AddComponent<Button>();
            Image buttonImage = backButton.AddComponent<Image>();
            buttonImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            
            RectTransform buttonRect = backButton.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.3f, 0.1f);
            buttonRect.anchorMax = new Vector2(0.7f, 0.17f);
            buttonRect.offsetMin = Vector2.zero;
            buttonRect.offsetMax = Vector2.zero;
            
            GameObject buttonText = new("Text");
            buttonText.transform.SetParent(backButton.transform, false);
            Text text = buttonText.AddComponent<Text>();
            text.text = "Back";
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 18;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            
            RectTransform textRect = buttonText.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            button.onClick.AddListener(ToggleSettingsPanel);

            // Settings UI elements creation
            GameObject uiContainer = new("UIContainer");
            uiContainer.transform.SetParent(panel.transform, false);
            RectTransform containerRect = uiContainer.AddComponent<RectTransform>();
            containerRect.anchorMin = Vector2.zero;
            containerRect.anchorMax = Vector2.one;
            containerRect.offsetMin = Vector2.zero;
            containerRect.offsetMax = Vector2.zero;
            containerRect.localScale = Vector3.one;

            // Title
            GameObject titleObj = new("Title");
            titleObj.transform.SetParent(uiContainer.transform, false);
            Text titleText = titleObj.AddComponent<Text>();
            titleText.text = "Settings Menu";
            titleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            titleText.fontSize = 24;
            titleText.alignment = TextAnchor.UpperCenter;
            titleText.color = Color.white;
            
            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0.9f);
            titleRect.anchorMax = new Vector2(1, 1f);
            titleRect.offsetMin = new Vector2(10, 0);
            titleRect.offsetMax = new Vector2(-10, 0);
            titleRect.localScale = Vector3.one;

            // Username text
            GameObject usernameObj = new("UsernameText");
            usernameObj.transform.SetParent(uiContainer.transform, false);
            usernameText = usernameObj.AddComponent<Text>();
            usernameText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            usernameText.fontSize = 16;
            usernameText.alignment = TextAnchor.UpperLeft;
            usernameText.color = Color.white;
            
            RectTransform usernameRect = usernameObj.GetComponent<RectTransform>();
            usernameRect.anchorMin = new Vector2(0, 0.75f);
            usernameRect.anchorMax = new Vector2(1, 0.85f);  
            usernameRect.offsetMin = new Vector2(20, 0);
            usernameRect.offsetMax = new Vector2(-20, 0);
            usernameRect.localScale = Vector3.one;

            // Duration text
            GameObject durationObj = new("DurationText");
            durationObj.transform.SetParent(uiContainer.transform, false);
            durationText = durationObj.AddComponent<Text>();
            durationText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            durationText.fontSize = 16;
            durationText.alignment = TextAnchor.UpperLeft;
            durationText.color = Color.white;
            
            RectTransform durationRect = durationObj.GetComponent<RectTransform>();
            durationRect.anchorMin = new Vector2(0, 0.65f);
            durationRect.anchorMax = new Vector2(1, 0.75f);
            durationRect.offsetMin = new Vector2(20, 0);
            durationRect.offsetMax = new Vector2(-20, 0);
            durationRect.localScale = Vector3.one;

            // Latest message text
            GameObject messageObj = new("MessageText");
            messageObj.transform.SetParent(uiContainer.transform, false);
            messageText = messageObj.AddComponent<Text>();
            messageText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            messageText.fontSize = 16;
            messageText.alignment = TextAnchor.UpperLeft;
            messageText.color = Color.white;
            
            RectTransform messageRect = messageObj.GetComponent<RectTransform>();
            messageRect.anchorMin = new Vector2(0, 0.55f);
            messageRect.anchorMax = new Vector2(1, 0.65f);
            messageRect.offsetMin = new Vector2(20, 0);
            messageRect.offsetMax = new Vector2(-20, 0);
            messageRect.localScale = Vector3.one;

            return panel;
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
        private void UpdateDisplayedValues()
        {
            if (usernameText != null)
                usernameText.text = $"Twitch Username: {Settings.Instance.twitchUsername}";
            
            if (durationText != null)
                durationText.text = $"Message Duration: {Settings.Instance.messageDuration} seconds";
            
            if (messageText != null)
                messageText.text = $"Message: {WebSocketManager.LastChatMessage}";
        }

        private Button CreateButton(string name, string text, Transform parent)
        {
            GameObject buttonObj = new GameObject(name);
            buttonObj.transform.SetParent(parent);

            RectTransform rect = buttonObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(100, 50);

            Button button = buttonObj.AddComponent<Button>();
            buttonObj.AddComponent<Image>().color = new Color(1, 1, 1, 0.9f);

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform);
            Text buttonText = textObj.AddComponent<Text>();
            buttonText.text = text;
            buttonText.alignment = TextAnchor.MiddleCenter;

            return button;
        }
        private void EnsureBasicInteraction()
        {
            var eventSystem = FindObjectOfType<EventSystem>();
            if (eventSystem == null)
            {
                GameObject es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>(); // Use StandaloneInputModule for basic interaction
            }
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
        private void OnSettingsButtonClicked()
        {
            isMenuVisible = false;
            settingsPanel!.SetActive(true);
            Main.LogEntry("OnSettingsButtonClicked", "Settings button clicked in VR.");
        }
    }
}
