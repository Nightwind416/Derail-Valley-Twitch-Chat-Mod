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

            // Dimensions - 200x300

            Main.LogEntry(methodName, "Creating main menu...");
            // Title
            CreateTitle("TwitchChatMod", 18, Color.white, TextAnchor.UpperCenter);

            // Menu buttons
            CreateMenuButton("Status", 300);
            CreateMenuButton("Settings", 200);
            CreateMenuButton("Large Display", 100);
            CreateMenuButton("Wide Display", 0);
            CreateMenuButton("Medium Display", -100);
            CreateMenuButton("Small Display", -200);
            CreateMenuButton("Debug", -300);
            Main.LogEntry(methodName, "Main menu creation completed");
        }

        private void CreateMenuButton(string text, int verticalPosition)
        {
            string methodName = MethodBase.GetCurrentMethod().Name;
            
            Main.LogEntry(methodName, $"Creating button: {text}");
            Button button = CreateButton($"{text} Button", text, 0, verticalPosition, 18);
            button.onClick.AddListener(() => {
                Main.LogEntry(methodName, $"Button clicked: {text}");
                MenuManager.Instance.OnMenuButtonClicked(text, menuIndex);
            });
        }
    }
}
