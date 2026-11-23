using UnityEngine;

namespace DonmniCrash.UI
{
    public enum UIThemeRole { Background, Panel, Elevated, Text, SecondaryText, Accent, OnAccent, Border, Focus }

    [CreateAssetMenu(menuName = "DonmniCrash/UI Theme")]
    public sealed partial class UITheme : ScriptableObject
    {
        public string displayName;
        public Color background, panel, elevated, text, secondaryText, accent, onAccent, border, focus;
        public Color highlighted, pressed, disabled;

        public Color Get(UIThemeRole role)
        {
            switch (role)
            {
                case UIThemeRole.Background: return background;
                case UIThemeRole.Panel: return panel;
                case UIThemeRole.Elevated: return elevated;
                case UIThemeRole.Text: return text;
                case UIThemeRole.SecondaryText: return secondaryText;
                case UIThemeRole.Accent: return accent;
                case UIThemeRole.OnAccent: return onAccent;
                case UIThemeRole.Border: return border;
                case UIThemeRole.Focus: return focus;
                default: return text;
            }
        }
    }
}
