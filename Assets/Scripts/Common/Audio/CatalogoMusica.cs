using UnityEngine;

namespace DonmniCrash.UI
{
    // El editor actualiza estas referencias a partir de las carpetas de Assets/Music.
    public sealed class CatalogoMusica : ScriptableObject
    {
        public AudioClip[] inicio = new AudioClip[0];
        public AudioClip[] menus = new AudioClip[0];
        public AudioClip[] partidas = new AudioClip[0];
        public AudioClip[] tutorial = new AudioClip[0];
    }
}
