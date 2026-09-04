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
        private readonly PanelHost? host;

        /// <summary>
        /// Initializes a new instance of the MainPanel.
        /// </summary>
        /// <param name="parent">The parent transform this panel will be attached to.</param>
        /// <param name="host">The host this menu belongs to, or null for the hidden template copy.</param>
        public MainPanel(Transform parent, PanelHost? host) : base(parent)
        {
            this.host = host;
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

            // Cab display controls side by side
            CreateActionButton("Place Display", 310, -42, () => MenuManager.Instance.PlaceCabDisplay());
            CreateActionButton("Toggle Display", 310, 42, () => MenuManager.Instance.ToggleCabDisplay());
        }

        /// <summary>
        /// Creates a navigation button that switches this host to the named panel.
        /// </summary>
        /// <param name="text">The button text, which is also the panel name.</param>
        /// <param name="verticalPosition">The vertical position of the button.</param>
        /// <param name="horizontalOffset">The horizontal offset of the button.</param>
        private void CreateMenuButton(string text, int verticalPosition, float horizontalOffset = 0)
        {
            CreateActionButton(text, verticalPosition, horizontalOffset, () =>
            {
                if (host != null)
                {
                    MenuManager.Instance.OnPanelButtonClicked(text, host);
                }
            });
        }

        /// <summary>
        /// Creates a button that runs an arbitrary action.
        /// </summary>
        private void CreateActionButton(string text, int verticalPosition, float horizontalOffset, System.Action action)
        {
            string methodName = "MainPanel";
            Main.LogEntry(methodName, $"Creating button: {text}");
            Button button = PanelConstructor.Button.Create(
                panelObject.transform,
                text,
                (int)(100 + horizontalOffset),  // Add horizontal offset to base position
                verticalPosition,
                clicked: () =>
                {
                    Main.LogEntry(methodName, $"Button clicked: {text}");
                    action();
                }
            );
        }
    }
}
