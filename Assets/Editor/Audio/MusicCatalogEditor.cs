using System;
using System.Linq;
using DonmniCrash.UI;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace DonmniCrash.Editor
{
    // Index tracks in their original folders so Unity includes the referenced clips in the player.
    [InitializeOnLoad]
    public sealed class MusicCatalogEditor : AssetPostprocessor
    {
        const string MusicFolder = "Assets/Music";
        const string CatalogFolder = "Assets/Resources/Audio";
        const string CatalogPath = CatalogFolder + "/CatalogoMusica.asset";

        static MusicCatalogEditor() => ScheduleRefresh();

        static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] oldPaths)
        {
            if (imported.Concat(deleted).Concat(moved).Concat(oldPaths)
                .Any(path => path == MusicFolder || path.StartsWith(MusicFolder + "/", StringComparison.Ordinal)))
                ScheduleRefresh();
        }

        static void ScheduleRefresh()
        {
            EditorApplication.delayCall -= RefreshCatalog;
            EditorApplication.delayCall += RefreshCatalog;
        }

        static AudioClip[] FindTracks(string folder)
        {
            string path = MusicFolder + "/" + folder;
            if (!AssetDatabase.IsValidFolder(path)) return Array.Empty<AudioClip>();
            return AssetDatabase.FindAssets("t:AudioClip", new[] { path })
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(path => path, StringComparer.Ordinal)
                .Select(AssetDatabase.LoadAssetAtPath<AudioClip>)
                .Where(clip => clip != null)
                .ToArray();
        }

        public static void RefreshCatalog()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder(CatalogFolder))
                AssetDatabase.CreateFolder("Assets/Resources", "Audio");

            var catalog = AssetDatabase.LoadAssetAtPath<MusicCatalog>(CatalogPath);
            if (!catalog)
            {
                catalog = ScriptableObject.CreateInstance<MusicCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            var startTracks = FindTracks("Start");
            var menus = FindTracks("OnlineRooms-OfflineSettings");
            var matchTracks = FindTracks("OnlineMatch-OfflineMatch");
            var tutorial = FindTracks("Tutorial");
            if (catalog.startTracks.SequenceEqual(startTracks) && catalog.menus.SequenceEqual(menus) &&
                catalog.matchTracks.SequenceEqual(matchTracks) && catalog.tutorial.SequenceEqual(tutorial)) return;

            catalog.startTracks = startTracks;
            catalog.menus = menus;
            catalog.matchTracks = matchTracks;
            catalog.tutorial = tutorial;
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
        }
    }

    public sealed class MusicBuildProcessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;
        public void OnPreprocessBuild(BuildReport report) => MusicCatalogEditor.RefreshCatalog();
    }
}
