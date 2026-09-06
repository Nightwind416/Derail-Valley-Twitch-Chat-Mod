using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json.Linq;
using TwitchChat.Api;
using UnityEngine;

namespace TwitchChat.Plugins.Bundled.Dispatch
{
    /// <summary>One length of track, as a run of points in the mod's map coordinates.</summary>
    internal sealed class TrackLine
    {
        internal TrackLine(string id, Vector2[] points)
        {
            Id = id;
            Points = points;

            // The mod's own map draws yard tracks differently from mainline, and tells them apart by
            // whether the id carries a '#'. Worth keeping: a yard drawn in the same weight as the main
            // line is an unreadable smudge at map scale
            IsSiding = !id.Contains("#");
        }

        internal string Id { get; }
        internal Vector2[] Points { get; }
        internal bool IsSiding { get; }
    }

    /// <summary>A junction, where it is and which way it is set.</summary>
    internal struct JunctionPoint
    {
        internal Vector2 Position;
        internal int SelectedBranch;
    }

    /// <summary>Something on the map that moves: a car, a locomotive or a player.</summary>
    internal struct MapMarker
    {
        internal Vector2 Position;
        internal float Rotation;
        internal bool IsPlayer;
        internal bool IsLoco;
    }

    /// <summary>
    /// Reads mspielberg's Remote Dispatch mod: the track layout, the junctions, and where everything is.
    /// </summary>
    /// <remarks>
    /// That mod renders nothing in the game at all. Its map is a Leaflet page served over HTTP to a
    /// browser, which is no use in a headset. What it does have is a data layer that is entirely public
    /// statics and quite separate from the HTTP layer, so the same figures its web page draws can be had
    /// in process - no port, no password, no dependency on the player having the server switched on.
    /// <para>
    /// The methods used here return the JSON its own endpoints return, so the shapes below are the shapes
    /// its own map already consumes: a public contract rather than an internal detail. Coordinates are
    /// the mod's own flat latitude and longitude, metres divided by the earth's circumference, which is
    /// only a projection convention and needs no undoing here.
    /// </para>
    /// </remarks>
    internal sealed class DispatchBridge
    {
        internal const string ModId = "RemoteDispatch";

        private readonly ModBinder binder = new(ModId);

        private MethodInfo? trackJson;
        private MethodInfo? junctionJson;
        private MethodInfo? junctionStateJson;
        private MethodInfo? carJson;
        private MethodInfo? playerJson;
        private bool lookedUp;

        internal bool IsInstalled => binder.IsModPresent;

        internal string? Error => binder.Error;

        /// <summary>
        /// The whole track layout. Large, and it does not change during a session, so read it once.
        /// </summary>
        internal List<TrackLine> Tracks()
        {
            Bind();
            List<TrackLine> tracks = new();

            if (Read(trackJson) is not { } json)
            {
                return tracks;
            }

            // { "trackId": [[lat, lon], ...], ... }
            foreach (KeyValuePair<string, JToken?> entry in JObject.Parse(json))
            {
                Vector2[] points = PointList(entry.Value);
                if (points.Length > 1)
                {
                    tracks.Add(new TrackLine(entry.Key, points));
                }
            }

            return tracks;
        }

        /// <summary>Junction positions, which are fixed for the session.</summary>
        internal List<Vector2> JunctionPositions()
        {
            Bind();
            List<Vector2> positions = new();

            if (Read(junctionJson) is not { } json)
            {
                return positions;
            }

            // [ { "position": [lat, lon], "branches": [trackId, trackId] }, ... ]
            foreach (JToken entry in JArray.Parse(json))
            {
                if (Point(entry["position"]) is { } position)
                {
                    positions.Add(position);
                }
            }

            return positions;
        }

        /// <summary>
        /// Which way each junction is set, in the same order as the positions. Changes as switches are
        /// thrown, so unlike the positions this is worth re-reading.
        /// </summary>
        internal int[] JunctionStates()
        {
            Bind();

            if (Read(junctionStateJson) is not { } json)
            {
                return Array.Empty<int>();
            }

            // [ selectedBranch, ... ], one per junction, in the same order as the positions
            return JArray.Parse(json).Select(state => state.Type == JTokenType.Integer ? (int)state : -1).ToArray();
        }

        /// <summary>Every car the mod is willing to report, plus every player.</summary>
        internal List<MapMarker> Markers()
        {
            Bind();
            List<MapMarker> markers = new();

            if (Read(carJson) is { } cars)
            {
                // { "carId": { "position": [lat, lon], "rotation": deg, ... }, ... }
                foreach (KeyValuePair<string, JToken?> entry in JObject.Parse(cars))
                {
                    if (Marker(entry.Value, isPlayer: false) is { } marker)
                    {
                        // Locomotive ids start with L-, and picking them out is what makes the map
                        // readable: a train is a lot of dots and only one of them is being driven
                        marker.IsLoco = entry.Key.StartsWith("L-", StringComparison.Ordinal);
                        markers.Add(marker);
                    }
                }
            }

            if (Read(playerJson) is { } players)
            {
                // { "playerId": { "color": name, "position": [lat, lon], "rotation": deg }, ... }
                foreach (KeyValuePair<string, JToken?> entry in JObject.Parse(players))
                {
                    if (Marker(entry.Value, isPlayer: true) is { } marker)
                    {
                        markers.Add(marker);
                    }
                }
            }

            return markers;
        }

        // ------------------------------------------------------------------

        private static MapMarker? Marker(JToken? token, bool isPlayer)
        {
            if (token == null || Point(token["position"]) is not { } position)
            {
                return null;
            }

            JToken? rotation = token["rotation"];

            return new MapMarker
            {
                Position = position,
                Rotation = rotation == null || rotation.Type == JTokenType.Null ? 0f : (float)rotation,
                IsPlayer = isPlayer
            };
        }

        private static Vector2? Point(JToken? token)
        {
            return token is JArray pair && pair.Count >= 2
                ? new Vector2((float)pair[0], (float)pair[1])
                : null;
        }

        private static Vector2[] PointList(JToken? token)
        {
            if (token is not JArray points)
            {
                return Array.Empty<Vector2>();
            }

            List<Vector2> result = new(points.Count);
            foreach (JToken point in points)
            {
                if (Point(point) is { } parsed)
                {
                    result.Add(parsed);
                }
            }

            return result.ToArray();
        }

        /// <summary>
        /// Calls one of the mod's JSON methods and returns what it produced, or null.
        /// </summary>
        /// <remarks>
        /// Some of these are iterators that yield the document in pieces rather than returning it whole,
        /// which is sensible when the other end is a socket and irrelevant here, so both shapes are
        /// accepted and joined.
        /// </remarks>
        private string? Read(MethodInfo? method)
        {
            object? result = binder.Invoke(method, null, Arguments(method));

            switch (result)
            {
                case null:
                    return null;
                case string json:
                    return json;
                case IEnumerable pieces:
                    StringBuilder joined = new();
                    foreach (object? piece in pieces)
                    {
                        joined.Append(piece);
                    }
                    return joined.ToString();
                default:
                    return result.ToString();
            }
        }

        /// <summary>
        /// Fills in a method's optional parameters, so a version that takes, say, a resolution still
        /// answers rather than failing on the argument count.
        /// </summary>
        private static object?[] Arguments(MethodInfo? method)
        {
            ParameterInfo[] parameters = method?.GetParameters() ?? Array.Empty<ParameterInfo>();
            return parameters.Length == 0
                ? Array.Empty<object?>()
                : parameters.Select(p => p.HasDefaultValue ? p.DefaultValue : DefaultOf(p.ParameterType)).ToArray();
        }

        private static object? DefaultOf(Type type) => type.IsValueType ? Activator.CreateInstance(type) : null;

        private void Bind()
        {
            if (lookedUp)
            {
                return;
            }

            lookedUp = true;

            trackJson = Find("DvMod.RemoteDispatch.RailTracks", "GetTrackPointJSON");
            junctionJson = Find("DvMod.RemoteDispatch.Junctions", "GetJunctionPointJSON");
            junctionStateJson = Find("DvMod.RemoteDispatch.Junctions", "GetJunctionStateJSON");
            carJson = Find("DvMod.RemoteDispatch.CarData", "GetAllCarDataJson");
            playerJson = Find("DvMod.RemoteDispatch.PlayerData", "GetPlayerDataJson");

            if (trackJson == null && binder.Assembly != null)
            {
                binder.Fail("RailTracks.GetTrackPointJSON was not found; this version of the mod is not one this panel knows.");
            }
        }

        private MethodInfo? Find(string typeName, string method) => binder.Method(binder.Type(typeName), method);
    }
}
