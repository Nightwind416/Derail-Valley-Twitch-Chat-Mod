using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.Menus
{
    public class MediumDisplayBoard : BaseMenu
    {
        public delegate void OnBackButtonClickedHandler();
        public event OnBackButtonClickedHandler OnBackButtonClicked;

        public MediumDisplayBoard(Transform parent) : base(parent)
        {
            CreateMediumDisplayBoard();
        }

        private void CreateMediumDisplayBoard()
        {
            // Dimensions - 500x500
            
            // Title
            CreateTitle("Medium Display Board", 18, Color.white, TextAnchor.UpperCenter);

            // Back button
            Button backButton = CreateButton(menuObject.transform, " X ", 490, 10, Color.white, () => OnBackButtonClicked?.Invoke());

            // Status Section
            GameObject statusSection = CreateSection("Status", 20, 80);
            
            // Message Section
            GameObject messageSection = CreateSection("Message", 125, 120);
            
            // Add Medium Display Board items here
            // TODO: Complete Medium Display Board
        }
    }
}
