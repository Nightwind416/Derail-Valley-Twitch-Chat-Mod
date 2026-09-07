using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using TwitchChat.Api;

namespace TwitchChat.Plugins.Bundled.Metrics
{
    /// <summary>
    /// Reads the consist figures out of DSH's "Unrestored Museum Loco Tracker And Loco Metrics".
    /// </summary>
    /// <remarks>
    /// That mod does the arithmetic in <c>TrainCalculator.CalculateSetStats</c>, a public static that
    /// takes a trainset and returns a struct of figures, with no side effects and nothing cached. That
    /// makes it callable from here, which is much better than working the numbers out again: the panel
    /// and the mod's own overlay then agree by construction rather than by our getting the same brake
    /// arithmetic right independently.
    /// <para>
    /// Reflection rather than a reference, and no code copied: the mod's licence is contradictory, its
    /// LICENSE file being GPL-3.0 while its README claims MIT.
    /// </para>
    /// </remarks>
    internal sealed class MuseumMetrics
    {
        internal const string ModId = "UnrestoredMuseumLocoTracker";

        private readonly ModBinder binder = new(ModId);

        private MethodInfo? calculate;
        private bool lookedUp;

        /// <summary>Whether the mod is installed and switched on.</summary>
        internal bool IsInstalled => binder.IsModPresent;

        /// <summary>What went wrong, in a few words, or null while nothing has.</summary>
        internal string? Error => binder.Error;

        /// <summary>
        /// The figures for a trainset, as a boxed struct to be read with <see cref="Number"/> and friends,
        /// or null if the mod is not there or would not answer.
        /// </summary>
        internal object? StatsFor(object? trainset)
        {
            if (trainset == null)
            {
                return null;
            }

            if (!lookedUp)
            {
                lookedUp = true;
                calculate = binder.Method(binder.Type("DerailValleyMuseumMap.TrainCalculator"), "CalculateSetStats");

                if (calculate == null && binder.Assembly != null)
                {
                    binder.Fail("TrainCalculator.CalculateSetStats was not found; this version of the mod is not one this panel knows.");
                }
            }

            return binder.Invoke(calculate, null, trainset);
        }

        internal float Number(object? stats, string field) => binder.ReadFloat(stats, field);

        internal int Count(object? stats, string field) => binder.ReadInt(stats, field);

        internal bool Flag(object? stats, string field) => binder.ReadBool(stats, field);

        /// <summary>
        /// The mod's own per-figure visibility settings, so the panel shows what the player asked that
        /// mod to show. Anything unrecognised is treated as shown, which is the friendlier way to be
        /// wrong: a new figure appears rather than silently going missing.
        /// </summary>
        internal bool Shows(string setting)
        {
            object? settings = binder.ReadStatic(binder.Type("DerailValleyMuseumMap.Main"), "Settings");
            return settings == null || binder.ReadBool(settings, setting, fallback: true);
        }

        /// <summary>
        /// The low-resource warnings, flattened to one line. The mod may hold these as a string or as a
        /// collection of them, and which it is has no business mattering here.
        /// </summary>
        internal string Warnings(object? stats)
        {
            object? value = binder.Read(stats, "LowResourceWarnings");

            return value switch
            {
                null => string.Empty,
                string text => text,
                IEnumerable items => string.Join(", ", items
                    .Cast<object?>()
                    .Select(item => item?.ToString() ?? string.Empty)
                    .Where(text => text.Length > 0)
                    .ToArray()),
                _ => value.ToString() ?? string.Empty
            };
        }
    }
}
