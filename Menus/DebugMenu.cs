using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.Menus
{
    public class DebugMenu : BaseMenu
    {
        private Toggle processOwnMessagesToggle;
        private Text debugLevelText;
        private InputField testMessageInput;

        public delegate void OnBackButtonClickedHandler();
        public event OnBackButtonClickedHandler OnBackButtonClicked;

        public DebugMenu(Transform parent) : base(parent)
        {
            CreateDebugMenu();
        }

        private void CreateDebugMenu()
        {
            CreateTitle("Debug Menu", 24, Color.white, TextAnchor.UpperCenter);

            // Debug Level Selection
            CreateDebugLevelSelector();

            // Process Own Messages Toggle
            GameObject toggleObj = new GameObject("ProcessOwnMessagesToggle");
            toggleObj.transform.SetParent(menuObject.transform, false);
            processOwnMessagesToggle = toggleObj.AddComponent<Toggle>();
            processOwnMessagesToggle.isOn = Settings.Instance.processOwnMessages;
            processOwnMessagesToggle.onValueChanged.AddListener(value => 
                Settings.Instance.processOwnMessages = value);

            RectTransform toggleRect = toggleObj.GetComponent<RectTransform>();
            toggleRect.anchorMin = new Vector2(0.1f, 0.6f);
            toggleRect.anchorMax = new Vector2(0.9f, 0.7f);
            
            // Test Message Section
            CreateTestMessageSection();

            // Warning Text
            GameObject warningObj = new GameObject("WarningText");
            warningObj.transform.SetParent(menuObject.transform, false);
            Text warningText = warningObj.AddComponent<Text>();
            warningText.text = "⚠️ Warning: Process Own Messages should only be enabled during testing!";
            warningText.color = Color.yellow;
            warningText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            warningText.fontSize = 14;
            
            RectTransform warningRect = warningObj.GetComponent<RectTransform>();
            warningRect.anchorMin = new Vector2(0.1f, 0.5f);
            warningRect.anchorMax = new Vector2(0.9f, 0.6f);

            Button backButton = CreateButton("BackButton", "Back", 
                new Vector2(0.3f, 0.1f), new Vector2(0.7f, 0.17f));
            backButton.onClick.AddListener(() => OnBackButtonClicked?.Invoke());
        }

        private void CreateDebugLevelSelector()
        {
            string[] options = { "Off", "Minimal", "Reduced", "Full" };
            for (int i = 0; i < options.Length; i++)
            {
                int index = i; // Capture for lambda
                Button button = CreateButton($"DebugLevel{i}", options[i],
                    new Vector2(0.1f + (i * 0.2f), 0.8f),
                    new Vector2(0.3f + (i * 0.2f), 0.9f));
                
                button.onClick.AddListener(() => Settings.Instance.debugLevel = (DebugLevel)index);
            }
        }

        private void CreateTestMessageSection()
        {
            GameObject inputObj = new GameObject("TestMessageInput");
            inputObj.transform.SetParent(menuObject.transform, false);
            
            testMessageInput = inputObj.AddComponent<InputField>();
            testMessageInput.text = Settings.Instance.testMessage;
            testMessageInput.onValueChanged.AddListener(value => 
                Settings.Instance.testMessage = value);

            Button testButton = CreateButton("TestMessageButton", "Send Test Message",
                new Vector2(0.1f, 0.3f), new Vector2(0.9f, 0.4f));
            testButton.onClick.AddListener(() => {
                MessageHandler.AttachNotification(Settings.Instance.testMessage, "DEBUG");
            });
        }
    }
}
