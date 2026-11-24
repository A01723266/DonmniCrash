using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace DonmniCrash.UI
{
    /// <summary>Expose a validated cyclic selection to configuration forms.</summary>
    public sealed partial class OptionSelector : MonoBehaviour
    {
        [UnityEngine.Serialization.FormerlySerializedAs("opciones")] public string[] options;
        [UnityEngine.Serialization.FormerlySerializedAs("indice")] public int index;
        // A selector value can be a literal number or a localized option.
        public string ValueText => options[index];
        void Awake()
        {
            foreach (var button in GetComponentsInChildren<Button>(true))
            {
                int delta = button.name == "NextButton" ? 1 : -1;
                button.onClick.AddListener(() => Choose(index + delta));
            }
            Choose(index);
        }
        public void Choose(int value)
        {
            index = (value % options.Length + options.Length) % options.Length;
            var labelText = GetComponentsInChildren<LocalizedText>(true).First(t => t.name == "ValueLabel");
            var key = LocalizedText.KeyForText(ValueText);
            if (key == null) labelText.Literal(ValueText);
            else labelText.Localize(key);
        }
    }
}
