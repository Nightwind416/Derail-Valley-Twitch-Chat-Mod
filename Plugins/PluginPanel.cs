using System;
using TwitchChat.Api;
using UnityEngine;

namespace TwitchChat.Plugins
{
    /// <summary>
    /// A display panel whose content comes from a plugin. It is an ordinary panel as far as the rest of
    /// the mod is concerned - it has the same title row, back and close buttons, and scrolling area - and
    /// it doubles as the surface the plugin draws on.
    /// </summary>
    /// <remarks>
    /// Everything the plugin does is wrapped: a plugin that throws loses its own panel and says so on it,
    /// rather than taking down the display it is on or the frame it threw in.
    /// </remarks>
    internal sealed class PluginPanel : PanelConstructor.BasePanel, IPanelSurface
    {
        private readonly ITwitchChatPlugin plugin;
        private IPanelContent? content;
        private Vector2 lastSize;

        public PluginPanel(Transform parent, ITwitchChatPlugin plugin) : base(parent, plugin.Title)
        {
            this.plugin = plugin;

            try
            {
                content = plugin.CreateContent(this);
            }
            catch (Exception ex)
            {
                Fault("could not build its content", ex);
            }
        }

        // ------------------------------------------------------------------
        // Panel behaviour, forwarded to the plugin
        // ------------------------------------------------------------------

        protected override void OnShown()
        {
            Guard("showing", () => content?.OnShow());

            // A display can be resized while showing something else, so tell the plugin where it stands
            // the moment it comes back rather than waiting for the next drag
            NotifyResizeIfChanged();
        }

        protected override void OnHidden() => Guard("hiding", () => content?.OnHide());

        public override void Tick(float deltaTime)
        {
            NotifyResizeIfChanged();
            Guard("updating", () => content?.Tick(deltaTime));
        }

        public override void OnResize(Vector2 size)
        {
            lastSize = size;
            Guard("resizing", () => content?.OnResize(size));
        }

        // ------------------------------------------------------------------
        // IPanelSurface: what the plugin is given to draw on
        // ------------------------------------------------------------------

        public Transform Root => panelObject.transform;

        public Transform Content => contentRectTransform;

        public Vector2 Size => rectTransform != null ? rectTransform.rect.size : MenuManager.DefaultPanelSize;

        public IWidgetFactory Widgets => WidgetFactory.Instance;

        public void Log(string message) => Main.LogEntry($"Plugin:{plugin.Id}", message);

        // ------------------------------------------------------------------

        private void NotifyResizeIfChanged()
        {
            Vector2 size = Size;
            if (size != lastSize && size.x > 0f && size.y > 0f)
            {
                OnResize(size);
            }
        }

        private void Guard(string what, Action action)
        {
            if (content == null)
            {
                return;
            }

            try
            {
                action();
            }
            catch (Exception ex)
            {
                Fault($"threw while {what}", ex);
            }
        }

        /// <summary>
        /// Stops driving the plugin and says so on the panel. Done once: a plugin that throws every frame
        /// would otherwise fill the log and cost a frame's worth of exceptions each time.
        /// </summary>
        private void Fault(string what, Exception ex)
        {
            content = null;
            Main.LogEntry("PluginPanel", $"Panel '{plugin.Id}' {what} and has been stopped: {ex}");

            try
            {
                PanelConstructor.Label.Create(panelObject.transform, $"{plugin.Title} stopped", 10, 40, Color.red);
                PanelConstructor.DisplayText.Create(panelObject.transform, ex.Message, 10, 60, Color.gray, lines: 4);
            }
            catch (Exception labelFailure)
            {
                Main.LogEntry("PluginPanel", $"Could not even report the failure of '{plugin.Id}': {labelFailure.Message}");
            }
        }
    }
}
