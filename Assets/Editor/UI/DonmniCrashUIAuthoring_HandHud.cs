using DonmniCrash.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DonmniCrash.EditorTools
{
    public static partial class DonmniCrashUIAuthoring
    {
        static void AddOwnSeatBadge(PlayerHudCard card)
        {
            if (card.ownSeatBadge) return;

            var layout = card.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10,10,3,3);
            Size(card.playerName,25);
            card.playerName.fontSizeMin = 14;
            card.playerName.fontSizeMax = 20;
            card.playerName.enableAutoSizing = true;
            card.playerName.alignment = TextAlignmentOptions.MidlineLeft;

            var badge = Panel("OwnSeatBadge",card.transform,UIThemeRole.Accent);
            badge.raycastTarget = false;
            Size(badge).ignoreLayout = true;
            Place(badge.rectTransform,Vector2.up,Vector2.up,new Vector2(10,-27),new Vector2(44,-4));
            var label = Label("OwnSeatLabel",badge.transform,"You",14,UIThemeRole.OnAccent,true);
            label.alignment = TextAlignmentOptions.Center;
            Stretch(label.rectTransform);
            Localize(label,"game.seat.you");
            badge.gameObject.SetActive(false);
            card.ownSeatBadge = badge.gameObject;
            card.ownSeatLabel = label;
        }

        static void PolishHandHud(RectTransform screen,bool tutorial)
        {
            RectTransform Find(string name) => ChildComponent<RectTransform>(screen.gameObject,name);
            var hud = Find("HandHUDPanel");
            if (hud.Find("HandTray")) return;

            // El borde inferior del área segura recorta solo la parte de la mano que se baja.
            Place(hud,Vector2.zero,Vector2.right,new Vector2(16,0),new Vector2(-16,232));
            hud.gameObject.AddComponent<RectMask2D>();
            var tray = Node("HandTray",hud);
            tray.SetAsFirstSibling();
            Place(tray,Vector2.zero,Vector2.right,new Vector2(0,10),new Vector2(0,181));

            var scroll = Find("HandScrollView");
            scroll.SetParent(tray,false);
            Stretch(scroll,0,3,0,3);

            var context = Find("HandContextLabel");
            context.SetParent(tray,false);
            Place(context,Vector2.up,Vector2.one,new Vector2(1,26),new Vector2(-5,50));
            context.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.MidlineLeft;

            var toolbar = Node("HandToolbar",tray);
            Place(toolbar,Vector2.up,Vector2.up,new Vector2(0,-11),new Vector2(458,25));
            var health = Panel("PersonalHealth",toolbar,UIThemeRole.Panel);
            health.raycastTarget = false;
            Place(health.rectTransform,Vector2.zero,Vector2.up,Vector2.zero,new Vector2(284,0));
            var label = Label("PersonalHealthLabel",health.transform,"Your HP — / —",17,UIThemeRole.Text,true);
            label.alignment = TextAlignmentOptions.MidlineLeft;
            Place(label.rectTransform,Vector2.zero,Vector2.up,new Vector2(10,0),new Vector2(130,0));
            var track = Panel("PersonalHealthTrack",health.transform,UIThemeRole.Background);
            track.raycastTarget = false;
            Place(track.rectTransform,new Vector2(0,.5f),new Vector2(1,.5f),new Vector2(132,-7),new Vector2(-10,7));
            var fill = Panel("PersonalHealthFill",track.transform,UIThemeRole.Accent);
            fill.raycastTarget = false;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            Stretch(fill.rectTransform,1,1,1,1);

            var toggle = Button("HandToggleButton",toolbar,"Lower tiles");
            Place(toggle.image.rectTransform,Vector2.zero,Vector2.up,new Vector2(292,0),new Vector2(458,0));
            FitButtonText(toggle.transform);
            Localize(toggle.GetComponentInChildren<TMP_Text>(true),"game.hand.lower");

            var actions = Find("ActionControlsPanel");
            Place(actions,Vector2.right,Vector2.right,new Vector2(-192,14),new Vector2(-4,177));
            actions.gameObject.SetActive(false);
            if (!tutorial)
            {
                var board = Find("HistoryScroll");
                board.offsetMin = new Vector2(board.offsetMin.x,236);
            }
        }
    }
}
