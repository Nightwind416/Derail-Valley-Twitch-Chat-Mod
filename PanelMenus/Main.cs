using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.PanelMenus
{
    /// <summary>
    /// Main navigation panel for the mod's interface.
    /// Provides access to all other panels and functionality.
    /// </summary>
    public class MainPanel : PanelConstructor.BasePanel
    {
        private readonly License menuIndex;

        /// <summary>
        /// Initializes a new instance of the MainPanel.
        /// </summary>
        /// <param name="parent">The parent transform this panel will be attached to.</param>
        /// <param name="index">The license index for the menu.</param>
        public MainPanel(Transform parent, License index) : base(parent)
        {
            menuIndex = index;
            CreateMainMenu();
        }

        /// <summary>
        /// Creates the main menu interface with navigation buttons.
        /// </summary>
        private void CreateMainMenu()
        {
            // Menu buttons
            CreateMenuButton("Status", 35);
            CreateMenuButton("Notifications", 60);
            CreateMenuButton("Standard Messages", 85);
            CreateMenuButton("Command Messages", 110);
            CreateMenuButton("Timed Messages", 135);
            CreateMenuButton("Debug", 160);
            
            // Config buttons side by side
            CreateMenuButton("Config1", 185, -35); // Offset to the left
            CreateMenuButton("Config2", 185, 35);  // Offset to the right
            
            // Display buttons
            CreateMenuButton("Wide Display", 210);
            CreateMenuButton("Large Display", 235);
            CreateMenuButton("Medium Display", 260);
            CreateMenuButton("Small Display", 285);
        }

        /// <summary>
        /// Creates a navigation button with the specified text and position.
        /// </summary>
        /// <param name="text">The button text.</param>
        /// <param name="verticalPosition">The vertical position of the button.</param>
        /// <param name="horizontalOffset">The horizontal offset of the button.</param>
        private void CreateMenuButton(string text, int verticalPosition, float horizontalOffset = 0)
        {
            string methodName = MethodBase.GetCurrentMethod().Name;
            
            Main.LogEntry(methodName, $"Creating button: {text}");
            Button button = PanelConstructor.Button.Create(
                panelObject.transform, 
                text, 
                (int)(100 + horizontalOffset),  // Add horizontal offset to base position
                verticalPosition,
                clicked: () => {
                    Main.LogEntry(methodName, $"Button clicked: {text}");
                    MenuManager.Instance.OnPanelButtonClicked(text, menuIndex);
                }
            );
        }
    }
}
