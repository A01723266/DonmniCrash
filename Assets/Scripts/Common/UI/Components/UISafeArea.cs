using UnityEngine;

namespace DonmniCrash.UI
{
    [ExecuteAlways, RequireComponent(typeof(RectTransform))]
    public sealed partial class UISafeArea : MonoBehaviour
    {
        Rect previous;
        Vector2Int previousSize;
        void OnEnable() => Apply();
        void Update()
        {
            if (previous != Screen.safeArea || previousSize != new Vector2Int(Screen.width, Screen.height)) Apply();
        }
        void Apply()
        {
            if (Screen.width < 1 || Screen.height < 1) return;
            previous = Screen.safeArea;
            previousSize = new Vector2Int(Screen.width, Screen.height);
            var rect = (RectTransform)transform;
            rect.anchorMin = previous.position / new Vector2(Screen.width, Screen.height);
            rect.anchorMax = (previous.position + previous.size) / new Vector2(Screen.width, Screen.height);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
