using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.Menus
{
    public class LargeDisplayBoard : BaseMenu
    {
        public delegate void OnBackButtonClickedHandler();
        public event OnBackButtonClickedHandler OnBackButtonClicked;

        public LargeDisplayBoard(Transform parent) : base(parent)
        {
            CreateLargeDisplayBoard();

        }

        private void CreateLargeDisplayBoard()
        {
            // Dimensions - 1200x650
            
            // Title
            CreateTitle("Large Display Board", 18, Color.white, TextAnchor.UpperCenter);

            // Back button
            Button backButton = CreateButton(menuObject.transform, " X ", 1190, 10, Color.white, () => OnBackButtonClicked?.Invoke());
        }

        private void CreateMessageSection(int i)
        {
            // Dimensions - Menu width minus 20

            // Message Section
            GameObject messageSection = CreateSection("Message", 20, 80);
            
            // TODO: Add message section content
        }
    }
}
