using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.PanelDisplays
{
    /// <summary>
    /// Implements a large display panel for chat messages.
    /// Provides maximum visibility with generous spacing and padding.
    /// </summary>
    public class LargeDisplayPanel : PanelConstructor.BasePanel
    {
        private int maxMessages = 500;
        private int messageCount = 0;

        public LargeDisplayPanel(Transform parent) : base(parent)
        {
            ModifyScrollView();
        }

        /// <summary>
        /// Modifies the scroll view settings for large display format.
        /// Adjusts padding and layout settings for maximum readability.
        /// </summary>
        private void ModifyScrollView()
        {
            if (contentRectTransform != null)
            {
                // Modify layout for large display
                VerticalLayoutGroup verticalLayout = contentRectTransform.GetComponent<VerticalLayoutGroup>();
                if (verticalLayout != null)
                {
                    verticalLayout.padding = new RectOffset(10, 10, 10, 10);
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
