using DV.CabControls;
using TwitchChat.PanelDisplays;
using TwitchChat.PanelMenus;
using UnityEngine;

namespace TwitchChat
{
    /// <summary>
    /// A surface that carries the mod's full panel stack: one world-space canvas plus an instance of
    /// every menu and display panel. Concrete hosts decide where the canvas lives (a license paper,
    /// the locomotive cab, or the player's wrist) and where the active panel choice is persisted.
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
    /// Legacy host: the canvas rides on one of the player's license papers and hides the printed page.
    /// </summary>
    public class LicenseHost : PanelHost
    {
        /// <summary>Slot in <see cref="Settings.activePanels"/> that remembers this host's panel.</summary>
        public int LicenseIndex { get; }

        /// <summary>The prefab name the game reports for this license item; identical to <see cref="PanelHost.Name"/>.</summary>
        public string PrefabName => Name;

        /// <summary>The live item component, or null until the item has been found in the world.</summary>
        public ItemBase? Item { get; set; }

        /// <summary>The license GameObject the canvas follows.</summary>
        public GameObject? LicenseObject { get; set; }

        /// <summary>The printed page renderer that is kept hidden while the canvas covers it.</summary>
        public Renderer? PaperRenderer { get; set; }

        /// <summary>Fallback paper object for prefabs without a Page component.</summary>
        public GameObject? PaperObject { get; set; }

        public bool AttachedToStickyTape { get; set; }
        public GameObject? StickyTapeBase { get; set; }

        public LicenseHost(string prefabName, int index) : base(prefabName)
        {
            LicenseIndex = index;
        }

        public override string ActivePanel
        {
            get
            {
                string[] panels = Settings.Instance.activePanels;
                return LicenseIndex < panels.Length && !string.IsNullOrEmpty(panels[LicenseIndex]) ? panels[LicenseIndex] : "Main";
            }
            set
            {
                if (LicenseIndex < Settings.Instance.activePanels.Length)
                {
                    Settings.Instance.activePanels[LicenseIndex] = value;
                }
            }
        }
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
