using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DonmniCrash.UI
{
    public sealed partial class GameAudio
    {
        // Web audio may be suspended until a pointer gesture. Keep the playlist cursor still until then.
        bool waitingForGesture;
        bool observedPlayback;
        MusicCatalog musicCatalog;
        readonly List<AudioClip> playlist = new List<AudioClip>();
        int trackIndex;
        AudioClip lastTrack;

        void InitializeMusic()
        {
            musicCatalog = Resources.Load<MusicCatalog>("Audio/CatalogoMusica");
            waitingForGesture = Application.platform == RuntimePlatform.WebGLPlayer;
            music.loop = false;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void ReleaseMusic() => SceneManager.sceneLoaded -= OnSceneLoaded;

        void OnSceneLoaded(Scene loadedScene, LoadSceneMode loadMode)
        {
            music.Stop();
            music.clip = null;
            playlist.Clear();
            lastTrack = null;
            trackIndex = 0;

            if (!musicCatalog) return;
            AudioClip[] tracks;
            switch (loadedScene.name)
            {
                case "Start": tracks = musicCatalog.startTracks; break;
                case "OnlineRooms":
                case "OfflineSetting": tracks = musicCatalog.menus; break;
                case "OnlineMatch":
                case "OfflineMatch": tracks = musicCatalog.matchTracks; break;
                case "Tutorial": tracks = musicCatalog.tutorial; break;
                default: return;
            }

            if (tracks != null)
                foreach (var trackClip in tracks)
                    if (trackClip && !playlist.Contains(trackClip)) playlist.Add(trackClip);
            if (playlist.Count == 0) return;

            ShufflePlaylist();
            if (!waitingForGesture) PlayNext();
        }

        void UpdateMusic()
        {
            if (waitingForGesture || playlist.Count == 0 || AudioListener.pause) return;
            if (music.isPlaying) observedPlayback = true;
            else if (observedPlayback) PlayNext();
        }

        void PlayNext()
        {
            if (trackIndex >= playlist.Count)
            {
                ShufflePlaylist();
                trackIndex = 0;
            }
            music.clip = playlist[trackIndex++];
            lastTrack = music.clip;
            observedPlayback = false;
            music.Play();
        }

        void ShufflePlaylist()
        {
            for (int i = playlist.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                var temp = playlist[i];
                playlist[i] = playlist[j];
                playlist[j] = temp;
            }
            if (playlist.Count > 1 && playlist[0] == lastTrack)
            {
                int j = Random.Range(1, playlist.Count);
                var temp = playlist[0];
                playlist[0] = playlist[j];
                playlist[j] = temp;
            }
        }
    }
}
