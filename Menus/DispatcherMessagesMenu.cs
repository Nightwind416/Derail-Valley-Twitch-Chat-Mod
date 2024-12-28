using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.Menus
{
    public class DispatcherMessagesMenu : BaseMenu
    {
        public delegate void OnBackButtonClickedHandler();
        public event OnBackButtonClickedHandler OnBackButtonClicked;

        public DispatcherMessagesMenu(Transform parent) : base(parent)
        {
            CreateDispatcherMessagesMenu();
        }

        private void CreateDispatcherMessagesMenu()
        {
            // Title
            CreateTitle("Dispatcher Messages", 18, Color.white, TextAnchor.UpperCenter);

            // Add Dispatcher Messages menu items here
            // TODO: Complete Dispatcher Messages menu

            // Back button
            Button backButton = CreateButton("BackButton", "Back", new Vector2(0.3f, 0.05f), new Vector2(0.7f, 0.12f), 18, Color.white, TextAnchor.MiddleCenter, () => OnBackButtonClicked?.Invoke());
        }
    }
}
