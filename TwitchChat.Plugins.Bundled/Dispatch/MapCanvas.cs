using System;
using System.Collections.Generic;
using UnityEngine;

namespace TwitchChat.Plugins.Bundled.Dispatch
{
    /// <summary>
    /// Draws the map into a pair of textures: a slow one for the track and junctions, which only changes
    /// when the view does, and a fast one for the things that move, redrawn on every refresh.
    /// </summary>
    /// <remarks>
    /// Two layers rather than one so that a hundred moving cars cost a clear and a hundred dots, not a
    /// redraw of every rail on the map. Two textures rather than a marker object per car for the same
    /// reason: creating and destroying that many UI objects several times a second is the expensive way
    /// to do this, and none of them need to be clickable.
    /// <para>
    /// The track layer is drawn a bounded number of segments at a time, across frames. Not because the
    /// railway is known to be too big to draw at once - at forty metres a sample it is not - but because
    /// how much work "the whole map" is depends on another mod's data and on how far the player has
    /// zoomed in, and no amount of that belongs in a single frame of a game being played in a headset.
    /// </para>
    /// </remarks>
    internal sealed class MapCanvas : IDisposable
    {
        /// <summary>
        /// Side of both textures in pixels. The window is redrawn whenever the view moves, so this is
        /// resolution within the current view rather than over the whole map, and 512 is plenty for a
        /// panel a person is reading from a couple of feet away.
        /// </summary>
        private const int Size = 512;

        private static readonly Color32 Transparent = new(0, 0, 0, 0);
        private static readonly Color32 Background = new(12, 16, 22, 255);
        private static readonly Color32 Mainline = new(150, 175, 205, 255);
        private static readonly Color32 Siding = new(95, 105, 120, 255);
        private static readonly Color32 JunctionSet = new(90, 200, 255, 255);
        private static readonly Color32 JunctionUnknown = new(150, 150, 150, 255);
        private static readonly Color32 Car = new(190, 190, 190, 255);
        private static readonly Color32 Loco = new(255, 190, 60, 255);
        private static readonly Color32 Player = new(90, 230, 130, 255);

        private readonly Color32[] terrainPixels = new Color32[Size * Size];
        private readonly Color32[] markerPixels = new Color32[Size * Size];
        private readonly Color32[] clearedMarkers = new Color32[Size * Size];

        // Where the track drawing has got to, since it is spread over several calls
        private IReadOnlyList<TrackLine>? pendingTracks;
        private IReadOnlyList<JunctionPoint>? pendingJunctions;
        private int trackIndex;
        private int pointIndex;

        internal MapCanvas()
        {
            Terrain = NewTexture("MapTerrain");
            Markers = NewTexture("MapMarkers");

            for (int i = 0; i < clearedMarkers.Length; i++)
            {
                clearedMarkers[i] = Transparent;
            }
        }

        /// <summary>The track and junction layer.</summary>
        internal Texture2D Terrain { get; }

        /// <summary>The trains and players layer, drawn over the other.</summary>
        internal Texture2D Markers { get; }

        /// <summary>
        /// The map coordinates at the centre of the view, and how many degrees of it the view spans.
        /// </summary>
        internal Vector2 Centre { get; set; }

        internal float Span { get; set; } = 0.16f;

        /// <summary>How many segments have been drawn since the track layer was last started.</summary>
        internal int SegmentsDrawn { get; private set; }

        /// <summary>Whether the track layer is part drawn and wants more calls to <see cref="ContinueTerrain"/>.</summary>
        internal bool TerrainInProgress => pendingTracks != null;

        /// <summary>
        /// Starts redrawing the track and junction layer. Nothing is drawn yet; call
        /// <see cref="ContinueTerrain"/> until it reports itself finished.
        /// </summary>
        internal void BeginTerrain(IReadOnlyList<TrackLine> tracks, IReadOnlyList<JunctionPoint> junctions)
        {
            for (int i = 0; i < terrainPixels.Length; i++)
            {
                terrainPixels[i] = Background;
            }

            pendingTracks = tracks;
            pendingJunctions = junctions;
            trackIndex = 0;
            pointIndex = 1;
            SegmentsDrawn = 0;
        }

        /// <summary>
        /// Draws up to <paramref name="segmentBudget"/> more segments of the track layer.
        /// </summary>
        /// <returns>True once the whole layer is drawn.</returns>
        internal bool ContinueTerrain(int segmentBudget)
        {
            if (pendingTracks == null)
            {
                return true;
            }

            int drawn = 0;

            while (trackIndex < pendingTracks.Count)
            {
                TrackLine track = pendingTracks[trackIndex];
                Color32 colour = track.IsSiding ? Siding : Mainline;

                while (pointIndex < track.Points.Length)
                {
                    Line(terrainPixels, ToPixel(track.Points[pointIndex - 1]), ToPixel(track.Points[pointIndex]), colour);
                    pointIndex++;

                    if (++drawn >= segmentBudget)
                    {
                        SegmentsDrawn += drawn;
                        Publish(Terrain, terrainPixels);
                        return false;
                    }
                }

                trackIndex++;
                pointIndex = 1;
            }

            foreach (JunctionPoint junction in pendingJunctions ?? Array.Empty<JunctionPoint>())
            {
                Dot(terrainPixels, ToPixel(junction.Position), 1,
                    junction.SelectedBranch >= 0 ? JunctionSet : JunctionUnknown);
            }

            SegmentsDrawn += drawn;
            pendingTracks = null;
            pendingJunctions = null;

            Publish(Terrain, terrainPixels);
            return true;
        }

        /// <summary>Redraws the trains and players for the current view.</summary>
        internal void DrawMarkers(IEnumerable<MapMarker> markers)
        {
            Array.Copy(clearedMarkers, markerPixels, markerPixels.Length);

            // Plain cars first, so a locomotive or a player in the middle of a rake stays visible
            foreach (MapMarker marker in markers)
            {
                if (!marker.IsPlayer && !marker.IsLoco)
                {
                    Dot(markerPixels, ToPixel(marker.Position), 1, Car);
                }
            }

            foreach (MapMarker marker in markers)
            {
                if (marker.IsLoco)
                {
                    Dot(markerPixels, ToPixel(marker.Position), 2, Loco);
                }
            }

            foreach (MapMarker marker in markers)
            {
                if (marker.IsPlayer)
                {
                    Dot(markerPixels, ToPixel(marker.Position), 3, Player);
                }
            }

            Publish(Markers, markerPixels);
        }

        public void Dispose()
        {
            UnityEngine.Object.Destroy(Terrain);
            UnityEngine.Object.Destroy(Markers);
        }

        // ------------------------------------------------------------------

        private static void Publish(Texture2D texture, Color32[] pixels)
        {
            texture.SetPixels32(pixels);
            texture.Apply(updateMipmaps: false);
        }

        /// <summary>
        /// Map coordinates to texture pixels, as floats. Longitude runs across and latitude up, which is
        /// the way round the mod's own map has them. A texture counts its rows up from the bottom, which
        /// for once is what is wanted: north ends up at the top with no flip.
        /// </summary>
        private Vector2 ToPixel(Vector2 point)
        {
            float half = Span / 2f;
            float x = (point.y - (Centre.y - half)) / Span;
            float y = (point.x - (Centre.x - half)) / Span;

            return new Vector2(x * (Size - 1), y * (Size - 1));
        }

        private static Texture2D NewTexture(string name)
        {
            return new Texture2D(Size, Size, TextureFormat.RGBA32, mipChain: false)
            {
                name = name,

                // Point filtering: a track one pixel wide goes grey and vanishes under bilinear
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
        }

        private static void Dot(Color32[] pixels, Vector2 at, int radius, Color32 colour)
        {
            // A marker well off the view would otherwise cost a loop over its whole area for nothing
            if (at.x < -radius || at.y < -radius || at.x > Size + radius || at.y > Size + radius ||
                float.IsNaN(at.x) || float.IsNaN(at.y))
            {
                return;
            }

            int cx = Mathf.RoundToInt(at.x);
            int cy = Mathf.RoundToInt(at.y);

            for (int dy = -radius; dy <= radius; dy++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    if (dx * dx + dy * dy <= radius * radius)
                    {
                        Plot(pixels, cx + dx, cy + dy, colour);
                    }
                }
            }
        }

        /// <summary>
        /// Draws a line, clipped to the view first.
        /// </summary>
        /// <remarks>
        /// Clipping rather than plotting-and-discarding is what keeps this bounded: zoomed well in, a
        /// segment can run tens of thousands of pixels past the edge of the view, and walking all of it
        /// to throw every pixel away is how a map turns into a frozen game.
        /// </remarks>
        private static void Line(Color32[] pixels, Vector2 from, Vector2 to, Color32 colour)
        {
            if (!ClipToView(ref from, ref to))
            {
                return;
            }

            int x = Mathf.RoundToInt(from.x);
            int y = Mathf.RoundToInt(from.y);
            int toX = Mathf.RoundToInt(to.x);
            int toY = Mathf.RoundToInt(to.y);

            int dx = Mathf.Abs(toX - x);
            int dy = -Mathf.Abs(toY - y);
            int stepX = x < toX ? 1 : -1;
            int stepY = y < toY ? 1 : -1;
            int error = dx + dy;

            // Clipped to the view, so this can never exceed twice its width. Kept as a guard anyway: a
            // rounding disagreement between the clip and the walk should cost a wonky line, not a hang
            int guard = (dx - dy) + 2;

            while (guard-- > 0)
            {
                Plot(pixels, x, y, colour);

                if (x == toX && y == toY)
                {
                    return;
                }

                int doubled = error * 2;
                if (doubled >= dy)
                {
                    error += dy;
                    x += stepX;
                }
                if (doubled <= dx)
                {
                    error += dx;
                    y += stepY;
                }
            }
        }

        /// <summary>
        /// Liang-Barsky: trims a segment to the visible rectangle, or reports it wholly outside.
        /// </summary>
        private static bool ClipToView(ref Vector2 from, ref Vector2 to)
        {
            if (float.IsNaN(from.x) || float.IsNaN(from.y) || float.IsNaN(to.x) || float.IsNaN(to.y))
            {
                return false;
            }

            const float min = 0f;
            float max = Size - 1;

            float dx = to.x - from.x;
            float dy = to.y - from.y;
            float enter = 0f;
            float exit = 1f;

            if (!Trim(-dx, from.x - min, ref enter, ref exit) ||
                !Trim(dx, max - from.x, ref enter, ref exit) ||
                !Trim(-dy, from.y - min, ref enter, ref exit) ||
                !Trim(dy, max - from.y, ref enter, ref exit))
            {
                return false;
            }

            Vector2 start = from;
            from = new Vector2(start.x + (enter * dx), start.y + (enter * dy));
            to = new Vector2(start.x + (exit * dx), start.y + (exit * dy));
            return true;

            static bool Trim(float edge, float distance, ref float enter, ref float exit)
            {
                if (Mathf.Approximately(edge, 0f))
                {
                    // Parallel to this edge: inside if it starts inside, and no trimming to do
                    return distance >= 0f;
                }

                float crossing = distance / edge;

                if (edge < 0f)
                {
                    if (crossing > exit) return false;
                    if (crossing > enter) enter = crossing;
                }
                else
                {
                    if (crossing < enter) return false;
                    if (crossing < exit) exit = crossing;
                }

                return true;
            }
        }

        private static void Plot(Color32[] pixels, int x, int y, Color32 colour)
        {
            if (x >= 0 && x < Size && y >= 0 && y < Size)
            {
                pixels[(y * Size) + x] = colour;
            }
        }
    }
}
