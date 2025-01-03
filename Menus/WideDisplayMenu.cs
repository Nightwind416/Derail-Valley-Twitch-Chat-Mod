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
            for (int i = 0; i < 3; i++)
            {
                CreateMessageSection(i);
            }
        }

        private void CreateWideDisplayBoard()
        {
            // Dimensions - 800x200
            
            // Title
            CreateTitle("Wide Display Board", 18, Color.white, TextAnchor.UpperCenter);

            // Back button
            Button backButton = CreateButton(menuObject.transform, " X ", 790, 10, Color.white, () => OnBackButtonClicked?.Invoke());
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
