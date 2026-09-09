using System;
using System.Collections.Generic;
using System.Linq;
using TwitchChat.PanelConstructor;
using UnityEngine;

namespace TwitchChat.Plugins
{
    /// <summary>
    /// One kind of panel a display can show. The mod's own panels and those contributed by plugins are
    /// both described this way, so there is one list of panels rather than a built-in set and a bolted-on
    /// second set.
    /// </summary>
    public sealed class PanelDescriptor
    {
        private readonly Func<Transform, PanelHost?, BasePanel> create;
        private readonly Func<bool> available;

        /// <param name="id">
        /// Stable name for the panel. Written into settings as the panel a display was last showing, so
        /// it must not change once released.
        /// </param>
        /// <param name="title">What the title row and the menu button read.</param>
        /// <param name="isPlugin">True for panels contributed from outside the mod.</param>
        /// <param name="create">Builds an instance of the panel under the given parent.</param>
        /// <param name="available">Whether the panel should exist at all; defaults to always.</param>
        /// <param name="transient">
        /// True for a panel that is only ever reached from somewhere else and should not be remembered as
        /// the panel a display was last showing.
        /// </param>
        public PanelDescriptor(
            string id,
            string title,
            bool isPlugin,
            Func<Transform, PanelHost?, BasePanel> create,
            Func<bool>? available = null,
            bool transient = false)
        {
            Id = id;
            Title = title;
            IsPlugin = isPlugin;
            Transient = transient;
            this.create = create;
            this.available = available ?? (() => true);
        }

        public string Id { get; }
        public string Title { get; }
        public bool IsPlugin { get; }

        /// <summary>
        /// Not worth remembering as a display's active panel. The colours panel is one: it is opened from
        /// another panel's gear and belongs to that panel, so coming back to a display left on it would mean
        /// arriving at an editor with nothing to edit.
        /// </summary>
        public bool Transient { get; }
        /// <summary>
        /// Whether a display should carry this panel. Plugin panels the player has switched off in the
        /// Mods menu, and those whose mod is not installed, are left out entirely rather than shown empty.
        /// </summary>
        public bool IsAvailable
        {
            get
            {
                if (IsPlugin && Settings.Instance.IsPanelDisabled(Id))
                {
                    return false;
                }

                try
                {
                    return available();
                }
                catch (Exception ex)
                {
                    Main.LogEntry("PanelDescriptor.IsAvailable", $"Panel '{Id}' failed its availability check and will be left out: {ex.Message}");
                    return false;
                }
            }
        }

        /// <summary>
        /// Builds the panel. A plugin that throws on the way up loses its panel and nothing else, so one
        /// bad plugin cannot stop a display from being built.
        /// </summary>
        public BasePanel? Create(Transform parent, PanelHost? host)
        {
            try
            {
                return create(parent, host);
            }
            catch (Exception ex)
            {
                Main.LogEntry("PanelDescriptor.Create", $"Panel '{Id}' failed to build and will be left out: {ex}");
                return null;
            }
        }
    }

    /// <summary>
    /// The list of panels a display can show, in menu order.
    /// </summary>
    /// <remarks>
    /// Everything that used to be spelled out in five parallel places - the properties on a host, the
    /// construction, the hiding, the showing, and the per-frame refresh - now comes from here, which is
    /// what lets a panel arrive from a plugin without touching any of it.
    /// </remarks>
    public static class PanelRegistry
    {
        private static readonly List<PanelDescriptor> descriptors = new();
        private static bool builtInsRegistered;

        /// <summary>Every registered panel, whether or not it is currently available.</summary>
        public static IReadOnlyList<PanelDescriptor> All => descriptors;

        /// <summary>The panels a display should be built with right now.</summary>
        public static IEnumerable<PanelDescriptor> Available => descriptors.Where(d => d.IsAvailable);

        /// <summary>The available panels that came from plugins, which is what the Mods menu lists.</summary>
        public static IEnumerable<PanelDescriptor> AvailablePlugins => Available.Where(d => d.IsPlugin);

        /// <summary>Every plugin panel, including ones the player has switched off.</summary>
        public static IEnumerable<PanelDescriptor> AllPlugins => descriptors.Where(d => d.IsPlugin);

        public static PanelDescriptor? Find(string id) => descriptors.FirstOrDefault(d => d.Id == id);

        /// <summary>
        /// Adds a panel. A second panel claiming an id that is taken is refused, since the id is how a
        /// display remembers what it was showing.
        /// </summary>
        public static bool Register(PanelDescriptor descriptor)
        {
            if (Find(descriptor.Id) != null)
            {
                Main.LogEntry("PanelRegistry.Register", $"Refused panel '{descriptor.Id}': that id is already registered.");
                return false;
            }

            descriptors.Add(descriptor);
            return true;
        }

        /// <summary>
        /// Registers the mod's own panels. Called once before the first display is built.
        /// </summary>
        public static void RegisterBuiltIns()
        {
            if (builtInsRegistered)
            {
                return;
            }

            builtInsRegistered = true;

            Add("Main", (parent, host) => new PanelMenus.MainPanel(parent, host));
            Add("Authentication", (parent, _) => new PanelMenus.AuthenticationPanel(parent));
            Add("Status", (parent, _) => new PanelMenus.StatusPanel(parent));
            Add("Notifications", (parent, _) => new PanelMenus.NotificationsPanel(parent));
            Add("Chat", (parent, _) => new PanelDisplays.ChatPanel(parent));
            Add("Standard Messages", (parent, _) => new PanelMenus.StandardMessagesPanel(parent));
            Add("Command Messages", (parent, _) => new PanelMenus.CommandMessagesPanel(parent));
            Add("Timed Messages", (parent, _) => new PanelMenus.TimedMessagesPanel(parent));
            Add("Config1", (parent, _) => new PanelMenus.Config1Panel(parent));
            Add("Config2", (parent, _) => new PanelMenus.Config2Panel(parent));
            Add("Displays", (parent, _) => new PanelMenus.DisplaysPanel(parent));
            Add("Mods", (parent, host) => new PanelMenus.ModsPanel(parent, host));
            Add("Wrist Adjust", (parent, _) => new PanelMenus.WristAdjustPanel(parent));
            Add("Debug", (parent, _) => new PanelMenus.DebugPanel(parent));

            // Reached only from the gear on another panel's title row, so it has no button in the main menu
            Register(new PanelDescriptor(
                "Appearance",
                "Appearance",
                isPlugin: false,
                (parent, _) => new PanelMenus.AppearancePanel(parent),
                transient: true));
            static void Add(string id, Func<Transform, PanelHost?, BasePanel> create)
            {
                Register(new PanelDescriptor(id, id, isPlugin: false, create));
            }
        }
    }
}
