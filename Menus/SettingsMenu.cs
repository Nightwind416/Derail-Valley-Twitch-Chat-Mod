using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.Menus
{
    public class SettingsMenu : BaseMenu
    {
        private GameObject usernameInput;
        private GameObject durationInput;

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

            // Back button
            Button backButton = CreateButton(menuObject.transform, " X ", 190, 10, Color.white, () => OnBackButtonClicked?.Invoke());

            // Setup Section
            GameObject settingsSection = CreateSection("Setup", 25, 80);
            RectTransform settingsSectionRect = settingsSection.GetComponent<RectTransform>();

            // Username Label
            CreateLabel(settingsSection.transform, "Username:", 10, 30, Color.white);
            
            // Username input
            CreateTextInput(settingsSection.transform, 20, 50, 140, "");
            usernameInput = textInputField;

            // Notifications Section
            GameObject notificationSection = CreateSection("Notifications", 125, 80);
            RectTransform notificationSectionRect = notificationSection.GetComponent<RectTransform>();
            
            // Duration Label
            CreateLabel(notificationSection.transform, "Duration:", 10, 30, Color.white);
            
            // Duration input
            CreateTextInput(notificationSection.transform, 20, 50, 50, "");
            durationInput = textInputField;
        }

        public void UpdateSettingsMenuValues(string username, float duration, string lastMessage)
        {
            if (usernameInput != null)
            {
                usernameInput.GetComponent<UnityEngine.UI.InputField>().text = username;
            }
            
            if (durationInput != null)
            {
                durationInput.GetComponent<UnityEngine.UI.InputField>().text = duration.ToString();
            }
        }

        public string GetUsername()
        {
            return usernameInput?.GetComponent<UnityEngine.UI.InputField>().text ?? string.Empty;
        }

        public float GetDuration()
        {
            if (durationInput == null) return 0f;
            if (float.TryParse(durationInput.GetComponent<UnityEngine.UI.InputField>().text, out float result))
            {
                return result;
            }
            return 0f;
        }
    }
}
