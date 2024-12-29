using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.Menus
{
    public class MainMenu : BaseMenu
    {
        private readonly int menuIndex;

        public delegate void OnMenuButtonClickedHandler(string menuName);
        public event OnMenuButtonClickedHandler OnMenuButtonClicked;

        public MainMenu(Transform parent, int index) : base(parent)
        {
            menuIndex = index;
            CreateMainMenu();
        }

        private void CreateMainMenu()
        {
            string methodName = MethodBase.GetCurrentMethod().Name;

            Main.LogEntry(methodName, "Creating main menu...");
            // Title
            CreateTitle("TwitchChatMod", 18, Color.white, TextAnchor.UpperCenter);

            // Menu buttons
            CreateMenuButton("Status", 0.8f);
            CreateMenuButton("Settings", 0.7f);
            CreateMenuButton("Large Display", 0.6f);
            CreateMenuButton("Medium Display", 0.5f);
            CreateMenuButton("Small Display", 0.4f);
            CreateMenuButton("Debug", 0.3f);
            Main.LogEntry(methodName, "Main menu creation completed");
        }

        private void CreateMenuButton(string text, float verticalPosition)
        {
            string methodName = MethodBase.GetCurrentMethod().Name;
            
            Main.LogEntry(methodName, $"Creating button: {text}");
            Button button = CreateButton($"{text} Button", text, new Vector2(0.2f, verticalPosition), new Vector2(0.8f, verticalPosition + 0.07f));
            button.onClick.AddListener(() => {
                Main.LogEntry(methodName, $"Button clicked: {text}");
                MenuManager.Instance.OnMenuButtonClicked(text, menuIndex);
            });
        }
    }
}
