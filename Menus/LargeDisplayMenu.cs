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
            // Title
            CreateTitle("Large Display Board", 18, Color.white, TextAnchor.UpperCenter);

            // Add Large Display Board items here
            // TODO: Complete Large Display Board

            // Back button
            Button backButton = CreateButton("BackButton", "Back", new Vector2(0.3f, 0.05f), new Vector2(0.7f, 0.12f), 18, Color.white, TextAnchor.MiddleCenter, () => OnBackButtonClicked?.Invoke());
        }
    }
}
