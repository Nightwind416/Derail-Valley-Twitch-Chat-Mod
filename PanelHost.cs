using TwitchChat.PanelDisplays;
using TwitchChat.PanelMenus;
using UnityEngine;

namespace TwitchChat
{
    /// <summary>
    /// A surface that carries the mod's full panel stack: one world-space canvas plus an instance of
    /// every menu and display panel. Concrete hosts decide where the canvas lives (the locomotive cab
    /// or the player's wrist) and where the active panel choice is persisted.
    /// </summary>
    public abstract class PanelHost
    {
        public string Name { get; }
        public GameObject? MenuCanvas { get; set; }

        // Panel Menus
        public MainPanel? MainPanel { get; set; }
        public AuthenticationPanel? AuthenticationPanel { get; set; }
        public StatusPanel? StatusPanel { get; set; }
        public NotificationsPanel? NotificationsPanel { get; set; }
        public StandardMessagesPanel? StandardMessagesPanel { get; set; }
        public CommandMessagesPanel? CommandMessagesPanel { get; set; }
        public TimedMessagesPanel? TimedMessagesPanel { get; set; }
        public Config1Panel? Config1Panel { get; set; }
        public Config2Panel? Config2Panel { get; set; }
        public DebugPanel? DebugPanel { get; set; }

        // Panel Displays
        public LargeDisplayPanel? LargeDisplayPanel { get; set; }
        public MediumDisplayPanel? MediumDisplayPanel { get; set; }
        public SmallDisplayPanel? SmallDisplayPanel { get; set; }
        public WideDisplayPanel? WideDisplayPanel { get; set; }

        protected PanelHost(string name)
        {
            Name = name;
        }

        /// <summary>Name of the panel currently shown on this host. Persisted in settings.</summary>
        public abstract string ActivePanel { get; set; }
    }

    /// <summary>
    /// Host parented to the current locomotive's interior so it rides along with the cab.
    /// </summary>
    public class CabDisplayHost : PanelHost
    {
        /// <summary>True once the display has been placed (or restored from a saved pose) on a car.</summary>
        public bool Placed { get; set; }

        public CabDisplayHost() : base("CabDisplay") { }

        public override string ActivePanel
        {
            get => string.IsNullOrEmpty(Settings.Instance.cabDisplayPanel) ? "Main" : Settings.Instance.cabDisplayPanel;
            set => Settings.Instance.cabDisplayPanel = value;
        }
    }

    /// <summary>
    /// Host parented to a VR controller so it sits on the player's forearm like a watch.
    /// </summary>
    public class WristPanelHost : PanelHost
    {
        /// <summary>Whether the canvas is currently attached to the left hand (false = right hand).</summary>
        public bool AttachedToLeftHand { get; set; }

        public WristPanelHost() : base("WristPanel") { }

        public override string ActivePanel
        {
            get => string.IsNullOrEmpty(Settings.Instance.wristPanel) ? "Main" : Settings.Instance.wristPanel;
            set => Settings.Instance.wristPanel = value;
        }
    }
}
