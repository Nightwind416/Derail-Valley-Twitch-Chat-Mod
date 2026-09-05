using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.PanelDisplays
{
    /// <summary>
    /// Shows the incoming Twitch chat. There is one of these per display, and how much of the chat you can
    /// read is decided by how big you have dragged the display, not by picking a preset size.
    /// </summary>
    public class ChatPanel : PanelConstructor.BasePanel
    {
        private const int MaxMessages = 500;

        private int messageCount;

        public ChatPanel(Transform parent) : base(parent)
        {
            ModifyScrollView();
        }

        /// <summary>
        /// Gives the message list a little breathing room inside the panel.
        /// </summary>
        private void ModifyScrollView()
        {
            if (contentRectTransform == null)
            {
                return;
            }

            VerticalLayoutGroup verticalLayout = contentRectTransform.GetComponent<VerticalLayoutGroup>();
            if (verticalLayout != null)
            {
                verticalLayout.padding = new RectOffset(8, 8, 8, 8);
            }
        }

        /// <summary>
        /// Adds a chat message, dropping the oldest once the panel is full.
        /// </summary>
        /// <param name="username">The username of the message sender.</param>
        /// <param name="message">The content of the chat message.</param>
        public void AddChatMessage(string username, string message)
        {
            if (messageCount >= MaxMessages && contentRectTransform.childCount > 0)
            {
                Object.Destroy(contentRectTransform.GetChild(0).gameObject);
                messageCount--;
            }

            AddMessage(username, message);
            messageCount++;
        }
    }
}
