using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.Menus
{
    public class DebugMenu : BaseMenu
    {
        public delegate void OnBackButtonClickedHandler();
        public event OnBackButtonClickedHandler OnBackButtonClicked;

        public DebugMenu(Transform parent) : base(parent)
        {
            CreateDebugMenu();
        }

        private void CreateDebugMenu()
        {
            // Dimensions - 200x300
            
            // Title
            CreateTitle("Debug Menu", 18, Color.white, TextAnchor.UpperCenter);

            // Add Debug Menu menu items here
            // TODO: Complete Debug Menu menu

            // Back button
            Button backButton = CreateButton("BackButton", "Back", 0, -200, 18, Color.white, TextAnchor.MiddleCenter, () => OnBackButtonClicked?.Invoke());
        }
    }
}
