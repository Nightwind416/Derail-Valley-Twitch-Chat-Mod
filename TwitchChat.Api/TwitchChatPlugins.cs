using System;
using System.Collections.Generic;
using UnityEngine;

namespace TwitchChat.Api
{
    /// <summary>
    /// Where plugins announce themselves to TwitchChat.
    /// </summary>
    /// <remarks>
    /// There are two ways in and they end up in the same place. A mod that already exists can reference
    /// this assembly and call <see cref="Register"/> from its own Load, in which case load order does not
    /// matter: TwitchChat picks up whatever is here when it starts and is told about anything registered
    /// afterwards. Alternatively a plugin assembly dropped in the mod's Plugins folder is found and
    /// registered by TwitchChat itself, with no code in the other mod at all.
    /// </remarks>
    public static class TwitchChatPlugins
    {
        /// <summary>
        /// The version of this contract. TwitchChat refuses plugins built against a different major
        /// version, so a plugin compiled against an old API says so plainly instead of failing oddly.
        /// </summary>
        public const int ApiVersion = 1;

        private static readonly List<ITwitchChatPlugin> registered = new();

        /// <summary>Everything registered so far, in the order it arrived.</summary>
        public static IReadOnlyList<ITwitchChatPlugin> Registered => registered;

        /// <summary>Raised when a plugin registers, so TwitchChat hears about late arrivals.</summary>
        public static event Action<ITwitchChatPlugin>? PluginRegistered;

        /// <summary>
        /// Offers a plugin to TwitchChat. Safe to call before TwitchChat has loaded. A second plugin
        /// claiming an id that is already taken is refused, since the id is what the player's settings
        /// remember a display by.
        /// </summary>
        /// <returns>True if the plugin was accepted.</returns>
        public static bool Register(ITwitchChatPlugin plugin)
        {
            if (plugin == null)
            {
                return false;
            }

            if (string.IsNullOrEmpty(plugin.Id))
            {
                Debug.LogWarning($"[TwitchChat.Api] Refused a plugin of type {plugin.GetType().FullName} because its Id is empty.");
                return false;
            }

            foreach (ITwitchChatPlugin existing in registered)
            {
                if (existing.Id == plugin.Id)
                {
                    Debug.LogWarning($"[TwitchChat.Api] Refused plugin '{plugin.Id}' from {plugin.GetType().FullName}: that id is already registered by {existing.GetType().FullName}.");
                    return false;
                }
            }

            registered.Add(plugin);
            PluginRegistered?.Invoke(plugin);
            return true;
        }
    }
}
