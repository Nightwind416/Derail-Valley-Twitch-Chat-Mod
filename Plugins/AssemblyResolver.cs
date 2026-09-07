using System;
using System.IO;
using System.Reflection;

namespace TwitchChat.Plugins
{
    /// <summary>
    /// Teaches the runtime to look in the mod's own folder for the assemblies the mod ships beside its
    /// main one, which is where TwitchChat.Api and any plugin's dependencies live.
    /// </summary>
    /// <remarks>
    /// Nothing here may touch a type from TwitchChat.Api. The runtime resolves an assembly the first time
    /// it compiles a method that mentions one of its types, so a resolver that mentioned them itself
    /// would need to be resolved before it could run. Keeping this to plain framework types means
    /// <see cref="Install"/> can safely be the first thing the mod does.
    /// </remarks>
    internal static class AssemblyResolver
    {
        private static string? modFolder;

        /// <summary>
        /// Starts resolving assembly loads out of the given folder. Safe to call more than once.
        /// </summary>
        /// <param name="folder">The mod's own folder, from its UMM entry.</param>
        internal static void Install(string folder)
        {
            if (modFolder != null)
            {
                return;
            }

            modFolder = folder;
            AppDomain.CurrentDomain.AssemblyResolve += Resolve;
        }

        private static Assembly? Resolve(object sender, ResolveEventArgs args)
        {
            if (modFolder == null)
            {
                return null;
            }

            try
            {
                string simpleName = new AssemblyName(args.Name).Name;

                // Anything already loaded is the one to hand back, so a plugin and the mod share a single
                // copy of the API rather than each getting their own and failing to see the same types
                foreach (Assembly loaded in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (loaded.GetName().Name == simpleName)
                    {
                        return loaded;
                    }
                }

                foreach (string folder in new[] { modFolder, Path.Combine(modFolder, "Plugins") })
                {
                    string candidate = Path.Combine(folder, simpleName + ".dll");
                    if (File.Exists(candidate))
                    {
                        return Assembly.LoadFrom(candidate);
                    }
                }
            }
            catch
            {
                // A resolver that throws breaks every load that follows it, including ones it has no
                // business in, so anything going wrong here means "not mine"
            }

            return null;
        }
    }
}
