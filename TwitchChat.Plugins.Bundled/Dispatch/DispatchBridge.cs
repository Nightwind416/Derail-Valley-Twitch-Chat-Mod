using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
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
    /// The methods used here return what its own endpoints return, so the shapes below are the shapes its
    /// own map already consumes: a public contract rather than an internal detail. Coordinates are the
    /// mod's own flat latitude and longitude, metres divided by the earth's circumference, which is only
    /// a projection convention and needs no undoing here.
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

        /// <summary>The track layout request, once made. See <see cref="TryReadTracks"/>.</summary>
        private Task<string>? trackRequest;

        private string diagnosis = "not looked up yet";

        internal bool IsInstalled => binder.IsModPresent;

        internal string? Error => binder.Error;

        /// <summary>
        /// What was and was not found when binding, for the log. Reading another mod by name is the part
        /// of this most likely to break when that mod changes, and "the map stopped" on its own tells
        /// nobody which of five lookups was the one that went missing.
        /// </summary>
        internal string Diagnosis
        {
            get
            {
                Bind();
                return diagnosis;
            }
        }

        /// <summary>
        /// The whole track layout, which is large and does not change during a session.
        /// </summary>
        /// <remarks>
        /// This one is asynchronous on the other side: it hands the work to that mod's own main-thread
        /// pump, because its usual caller is an HTTP worker thread. We are already on the main thread, so
        /// waiting on the result here would stop the very loop that has to run for it to be produced, and
        /// hang the game. Hence a request left in flight and picked up on a later frame.
        /// </remarks>
        /// <returns>Null while the answer is still coming, otherwise the tracks - empty if it failed.</returns>
        internal List<TrackLine>? TryReadTracks()
        {
            Bind();

            if (trackJson == null)
            {
                return new List<TrackLine>();
            }

            if (trackRequest == null)
            {
                object? result = binder.Invoke(trackJson, null, Arguments(trackJson));

                switch (result)
                {
                    case null:
                        return new List<TrackLine>();

                    // Older builds answered directly; take that at once rather than making a frame of it
                    case string immediate:
                        return ParseTracks(immediate);

                    case Task<string> pending:
                        trackRequest = pending;
                        break;

                    default:
                        binder.Fail($"RailTracks.GetTrackPointJSON returned {result.GetType().Name}, which this panel does not know how to read.");
                        return new List<TrackLine>();
                }
            }

            if (!trackRequest.IsCompleted)
            {
                return null;
            }

            Task<string> finished = trackRequest;
            trackRequest = null;

            if (finished.IsFaulted)
            {
                binder.Fail($"reading the track layout threw: {finished.Exception?.GetBaseException().Message}");
                return new List<TrackLine>();
            }

            return ParseTracks(finished.Result);
        }

        private List<TrackLine> ParseTracks(string json)
        {
            List<TrackLine> tracks = new();

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

            if (ReadJson(junctionJson) is not JArray entries)
            {
                return positions;
            }

            // [ { "position": [lat, lon], "branches": [trackId, trackId] }, ... ]
            foreach (JToken entry in entries)
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

            // [ selectedBranch, ... ], one per junction, in the same order as the positions
            return ReadJson(junctionStateJson) is not JArray states
                ? Array.Empty<int>()
                : states.Select(state => state.Type == JTokenType.Integer ? (int)state : -1).ToArray();
        }

        /// <summary>Every car the mod is willing to report, plus every player.</summary>
        internal List<MapMarker> Markers()
        {
            Bind();
            List<MapMarker> markers = new();

            if (ReadJson(carJson) is JObject cars)
            {
                // { "carId": { "position": [lat, lon], "rotation": deg, ... }, ... }
                foreach (KeyValuePair<string, JToken?> entry in cars)
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

            if (ReadJson(playerJson) is JObject players)
            {
                // { "playerId": { "color": name, "position": [lat, lon], "rotation": deg }, ... }
                foreach (KeyValuePair<string, JToken?> entry in players)
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
        /// Calls one of the mod's synchronous data methods and returns what it produced as JSON.
        /// </summary>
        /// <remarks>
        /// Some of these hand back a parsed document and some the text of one, which is the sort of
        /// difference that should cost a line here rather than a panel.
        /// </remarks>
        private JToken? ReadJson(MethodInfo? method)
        {
            object? result = binder.Invoke(method, null, Arguments(method));

            try
            {
                return result switch
                {
                    null => null,
                    JToken token => token,
                    string text => JToken.Parse(text),
                    _ => null
                };
            }
            catch (Exception ex)
            {
                binder.Fail($"the answer from '{method?.Name}' could not be read as JSON: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Fills in a method's parameters, so a version that takes, say, a resolution still answers
        /// rather than failing on the argument count.
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

            List<string> found = new();
            List<string> missing = new();

            trackJson = Find("DvMod.RemoteDispatch.RailTracks", "GetTrackPointJSON");
            junctionJson = Find("DvMod.RemoteDispatch.Junctions", "GetJunctionPointJSON");
            junctionStateJson = Find("DvMod.RemoteDispatch.Junctions", "GetJunctionStateJSON");
            carJson = Find("DvMod.RemoteDispatch.CarData", "GetAllCarDataJson");
            playerJson = Find("DvMod.RemoteDispatch.PlayerData", "GetPlayerDataJson");

            diagnosis = $"found [{string.Join(", ", found.ToArray())}]"
                + (missing.Count > 0 ? $", missing [{string.Join(", ", missing.ToArray())}]" : ", nothing missing");

            if (trackJson == null && binder.Assembly != null)
            {
                binder.Fail($"the track layout could not be read from Remote Dispatch: {diagnosis}.");
            }

            MethodInfo? Find(string typeName, string method)
            {
                Type? owner = binder.Type(typeName);
                MethodInfo? info = binder.Method(owner, method);

                // Which half failed matters: a missing type means the mod was rearranged, a missing method
                // on a type that is there means only that one call was renamed
                (info != null ? found : missing).Add(owner == null ? $"{typeName} (no such type)" : $"{typeName}.{method}");
                return info;
            }
        }
    }
}
