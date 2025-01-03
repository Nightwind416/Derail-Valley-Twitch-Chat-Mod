using System.Reflection;
using DV.Booklets;
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
            CreateMenuButton("Status", 35);
            CreateMenuButton("Notification Settings", 60);
            CreateMenuButton("Large Display", 85);
            CreateMenuButton("Wide Display", 110);
            CreateMenuButton("Medium Display", 135);
            CreateMenuButton("Small Display", 160);
            CreateMenuButton("Configuration", 185);
            CreateMenuButton("Standard Messages", 210);
            CreateMenuButton("Command Messages", 235);
            CreateMenuButton("Timed Messages", 260);
            CreateMenuButton("Debug", 285);
            
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
