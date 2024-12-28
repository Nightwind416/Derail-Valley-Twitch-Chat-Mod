using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.Menus
{
    public class StandardMessagesMenu : BaseMenu
    {
        public delegate void OnBackButtonClickedHandler();
        public event OnBackButtonClickedHandler OnBackButtonClicked;

        public StandardMessagesMenu(Transform parent) : base(parent)
        {
            CreateStandardMessagesMenu();
        }

        private void CreateStandardMessagesMenu()
        {
            // Title
            CreateTitle("Standard Messages", 18, Color.white, TextAnchor.UpperCenter);

            // Add Standard Messages menu items here
            // TODO: Complete Standard Messages menu

            // Back button
            Button backButton = CreateButton("BackButton", "Back", new Vector2(0.3f, 0.05f), new Vector2(0.7f, 0.12f), 18, Color.white, TextAnchor.MiddleCenter, () => OnBackButtonClicked?.Invoke());
        }
    }
}
