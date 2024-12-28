using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.Menus
{
    public class MainMenu : BaseMenu
    {
        public delegate void OnMenuButtonClickedHandler(string menuName);
        public event OnMenuButtonClickedHandler OnMenuButtonClicked;

        public MainMenu(Transform parent) : base(parent)
        {
            CreateMainMenu();
        }

        private void CreateMainMenu()
        {
            // Title
            CreateTitle("TwitchChatMod", 18, Color.white, TextAnchor.UpperCenter);

            // Menu buttons
            CreateMenuButton("Status", 0.8f);
            CreateMenuButton("Settings", 0.7f);
            CreateMenuButton("Standard Messages", 0.6f);
            CreateMenuButton("Command Messages", 0.5f);
            CreateMenuButton("Custom Commands", 0.4f);
            CreateMenuButton("Timed Messages", 0.3f);
            CreateMenuButton("Dispatcher Messages", 0.2f);
            CreateMenuButton("Debug", 0.1f);
        }

        private void CreateMenuButton(string text, float verticalPosition)
        {
            Button button = CreateButton($"{text}Button", text,
                new Vector2(0.2f, verticalPosition), new Vector2(0.8f, verticalPosition + 0.07f));
            button.onClick.AddListener(() => OnMenuButtonClicked?.Invoke(text));
        }
    }
}
