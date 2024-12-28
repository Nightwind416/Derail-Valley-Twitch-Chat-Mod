using UnityEngine;
using UnityEngine.UI;
// using System;
using System.Reflection;

namespace TwitchChat
{
    public class MenuManager : MonoBehaviour
    {
        private static MenuManager? instance;
        private static GameObject? menuCanvas;
        private bool isMenuVisible = false;
        private bool isSettingsPanelVisible = false;
        private GameObject? mainPanel;
        private GameObject? settingsPanel;

        private Text? usernameText;
        private Text? durationText;
        private Text? messageText;

        public static MenuManager Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new("CustomMenuManager");
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
            CreateMenu();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F7))
            {
                string methodName = MethodBase.GetCurrentMethod().Name;
                Main.LogEntry(methodName, "F7 key pressed - toggling menu");
                ToggleMenu();
            }
            
            if (isMenuVisible)
            {
                // PositionMenuInVR();
                UpdateDisplayedValues();
                PositionMenuNearObject();
            }
        }

        private void CreateMenu()
        {
            string methodName = MethodBase.GetCurrentMethod().Name;
            Main.LogEntry(methodName, "Creating VR menu UI elements");

            if (menuCanvas != null)
            {
                Main.LogEntry(methodName, "VR Menu already exists, destroying old instance");
                Destroy(menuCanvas);
            }

            // Create canvas and basic setup
            menuCanvas = new GameObject("CustomVRMenuCanvas");
            Canvas canvas = menuCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 1000;
            
            menuCanvas.AddComponent<GraphicRaycaster>();
            
            RectTransform canvasRect = menuCanvas.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(200, 300);
            canvasRect.localScale = Vector3.one * 0.001f;
            
            // Create main panel and settings panel
            mainPanel = CreateMainPanel();
            settingsPanel = CreateSettingsPanel();

            // Set initial panel states
            mainPanel.SetActive(true);
            settingsPanel.SetActive(false);
            isSettingsPanelVisible = false;

            // PositionMenuInVR();
            // PositionMenuNearObject();
            menuCanvas.SetActive(isMenuVisible);
            Main.LogEntry(methodName, "Menu creation completed");

            // UpdateDisplayedValues(); // Initial update of values
        }

        private GameObject CreateMainPanel()
        {
            GameObject panel = new("MainPanel");
            panel.transform.SetParent(menuCanvas!.transform, false);
            
            Image panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0, 0, 0, 1f);
            
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
            titleText.fontSize = 24;  // Reduced from 36
            titleText.alignment = TextAnchor.UpperCenter;
            titleText.color = Color.white;
            
            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0.9f);  // Adjusted from 0.85f
            titleRect.anchorMax = new Vector2(1, 1f);    // Adjusted from 0.95f
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
            buttonText.fontSize = 18;  // Reduced from 24
            buttonText.alignment = TextAnchor.MiddleCenter;
            buttonText.color = Color.white;
            
            RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.3f, 0.8f);  // Adjusted from 0.4f, 0.7f
            buttonRect.anchorMax = new Vector2(0.7f, 0.87f);  // Adjusted from 0.6f, 0.8f
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

        private GameObject CreateSettingsPanel()
        {
            GameObject panel = new("SettingsPanel");
            panel.transform.SetParent(menuCanvas!.transform, false);
            
            Image panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0, 0, 0, 1f);
            
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            panelRect.localScale = Vector3.one;

            // Move all the settings UI elements creation here from CreateSettingsMenu()
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
            titleText.fontSize = 24;  // Reduced from 36
            titleText.alignment = TextAnchor.UpperCenter;
            titleText.color = Color.white;
            
            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0.9f);  // Adjusted from 0.85f
            titleRect.anchorMax = new Vector2(1, 1f);    // Adjusted from 0.95f
            titleRect.offsetMin = new Vector2(10, 0);
            titleRect.offsetMax = new Vector2(-10, 0);
            titleRect.localScale = Vector3.one;

            // Username text
            GameObject usernameObj = new("UsernameText");
            usernameObj.transform.SetParent(uiContainer.transform, false);
            usernameText = usernameObj.AddComponent<Text>();
            usernameText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            usernameText.fontSize = 16;  // Reduced from 28
            usernameText.alignment = TextAnchor.UpperLeft;
            usernameText.color = Color.white;
            
            RectTransform usernameRect = usernameObj.GetComponent<RectTransform>();
            usernameRect.anchorMin = new Vector2(0, 0.75f);  // Adjusted from 0.7f
            usernameRect.anchorMax = new Vector2(1, 0.85f);  // Adjusted from 0.8f
            usernameRect.offsetMin = new Vector2(20, 0);
            usernameRect.offsetMax = new Vector2(-20, 0);
            usernameRect.localScale = Vector3.one;

            // Duration text
            GameObject durationObj = new("DurationText");
            durationObj.transform.SetParent(uiContainer.transform, false);
            durationText = durationObj.AddComponent<Text>();
            durationText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            durationText.fontSize = 16;  // Reduced from 28
            durationText.alignment = TextAnchor.UpperLeft;
            durationText.color = Color.white;
            
            RectTransform durationRect = durationObj.GetComponent<RectTransform>();
            durationRect.anchorMin = new Vector2(0, 0.65f);  // Adjusted from 0.6f
            durationRect.anchorMax = new Vector2(1, 0.75f);  // Adjusted from 0.7f
            durationRect.offsetMin = new Vector2(20, 0);
            durationRect.offsetMax = new Vector2(-20, 0);
            durationRect.localScale = Vector3.one;

            // Latest message text
            GameObject messageObj = new("MessageText");
            messageObj.transform.SetParent(uiContainer.transform, false);
            messageText = messageObj.AddComponent<Text>();
            messageText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            messageText.fontSize = 16;  // Reduced from 28
            messageText.alignment = TextAnchor.UpperLeft;
            messageText.color = Color.white;
            
            RectTransform messageRect = messageObj.GetComponent<RectTransform>();
            messageRect.anchorMin = new Vector2(0, 0.55f);  // Adjusted from 0.5f
            messageRect.anchorMax = new Vector2(1, 0.65f);  // Adjusted from 0.6f
            messageRect.offsetMin = new Vector2(20, 0);
            messageRect.offsetMax = new Vector2(-20, 0);
            messageRect.localScale = Vector3.one;

            return panel;
        }
        private void PositionMenuNearObject()
        {
            if (menuCanvas == null)
            {
                Debug.LogWarning("Menu Canvas is not initialized.");
                return;
            }

            // Locate the LicenseTrainDriver GameObject
            GameObject targetObject = GameObject.Find("LicenseTrainDriver");
            if (targetObject == null)
            {
                Debug.LogWarning("LicenseTrainDriver GameObject not found in the scene.");
                return;
            }

            // Position the menu to hover in front of the target object
            Vector3 targetPosition = targetObject.transform.position;
            Vector3 offset1 = Vector3.forward * 0.001f;
            // Vector3 offset2 = Vector3.up * 0.3f;
            menuCanvas.transform.position = targetPosition;
            menuCanvas.transform.position += offset1;
            // menuCanvas.transform.position += offset2;

            // Match the rotation of the target object and add 90 degrees X rotation
            menuCanvas.transform.rotation = targetObject.transform.rotation * Quaternion.Euler(90f, 180f, 0f);
        }
        public void ToggleMenu()
        {
            string methodName = MethodBase.GetCurrentMethod().Name;
            isMenuVisible = !isMenuVisible;
            Main.LogEntry(methodName, $"Toggling VR menu visibility to: {isMenuVisible}");
            
            if (menuCanvas == null)
            {
                Main.LogEntry(methodName, "VR Menu canvas was null, recreating...");
                CreateMenu();
            }
            
            menuCanvas!.SetActive(isMenuVisible);
            
            // Always show main panel and hide settings panel when toggling menu
            if (isMenuVisible)
            {
                mainPanel!.SetActive(true);
                settingsPanel!.SetActive(false);
                isSettingsPanelVisible = false;
            }
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
    }
}
