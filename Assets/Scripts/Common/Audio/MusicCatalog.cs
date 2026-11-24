using UnityEngine;

namespace DonmniCrash.UI
{
    // The editor indexes these references from the four music folders.
    public sealed class MusicCatalog : ScriptableObject
    {
        [UnityEngine.Serialization.FormerlySerializedAs("inicio")] public AudioClip[] startTracks = new AudioClip[0];
        public AudioClip[] menus = new AudioClip[0];
        [UnityEngine.Serialization.FormerlySerializedAs("partidas")] public AudioClip[] matchTracks = new AudioClip[0];
        public AudioClip[] tutorial = new AudioClip[0];
    }
}
