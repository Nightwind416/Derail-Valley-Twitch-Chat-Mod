using System.Collections.Generic;
using System.Linq;
using TwitchChat.Api;
using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.Plugins.Bundled.Dispatch
{
    /// <summary>
    /// A live map of the railway: the track, the junctions, every train and every player, drawn from the
    /// Remote Dispatch mod's own data.
    /// </summary>
    /// <remarks>
    /// The mod this reads from shows all of this already, as a Leaflet page in a browser. A browser is
    /// exactly what a player in a headset has not got, which is the whole reason for this panel. It is an
    /// overview, not a replacement: there are no job details, no car list and no locomotive control here.
    /// </remarks>
    internal sealed class DispatchMapContent : IPanelContent
    {
        /// <summary>How often the moving things are re-read. The mod's own page polls faster; a panel a
        /// person glances at does not need to.</summary>
        private const float RefreshInterval = 0.5f;

        /// <summary>Room for the two rows of buttons, in canvas units, below the title row.</summary>
        private const float HeaderHeight = 46f;

        /// <summary>Room for the one line of status under the buttons, above the map.</summary>
        private const float StatusHeight = 22f;

        /// <summary>How far the map spans at each end of the zoom range, in the mod's degrees.</summary>
        private const float WidestSpan = 0.17f;
        private const float NarrowestSpan = 0.004f;

        private readonly IPanelSurface surface;
        private readonly DispatchBridge dispatch;

        private List<TrackLine> tracks = new();
        private List<Vector2> junctionPositions = new();
        private int[] junctionStates = System.Array.Empty<int>();

        /// <summary>
        /// Built on first use rather than in the constructor. A panel is created for every display
        /// whether or not it is the one on show, and the map's textures are the largest thing any panel
        /// here allocates; a display showing chat should not be paying for them.
        /// </summary>
        private MapCanvas? canvas;

        private GameObject? mapArea;
        private Text? status;
        private Text? followLabel;

        private bool loaded;
        private bool announced;
        private bool follow = true;
        private bool terrainDirty = true;
        private float untilRefresh;

        internal DispatchMapContent(IPanelSurface surface, DispatchBridge dispatch)
        {
            this.surface = surface;
            this.dispatch = dispatch;

            surface.ReserveHeader(HeaderHeight);
            Build();
        }

        public void OnShow()
        {
            terrainDirty = true;
            Refresh();
        }

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
        // Building
        // ------------------------------------------------------------------

        private void Build()
        {
            // Zoom and follow on the first row, panning on the second
            surface.Widgets.CreateButton(surface.Root, "-", 24, 44, Color.white, () => Zoom(1f / 0.6f), 28);
            surface.Widgets.CreateButton(surface.Root, "+", 56, 44, Color.white, () => Zoom(0.6f), 28);

            Button followButton = surface.Widgets.CreateButton(surface.Root, "Follow", 112, 44, Color.white, ToggleFollow, 76);
            followLabel = followButton.GetComponentInChildren<Text>();

            surface.Widgets.CreateButton(surface.Root, "^", 172, 44, Color.white, () => Pan(1, 0), 26);
            surface.Widgets.CreateButton(surface.Root, "v", 202, 44, Color.white, () => Pan(-1, 0), 26);
            surface.Widgets.CreateButton(surface.Root, "<", 172, 66, Color.white, () => Pan(0, -1), 26);
            surface.Widgets.CreateButton(surface.Root, ">", 202, 66, Color.white, () => Pan(0, 1), 26);

            // The map keeps its square shape whatever shape the display has been dragged into, since the
            // texture and the coordinates behind it are both square
            GameObject container = new("MapContainer", typeof(RectTransform));
            container.transform.SetParent(surface.Root, false);

            RectTransform containerRect = container.GetComponent<RectTransform>();
            containerRect.anchorMin = Vector2.zero;
            containerRect.anchorMax = Vector2.one;
            containerRect.offsetMin = new Vector2(6, 6);

            // Below the title row, the two rows of buttons, and the status line under them
            containerRect.offsetMax = new Vector2(-6, -(35f + HeaderHeight + StatusHeight));

            mapArea = new GameObject("Map", typeof(RectTransform));
            mapArea.transform.SetParent(container.transform, false);

            RectTransform mapRect = mapArea.GetComponent<RectTransform>();
            mapRect.anchorMin = new Vector2(0.5f, 0.5f);
            mapRect.anchorMax = new Vector2(0.5f, 0.5f);
            mapRect.pivot = new Vector2(0.5f, 0.5f);

            AspectRatioFitter fitter = mapArea.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = 1f;

            status = surface.Widgets.CreateText(surface.Root, "Reading the track layout...", 10, 35 + (int)HeaderHeight + 4, Color.gray);

            UpdateFollowLabel();
        }

        /// <summary>
        /// Makes the drawing surface, the first time the panel is actually asked to draw one.
        /// </summary>
        private MapCanvas EnsureCanvas()
        {
            if (canvas != null)
            {
                return canvas;
            }

            canvas = new MapCanvas();

            if (mapArea != null)
            {
                AddLayer(mapArea.transform, canvas.Terrain);
                AddLayer(mapArea.transform, canvas.Markers);

                // Textures outlive their references unless something destroys them, and the panel's own
                // object is the right thing to tie them to
                mapArea.AddComponent<MapTextureOwner>().Own(canvas);
            }

            return canvas;
        }

        private static void AddLayer(Transform parent, Texture2D texture)
        {
            GameObject layer = new(texture.name, typeof(RectTransform));
            layer.transform.SetParent(parent, false);

            RawImage image = layer.AddComponent<RawImage>();
            image.texture = texture;
            image.raycastTarget = false;

            RectTransform rect = layer.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        // ------------------------------------------------------------------
        // Controls
        // ------------------------------------------------------------------

        private void Zoom(float factor)
        {
            MapCanvas map = EnsureCanvas();
            map.Span = Mathf.Clamp(map.Span * factor, NarrowestSpan, WidestSpan);
            terrainDirty = true;
        }

        private void Pan(int latSteps, int lonSteps)
        {
            // Panning by hand means the player wants to look somewhere other than at themselves
            follow = false;
            UpdateFollowLabel();

            MapCanvas map = EnsureCanvas();
            float step = map.Span * 0.25f;
            map.Centre += new Vector2(latSteps * step, lonSteps * step);
            terrainDirty = true;
        }

        private void ToggleFollow()
        {
            follow = !follow;
            UpdateFollowLabel();
        }

        private void UpdateFollowLabel()
        {
            if (followLabel != null)
            {
                followLabel.text = follow ? "Following" : "Follow";
                followLabel.color = follow ? Color.cyan : Color.white;
            }
        }

        // ------------------------------------------------------------------
        // Drawing
        // ------------------------------------------------------------------

        private void Refresh()
        {
            if (!dispatch.IsInstalled)
            {
                Say("The Remote Dispatch mod is not installed.");
                return;
            }

            if (!loaded && !LoadLayout())
            {
                return;
            }

            MapCanvas map = EnsureCanvas();
            List<MapMarker> markers = dispatch.Markers();

            if (follow && markers.FirstOrDefault(marker => marker.IsPlayer) is { IsPlayer: true } player)
            {
                // Recentre only once the player has drifted well away from the middle, so that walking
                // about does not redraw every rail on the map twice a second
                if ((player.Position - map.Centre).sqrMagnitude > Mathf.Pow(map.Span * 0.2f, 2f))
                {
                    map.Centre = player.Position;
                    terrainDirty = true;
                }
            }

            // A switch thrown anywhere on the map changes the terrain layer, and a dispatcher watching
            // this panel is precisely the person who wants to see that happen
            int[] states = dispatch.JunctionStates();
            if (!states.SequenceEqual(junctionStates))
            {
                junctionStates = states;
                terrainDirty = true;
            }

            if (terrainDirty)
            {
                terrainDirty = false;
                map.DrawTerrain(tracks, Junctions());
            }

            map.DrawMarkers(markers);

            int trains = markers.Count(marker => marker.IsLoco);
            // One short line: the panel can be dragged narrow, and a status that wraps eats the map
            Say($"{markers.Count(m => !m.IsPlayer)} cars, {trains} locos, {Mathf.RoundToInt(map.Span / WidestSpan * 100f)}%");
        }

        /// <summary>The junctions, paired with which way each is currently set.</summary>
        private IEnumerable<JunctionPoint> Junctions()
        {
            for (int i = 0; i < junctionPositions.Count; i++)
            {
                yield return new JunctionPoint
                {
                    Position = junctionPositions[i],
                    SelectedBranch = i < junctionStates.Length ? junctionStates[i] : -1
                };
            }
        }

        /// <summary>
        /// Reads the track layout, which is the expensive part and does not change during a session.
        /// </summary>
        /// <remarks>
        /// Deliberately not done on the first tick: the panel says what it is doing, and only reads on the
        /// tick after that, so the message is on screen before the game stops to parse the whole railway.
        /// </remarks>
        private bool LoadLayout()
        {
            if (!announced)
            {
                announced = true;
                Say("Reading the track layout...");
                return false;
            }

            tracks = dispatch.Tracks();

            if (tracks.Count == 0)
            {
                Say(dispatch.Error ?? "No track layout yet. It appears once a save is loaded.");

                // Not a permanent failure: the world may simply not be loaded, so try again next refresh
                announced = false;
                return false;
            }

            junctionPositions = dispatch.JunctionPositions();
            loaded = true;

            // Start looking at the whole railway, centred on it
            Bounds bounds = new(tracks[0].Points[0], Vector3.zero);
            foreach (TrackLine track in tracks)
            {
                foreach (Vector2 point in track.Points)
                {
                    bounds.Encapsulate(point);
                }
            }

            MapCanvas map = EnsureCanvas();
            map.Centre = bounds.center;
            map.Span = Mathf.Clamp(Mathf.Max(bounds.size.x, bounds.size.y) * 1.05f, NarrowestSpan, WidestSpan);
            terrainDirty = true;

            surface.Log($"Read {tracks.Count} tracks and {junctionPositions.Count} junctions from Remote Dispatch.");
            return true;
        }

        private void Say(string message)
        {
            if (status != null)
            {
                status.text = message;
            }
        }
    }
}
