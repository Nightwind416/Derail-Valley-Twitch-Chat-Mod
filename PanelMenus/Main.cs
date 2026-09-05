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
            CreateMenuButton("Authentication", 35);
            CreateMenuButton("Status", 60);
            CreateMenuButton("Notifications", 85);
            CreateMenuButton("Standard Messages", 110);
            CreateMenuButton("Command Messages", 135);
            CreateMenuButton("Timed Messages", 160);

            // Debug and Displays side by side
            CreateMenuButton("Debug", 185, -35);
            CreateMenuButton("Displays", 185, 35);

            // Config buttons side by side
            CreateMenuButton("Config1", 210, -35); // Offset to the left
            CreateMenuButton("Config2", 210, 35);  // Offset to the right

            // Chat, sized by dragging the display it is on rather than by picking a preset
            CreateMenuButton("Chat", 235);

            // Cab display controls side by side
            CreateActionButton("Place Display", 260, -42, () => MenuManager.Instance.PlaceCabDisplay());
            CreateActionButton("Toggle Display", 260, 42, () => MenuManager.Instance.ToggleCabDisplay());
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
                0,
                verticalPosition,
                clicked: () =>
                {
                    Main.LogEntry(methodName, $"Button clicked: {text}");
                    action();
                }
            );

            // Anchor to the top centre so the menu stays centred however wide the display is dragged
            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(horizontalOffset, -verticalPosition);
        }
    }
}
