using UnityEngine;
using UnityEngine.UI;

namespace DonmniCrash.UI
{
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class UIThemeBinding : MonoBehaviour
    {
        public UITheme theme;
        public UIThemeRole role;
        [Range(0, 1)] public float opacity = 1;
        public bool tintSelectable;

        void OnEnable() => Apply();
        void OnValidate() => Apply();

        [ContextMenu("Apply assigned theme")]
        public void Apply()
        {
            if (!theme) return;
            var graphic = GetComponent<Graphic>();
            Color color = theme.Get(role);
            color.a *= opacity;
            if (graphic) graphic.color = color;
            var selectable = GetComponent<Selectable>();
            if (!tintSelectable || !selectable) return;
            // ColorBlock multiplica el color base del Graphic.
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
