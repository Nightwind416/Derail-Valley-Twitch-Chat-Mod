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
            CreateMenuButton("Status", 50);
            CreateMenuButton("Notification Settings", 80);
            CreateMenuButton("Large Display", 110);
            CreateMenuButton("Wide Display", 140);
            CreateMenuButton("Medium Display", 170);
            CreateMenuButton("Small Display", 200);
            CreateMenuButton("Configuration", 230);
            CreateMenuButton("Debug", 260);
            
            Main.LogEntry(methodName, "Main menu creation completed");
        }

        private void CreateMenuButton(string text, int verticalPosition)
        {
            string methodName = MethodBase.GetCurrentMethod().Name;
            
            Main.LogEntry(methodName, $"Creating button: {text}");
            Button button = CreateButton(menuObject.transform, text, 100, verticalPosition);
            button.onClick.AddListener(() => {
                Main.LogEntry(methodName, $"Button clicked: {text}");
                MenuManager.Instance.OnMenuButtonClicked(text, menuIndex);
            });
        }
    }
}
