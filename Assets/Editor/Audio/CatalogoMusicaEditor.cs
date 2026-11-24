using System;
using System.Linq;
using DonmniCrash.UI;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace DonmniCrash.Editor
{
    // Conserva las canciones en sus carpetas y guarda referencias que Unity incluye en la versión final.
    [InitializeOnLoad]
    public sealed class CatalogoMusicaEditor : AssetPostprocessor
    {
        const string CarpetaMusica = "Assets/Music";
        const string CarpetaCatalogo = "Assets/Resources/Audio";
        const string RutaCatalogo = CarpetaCatalogo + "/CatalogoMusica.asset";

        static CatalogoMusicaEditor() => ProgramarActualizacion();

        static void OnPostprocessAllAssets(string[] importados, string[] borrados, string[] movidos, string[] anteriores)
        {
            if (importados.Concat(borrados).Concat(movidos).Concat(anteriores)
                .Any(ruta => ruta == CarpetaMusica || ruta.StartsWith(CarpetaMusica + "/", StringComparison.Ordinal)))
                ProgramarActualizacion();
        }

        static void ProgramarActualizacion()
        {
            EditorApplication.delayCall -= ActualizarCatalogo;
            EditorApplication.delayCall += ActualizarCatalogo;
        }

        static AudioClip[] Canciones(string carpeta)
        {
            string ruta = CarpetaMusica + "/" + carpeta;
            if (!AssetDatabase.IsValidFolder(ruta)) return Array.Empty<AudioClip>();
            return AssetDatabase.FindAssets("t:AudioClip", new[] { ruta })
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(path => path, StringComparer.Ordinal)
                .Select(AssetDatabase.LoadAssetAtPath<AudioClip>)
                .Where(clip => clip != null)
                .ToArray();
        }

        public static void ActualizarCatalogo()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder(CarpetaCatalogo))
                AssetDatabase.CreateFolder("Assets/Resources", "Audio");

            var catalogo = AssetDatabase.LoadAssetAtPath<CatalogoMusica>(RutaCatalogo);
            if (!catalogo)
            {
                catalogo = ScriptableObject.CreateInstance<CatalogoMusica>();
                AssetDatabase.CreateAsset(catalogo, RutaCatalogo);
            }

            var inicio = Canciones("Start");
            var menus = Canciones("OnlineRooms-OfflineSettings");
            var partidas = Canciones("OnlineMatch-OfflineMatch");
            var tutorial = Canciones("Tutorial");
            if (catalogo.inicio.SequenceEqual(inicio) && catalogo.menus.SequenceEqual(menus) &&
                catalogo.partidas.SequenceEqual(partidas) && catalogo.tutorial.SequenceEqual(tutorial)) return;

            catalogo.inicio = inicio;
            catalogo.menus = menus;
            catalogo.partidas = partidas;
            catalogo.tutorial = tutorial;
            EditorUtility.SetDirty(catalogo);
            AssetDatabase.SaveAssets();
        }
    }

    public sealed class PrepararCatalogoMusicaParaBuild : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;
        public void OnPreprocessBuild(BuildReport informe) => CatalogoMusicaEditor.ActualizarCatalogo();
    }
}
