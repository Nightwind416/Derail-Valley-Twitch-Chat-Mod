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
            // Dimensions - 1000x700
            
            // Title
            CreateTitle("Large Display Board", 18, Color.white, TextAnchor.UpperCenter);

            // Status Section
            GameObject statusSection = CreateSection("Status", 25, 75);
            
            // Message Section
            GameObject messageSection = CreateSection("Message", 100, 125);

            // Add Large Display Board items here
            // TODO: Complete Large Display Board

            // Back button
            Button backButton = CreateButton("Back", 0, -320, Color.white, () => OnBackButtonClicked?.Invoke());
        }
    }
}
