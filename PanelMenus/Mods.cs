using System.Collections.Generic;
using System.Linq;
using TwitchChat.Plugins;
using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.PanelMenus
{
    /// <summary>
    /// Lists the panels contributed by plugins, with a button to open each and a switch to turn each one
    /// off. The main menu has a fixed set of buttons and no room to grow, so plugin panels gather here
    /// instead and the list can be as long as it likes.
    /// </summary>
    /// <remarks>
    /// A plugin whose mod is not installed does not appear at all. One the player has switched off is
    /// listed, so it can be switched back on, but no display carries its panel: switching it back on takes
    /// effect the next time the display's panel stack is built, which is on boarding a locomotive.
    /// </remarks>
    public class ModsPanel : PanelConstructor.BasePanel
    {
        private const int RowHeight = 46;
        private const int FirstRowY = 35;

        private readonly PanelHost? host;
        private readonly List<GameObject> rows = new();

        /// <param name="parent">The parent transform this panel will be attached to.</param>
        /// <param name="host">The host this menu belongs to, or null for the hidden template copy.</param>
        public ModsPanel(Transform parent, PanelHost? host) : base(parent)
        {
            this.host = host;
            Rebuild();
        }

        /// <summary>
        /// Redraws the list. Called when the panel is opened, since a plugin can be switched off from one
        /// display and the others need to catch up.
        /// </summary>
        public void Rebuild()
        {
            foreach (GameObject row in rows)
            {
                Object.Destroy(row);
            }
            rows.Clear();

            List<PanelDescriptor> plugins = PanelRegistry.AllPlugins.ToList();

            if (plugins.Count == 0)
            {
                AddLabel("No mod panels are installed.", 5, FirstRowY);
                AddLabel("Panels for other mods live in the", 5, FirstRowY + 18);
                AddLabel("mod's Plugins folder.", 5, FirstRowY + 36);
                return;
            }

            for (int i = 0; i < plugins.Count; i++)
            {
                PanelDescriptor plugin = plugins[i];
                int y = FirstRowY + (i * RowHeight);

                bool switchedOff = Settings.Instance.IsPanelDisabled(plugin.Id);
                bool present = host?.GetPanel(plugin.Id) != null;

                AddLabel(plugin.Title, 5, y);
                AddOpenButton(plugin, y + 18, 55, enabled: present);
                AddEnableButton(plugin, y + 18, 150, switchedOff);
            }
        }

        private void AddLabel(string text, int x, int y)
        {
            Text label = PanelConstructor.Label.Create(panelObject.transform, text, x, y, Color.white);
            rows.Add(label.gameObject);
        }

        /// <summary>
        /// Opens the plugin's panel on this display. Greyed out when this display has no such panel, which
        /// happens for a plugin switched on since the display was built, or one whose mod has gone away.
        /// </summary>
        private void AddOpenButton(PanelDescriptor plugin, int y, int x, bool enabled)
        {
            Button button = PanelConstructor.Button.Create(
                panelObject.transform,
                enabled ? "Open" : "Reboard",
                x,
                y,
                enabled ? Color.white : Color.gray,
                () =>
                {
                    if (enabled && host != null)
                    {
                        MenuManager.Instance.OnPanelButtonClicked(plugin.Id, host);
                    }
                },
                70);

            rows.Add(button.gameObject);
        }

        /// <summary>
        /// Turns a plugin panel on or off. Taking effect on the next rebuild rather than at once is
        /// deliberate: tearing a panel out from under a display while it is on screen is a good way to
        /// leave a half-destroyed canvas behind.
        /// </summary>
        private void AddEnableButton(PanelDescriptor plugin, int y, int x, bool switchedOff)
        {
            Button? button = null;
            button = PanelConstructor.Button.Create(
                panelObject.transform,
                switchedOff ? "Off" : "On",
                x,
                y,
                Color.white,
                () =>
                {
                    bool nowOff = !Settings.Instance.IsPanelDisabled(plugin.Id);
                    Settings.Instance.SetPanelDisabled(plugin.Id, nowOff);

                    Text? label = button != null ? button.GetComponentInChildren<Text>() : null;
                    if (label != null)
                    {
                        label.text = nowOff ? "Off" : "On";
                    }
                },
                40);

            rows.Add(button.gameObject);
        }
    }
}
