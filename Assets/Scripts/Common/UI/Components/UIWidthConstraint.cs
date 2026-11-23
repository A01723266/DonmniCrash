using UnityEngine;

namespace DonmniCrash.UI
{
    [ExecuteAlways, RequireComponent(typeof(RectTransform))]
    public sealed partial class UIWidthConstraint : MonoBehaviour
    {
        public float maximumWidth = 1000;
        public float horizontalMargin = 32;
        void OnEnable() => Refresh();
        void LateUpdate() => Refresh();
        void Refresh()
        {
            var rect = (RectTransform)transform;
            var parent = transform.parent as RectTransform;
            if (!parent) return;
            float width = Mathf.Min(maximumWidth, Mathf.Max(1, parent.rect.width - horizontalMargin * 2));
            if (!Mathf.Approximately(rect.rect.width, width)) rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        }
    }
}
