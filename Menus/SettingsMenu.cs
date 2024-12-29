using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.Menus
{
    public class SettingsMenu : BaseMenu
    {
        private Text usernameText;
        private Text durationText;
        private Text messageText;

        public delegate void OnBackButtonClickedHandler();
        public event OnBackButtonClickedHandler OnBackButtonClicked;

        public SettingsMenu(Transform parent) : base(parent)
        {
            CreateSettingsMenu();
        }

        private void CreateSettingsMenu()
        {
            // Dimensions - 200x300
            
            // Title
            CreateTitle("Settings Menu", 18, Color.white, TextAnchor.UpperCenter);

            CreateSettingsTexts();
            
            // Back button
            CreateButton("Back", 0, -125, Color.white, () => OnBackButtonClicked?.Invoke());
        }

        private void CreateSettingsTexts()
        {
            // Username text
            usernameText = CreateTextElement("UsernameText", "", 16, Color.white, TextAnchor.UpperLeft, 1f, true);
            RectTransform usernameRect = usernameText.GetComponent<RectTransform>();
            usernameRect.anchorMin = new Vector2(0, 0.75f);
            usernameRect.anchorMax = new Vector2(1, 0.85f);
            usernameRect.offsetMin = new Vector2(20, 0);
            usernameRect.offsetMax = new Vector2(-20, 0);

            // Duration text
            durationText = CreateTextElement("DurationText", "", 16, Color.white, TextAnchor.UpperLeft, 1f, true);
            RectTransform durationRect = durationText.GetComponent<RectTransform>();
            durationRect.anchorMin = new Vector2(0, 0.65f);
            durationRect.anchorMax = new Vector2(1, 0.75f);
            durationRect.offsetMin = new Vector2(20, 0);
            durationRect.offsetMax = new Vector2(-20, 0);

            // Message text
            messageText = CreateTextElement("MessageText", "", 16, Color.white, TextAnchor.UpperLeft, 1f, true);
            RectTransform messageRect = messageText.GetComponent<RectTransform>();
            messageRect.anchorMin = new Vector2(0, 0.55f);
            messageRect.anchorMax = new Vector2(1, 0.65f);
            messageRect.offsetMin = new Vector2(20, 0);
            messageRect.offsetMax = new Vector2(-20, 0);
        }

        public void UpdateDisplayedValues(string username, float duration, string lastMessage)
        {
            UpdateTextElement("UsernameText", $"Twitch Username: {username}");
            UpdateTextElement("DurationText", $"Message Duration: {duration} seconds");
            UpdateTextElement("MessageText", $"Message: {lastMessage}");
        }
    }
}
