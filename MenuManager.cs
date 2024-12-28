using UnityEngine;
using UnityEngine.UI;
using System.Reflection;
using System.Text;

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
            Panel();
        }

        private void Update()
        {
            string methodName = MethodBase.GetCurrentMethod().Name;

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
                        Panel();
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
                while (current.parent != null)
                {
                    if (current.parent.name.Contains("StickyTape_Gadget"))
                    {
                        currentlyAttached = true;
                        break;
                    }
                    current = current.parent;
                }

                if (currentlyAttached != isAttachedToStickyTape)
                {
                    isAttachedToStickyTape = currentlyAttached;
                    Main.LogEntry(methodName, $"License attachment to sticky tape changed: {isAttachedToStickyTape}");
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

        private void Panel()
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
            
            menuCanvas.AddComponent<GraphicRaycaster>();
            
            RectTransform canvasRect = menuCanvas.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(200, 300);
            canvasRect.localScale = Vector3.one * 0.001f;
            
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
        }

        private GameObject SettingsPanel()
        {
            GameObject panel = new("SettingsPanel");
            panel.transform.SetParent(menuCanvas!.transform, false);
            
            Image panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0, 0, 0, 0.8f);
            
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            panelRect.localScale = Vector3.one;

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

        private void LogGameObjectHierarchy(GameObject obj)
        {
            string methodName = MethodBase.GetCurrentMethod().Name;
            StringBuilder hierarchy = new StringBuilder();
            Transform current = obj.transform;
            
            while (current != null)
            {
                hierarchy.Insert(0, current.name);
                if (current.parent != null)
                    hierarchy.Insert(0, " <- ");
                current = current.parent!;
            }
            
            Main.LogEntry(methodName, $"GameObject hierarchy: {hierarchy}");
        }
    }
}
