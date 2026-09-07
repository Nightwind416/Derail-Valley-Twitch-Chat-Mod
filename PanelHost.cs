using System;
using System.Collections.Generic;
using System.Linq;
using TwitchChat.PanelConstructor;
using TwitchChat.PanelDisplays;
using UnityEngine;

namespace TwitchChat
{
    /// <summary>
    /// A surface that carries the mod's full panel stack: one world-space canvas plus an instance of
    /// every panel the registry offers. Concrete hosts decide where the canvas lives (the locomotive cab
    /// or the player's wrist) and where the active panel choice is persisted.
    /// </summary>
    /// <remarks>
    /// A host holds its panels by id rather than as a property each, so a panel contributed by a plugin
    /// rides along with the mod's own without anything here knowing about it.
    /// </remarks>
    public abstract class PanelHost
    {
        private readonly Dictionary<string, BasePanel> panels = new();

        public string Name { get; }
        public GameObject? MenuCanvas { get; set; }

        protected PanelHost(string name)
        {
            Name = name;
        }

        /// <summary>Name of the panel currently shown on this host. Persisted in settings.</summary>
        public abstract string ActivePanel { get; set; }

        /// <summary>Every panel built for this host, in the order the registry offered them.</summary>
        public IEnumerable<BasePanel> Panels => panels.Values;

        /// <summary>Adds a panel under the id displays remember it by.</summary>
        public void AddPanel(string id, BasePanel panel) => panels[id] = panel;

        /// <summary>Forgets every panel, for when the canvas carrying them has gone.</summary>
        public void ClearPanels() => panels.Clear();

        /// <summary>The panel with this id, or null if this host has not got one.</summary>
        public BasePanel? GetPanel(string id) => panels.TryGetValue(id, out BasePanel? panel) ? panel : null;

        /// <summary>
        /// The panel of a given type. Used by the few places that need to say something specific to one
        /// panel, such as handing a chat message to the chat panel.
        /// </summary>
        public T? Get<T>() where T : BasePanel => panels.Values.OfType<T>().FirstOrDefault();

        /// <summary>The panel that shows incoming chat, if this host has one.</summary>
        public ChatPanel? ChatPanel => Get<ChatPanel>();

        /// <summary>
        /// Puts a close button on every panel of this host. Hosts that cannot be closed, such as the wrist
        /// panel, simply never call this and their close buttons stay hidden.
        /// </summary>
        public void SetCloseAction(Action? action)
        {
            foreach (BasePanel panel in panels.Values)
            {
                panel.SetCloseAction(action);
            }
        }
    }

    /// <summary>
    /// Host parented to the current locomotive's interior so it rides along with the cab. A locomotive type
    /// can carry several of these, each backed by its own saved slot.
    /// </summary>
    public class CabDisplayHost : PanelHost
    {
        /// <summary>The saved slot this display reads and writes: pose, size, panel and locks.</summary>
        public CabDisplayPose Slot { get; }

        /// <summary>True once the display has been placed, or restored from its saved pose, on a car.</summary>
        public bool Placed { get; set; }

        /// <summary>The grab bars framing this display, once its canvas exists.</summary>
        public PanelGrabHandles? Handles { get; set; }

        public CabDisplayHost(CabDisplayPose slot, int number) : base($"CabDisplay{number}")
        {
            Slot = slot;
        }

        public override string ActivePanel
        {
            get => string.IsNullOrEmpty(Slot.activePanel) ? "Main" : Slot.activePanel;
            set => Slot.activePanel = value;
        }
    }

    /// <summary>
    /// Host parented to the pad on the back of a VR hand, the one the game sticks carried items to, so it sits
    /// there like a watch. It rests as a small button and only unfolds into the full panel stack when opened.
    /// </summary>
    public class WristPanelHost : PanelHost
    {
        /// <summary>Whether the canvas is currently attached to the left hand (false = right hand).</summary>
        public bool AttachedToLeftHand { get; set; }

        /// <summary>
        /// The canvas carrying the small button the panel rests as, shown in place of the menus while
        /// collapsed. It is a canvas of its own so that it can be placed on the hand separately from them.
        /// </summary>
        public GameObject? ButtonCanvas { get; set; }

        /// <summary>
        /// True while the menus are open. Starts false so the panel is out of the way until the player
        /// presses its button, and going back from the main menu folds it away again.
        /// </summary>
        public bool Expanded { get; set; }

        public WristPanelHost() : base("WristPanel") { }

        public override string ActivePanel
        {
            get => string.IsNullOrEmpty(Settings.Instance.wristPanel) ? "Main" : Settings.Instance.wristPanel;
            set => Settings.Instance.wristPanel = value;
        }
    }
}
