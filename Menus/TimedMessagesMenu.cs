using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.Menus
{
    public class TimedMessagesMenu : BaseMenu
    {
        private Toggle systemToggle;
        private Text statusText;
        private Text lastMessageText;

        public delegate void OnBackButtonClickedHandler();
        public event OnBackButtonClickedHandler OnBackButtonClicked;

        public TimedMessagesMenu(Transform parent) : base(parent)
        {
            CreateTimedMessagesMenu();
        }

        private void CreateTimedMessagesMenu()
        {
            // Title
            CreateTitle("Timed Messages", 18, Color.white, TextAnchor.UpperCenter);

            // System Toggle
            systemToggle = CreateToggle("System Toggle", Settings.Instance.timedMessageSystemToggle,
                isActive => {
                    Settings.Instance.timedMessageSystemToggle = isActive;
                    AutomatedMessages.ToggleTimedMessages();
                });

            // Status Text
            statusText = CreateStatusText();

            // Last Message Text
            lastMessageText = CreateLastMessageText();

            // Timed Message 1-5 settings
            float[] positions = { 0.7f, 0.55f, 0.4f, 0.25f, 0.1f };
            for (int i = 1; i <= 5; i++)
            {
                CreateTimedMessageSet($"Message {i}", positions[i-1], i);
            }

            Button backButton = CreateButton("BackButton", "Back", 
                new Vector2(0.3f, 0.05f), new Vector2(0.7f, 0.12f), 18, Color.white, TextAnchor.MiddleCenter, () => OnBackButtonClicked?.Invoke());
        }

        private void CreateTimedMessageSet(string label, float verticalPosition, int messageIndex)
        {
            GameObject container = new GameObject($"{label}Container");
            container.transform.SetParent(menuObject.transform, false);
            
            RectTransform containerRect = container.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.1f, verticalPosition);
            containerRect.anchorMax = new Vector2(0.9f, verticalPosition + 0.12f);
            containerRect.offsetMin = Vector2.zero;
            containerRect.offsetMax = Vector2.zero;

            // Create message input and timer input fields based on messageIndex
            // Use reflection or switch statement to access the correct Settings.Instance properties
        }
    }
}
