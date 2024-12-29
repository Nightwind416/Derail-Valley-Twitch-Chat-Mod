using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.Menus
{
    public class StatusMenu : BaseMenu
    {
        public delegate void OnBackButtonClickedHandler();
        public event OnBackButtonClickedHandler OnBackButtonClicked;

        public StatusMenu(Transform parent) : base(parent)
        {
            CreateStatusMenu();
        }

        private void CreateStatusMenu()
        {
            // Dimensions - 200x300
            
            // Title
            CreateTitle("Status Menu", 18, Color.white, TextAnchor.UpperCenter);

            // Add Status Menu menu items here
            // TODO: Complete Status Menu menu

            // Back button
            Button backButton = CreateButton("BackButton", "Back", 0, -200, 18, Color.white, TextAnchor.MiddleCenter, () => OnBackButtonClicked?.Invoke());
        }
    }
}
