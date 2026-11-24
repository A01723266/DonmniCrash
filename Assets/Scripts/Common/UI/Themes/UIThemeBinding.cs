using UnityEngine;
using UnityEngine.UI;

namespace DonmniCrash.UI
{
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed partial class UIThemeBinding : MonoBehaviour
    {
        public UITheme theme;
        public UIThemeRole role;
        [Range(0, 1)] public float opacity = 1;
        public bool tintSelectable;

        // Use the active palette at runtime and the Inspector reference when authoring.
        void OnEnable() { if (Application.isPlaying) SessionAppearance.Changed += Apply; Apply(); }
        void OnDisable() { SessionAppearance.Changed -= Apply; }
        void OnValidate() => Apply();

        [ContextMenu("Apply assigned theme")]
        public void Apply()
        {
            var activeTheme = Application.isPlaying && SessionAppearance.Catalog ? SessionAppearance.Catalog.palettes[SessionAppearance.Palette] : theme;
            if (!activeTheme) return;
            var graphic = GetComponent<Graphic>();
            Color color = activeTheme.Get(role);
            color.a *= opacity;
            if (graphic) graphic.color = color;
            var selectable = GetComponent<Selectable>();
            if (!tintSelectable || !selectable) return;
            // ColorBlock multiplies the graphic's base color.
            var colors = selectable.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1);
            colors.pressedColor = new Color(.78f, .78f, .78f, 1);
            colors.selectedColor = new Color(1.18f, 1.18f, 1.18f, 1);
            colors.disabledColor = new Color(.65f, .65f, .65f, .45f);
            colors.fadeDuration = .12f;
            selectable.colors = colors;
        }
    }
}
