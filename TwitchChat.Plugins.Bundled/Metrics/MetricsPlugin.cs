using TwitchChat.Api;

namespace TwitchChat.Plugins.Bundled.Metrics
{
    /// <summary>
    /// Puts the consist metrics from "Unrestored Museum Loco Tracker And Loco Metrics" on a display,
    /// where a VR driver can actually read them. That mod draws them in a screen corner with legacy
    /// IMGUI, which a headset never sees.
    /// </summary>
    public sealed class MetricsPlugin : ITwitchChatPlugin
    {
        private readonly MuseumMetrics metrics = new();

        public string Id => "Metrics";

        public string Title => "Metrics";

        public bool IsAvailable => metrics.IsInstalled;

        public IPanelContent CreateContent(IPanelSurface surface) => new MetricsContent(surface, metrics);
    }
}
