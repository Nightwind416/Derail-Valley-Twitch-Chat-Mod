using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.Menus
{
    public class SmallDisplayBoard : BaseMenu
    {
        public delegate void OnBackButtonClickedHandler();
        public event OnBackButtonClickedHandler OnBackButtonClicked;

        public SmallDisplayBoard(Transform parent) : base(parent)
        {
            CreateSmallDisplayBoard();
            CreateMessageSection();
        }

        private void CreateSmallDisplayBoard()
        {
            // Dimensions - 200x300

            // Title
            CreateTitle("Small Display Board", 18, Color.white, TextAnchor.UpperCenter);
            
            // Back button
            Button backButton = CreateButton(menuObject.transform, " X ", 190, 10, Color.white, () => OnBackButtonClicked?.Invoke());
        }

        private void CreateMessageSection()
        {
            GameObject messageSection = CreateSection("Message", 25, 100);
            
            // Add message section components here
            // TODO: Add message section content
        }
    }
}
