using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.PanelDisplays
{
    /// <summary>
    /// Implements a small display panel for chat messages.
    /// Optimized for minimal screen space usage with compact layout.
    /// </summary>
    public class SmallDisplayPanel : PanelConstructor.BasePanel
    {
        private int maxMessages = 500;
        private int messageCount = 0;

        public SmallDisplayPanel(Transform parent) : base(parent)
        {
            ModifyScrollView();
        }

        /// <summary>
        /// Modifies the scroll view settings for small display format.
        /// Adjusts padding and layout settings for compact display.
        /// </summary>
        private void ModifyScrollView()
        {
            if (contentRectTransform != null)
            {
                // Modify layout for small display
                VerticalLayoutGroup verticalLayout = contentRectTransform.GetComponent<VerticalLayoutGroup>();
                if (verticalLayout != null)
                {
                    verticalLayout.padding = new RectOffset(5, 5, 5, 5);
                }
            }
        }

        /// <summary>
        /// Adds a new chat message to the display panel.
        /// Manages message limit by removing oldest messages when necessary.
        /// </summary>
        /// <param name="username">The username of the message sender.</param>
        /// <param name="message">The content of the chat message.</param>
        public void AddChatMessage(string username, string message)
        {
            if (messageCount >= maxMessages)
            {
                if (contentRectTransform.childCount > 0)
                {
                    GameObject.Destroy(contentRectTransform.GetChild(0).gameObject);
                    messageCount--;
                }
            }
            
            AddMessage(username, message);
            messageCount++;
        }
    }
}
