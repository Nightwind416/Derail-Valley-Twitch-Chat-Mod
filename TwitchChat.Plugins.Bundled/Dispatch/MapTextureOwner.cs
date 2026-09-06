using System;
using UnityEngine;

namespace TwitchChat.Plugins.Bundled.Dispatch
{
    /// <summary>
    /// Ties the map's textures to the lifetime of the object they are shown on.
    /// </summary>
    /// <remarks>
    /// A texture made with <c>new Texture2D</c> belongs to nothing and is not collected when the last
    /// thing referencing it goes away, so a display closed and reopened a few times would quietly leave
    /// megabytes behind. Hanging this on the map object means Unity destroys the textures when it
    /// destroys the panel, which is exactly when they stop being wanted.
    /// </remarks>
    internal sealed class MapTextureOwner : MonoBehaviour
    {
        private IDisposable? owned;

        internal void Own(IDisposable disposable) => owned = disposable;

        private void OnDestroy()
        {
            owned?.Dispose();
            owned = null;
        }
    }
}
