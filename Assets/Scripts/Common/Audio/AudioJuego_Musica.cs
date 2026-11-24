using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DonmniCrash.UI
{
    public sealed partial class AudioJuego
    {
        CatalogoMusica catalogoMusica;
        readonly List<AudioClip> listaMusica = new List<AudioClip>();
        int indiceMusica;
        AudioClip ultimaCancion;

        void PrepararMusica()
        {
            catalogoMusica = Resources.Load<CatalogoMusica>("Audio/CatalogoMusica");
            musica.loop = false;
            SceneManager.sceneLoaded += AlCargarEscena;
        }

        void LiberarMusica() => SceneManager.sceneLoaded -= AlCargarEscena;

        void AlCargarEscena(Scene escena, LoadSceneMode modo)
        {
            musica.Stop();
            musica.clip = null;
            listaMusica.Clear();
            ultimaCancion = null;
            indiceMusica = 0;

            if (!catalogoMusica) return;
            AudioClip[] canciones;
            switch (escena.name)
            {
                case "Start": canciones = catalogoMusica.inicio; break;
                case "OnlineRooms":
                case "OfflineSetting": canciones = catalogoMusica.menus; break;
                case "OnlineMatch":
                case "OfflineMatch": canciones = catalogoMusica.partidas; break;
                case "Tutorial": canciones = catalogoMusica.tutorial; break;
                default: return;
            }

            if (canciones != null)
                foreach (var cancion in canciones)
                    if (cancion && !listaMusica.Contains(cancion)) listaMusica.Add(cancion);
            if (listaMusica.Count == 0) return;

            MezclarLista();
            ReproducirSiguiente();
        }

        void ActualizarMusica()
        {
            if (listaMusica.Count > 0 && !musica.isPlaying && !AudioListener.pause)
                ReproducirSiguiente();
        }

        void ReproducirSiguiente()
        {
            if (indiceMusica >= listaMusica.Count)
            {
                MezclarLista();
                indiceMusica = 0;
            }
            musica.clip = listaMusica[indiceMusica++];
            ultimaCancion = musica.clip;
            musica.Play();
        }

        void MezclarLista()
        {
            for (int i = listaMusica.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                var temporal = listaMusica[i];
                listaMusica[i] = listaMusica[j];
                listaMusica[j] = temporal;
            }
            if (listaMusica.Count > 1 && listaMusica[0] == ultimaCancion)
            {
                int j = Random.Range(1, listaMusica.Count);
                var temporal = listaMusica[0];
                listaMusica[0] = listaMusica[j];
                listaMusica[j] = temporal;
            }
        }
    }
}
