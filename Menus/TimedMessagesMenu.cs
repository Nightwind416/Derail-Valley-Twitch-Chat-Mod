using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.Menus
{
    public class TimedMessagesMenu : BaseMenu
    {
        public delegate void OnBackButtonClickedHandler();
        public event OnBackButtonClickedHandler OnBackButtonClicked;

        public TimedMessagesMenu(Transform parent) : base(parent)
        {
            CreateTimedMessagesMenu();
        }

        private void CreateTimedMessagesMenu()
        {
            // Title
            CreateTitle("Timed Messages", 18, Color.white, TextAnchor.UpperCenter);

            // Add Timed Messages enu items here
            // TODO: Complete Timed Messages menu

            // Back button
            Button backButton = CreateButton("BackButton", "Back", new Vector2(0.3f, 0.05f), new Vector2(0.7f, 0.12f), 18, Color.white, TextAnchor.MiddleCenter, () => OnBackButtonClicked?.Invoke());
        }
    }
}
