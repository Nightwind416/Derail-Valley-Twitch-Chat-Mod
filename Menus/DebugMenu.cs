using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.Menus
{
    public class DebugMenu : BaseMenu
    {
        public delegate void OnBackButtonClickedHandler();
        public event OnBackButtonClickedHandler OnBackButtonClicked;

        public DebugMenu(Transform parent) : base(parent)
        {
            CreateDebugMenu();
        }

        private void CreateDebugMenu()
        {
            // Dimensions - 200x300
            
            // Title
            CreateTitle("Debug Menu", 18, Color.white, TextAnchor.UpperCenter);

            // Debug Section
            GameObject debugSection = CreateSection("Debug", 25, 100);

            // Add Debug Menu menu items here
            // TODO: Complete Debug Menu menu

<<<<<<< HEAD
            // Back button
            Button backButton = CreateButton("Back", 0, -125, Color.white, () => OnBackButtonClicked?.Invoke());
=======
            RectTransform toggleRect = processOwnMessagesToggle.GetComponent<RectTransform>();
            toggleRect.anchorMin = new Vector2(0.1f, 0.6f);
            toggleRect.anchorMax = new Vector2(0.9f, 0.7f);
            
            // Test Message Section
            CreateTestMessageSection();

            // Warning Text
            CreateTextElement("WarningText", "⚠️ Warning: Process Own Messages should only be enabled during testing!", 14, Color.yellow, TextAnchor.MiddleCenter, 1f, true);

            RectTransform warningRect = menuObject.transform.Find("WarningText").GetComponent<RectTransform>();
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
            testMessageInput = CreateInputField("TestMessageInput", Settings.Instance.testMessage, 
                value => Settings.Instance.testMessage = value);

            RectTransform inputRect = testMessageInput.GetComponent<RectTransform>();
            inputRect.anchorMin = new Vector2(0.1f, 0.3f);
            inputRect.anchorMax = new Vector2(0.9f, 0.4f);

            Button testButton = CreateButton("TestMessageButton", "Send Test Message",
                new Vector2(0.1f, 0.2f), new Vector2(0.9f, 0.3f));
            testButton.onClick.AddListener(() => {
                MessageHandler.AttachNotification(Settings.Instance.testMessage, "DEBUG");
            });
>>>>>>> origin/common-text-function
        }
    }
}
