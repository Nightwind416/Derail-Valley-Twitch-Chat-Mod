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

        /// <summary>Redraws the track and junctions for the current view.</summary>
        internal void DrawTerrain(IEnumerable<TrackLine> tracks, IEnumerable<JunctionPoint> junctions)
        {
            for (int i = 0; i < terrainPixels.Length; i++)
            {
                terrainPixels[i] = Background;
            }

            // Yards first, so a mainline running through one is drawn over it rather than under
            foreach (TrackLine track in tracks)
            {
                Color32 colour = track.IsSiding ? Siding : Mainline;

                for (int i = 1; i < track.Points.Length; i++)
                {
                    Line(terrainPixels, ToPixel(track.Points[i - 1]), ToPixel(track.Points[i]), colour);
                }
            }

            foreach (JunctionPoint junction in junctions)
            {
                Dot(terrainPixels, ToPixel(junction.Position), 1,
                    junction.SelectedBranch >= 0 ? JunctionSet : JunctionUnknown);
            }

            Terrain.SetPixels32(terrainPixels);
            Terrain.Apply(updateMipmaps: false);
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

            Markers.SetPixels32(markerPixels);
            Markers.Apply(updateMipmaps: false);
        }

        public void Dispose()
        {
            UnityEngine.Object.Destroy(Terrain);
            UnityEngine.Object.Destroy(Markers);
        }

        // ------------------------------------------------------------------

        /// <summary>
        /// Map coordinates to texture pixels. Longitude runs across and latitude up, which is the way
        /// round the mod's own map has them. A texture counts its rows up from the bottom, which for once
        /// is what is wanted: north ends up at the top with no flip.
        /// </summary>
        private Vector2Int ToPixel(Vector2 point)
        {
            float half = Span / 2f;
            float x = (point.y - (Centre.y - half)) / Span;
            float y = (point.x - (Centre.x - half)) / Span;

            return new Vector2Int(Mathf.RoundToInt(x * (Size - 1)), Mathf.RoundToInt(y * (Size - 1)));
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

        private static void Dot(Color32[] pixels, Vector2Int at, int radius, Color32 colour)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    if (dx * dx + dy * dy <= radius * radius)
                    {
                        Plot(pixels, at.x + dx, at.y + dy, colour);
                    }
                }
            }
        }

        /// <summary>Bresenham, so a line off the edge of the view costs a few comparisons and no memory.</summary>
        private static void Line(Color32[] pixels, Vector2Int from, Vector2Int to, Color32 colour)
        {
            // Both ends far off the same side means the whole segment is: skip it without walking it
            if ((from.x < 0 && to.x < 0) || (from.y < 0 && to.y < 0) ||
                (from.x >= Size && to.x >= Size) || (from.y >= Size && to.y >= Size))
            {
                return;
            }

            int dx = Mathf.Abs(to.x - from.x);
            int dy = -Mathf.Abs(to.y - from.y);
            int stepX = from.x < to.x ? 1 : -1;
            int stepY = from.y < to.y ? 1 : -1;
            int error = dx + dy;

            int x = from.x;
            int y = from.y;

            // A very long segment out of view could otherwise walk a great many steps for nothing
            int guard = (dx - dy) + 2;

            while (guard-- > 0)
            {
                Plot(pixels, x, y, colour);

                if (x == to.x && y == to.y)
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

        private static void Plot(Color32[] pixels, int x, int y, Color32 colour)
        {
            if (x >= 0 && x < Size && y >= 0 && y < Size)
            {
                pixels[(y * Size) + x] = colour;
            }
        }
    }
}
