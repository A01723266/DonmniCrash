using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DonmniCrash.UI
{
    public sealed partial class PlayerHudCard : MonoBehaviour
    {
        public TMP_Text playerName, stats;
        public Image emblem, healthFill, turnIndicator;
        public GameObject healthTrack;
        public GameObject ownSeatBadge;
        public TMP_Text ownSeatLabel;
        public LayoutElement size;

        public void Show(string displayName,Sprite icon,int health,int startingHealth,int handCount,bool active,bool compact,bool ownSeat)
        {
            var localizedName = playerName.GetComponent<LocalizedText>();
            if (localizedName) localizedName.Literal(displayName);
            else playerName.text = displayName;
            emblem.sprite = icon;
            emblem.gameObject.SetActive(!compact);
            ownSeatBadge.SetActive(ownSeat);
            if (ownSeat) ownSeatLabel.text = LocalizedText.Read("game.seat.you");
            playerName.margin = new Vector4(ownSeat ? 39 : 0,0,compact ? 0 : 23,0);
            stats.text = string.Format(LocalizedText.Read("game.bar.stats"),health,startingHealth,handCount);
            stats.gameObject.SetActive(!compact);
            healthTrack.SetActive(!compact);
            healthFill.fillAmount = Mathf.Clamp01((float)health / Mathf.Max(1,startingHealth));
            turnIndicator.enabled = active;
            size.minWidth = size.preferredWidth = compact ? 128 : 168;
            size.minHeight = size.preferredHeight = compact ? 36 : 82;
            var background = GetComponent<UIThemeBinding>();
            background.role = active ? UIThemeRole.Elevated : UIThemeRole.Panel;
            background.opacity = active ? .87f : .64f;
            background.Apply();
        }
    }
}
