using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.PanelDisplays
{
    /// <summary>
    /// Implements a medium display panel for chat messages.
    /// Provides a balanced layout between compact and expanded views.
    /// </summary>
    public class MediumDisplayPanel : PanelConstructor.BasePanel
    {
        private int maxMessages = 500;
        private int messageCount = 0;

        public MediumDisplayPanel(Transform parent) : base(parent)
        {
            ModifyScrollView();
        }

        /// <summary>
        /// Modifies the scroll view settings for medium display format.
        /// Adjusts padding and layout settings for balanced display.
        /// </summary>
        private void ModifyScrollView()
        {
            if (contentRectTransform != null)
            {
                // Modify layout for medium display
                VerticalLayoutGroup verticalLayout = contentRectTransform.GetComponent<VerticalLayoutGroup>();
                if (verticalLayout != null)
                {
                    verticalLayout.padding = new RectOffset(8, 8, 8, 8);
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
