using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.Menus
{
    public class WideDisplayBoard : BaseMenu
    {
        public delegate void OnBackButtonClickedHandler();
        public event OnBackButtonClickedHandler OnBackButtonClicked;

        public WideDisplayBoard(Transform parent) : base(parent)
        {
            CreateWideDisplayBoard();
        }

        private void CreateWideDisplayBoard()
        {
            // Dimensions - 800x200
            
            // Title
            CreateTitle("Wide Display Board", 18, Color.white, TextAnchor.UpperCenter);

            // Back button
            Button backButton = CreateButton(menuObject.transform, " X ", 790, 10, Color.white, () => OnBackButtonClicked?.Invoke());

            // Message Section
            GameObject messageSection1 = CreateSection("Message", 20, 80);
            
            // Message Section
            GameObject messageSection2 = CreateSection("Message", 120, 80);

            // Add WideDisplayBoard items here
            // TODO: Complete WideDisplayBoard
        }
    }
}
