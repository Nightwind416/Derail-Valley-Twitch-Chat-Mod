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

            // Add Medium Display Board items here
            // TODO: Complete Medium Display Board

            // Back button
            Button backButton = CreateButton("BackButton", "Back", 0, -350, 18, Color.white, TextAnchor.MiddleCenter, () => OnBackButtonClicked?.Invoke());
        }
    }
}
