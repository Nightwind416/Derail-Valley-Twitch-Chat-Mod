using TwitchChat.Api;

namespace TwitchChat.Plugins.Bundled.Dispatch
{
    /// <summary>
    /// Puts a live map of the railway on a display, drawn from the Remote Dispatch mod's own data.
    /// </summary>
    public sealed class DispatchMapPlugin : ITwitchChatPlugin
    {
        private readonly DispatchBridge dispatch = new();

        public string Id => "Dispatch Map";

        public string Title => "Dispatch Map";

        public bool IsAvailable => dispatch.IsInstalled;

        public IPanelContent CreateContent(IPanelSurface surface) => new DispatchMapContent(surface, dispatch);
    }
}
