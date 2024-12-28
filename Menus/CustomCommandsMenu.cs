using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.Menus
{
    public class CustomCommandsMenu : BaseMenu
    {
        public delegate void OnBackButtonClickedHandler();
        public event OnBackButtonClickedHandler OnBackButtonClicked;

        public CustomCommandsMenu(Transform parent) : base(parent)
        {
            CreateCustomCommandsMenu();
        }

        private void CreateCustomCommandsMenu()
        {
            // Title
            CreateTitle("Custom Commands", 18, Color.white, TextAnchor.UpperCenter);

            // Add Custom Commands menu items here
            // TODO: Complete Custom Commands menu

            // Back button
            Button backButton = CreateButton("BackButton", "Back", new Vector2(0.3f, 0.05f), new Vector2(0.7f, 0.12f), 18, Color.white, TextAnchor.MiddleCenter, () => OnBackButtonClicked?.Invoke());
        }
    }
}
