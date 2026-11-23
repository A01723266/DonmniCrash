using UnityEngine;

namespace DonmniCrash.UI
{
    [ExecuteAlways]
    public sealed partial class UIOrientationHint : MonoBehaviour
    {
        public GameObject portraitPanel;
        void OnEnable() => Refresh();
        void Update() => Refresh();
        void Refresh()
        {
            if (portraitPanel && portraitPanel.activeSelf != (Screen.height > Screen.width))
                portraitPanel.SetActive(Screen.height > Screen.width);
        }
    }
}
