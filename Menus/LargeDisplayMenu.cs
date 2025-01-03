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
            CreateStatusSection();
            for (int i = 0; i < 3; i++)
            {
                CreateMessageSection(i);
            }
        }

        private void CreateLargeDisplayBoard()
        {
            // Dimensions - 1000x700
            
            // Title
            CreateTitle("Large Display Board", 18, Color.white, TextAnchor.UpperCenter);

            // Back button
            Button backButton = CreateButton(menuObject.transform, " X ", 990, 10, Color.white, () => OnBackButtonClicked?.Invoke());
        }

        private void CreateStatusSection()
        {
            // Dimensions - Menu width minus 20

            GameObject statusSection = CreateSection("Status", 20, 60);
            
            // Add status section components here
            // TODO: Add status section content
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
