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
            CreateMenuButton("Status", 85);
            CreateMenuButton("Settings", 50);
            CreateMenuButton("Large Display", 15);
            CreateMenuButton("Wide Display", -20);
            CreateMenuButton("Medium Display", -55);
            CreateMenuButton("Small Display", -90);
            CreateMenuButton("Debug", -125);
            Main.LogEntry(methodName, "Main menu creation completed");
        }

        private void CreateMenuButton(string text, int verticalPosition)
        {
            string methodName = MethodBase.GetCurrentMethod().Name;
            
            Main.LogEntry(methodName, $"Creating button: {text}");
            Button button = CreateButton(text, 0, verticalPosition);
            button.onClick.AddListener(() => {
                Main.LogEntry(methodName, $"Button clicked: {text}");
                MenuManager.Instance.OnMenuButtonClicked(text, menuIndex);
            });
        }
    }
}
