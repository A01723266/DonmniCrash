using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DonmniCrash.UI
{
    public abstract partial class PlayableTableUI
    {
        // Con fichas de 142 unidades, el escudo queda a la vista al bajar la mano.
        const float HandDrop = 86;
        RectTransform handTray;
        GameObject handActions, personalHealth, handPanel, spectatorPanel;
        Image personalHealthFill;
        TMP_Text personalHealthLabel, handToggleLabel;
        Vector2 raisedHandPosition;
        float raisedBoardBottom;
        bool handLowered;

        void BindHandHud()
        {
            handPanel = FindNode("HandHUDPanel").gameObject;
            spectatorPanel = FindNode("SpectatorPanel").gameObject;
            handTray = ComponentOf<RectTransform>("HandTray");
            handActions = FindNode("ActionControlsPanel").gameObject;
            personalHealth = FindNode("PersonalHealth").gameObject;
            personalHealthFill = ComponentOf<Image>("PersonalHealthFill");
            personalHealthLabel = ComponentOf<TMP_Text>("PersonalHealthLabel");
            var toggle = ComponentOf<Button>("HandToggleButton");
            handToggleLabel = toggle.GetComponentInChildren<TMP_Text>(true);
            raisedHandPosition = handTray.anchoredPosition;
            raisedBoardBottom = boardArea.offsetMin.y;
            toggle.onClick.AddListener(ToggleHand);
            ShowOwnTurnActions(false);
            RefreshHandToggle();
        }

        void ToggleHand()
        {
            handLowered = !handLowered;
            handTray.anchoredPosition = raisedHandPosition - Vector2.up * (handLowered ? HandDrop : 0);
            boardArea.offsetMin = new Vector2(boardArea.offsetMin.x,raisedBoardBottom - (handLowered ? HandDrop : 0));
            RefreshHandToggle();
        }

        void RefreshHandToggle() => WriteLabel(handToggleLabel,
            T(handLowered ? "game.hand.raise" : "game.hand.lower"));

        void RenderPersonalHealth()
        {
            int seat = LocalSeat;
            personalHealth.SetActive(seat >= 0);
            if (seat < 0) return;

            int current = Display.Health(seat);
            int initial = Mathf.Max(1,StartingHealth);
            personalHealthFill.fillAmount = Mathf.Clamp01((float)current / initial);
            WriteLabel(personalHealthLabel,T("game.health.own",current,initial));
        }

        void ShowOwnTurnActions(bool visible)
        {
            handActions.SetActive(visible);
            var viewport = (RectTransform)hand.transform;
            viewport.offsetMax = new Vector2(visible ? -202 : 0,viewport.offsetMax.y);
        }
    }
}
