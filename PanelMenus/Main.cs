using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.PanelMenus
{
    public class MainPanel : PanelConstructor.BasePanel
    {
        private readonly License menuIndex;

        public MainPanel(Transform parent, License index) : base(parent)
        {
            menuIndex = index;
            CreateMainMenu();
        }

        private void CreateMainMenu()
        {
            // Menu buttons
            CreateMenuButton("Status", 35);
            CreateMenuButton("Notifications", 60);
            CreateMenuButton("Wide Display", 85);
            CreateMenuButton("Large Display", 110);
            CreateMenuButton("Medium Display", 135);
            CreateMenuButton("Small Display", 160);
            // CreateMenuButton("Configuration", 185);
            PanelConstructor.HorizontalBar.Create(panelObject.transform, 185);
            CreateMenuButton("Standard Messages", 210);
            CreateMenuButton("Command Messages", 235);
            CreateMenuButton("Timed Messages", 260);
            CreateMenuButton("Debug", 285);
        }

        private void CreateMenuButton(string text, int verticalPosition)
        {
            string methodName = MethodBase.GetCurrentMethod().Name;
            
            Main.LogEntry(methodName, $"Creating button: {text}");
            Button button = PanelConstructor.Button.Create(
                panelObject.transform, 
                text, 
                100, 
                verticalPosition,
                clicked: () => {
                    Main.LogEntry(methodName, $"Button clicked: {text}");
                    MenuManager.Instance.OnPanelButtonClicked(text, menuIndex);
                }
            );
        }
    }
}
