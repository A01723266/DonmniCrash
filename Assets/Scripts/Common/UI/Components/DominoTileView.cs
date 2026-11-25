using UnityEngine;
using UnityEngine.UI;

namespace DonmniCrash.UI
{
    // Tile graphics only. The rules belong to the engine.
    public sealed partial class DominoTileView : MonoBehaviour
    {
        // Each tile reapplies the selected slab when its panel becomes visible.
        void OnEnable() { SessionAppearance.Changed += ApplySlab; ApplySlab(); }
        void OnDisable() { SessionAppearance.Changed -= ApplySlab; }
        void ApplySlab()
        {
            if (slab && SessionAppearance.Catalog) slab.sprite = SessionAppearance.Catalog.slabs[SessionAppearance.Slab];
        }

        public Image slab;
        public Image attackIcon;
        public Image defenseIcon;
        public GameObject selectionOutline;
        public GameObject assistMarker;
        public GameObject latestOutline;
        public Image carriedAttack;
    }
}
