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

    /// <summary>The parts of the map that do not change during a session.</summary>
    internal sealed class DispatchLayout
    {
        internal List<TrackLine> Tracks = new();
        internal List<Vector2> Junctions = new();
    }

    /// <summary>The parts that do.</summary>
    internal sealed class DispatchLive
    {
        internal List<MapMarker> Markers = new();
        internal int[] JunctionStates = Array.Empty<int>();
    }

    /// <summary>
    /// Reads mspielberg's Remote Dispatch mod: the track layout, the junctions, and where everything is.
    /// </summary>
    /// <remarks>
    /// That mod renders nothing in the game at all. Its map is a Leaflet page served over HTTP to a
    /// browser, which is no use in a headset. It does have a data layer of public statics, quite separate
    /// from the HTTP layer, which is what this reads.
    /// <para>
    /// <b>Every one of those calls is made from a background thread, and none from the game loop.</b>
    /// They look synchronous and are not: they hand their work to that mod's own main-thread pump and
    /// wait for the answer, because the callers they were written for are HTTP worker threads. Called
    /// from the main thread, such a method waits for the main thread to do something the main thread
    /// cannot do while waiting, and the game stops dead with no exception and nothing in any log. That is
    /// not a fault in either mod; it is what those methods are for, used wrongly.
    /// </para>
    /// <para>
    /// So requests are started on a worker and collected a frame or two later. The shapes below are the
    /// ones its own map already consumes, and coordinates are its own flat latitude and longitude, metres
    /// divided by the earth's circumference, which is only a projection convention.
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

        private BackgroundRead<DispatchLayout>? layoutRead;
        private BackgroundRead<DispatchLive>? liveRead;

        private string diagnosis = "not looked up yet";

        internal bool IsInstalled => binder.IsModPresent;

        /// <summary>Whatever last went wrong, in a sentence, or null.</summary>
        internal string? Error { get; private set; }

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
        /// The track and junction layout. Asks for it the first time, and returns null on every call
        /// until the answer arrives.
        /// </summary>
        internal DispatchLayout? PollLayout()
        {
            Bind();

            if (trackJson == null)
            {
                return new DispatchLayout();
            }

            return Poll(ref layoutRead, ReadLayout);
        }

        /// <summary>
        /// Where everything is now. Asks again each time the previous answer has been collected, so there
        /// is never more than one request in flight.
        /// </summary>
        internal DispatchLive? PollLive()
        {
            Bind();
            return Poll(ref liveRead, ReadLive);
        }

        private T? Poll<T>(ref BackgroundRead<T>? slot, Func<T> work) where T : class
        {
            slot ??= new BackgroundRead<T>(work);

            if (!slot.Done)
            {
                return null;
            }

            BackgroundRead<T> finished = slot;
            slot = null;

            if (finished.Failure is { } failure)
            {
                Error = failure;
                return null;
            }

            return finished.Result;
        }

        // ------------------------------------------------------------------
        // Everything below this line runs on a worker thread
        // ------------------------------------------------------------------

        private DispatchLayout ReadLayout()
        {
            DispatchLayout layout = new();

            // This one is declared as returning a Task because that mod produces it on its own pump.
            // Waiting on it here is fine and is the point: this is a worker, and the main thread is free
            // to run the pump that completes it
            if (Text(binder.Invoke(trackJson, null, Arguments(trackJson))) is { } trackText)
            {
                // { "trackId": [[lat, lon], ...], ... }
                foreach (KeyValuePair<string, JToken?> entry in JObject.Parse(trackText))
                {
                    Vector2[] points = PointList(entry.Value);
                    if (points.Length > 1)
                    {
                        layout.Tracks.Add(new TrackLine(entry.Key, points));
                    }
                }
            }

            // [ { "position": [lat, lon], "branches": [trackId, trackId] }, ... ]
            if (Json(junctionJson) is JArray junctions)
            {
                foreach (JToken entry in junctions)
                {
                    if (Point(entry["position"]) is { } position)
                    {
                        layout.Junctions.Add(position);
                    }
                }
            }

            return layout;
        }

        private DispatchLive ReadLive()
        {
            DispatchLive live = new();

            if (Json(carJson) is JObject cars)
            {
                // { "carId": { "position": [lat, lon], "rotation": deg, ... }, ... }
                foreach (KeyValuePair<string, JToken?> entry in cars)
                {
                    if (Marker(entry.Value, isPlayer: false) is { } marker)
                    {
                        // Locomotive ids start with L-, and picking them out is what makes the map
                        // readable: a train is a lot of dots and only one of them is being driven
                        marker.IsLoco = entry.Key.StartsWith("L-", StringComparison.Ordinal);
                        live.Markers.Add(marker);
                    }
                }
            }

            if (Json(playerJson) is JObject players)
            {
                // { "playerId": { "color": name, "position": [lat, lon], "rotation": deg }, ... }
                foreach (KeyValuePair<string, JToken?> entry in players)
                {
                    if (Marker(entry.Value, isPlayer: true) is { } marker)
                    {
                        live.Markers.Add(marker);
                    }
                }
            }

            // [ selectedBranch, ... ], one per junction, in the same order as the positions
            if (Json(junctionStateJson) is JArray states)
            {
                live.JunctionStates = states
                    .Select(state => state.Type == JTokenType.Integer ? (int)state : -1)
                    .ToArray();
            }

            return live;
        }

        /// <summary>Calls one of the mod's data methods and reads the answer as JSON.</summary>
        private JToken? Json(MethodInfo? method)
        {
            object? result = binder.Invoke(method, null, Arguments(method));

            return result switch
            {
                JToken token => token,
                _ => Text(result) is { } text ? JToken.Parse(text) : null
            };
        }

        /// <summary>
        /// The text of an answer, waiting for it if the method handed back a promise of one.
        /// </summary>
        private static string? Text(object? result)
        {
            return result switch
            {
                null => null,
                string text => text,
                Task<string> pending => pending.GetAwaiter().GetResult(),
                _ => result.ToString()
            };
        }

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
                Error = $"the track layout could not be read from Remote Dispatch: {diagnosis}.";
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

        /// <summary>
        /// One reading of another mod, in flight on a worker thread.
        /// </summary>
        private sealed class BackgroundRead<T> where T : class
        {
            private readonly Task<T> task;

            internal BackgroundRead(Func<T> work)
            {
                task = Task.Run(work);
            }

            internal bool Done => task.IsCompleted;

            internal T? Result => task.Status == TaskStatus.RanToCompletion ? task.Result : null;

            internal string? Failure => task.IsFaulted
                ? task.Exception?.GetBaseException().Message ?? "the read failed"
                : task.IsCanceled ? "the read was cancelled" : null;
        }
    }
}
