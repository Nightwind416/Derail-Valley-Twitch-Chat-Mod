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
            // Title
            CreateTitle("Status Menu", 18, Color.white, TextAnchor.UpperCenter);

            // Add Status Menu menu items here
            // TODO: Complete Status Menu menu

            // Back button
            Button backButton = CreateButton("BackButton", "Back", new Vector2(0.3f, 0.05f), new Vector2(0.7f, 0.12f), 18, Color.white, TextAnchor.MiddleCenter, () => OnBackButtonClicked?.Invoke());
        }
    }
}
