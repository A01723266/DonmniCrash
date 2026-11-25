using DonmniCrash.Game;
using UnityEngine;
using UnityEngine.UI;

namespace DonmniCrash.UI
{
    public sealed partial class ElementHelpView : MonoBehaviour
    {
        public Sprite[] swords, shields, symbols;
        public Image[] firstShields, secondShields;
        public LocalizedText[] firstNames, secondNames;

        void Awake()
        {
            for (int attack = 0; attack < swords.Length; attack++)
            {
                int slot = 0;
                for (int defense = 0; defense < shields.Length; defense++)
                {
                    if (!TileRules.Beats((Element)defense,(Element)attack)) continue;
                    var icon = slot == 0 ? firstShields[attack] : secondShields[attack];
                    var label = slot == 0 ? firstNames[attack] : secondNames[attack];
                    icon.sprite = shields[defense];
                    label.Localize("game.element." + defense);
                    slot++;
                }
            }
        }
    }
}
