using TwitchChat.Api;

namespace TwitchChat.Plugins.Bundled.AiTraffic
{
    /// <summary>
    /// Puts the AI Traffic mod's debug overlay on a display: what the AI trains are doing, and the
    /// switches for the mod's own route lines and nametags.
    /// </summary>
    public sealed class AiTrafficPlugin : ITwitchChatPlugin
    {
        private readonly AiTrafficBridge traffic = new();

        public string Id => "AI Traffic";

        public string Title => "AI Traffic";

        public bool IsAvailable => traffic.IsInstalled;

        public IPanelContent CreateContent(IPanelSurface surface) => new AiTrafficContent(surface, traffic);
    }
}
