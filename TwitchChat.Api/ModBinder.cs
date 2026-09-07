using System;
using System.Collections.Generic;
using System.Reflection;
using UnityModManagerNet;

namespace TwitchChat.Api
{
    /// <summary>
    /// Reads another mod's public runtime API by reflection, with everything cached and every failure
    /// turned into a null rather than an exception.
    /// </summary>
    /// <remarks>
    /// Reflection is the right tool here even when the other mod is open source: referencing its assembly
    /// directly would make TwitchChat fail to load whenever that mod is absent, and it would tie the two
    /// mods' versions together. Binding by name instead means a panel degrades to "that mod is not
    /// installed", or to a missing row, rather than taking the whole mod down with it.
    /// <para>
    /// A binder holds no game state, so it is safe to create one in a plugin's constructor and bind
    /// lazily. Nothing here touches the world.
    /// </para>
    /// </remarks>
    public sealed class ModBinder
    {
        private readonly Dictionary<string, Type?> types = new();
        private readonly Dictionary<string, MemberInfo?> members = new();

        private UnityModManager.ModEntry? modEntry;
        private Assembly? assembly;
        private bool failed;

        /// <param name="modId">The other mod's UMM id, as it appears in its info.json.</param>
        public ModBinder(string modId)
        {
            ModId = modId;
        }

        /// <summary>The UMM id this binder is looking for.</summary>
        public string ModId { get; }

        /// <summary>Why binding failed, in a few words, or null while nothing has gone wrong.</summary>
        public string? Error { get; private set; }

        /// <summary>
        /// Whether the mod is installed and switched on. Cheap once resolved, and it keeps looking until
        /// it finds the mod, so it can be called before the other mod has finished loading.
        /// </summary>
        public bool IsModPresent
        {
            get
            {
                if (failed)
                {
                    return false;
                }

                modEntry ??= UnityModManager.FindMod(ModId);
                return modEntry is { Active: true };
            }
        }

        /// <summary>The other mod's assembly once it has loaded, or null.</summary>
        public Assembly? Assembly
        {
            get
            {
                if (assembly != null || !IsModPresent)
                {
                    return assembly;
                }

                try
                {
                    assembly = modEntry?.Assembly;
                }
                catch (Exception ex)
                {
                    Fail($"could not reach the assembly of '{ModId}': {ex.Message}");
                }

                return assembly;
            }
        }

        /// <summary>
        /// Stops this binder for good, so a mod whose shape is not what was expected is reported once
        /// rather than throwing on every frame.
        /// </summary>
        public void Fail(string reason)
        {
            if (failed)
            {
                return;
            }

            failed = true;
            Error = reason;
        }

        /// <summary>Looks up a type by its full name, or null if it is not there.</summary>
        public Type? Type(string fullName)
        {
            if (types.TryGetValue(fullName, out Type? cached))
            {
                return cached;
            }

            Type? found = null;
            try
            {
                found = Assembly?.GetType(fullName, throwOnError: false);
            }
            catch (Exception ex)
            {
                Fail($"looking up type '{fullName}' in '{ModId}' threw: {ex.Message}");
            }

            // Only remember the answer once the assembly is actually there, so a lookup made too early
            // does not cache a null for the rest of the session
            if (Assembly != null)
            {
                types[fullName] = found;
            }

            return found;
        }

        /// <summary>Looks up a public static or instance method, matching on name alone.</summary>
        public MethodInfo? Method(Type? owner, string name)
        {
            return Member(owner, name, "m", t => t.GetMethod(name, PublicAny)) as MethodInfo;
        }

        /// <summary>Looks up a public property.</summary>
        public PropertyInfo? Property(Type? owner, string name)
        {
            return Member(owner, name, "p", t => t.GetProperty(name, PublicAny)) as PropertyInfo;
        }

        /// <summary>Looks up a public field.</summary>
        public FieldInfo? Field(Type? owner, string name)
        {
            return Member(owner, name, "f", t => t.GetField(name, PublicAny)) as FieldInfo;
        }

        /// <summary>
        /// Reads a property or field by name off an object, whichever it turns out to be. Handy for
        /// walking a result struct whose shape may change between versions of the other mod: a member
        /// that is no longer there simply reads as null.
        /// </summary>
        public object? Read(object? target, string name)
        {
            if (target == null)
            {
                return null;
            }

            Type owner = target.GetType();

            try
            {
                PropertyInfo? property = Property(owner, name);
                if (property != null)
                {
                    return property.GetValue(target);
                }

                FieldInfo? field = Field(owner, name);
                return field?.GetValue(target);
            }
            catch (Exception ex)
            {
                Fail($"reading '{owner.Name}.{name}' from '{ModId}' threw: {ex.Message}");
                return null;
            }
        }

        /// <summary>Reads a public static property or field off a type, whichever it turns out to be.</summary>
        public object? ReadStatic(Type? owner, string name)
        {
            if (owner == null)
            {
                return null;
            }

            try
            {
                PropertyInfo? property = Property(owner, name);
                if (property != null)
                {
                    return property.GetValue(null);
                }

                FieldInfo? field = Field(owner, name);
                return field?.GetValue(null);
            }
            catch (Exception ex)
            {
                Fail($"reading static '{owner.Name}.{name}' from '{ModId}' threw: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Reads a value type off an object by name, falling back to the given default when the member is
        /// missing or holds something unexpected.
        /// </summary>
        public T ReadOr<T>(object? target, string name, T fallback)
        {
            object? value = Read(target, name);
            return value is T typed ? typed : fallback;
        }

        /// <summary>
        /// Reads a number off an object by name, whatever numeric type the other mod happens to use for
        /// it. Worth having: whether a figure is stored as a float, a double or an int is exactly the sort
        /// of detail that changes between versions and should not cost a row on a panel.
        /// </summary>
        public float ReadFloat(object? target, string name, float fallback = 0f)
        {
            return AsFloat(Read(target, name), fallback);
        }

        /// <summary>Reads a whole number off an object by name, rounding if it is stored as a fraction.</summary>
        public int ReadInt(object? target, string name, int fallback = 0)
        {
            object? value = Read(target, name);
            if (value == null)
            {
                return fallback;
            }

            try
            {
                return Convert.ToInt32(value);
            }
            catch (Exception)
            {
                return fallback;
            }
        }

        /// <summary>Reads a flag off an object by name.</summary>
        public bool ReadBool(object? target, string name, bool fallback = false)
        {
            return Read(target, name) is bool value ? value : fallback;
        }

        /// <summary>Reads text off an object by name, using the value's own ToString if it is not a string.</summary>
        public string ReadString(object? target, string name, string fallback = "")
        {
            object? value = Read(target, name);
            return value == null ? fallback : value as string ?? value.ToString();
        }

        private static float AsFloat(object? value, float fallback)
        {
            if (value == null)
            {
                return fallback;
            }

            try
            {
                return Convert.ToSingle(value);
            }
            catch (Exception)
            {
                return fallback;
            }
        }

        /// <summary>Calls a method, returning null rather than throwing if anything goes wrong.</summary>
        public object? Invoke(MethodInfo? method, object? target, params object?[] args)
        {
            if (method == null)
            {
                return null;
            }

            try
            {
                return method.Invoke(target, args);
            }
            catch (Exception ex)
            {
                // The interesting exception is the one the other mod threw, not the reflection wrapper
                Exception cause = ex is TargetInvocationException { InnerException: { } inner } ? inner : ex;
                Fail($"calling '{method.DeclaringType?.Name}.{method.Name}' in '{ModId}' threw: {cause.Message}");
                return null;
            }
        }

        /// <summary>Writes a public field or property, and reports whether it worked.</summary>
        public bool Write(object? target, string name, object? value)
        {
            Type? owner = target?.GetType();
            if (owner == null)
            {
                return false;
            }

            try
            {
                PropertyInfo? property = Property(owner, name);
                if (property is { CanWrite: true })
                {
                    property.SetValue(target, value);
                    return true;
                }

                FieldInfo? field = Field(owner, name);
                if (field != null)
                {
                    field.SetValue(target, value);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Fail($"writing '{owner.Name}.{name}' in '{ModId}' threw: {ex.Message}");
            }

            return false;
        }

        private const BindingFlags PublicAny =
            BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.FlattenHierarchy;

        private MemberInfo? Member(Type? owner, string name, string kind, Func<Type, MemberInfo?> lookup)
        {
            if (owner == null)
            {
                return null;
            }

            string key = $"{kind}:{owner.FullName}.{name}";
            if (members.TryGetValue(key, out MemberInfo? cached))
            {
                return cached;
            }

            MemberInfo? found = null;
            try
            {
                found = lookup(owner);
            }
            catch (Exception ex)
            {
                Fail($"looking up '{owner.Name}.{name}' in '{ModId}' threw: {ex.Message}");
            }

            members[key] = found;
            return found;
        }
    }
}
