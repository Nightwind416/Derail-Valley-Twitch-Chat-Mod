using System;
using System.Collections.Generic;
using TwitchChat.Api;
using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.Plugins.Bundled.Metrics
{
    /// <summary>
    /// The consist readout: how long the train is, what it weighs, how much of it is braked, and where
    /// the driver has the controls.
    /// </summary>
    internal sealed class MetricsContent : IPanelContent
    {
        /// <summary>
        /// How often the figures are re-read. The mod this reads from walks the whole consist on every
        /// call and does it twice a frame in its own overlay; four times a second is plenty for numbers
        /// a human is reading, and costs a fraction of that.
        /// </summary>
        private const float RefreshInterval = 0.25f;

        private const int FirstRowY = 35;
        private const int RowHeight = 18;
        private const int BarHeight = 18;
        private const int CaptionX = 10;
        private const int ValueX = 105;

        private readonly IPanelSurface surface;
        private readonly MuseumMetrics metrics;

        private readonly List<GameObject> built = new();
        private readonly List<Row> rows = new();
        private readonly List<Bar> bars = new();

        private Text? status;
        private int nextY;
        private float untilRefresh;
        private bool layoutDirty;

        internal MetricsContent(IPanelSurface surface, MuseumMetrics metrics)
        {
            this.surface = surface;
            this.metrics = metrics;
            Rebuild();
        }

        public void OnShow()
        {
            // The mod's per-figure settings can have been changed since this panel was last looked at,
            // and they decide which rows exist at all
            Rebuild();
            Refresh();
        }

        public void OnHide() { }

        public void OnResize(Vector2 size)
        {
            // Resizing fires every frame while an edge is being dragged, and the rows are laid out
            // against the width, so note it and rebuild on the next refresh instead of on every frame
            layoutDirty = true;
        }

        public void Tick(float deltaTime)
        {
            untilRefresh -= deltaTime;
            if (untilRefresh > 0f)
            {
                return;
            }

            untilRefresh = RefreshInterval;

            if (layoutDirty)
            {
                layoutDirty = false;
                Rebuild();
            }

            Refresh();
        }

        // ------------------------------------------------------------------
        // Building
        // ------------------------------------------------------------------

        private void Rebuild()
        {
            foreach (GameObject obj in built)
            {
                UnityEngine.Object.Destroy(obj);
            }

            built.Clear();
            rows.Clear();
            bars.Clear();
            status = null;
            nextY = FirstRowY;

            AddRow("Cars", "showCars", stats => metrics.Count(stats, "CarCount").ToString());
            AddRow("Axles", null, stats => metrics.Count(stats, "AxleCount").ToString());
            AddRow("Length", "showLength", stats => $"{metrics.Number(stats, "TotalLengthMeters"):F0} m");
            AddRow("Mass", "showMass", stats => $"{metrics.Number(stats, "TotalMassTons"):F1} t");
            AddRow("  tare / cargo", "showMass", stats =>
                $"{metrics.Number(stats, "TareMassTons"):F1} / {metrics.Number(stats, "CargoMassTons"):F1} t");
            AddRow("Braked mass", "showBrakedMass", stats =>
                $"{metrics.Number(stats, "BrakedMassTons"):F1} t  ({metrics.Number(stats, "BrakedPercentage"):F0}%)");
            AddRow("Handbrakes", "showHandbrake", stats =>
                $"{metrics.Count(stats, "HandbrakesSetCount")} / {metrics.Count(stats, "TotalHandbrakesCount")}");
            AddRow("Brake line", "showBrakeLine", stats =>
                metrics.Flag(stats, "IsBrakeLineComplete") ? "continuous" : "BROKEN");
            AddRow("Rear pipe", "showRearPressure", stats => $"{metrics.Number(stats, "RearBrakePipePressure"):F2} bar");
            AddRow("Locos running", "showActiveLocos", stats => metrics.Count(stats, "ActiveLocoCount").ToString());

            // Hazmat has no setting of its own in that mod, and a consist with none is the normal case,
            // so this row says nothing rather than saying zero
            AddRow("Hazmat cars", null, stats =>
            {
                int hazmat = metrics.Count(stats, "HazmatCarCount");
                return hazmat > 0 ? hazmat.ToString() : "none";
            });

            AddSeparator();

            AddBar("Throttle", "showThrottle", "ThrottleValue");
            AddBar("Train brake", "showTrainBrake", "TrainBrakeValue");
            AddBar("Loco brake", "showLocoBrake", "LocoBrakeValue");

            // Only worth a row on a locomotive that has one
            AddBar("Dynamic", "showTrainBrake", "DynamicBrakeValue", stats => metrics.Flag(stats, "HasDynamicBrake"));

            nextY += 6;
            status = surface.Widgets.CreateText(surface.Root, string.Empty, CaptionX, nextY, Color.gray, lines: 3);
            built.Add(status.gameObject);
        }

        private void AddRow(string caption, string? setting, Func<object, string> value)
        {
            if (setting != null && !metrics.Shows(setting))
            {
                return;
            }

            Text captionLabel = surface.Widgets.CreateLabel(surface.Root, caption, CaptionX, nextY, Color.gray);
            Text valueLabel = surface.Widgets.CreateText(surface.Root, "--", ValueX, nextY, Color.white);

            built.Add(captionLabel.gameObject);
            built.Add(valueLabel.gameObject);
            rows.Add(new Row(valueLabel, value));

            nextY += RowHeight;
        }

        private void AddSeparator()
        {
            nextY += 4;
            built.Add(surface.Widgets.CreateSeparator(surface.Root, nextY));
            nextY += 6;
        }

        private void AddBar(string caption, string? setting, string field, Func<object, bool>? applies = null)
        {
            if (setting != null && !metrics.Shows(setting))
            {
                return;
            }

            GameObject bar = surface.Widgets.CreateValueBar(surface.Root, caption, nextY, BarColour(field));
            built.Add(bar);
            bars.Add(new Bar(bar, field, applies));

            nextY += BarHeight;
        }

        /// <summary>Brakes read red, power reads blue, so a glance says which way the train is being asked to go.</summary>
        private static Color BarColour(string field)
        {
            return field == "ThrottleValue"
                ? new Color(0.2f, 0.7f, 0.3f, 0.9f)
                : new Color(0.85f, 0.35f, 0.25f, 0.9f);
        }

        // ------------------------------------------------------------------
        // Refreshing
        // ------------------------------------------------------------------

        private void Refresh()
        {
            object? stats = ReadStats(out string problem);

            foreach (Row row in rows)
            {
                row.Value.text = stats == null ? "--" : row.Format(stats);
            }

            foreach (Bar bar in bars)
            {
                bool applies = stats != null && (bar.Applies == null || bar.Applies(stats));
                bar.Object.SetActive(applies);

                if (applies && stats != null)
                {
                    surface.Widgets.SetValueBar(bar.Object, metrics.Number(stats, bar.Field));
                }
            }

            if (status != null)
            {
                string warnings = stats == null ? string.Empty : metrics.Warnings(stats);
                status.text = problem.Length > 0 ? problem : warnings;
                status.color = problem.Length > 0 ? Color.gray : new Color(1f, 0.75f, 0.2f);
            }
        }

        /// <summary>
        /// The figures for the train the player is on, and if there are none, why not in words the player
        /// can act on.
        /// </summary>
        private object? ReadStats(out string problem)
        {
            problem = string.Empty;

            if (!metrics.IsInstalled)
            {
                problem = "The Loco Metrics mod is not installed.";
                return null;
            }

            TrainCar? car = PlayerManager.Car;
            if (car == null)
            {
                problem = "Board a train to see its figures.";
                return null;
            }

            object? stats = metrics.StatsFor(car.trainset);
            if (stats == null)
            {
                problem = metrics.Error ?? "The Loco Metrics mod did not answer.";
            }

            return stats;
        }

        private sealed class Row
        {
            internal Row(Text value, Func<object, string> format)
            {
                Value = value;
                Format = format;
            }

            internal Text Value { get; }
            internal Func<object, string> Format { get; }
        }

        private sealed class Bar
        {
            internal Bar(GameObject obj, string field, Func<object, bool>? applies)
            {
                Object = obj;
                Field = field;
                Applies = applies;
            }

            internal GameObject Object { get; }
            internal string Field { get; }
            internal Func<object, bool>? Applies { get; }
        }
    }
}
