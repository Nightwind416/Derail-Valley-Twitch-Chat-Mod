using System.Collections.Generic;
using System.Linq;
using System.Text;
using TwitchChat.Api;
using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.Plugins.Bundled.AiTraffic
{
    /// <summary>
    /// A header saying how the AI traffic is set up and how much of it is running, a row of switches for
    /// the mod's own world-space overlays, and a scrolling list of every AI train with what it is doing.
    /// </summary>
    internal sealed class AiTrafficContent : IPanelContent
    {
        /// <summary>
        /// How often the trains are re-read. Ten AI drivers each answering a dozen questions is not free,
        /// and none of the answers change fast enough to be worth a frame.
        /// </summary>
        private const float RefreshInterval = 0.4f;

        /// <summary>Room the header and the switches need, in canvas units, below the title row.</summary>
        private const float HeaderHeight = 55f;

        private const int CaptionX = 10;
        private const int ValueX = 105;

        /// <summary>
        /// The mod's own overlays, by the settings field each is stored in. The three world-space ones
        /// are the point of this: they are drawn in the world, so a headset can see them, and the only
        /// other way to reach them is the Unity Mod Manager menu.
        /// </summary>
        private static readonly (string Flag, string Caption)[] Overlays =
        {
            ("ShowLocoTags", "Locos"),
            ("ShowRouteVisualizer", "Path"),
            ("ShowSignalTags", "Signals"),
            ("DebugVisuals", "HUD")
        };

        private readonly IPanelSurface surface;
        private readonly AiTrafficBridge traffic;

        private readonly List<GameObject> trainRows = new();
        private readonly Dictionary<string, Text> overlayLabels = new();

        private Text? summary;
        private Text? counts;
        private float untilRefresh;

        internal AiTrafficContent(IPanelSurface surface, AiTrafficBridge traffic)
        {
            this.surface = surface;
            this.traffic = traffic;

            surface.ReserveHeader(HeaderHeight);
            BuildHeader();
        }

        public void OnShow() => Refresh();

        public void OnHide() { }

        public void OnResize(Vector2 size) { }

        public void Tick(float deltaTime)
        {
            untilRefresh -= deltaTime;
            if (untilRefresh > 0f)
            {
                return;
            }

            untilRefresh = RefreshInterval;
            Refresh();
        }

        // ------------------------------------------------------------------

        private void BuildHeader()
        {
            surface.Widgets.CreateLabel(surface.Root, "Traffic", CaptionX, 35, Color.gray);
            summary = surface.Widgets.CreateText(surface.Root, "--", ValueX, 35, Color.white);

            surface.Widgets.CreateLabel(surface.Root, "Trains", CaptionX, 53, Color.gray);
            counts = surface.Widgets.CreateText(surface.Root, "--", ValueX, 53, Color.white);

            // Four switches in a row. A button is placed by its centre, measured from the panel's left
            // edge, so the first sits half a button in
            const int buttonWidth = 52;
            int x = CaptionX + (buttonWidth / 2);

            foreach ((string flag, string caption) in Overlays)
            {
                string overlay = flag;
                Button button = surface.Widgets.CreateButton(
                    surface.Root,
                    caption,
                    x,
                    76,
                    Color.white,
                    () =>
                    {
                        traffic.ToggleOverlay(overlay);
                        UpdateOverlayButtons();
                    },
                    buttonWidth - 4);

                Text? label = button.GetComponentInChildren<Text>();
                if (label != null)
                {
                    overlayLabels[flag] = label;
                }

                x += buttonWidth;
            }

            UpdateOverlayButtons();
        }

        /// <summary>Colours each switch by whether that overlay is currently on.</summary>
        private void UpdateOverlayButtons()
        {
            foreach (KeyValuePair<string, Text> entry in overlayLabels)
            {
                entry.Value.color = traffic.ShowsOverlay(entry.Key) ? Color.cyan : Color.gray;
            }
        }

        private void Refresh()
        {
            UpdateOverlayButtons();

            if (summary != null)
            {
                summary.text = traffic.IsRunning
                    ? $"{traffic.Mode}, {traffic.Density}"
                    : "not running";
            }

            List<object> engineers = traffic.Engineers().ToList();

            if (counts != null)
            {
                int max = traffic.MaxTrains;
                int active = traffic.ActiveTrainCount;

                // The manager's own count and the list it hands out should agree; when they do not, the
                // list is the one that has rows behind it, so say both rather than pick
                counts.text = active == engineers.Count
                    ? $"{active} of {max}"
                    : $"{engineers.Count} listed, {active} of {max}";
            }

            RebuildTrainList(engineers);
        }

        /// <summary>
        /// Redraws the list of trains. One text block per train rather than a widget per figure: the list
        /// is rebuilt on every refresh, and a dozen objects per train would make that expensive.
        /// </summary>
        private void RebuildTrainList(List<object> engineers)
        {
            foreach (GameObject row in trainRows)
            {
                Object.Destroy(row);
            }
            trainRows.Clear();

            if (engineers.Count == 0)
            {
                string message = traffic.Error
                    ?? (traffic.IsRunning ? "No AI trains are running just now." : "AI traffic is not running.");
                AddRow(message, Color.gray);
                return;
            }

            foreach (object engineer in engineers)
            {
                AddRow(Describe(engineer), Color.white);
            }
        }

        private string Describe(object engineer)
        {
            TrainCar? car = traffic.CarOf(engineer);
            StringBuilder text = new();

            string id = car != null ? car.ID : "unknown";
            text.Append(id).Append("  ").Append(traffic.State(engineer));

            if (traffic.IsWorkerDriven(engineer))
            {
                text.Append("  (worker)");
            }

            text.AppendLine();
            text.Append($"  {traffic.SpeedKmh(engineer):F0} of {traffic.TargetSpeedKmh(engineer):F0} km/h");
            text.Append($"   T {Percent(traffic.Throttle(engineer))}  B {Percent(traffic.TrainBrake(engineer))}");

            float dynamic = traffic.DynamicBrake(engineer);
            if (dynamic > 0.01f)
            {
                text.Append($"  D {Percent(dynamic)}");
            }

            string location = traffic.LocationOf(car);
            string destination = traffic.DestinationOf(engineer);
            if (location.Length > 0 || destination.Length > 0)
            {
                text.AppendLine();
                text.Append("  ").Append(location.Length > 0 ? location : "?");
                text.Append(" to ").Append(destination.Length > 0 ? destination : "?");
            }

            string ahead = Ahead(traffic.DistanceToSignal(engineer), "signal");
            string obstacle = Ahead(traffic.DistanceToObstacle(engineer), "ahead");
            if (ahead.Length > 0 || obstacle.Length > 0)
            {
                text.AppendLine();
                text.Append("  ").Append(string.Join("   ", new[] { ahead, obstacle }.Where(part => part.Length > 0).ToArray()));
            }

            return text.ToString();
        }

        /// <summary>
        /// A distance the mod may or may not have. It reports nothing in sight as an unusable number,
        /// and a row reading "signal at infinity" helps nobody.
        /// </summary>
        private static string Ahead(float metres, string what)
        {
            return float.IsNaN(metres) || float.IsInfinity(metres) || metres <= 0f || metres > 10000f
                ? string.Empty
                : $"{what} {metres:F0} m";
        }

        private static string Percent(float fraction) => $"{Mathf.RoundToInt(Mathf.Clamp01(fraction) * 100f)}%";

        private void AddRow(string text, Color color)
        {
            // Four lines' worth of room: the longest description runs to four, and the scrolling area
            // gives each row whatever height it asks for
            Text row = surface.Widgets.CreateText(surface.Content, text, 0, 0, color, lines: 4);
            trainRows.Add(row.gameObject);
        }
    }
}
