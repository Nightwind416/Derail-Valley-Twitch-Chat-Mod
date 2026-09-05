using System;
using System.Security.Cryptography;
using System.Text;

namespace TwitchChat
{
    /// <summary>
    /// Reads and writes the Twitch credentials held in Settings.xml.
    /// </summary>
    /// <remarks>
    /// Tokens are encrypted with Windows DPAPI, tied to the current user account, so another user
    /// on the same machine cannot read them out of the settings file. The game runs on Unity's Mono
    /// runtime, where DPAPI is not guaranteed to be implemented, so every stored value carries a
    /// prefix saying how it was written and the store falls back to plain base64 when encryption is
    /// unavailable. <see cref="IsEncrypted"/> reports which is in use so the UI can tell the user
    /// the truth rather than a marketing claim.
    /// </remarks>
    public static class TokenStore
    {
        private const string EncryptedPrefix = "dpapi:";
        private const string PlainPrefix = "b64:";

        /// <summary>
        /// Extra entropy mixed into the encryption. Not a secret, and not a substitute for one;
        /// it only keeps this mod's blobs from being interchangeable with another application's.
        /// </summary>
        private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("TwitchChat.DerailValley.TokenStore.v1");

        private static bool? encryptionAvailable;

        /// <summary>
        /// True when tokens on this machine are encrypted at rest, false when the runtime does not
        /// provide DPAPI and the store has fallen back to plain base64 encoding.
        /// </summary>
        public static bool IsEncrypted => encryptionAvailable ?? ProbeEncryption();

        /// <summary>
        /// Encrypts a token for storage in the settings file.
        /// </summary>
        /// <param name="token">The plain-text token. May be empty, which stores nothing.</param>
        /// <returns>The value to persist, or an empty string when <paramref name="token"/> is empty.</returns>
        public static string Protect(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return string.Empty;
            }

            if (ProbeEncryption())
            {
                try
                {
                    byte[] cipher = ProtectedData.Protect(
                        Encoding.UTF8.GetBytes(token), Entropy, DataProtectionScope.CurrentUser);
                    return EncryptedPrefix + Convert.ToBase64String(cipher);
                }
                catch (Exception ex)
                {
                    // Encryption probed as working but failed for this value. Fall through rather
                    // than lose the token, and stop claiming encryption from here on.
                    encryptionAvailable = false;
                    Main.LogEntry("TokenStore", $"Encrypting the token failed ({ex.GetType().Name}). Storing it encoded instead.");
                }
            }

            return PlainPrefix + Convert.ToBase64String(Encoding.UTF8.GetBytes(token));
        }

        /// <summary>
        /// Recovers a token previously written by <see cref="Protect"/>.
        /// </summary>
        /// <param name="stored">The value read from the settings file.</param>
        /// <returns>The plain-text token, or an empty string if there is nothing readable.</returns>
        /// <remarks>
        /// Values written before this store existed carry no prefix and are plain base64.
        /// They are read here so an upgrade does not force everyone to re-authorize.
        /// </remarks>
        public static string Unprotect(string stored)
        {
            if (string.IsNullOrEmpty(stored))
            {
                return string.Empty;
            }

            try
            {
                if (stored.StartsWith(EncryptedPrefix, StringComparison.Ordinal))
                {
                    byte[] cipher = Convert.FromBase64String(stored.Substring(EncryptedPrefix.Length));
                    byte[] plain = ProtectedData.Unprotect(cipher, Entropy, DataProtectionScope.CurrentUser);
                    return Encoding.UTF8.GetString(plain);
                }

                if (stored.StartsWith(PlainPrefix, StringComparison.Ordinal))
                {
                    return Encoding.UTF8.GetString(Convert.FromBase64String(stored.Substring(PlainPrefix.Length)));
                }

                // Written by a version of the mod before this store existed.
                return Encoding.UTF8.GetString(Convert.FromBase64String(stored));
            }
            catch (Exception ex)
            {
                // A hand-edited settings file, or a blob copied from another Windows account.
                Main.LogEntry("TokenStore", $"Stored token could not be read ({ex.GetType().Name}). Treating it as absent.");
                return string.Empty;
            }
        }

        /// <summary>
        /// Checks once whether DPAPI actually works on this runtime by round-tripping a probe value.
        /// </summary>
        private static bool ProbeEncryption()
        {
            if (encryptionAvailable.HasValue)
            {
                return encryptionAvailable.Value;
            }

            try
            {
                byte[] probe = Encoding.UTF8.GetBytes("probe");
                byte[] cipher = ProtectedData.Protect(probe, Entropy, DataProtectionScope.CurrentUser);
                byte[] plain = ProtectedData.Unprotect(cipher, Entropy, DataProtectionScope.CurrentUser);
                encryptionAvailable = Encoding.UTF8.GetString(plain) == "probe";
            }
            catch (Exception ex)
            {
                // Mono without a DPAPI implementation, or a policy that blocks it.
                Main.LogEntry("TokenStore", $"Token encryption is unavailable on this runtime ({ex.GetType().Name}). Tokens will be stored encoded only.");
                encryptionAvailable = false;
            }

            Main.LogEntry("TokenStore", $"Token encryption at rest: {(encryptionAvailable == true ? "enabled" : "unavailable")}.");
            return encryptionAvailable.Value;
        }
    }
}
