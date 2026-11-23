using UnityEngine;
using UnityEngine.UI;

namespace DonmniCrash.UI
{
    // Solo referencias de presentación; no contiene reglas de ataque o defensa.
    public sealed partial class DominoTileView : MonoBehaviour
    {
        // Cada ficha, incluidas las que se abran después en un modal, sigue la losa de sesión.
        void OnEnable() { AspectoSesion.Cambio += AplicarLosa; AplicarLosa(); }
        void OnDisable() { AspectoSesion.Cambio -= AplicarLosa; }
        void AplicarLosa()
        {
            if (slab && AspectoSesion.Catalogo) slab.sprite = AspectoSesion.Catalogo.losas[AspectoSesion.Losa];
        }

        public Image slab;
        public Image attackIcon;
        public Image defenseIcon;
        public GameObject selectionOutline;
    }
}
