using UnityEngine;
using UnityEngine.UI;

namespace DonmniCrash.UI
{
    // Solo referencias de presentación; no contiene reglas de ataque o defensa.
    public sealed class DominoTileView : MonoBehaviour
    {
        public Image slab;
        public Image attackIcon;
        public Image defenseIcon;
        public GameObject selectionOutline;
    }
}
