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
            // Dimensions - 900x220
            
            // Title
            CreateTitle("Wide Display Board", 18, Color.white, TextAnchor.UpperCenter);

            // Back button
            Button backButton = CreateButton(menuObject.transform, " X ", 890, 10, Color.white, () => OnBackButtonClicked?.Invoke());
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
