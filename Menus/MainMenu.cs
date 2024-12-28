using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.Menus
{
    public class MainMenu : BaseMenu
    {
        public delegate void OnSettingsButtonClickedHandler();
        public event OnSettingsButtonClickedHandler? OnSettingsButtonClicked;

        public MainMenu(Transform parent) : base(parent)
        {
            CreateMainMenu();
        }

        private void CreateMainMenu()
        {
            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(menuObject.transform, false);
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

            // Settings button
            Button settingsButton = CreateButton("SettingsButton", "Settings", 
                new Vector2(0.3f, 0.8f), new Vector2(0.7f, 0.87f));
            settingsButton.onClick.AddListener(() => OnSettingsButtonClicked?.Invoke());
        }
    }
}
