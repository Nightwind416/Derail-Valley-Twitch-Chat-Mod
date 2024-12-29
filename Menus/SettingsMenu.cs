using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.Menus
{
    public class SettingsMenu : BaseMenu
    {
        private GameObject usernameInput;
        private GameObject durationInput;
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

            // Settings Section
            GameObject settingsSection = CreateSection("Settings", 25, 200, false);
            RectTransform sectionRect = settingsSection.GetComponent<RectTransform>();

            // Username Label
            CreateLabel(settingsSection.transform, "Username:", 15, -10, Color.white);
            
            // Username input field
            CreateTextInput(settingsSection.transform, 5, 50, 100, "");
            usernameInput = textInputField; // Store reference to current input field

            // Duration Label
            CreateLabel(settingsSection.transform, "Duration:", 15, -50, Color.white);
            
            // Duration input field
            CreateTextInput(settingsSection.transform, 5, 10, 100, "");
            durationInput = textInputField; // Store reference to current input field

            // Message Label
            CreateLabel(settingsSection.transform, "Last Message:", 15, -90, Color.white);

            // Back button
            CreateButton("Back", 0, -125, Color.white, () => OnBackButtonClicked?.Invoke());
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

            if (messageText != null)
            {
                messageText.text = lastMessage;
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
