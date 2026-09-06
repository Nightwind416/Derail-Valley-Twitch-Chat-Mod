using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TwitchChat.Api;

namespace TwitchChat.Plugins
{
    /// <summary>
    /// Finds the panels contributed from outside the mod and puts them in the registry alongside the
    /// mod's own.
    /// </summary>
    /// <remarks>
    /// Plugins arrive two ways. A mod that references TwitchChat.Api and calls
    /// <see cref="TwitchChatPlugins.Register"/> is picked up whether it got there first or arrives later,
    /// so neither mod needs to care about load order. An assembly dropped in the mod's Plugins folder is
    /// found here instead, which is how the panels shipped with the mod get in and how a plugin can exist
    /// for a mod that knows nothing about TwitchChat.
    /// </remarks>
    internal static class PluginLoader
    {
        private const string PluginFolderName = "Plugins";

        private static bool loaded;

        /// <summary>
        /// Registers the mod's own panels, then everything the plugins offer. Called once during Load.
        /// </summary>
        /// <param name="modPath">The mod's own folder, from its UMM entry.</param>
        internal static void LoadAll(string modPath)
        {
            if (loaded)
            {
                return;
            }

            loaded = true;

            PanelRegistry.RegisterBuiltIns();

            // Anything that registered before the mod got this far, plus anything that turns up after
            foreach (ITwitchChatPlugin plugin in TwitchChatPlugins.Registered.ToList())
            {
                Adopt(plugin);
            }

            TwitchChatPlugins.PluginRegistered += Adopt;

            LoadPluginAssemblies(Path.Combine(modPath, PluginFolderName));
        }

        /// <summary>
        /// Turns a plugin into a panel the rest of the mod can treat like any other.
        /// </summary>
        private static void Adopt(ITwitchChatPlugin plugin)
        {
            string id;
            string title;

            try
            {
                id = plugin.Id;
                title = plugin.Title;
            }
            catch (Exception ex)
            {
                Main.LogEntry("PluginLoader", $"Ignoring plugin {plugin.GetType().FullName}: it threw when asked to name itself: {ex.Message}");
                return;
            }

            bool registered = PanelRegistry.Register(new PanelDescriptor(
                id,
                title,
                isPlugin: true,
                create: (parent, _) => new PluginPanel(parent, plugin),
                available: () => plugin.IsAvailable));

            if (registered)
            {
                Main.LogEntry("PluginLoader", $"Registered plugin panel '{id}' from {plugin.GetType().Assembly.GetName().Name}.");
            }
        }

        private static void LoadPluginAssemblies(string folder)
        {
            if (!Directory.Exists(folder))
            {
                Main.LogEntry("PluginLoader", $"No plugins folder at {folder}; only built-in panels are available.");
                return;
            }

            foreach (string path in Directory.GetFiles(folder, "*.dll"))
            {
                // The API assembly lives beside the mod, not in here, but a tidy-minded player may well
                // have copied it in; loading it as a plugin would find nothing and log a puzzling failure
                if (Path.GetFileNameWithoutExtension(path) == "TwitchChat.Api")
                {
                    continue;
                }

                LoadPluginAssembly(path);
            }
        }

        private static void LoadPluginAssembly(string path)
        {
            string name = Path.GetFileName(path);

            try
            {
                Assembly assembly = Assembly.LoadFrom(path);

                if (!ApiVersionMatches(assembly, out string mismatch))
                {
                    Main.LogEntry("PluginLoader", $"Skipped plugin {name}: {mismatch}");
                    return;
                }

                int found = 0;
                foreach (Type type in PluginTypesIn(assembly))
                {
                    if (Instantiate(type) is { } plugin)
                    {
                        TwitchChatPlugins.Register(plugin);
                        found++;
                    }
                }

                if (found == 0)
                {
                    Main.LogEntry("PluginLoader", $"Loaded {name} but it contained no usable plugin.");
                }
            }
            catch (Exception ex)
            {
                Main.LogEntry("PluginLoader", $"Failed to load plugin {name}: {ex}");
            }
        }

        /// <summary>
        /// Checks the plugin was built against a compatible API. A plugin that predates a breaking change
        /// is refused here, with a line saying so, rather than throwing something unreadable later on.
        /// </summary>
        private static bool ApiVersionMatches(Assembly assembly, out string reason)
        {
            Version ours = typeof(TwitchChatPlugins).Assembly.GetName().Version;
            AssemblyName? reference = assembly.GetReferencedAssemblies()
                .FirstOrDefault(a => a.Name == "TwitchChat.Api");

            if (reference == null)
            {
                reason = "it does not reference TwitchChat.Api, so it is not a plugin.";
                return false;
            }

            if (reference.Version.Major != ours.Major)
            {
                reason = $"it was built against TwitchChat.Api {reference.Version} and this is {ours}. It needs rebuilding.";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        private static IEnumerable<Type> PluginTypesIn(Assembly assembly)
        {
            Type[] types;

            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                // A plugin referencing something absent still yields the types that did load, which is
                // usually enough: a panel for a mod that is not installed is exactly the case to survive
                types = ex.Types.Where(t => t != null).ToArray()!;
                Main.LogEntry("PluginLoader", $"Some types in {assembly.GetName().Name} could not be loaded: {ex.LoaderExceptions.FirstOrDefault()?.Message}");
            }

            return types.Where(t =>
                t != null &&
                t.IsClass &&
                !t.IsAbstract &&
                typeof(ITwitchChatPlugin).IsAssignableFrom(t) &&
                t.GetConstructor(Type.EmptyTypes) != null);
        }

        private static ITwitchChatPlugin? Instantiate(Type type)
        {
            try
            {
                return (ITwitchChatPlugin?)Activator.CreateInstance(type);
            }
            catch (Exception ex)
            {
                Main.LogEntry("PluginLoader", $"Could not create plugin {type.FullName}: {ex.Message}");
                return null;
            }
        }
    }
}
