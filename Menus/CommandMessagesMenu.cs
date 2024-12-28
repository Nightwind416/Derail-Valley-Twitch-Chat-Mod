using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.Menus
{
    public class CommandMessagesMenu : BaseMenu
    {
        public delegate void OnBackButtonClickedHandler();
        public event OnBackButtonClickedHandler OnBackButtonClicked;

        public CommandMessagesMenu(Transform parent) : base(parent)
        {
            CreateCommandMessagesMenu();
        }

        private void CreateCommandMessagesMenu()
        {
            // Title
            CreateTitle("Command Messages", 18, Color.white, TextAnchor.UpperCenter);

            // Add Command Messages menu items here
            // TODO: Complete Command Messages menu

            // Back button
            Button backButton = CreateButton("BackButton", "Back", new Vector2(0.3f, 0.05f), new Vector2(0.7f, 0.12f), 18, Color.white, TextAnchor.MiddleCenter, () => OnBackButtonClicked?.Invoke());
        }
    }
}
