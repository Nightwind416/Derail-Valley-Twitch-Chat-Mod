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
        }

        private void CreateSmallDisplayBoard()
        {
            // Dimensions - 200x300
            
            // Back button
            Button backButton = CreateButton(menuObject.transform, " X ", 190, 10, Color.white, () => OnBackButtonClicked?.Invoke());
            
            // Title
            CreateTitle("Small Display Board", 18, Color.white, TextAnchor.UpperCenter);

            // Message Section
            GameObject messageSection = CreateSection("Message", 25, 100);
            
            // Add SmallDisplayBoard items here
            // TODO: Complete SmallDisplayBoard
        }
    }
}
