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
            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(menuObject.transform, false);
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

            CreateSettingsTexts();
            
            // Back button
            Button backButton = CreateButton("BackButton", "Back", 
                new Vector2(0.3f, 0.1f), new Vector2(0.7f, 0.17f));
            backButton.onClick.AddListener(() => OnBackButtonClicked?.Invoke());
        }

        private void CreateSettingsTexts()
        {
            // Username text
            GameObject usernameObj = new GameObject("UsernameText");
            usernameObj.transform.SetParent(menuObject.transform, false);
            usernameText = CreateSettingsText(usernameObj, new Vector2(0, 0.75f), new Vector2(1, 0.85f));

            // Duration text
            GameObject durationObj = new GameObject("DurationText");
            durationObj.transform.SetParent(menuObject.transform, false);
            durationText = CreateSettingsText(durationObj, new Vector2(0, 0.65f), new Vector2(1, 0.75f));

            // Message text
            GameObject messageObj = new GameObject("MessageText");
            messageObj.transform.SetParent(menuObject.transform, false);
            messageText = CreateSettingsText(messageObj, new Vector2(0, 0.55f), new Vector2(1, 0.65f));
        }

        private Text CreateSettingsText(GameObject obj, Vector2 anchorMin, Vector2 anchorMax)
        {
            Text text = obj.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 16;
            text.alignment = TextAnchor.UpperLeft;
            text.color = Color.white;
            
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = new Vector2(20, 0);
            rect.offsetMax = new Vector2(-20, 0);

            return text;
        }

        public void UpdateDisplayedValues(string username, float duration, string lastMessage)
        {
            usernameText.text = $"Twitch Username: {username}";
            durationText.text = $"Message Duration: {duration} seconds";
            messageText.text = $"Message: {lastMessage}";
        }
    }
}
