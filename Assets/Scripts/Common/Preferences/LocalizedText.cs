using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace DonmniCrash.UI
{
    /// <summary>Bind a stable localization key or display player-provided text literally.</summary>
    [RequireComponent(typeof(TMP_Text))]
    public sealed partial class LocalizedText : MonoBehaviour
    {
        [UnityEngine.Serialization.FormerlySerializedAs("clave")] public string key;
        static readonly Dictionary<string, string[]> translations = new Dictionary<string, string[]>();
        static readonly Dictionary<string, string> bySource = new Dictionary<string, string>();
        object[] arguments;
        string literal;

        // Load both translations together once per domain.
        static void Load()
        {
            if (translations.Count > 0) return;
            foreach (var line in Resources.Load<TextAsset>("UI/Idiomas").text.Split('\n'))
            {
                var c = line.TrimEnd('\r').Split('\t');
                if (c.Length != 3) continue;
                var es = c[1].Replace("\\n", "\n");
                translations[c[0]] = new[] { es, c[2].Replace("\\n", "\n") };
                bySource[es] = c[0];
                bySource[translations[c[0]][1]] = c[0];
            }
        }

        /// <summary>Resolve a static label to a stable catalog key during scene authoring.</summary>
        public static string KeyForText(string labelText)
        {
            Load();
            return labelText != null && bySource.TryGetValue(labelText, out var id) ? id : null;
        }

        public static string Read(string id)
        {
            Load();
            return id != null && translations.TryGetValue(id, out var t) ? t[SessionAppearance.English ? 1 : 0] : id;
        }

        public void Localize(string id, params object[] values)
        {
            key = id;
            literal = null;
            arguments = values;
            Render();
        }

        public void Literal(string value)
        {
            literal = value;
            Render();
        }

        void OnEnable() { SessionAppearance.Changed += Render; Render(); }
        void OnDisable() { SessionAppearance.Changed -= Render; }
        void Render()
        {
            if (literal == null && string.IsNullOrEmpty(key)) return;
            var template = Read(key);
            GetComponent<TMP_Text>().text = literal ?? (arguments == null || arguments.Length == 0 ? template : string.Format(template, arguments));
        }
    }
}
