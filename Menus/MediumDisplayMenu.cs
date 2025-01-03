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
            CreateStatusSection();
            for (int i = 0; i < 3; i++)
            {
                CreateMessageSection(i);
            }
        }

        private void CreateMediumDisplayBoard()
        {
            // Dimensions - 500x500
            
            // Title
            CreateTitle("Medium Display Board", 18, Color.white, TextAnchor.UpperCenter);

            // Back button
            Button backButton = CreateButton(menuObject.transform, " X ", 490, 10, Color.white, () => OnBackButtonClicked?.Invoke());
        }

        private void CreateStatusSection()
        {
            GameObject statusSection = CreateSection("Status", 20, 80);
            
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
