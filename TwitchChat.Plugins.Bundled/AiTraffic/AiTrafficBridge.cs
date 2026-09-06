using System;
using System.Collections;
using System.Collections.Generic;
using TwitchChat.Api;
using UnityEngine;

namespace TwitchChat.Plugins.Bundled.AiTraffic
{
    /// <summary>
    /// Reads Killermops27's AI Traffic mod: which AI trains are running, what each is doing, and the
    /// switches for its own world-space route lines and nametags.
    /// </summary>
    /// <remarks>
    /// That mod keeps its overlay as a pure view over <c>AIEngineer</c>, which exposes everything as
    /// public properties, and reaches them through a <c>TrafficManager</c> singleton. That is a better
    /// seam than most, and it is the only one available: the mod has no licence file at all, so nothing
    /// here is copied from it and nothing is referenced. Its public runtime API is read and that is all.
    /// </remarks>
    internal sealed class AiTrafficBridge
    {
        internal const string ModId = "AITraffic";

        private readonly ModBinder binder = new(ModId);

        private Type? managerType;
        private bool lookedUp;

        internal bool IsInstalled => binder.IsModPresent;

        internal string? Error => binder.Error;

        /// <summary>The mod's own settings object, whose flags the panel offers to flip.</summary>
        private object? Settings => binder.ReadStatic(Type("AITraffic.Main"), "Settings");

        /// <summary>The traffic manager, or null before a world is loaded and traffic has started.</summary>
        private object? Manager
        {
            get
            {
                Bind();
                return binder.ReadStatic(managerType, "Instance");
            }
        }

        /// <summary>Whether the mod is actually dispatching trains, as opposed to merely installed.</summary>
        internal bool IsRunning
        {
            get
            {
                Bind();
                return binder.ReadStatic(managerType, "IsRunning") is true;
            }
        }

        /// <summary>How the mod is set up: ambient traffic or worker-driven, and how much of it.</summary>
        internal string Mode => binder.ReadString(Settings, "Mode", "unknown");

        internal string Density => binder.ReadString(Settings, "Density", "unknown");

        internal int MaxTrains => binder.ReadInt(Settings, "MaxActiveTrains");

        internal int ActiveTrainCount => binder.ReadInt(Manager, "ActiveTrainCount");

        /// <summary>
        /// The AI drivers currently at the controls of something. Empty rather than null whenever the mod
        /// has nothing to say, so a caller need not tell "no trains" from "no mod".
        /// </summary>
        internal IEnumerable<object> Engineers()
        {
            if (binder.Read(Manager, "ActiveEngineers") is not IEnumerable list)
            {
                yield break;
            }

            foreach (object? engineer in list)
            {
                if (engineer != null)
                {
                    yield return engineer;
                }
            }
        }

        // ------------------------------------------------------------------
        // One AI driver
        // ------------------------------------------------------------------

        /// <summary>
        /// The locomotive an AI driver is aboard. Its driver is a component on the car, so this needs no
        /// reflection at all: TrainCar is the game's own type and both mods see the same one.
        /// </summary>
        internal TrainCar? CarOf(object engineer)
        {
            return engineer is Component component ? component.GetComponent<TrainCar>() : null;
        }

        internal float SpeedKmh(object engineer) => binder.ReadFloat(engineer, "CurrentSpeedKmh");

        internal float TargetSpeedKmh(object engineer) => binder.ReadFloat(engineer, "TargetSpeedKmh");

        internal string State(object engineer) => binder.ReadString(engineer, "State", "?");

        internal float Throttle(object engineer) => binder.ReadFloat(engineer, "CommandedThrottle");

        internal float TrainBrake(object engineer) => binder.ReadFloat(engineer, "CommandedTrainBrake");

        internal float DynamicBrake(object engineer) => binder.ReadFloat(engineer, "CommandedDynamicBrake");

        internal float DistanceToSignal(object engineer) => binder.ReadFloat(engineer, "DistanceToSignal", float.NaN);

        internal float DistanceToObstacle(object engineer) => binder.ReadFloat(engineer, "DistanceToObstacle", float.NaN);

        internal bool IsWorkerDriven(object engineer) => binder.ReadBool(engineer, "IsWorkerDriven");

        /// <summary>Where the mod says a train is, in its own words, or empty if it will not say.</summary>
        internal string LocationOf(TrainCar? car)
        {
            if (car == null)
            {
                return string.Empty;
            }

            Bind();
            return binder.Invoke(binder.Method(managerType, "GetTrainLocationDescription"), null, car) as string ?? string.Empty;
        }

        /// <summary>Where the mod says a train is going, in its own words.</summary>
        internal string DestinationOf(object engineer)
        {
            Bind();
            string described = binder.Invoke(binder.Method(managerType, "GetTrainDestinationDescription"), null, engineer) as string ?? string.Empty;

            // Older builds may not have the helper, in which case the station name is the next best thing
            return described.Length > 0 ? described : binder.ReadString(engineer, "DestinationStationName");
        }

        // ------------------------------------------------------------------
        // The mod's own displays
        // ------------------------------------------------------------------

        /// <summary>Whether one of the mod's own overlays is switched on.</summary>
        internal bool ShowsOverlay(string flag) => binder.ReadBool(Settings, flag);

        /// <summary>
        /// Switches one of the mod's own overlays on or off. Its route lines, signal tags and loco
        /// nametags are drawn in the world, so they are worth having in VR - and until now the only way
        /// to reach them was the Unity Mod Manager menu, which means taking the headset off.
        /// </summary>
        internal void ToggleOverlay(string flag)
        {
            object? settings = Settings;
            binder.Write(settings, flag, !binder.ReadBool(settings, flag));
        }

        // ------------------------------------------------------------------

        /// <summary>
        /// Finds the mod's types once. Namespaces are the sort of thing that gets rearranged between
        /// versions, so each is tried in a couple of plausible places before giving up.
        /// </summary>
        private void Bind()
        {
            if (lookedUp)
            {
                return;
            }

            lookedUp = true;
            managerType = Type("AITraffic.Core.TrafficManager", "AITraffic.TrafficManager");

            if (managerType == null && binder.Assembly != null)
            {
                binder.Fail("TrafficManager was not found; this version of the mod is not one this panel knows.");
            }
        }

        private Type? Type(params string[] candidates)
        {
            foreach (string candidate in candidates)
            {
                if (binder.Type(candidate) is { } found)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
