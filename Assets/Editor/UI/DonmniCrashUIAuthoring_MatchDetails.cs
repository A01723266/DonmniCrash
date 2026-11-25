using System.Linq;
using DonmniCrash.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DonmniCrash.EditorTools
{
    public static partial class DonmniCrashUIAuthoring
    {
        [MenuItem("DonmniCrash/Apply match results and table controls")]
        public static void ApplyMatchDetails()
        {
            EnsureMatchScenesSaved();
            LoadTablePolishArt();
            UpdatePolishPrefab("Panels/SettingsPanel",root => AddPlacementSlider(root.GetComponent<SettingsPanel>()));
            UpdatePolishPrefab("Gameplay/DominoTileView",root =>
                AddAssistCheck(root.transform.Find("AssistMarker/AssistBadge")));
            const string path = "Assets/Resources/Prefabs/Audio/AudioJuego.prefab";
            var audio = PrefabUtility.LoadPrefabContents(path);
            try
            {
                AddPlacementAudio(audio.GetComponent<GameAudio>());
                PrefabUtility.SaveAsPrefabAsset(audio,path);
            }
            finally { PrefabUtility.UnloadPrefabContents(audio); }
            EditMatchScenes(AddMatchDetails);
        }

        static void AddAssistCheck(Transform badge)
        {
            if (badge.Find("CheckIcon")) return;
            var old = badge.Find("Check");
            if (old) Object.DestroyImmediate(old.gameObject);
            var art = Sprites("Assets/Kenney/UI/kenney_ui-pack/PNG/Grey/Default/icon_checkmark.png").First();
            var icon = Icon("CheckIcon",badge,art);
            Stretch(icon.rectTransform,3,3,3,3);
            icon.raycastTarget = false;
            Bind(icon,UIThemeRole.Accent);
        }

        static void AddPlacementSlider(SettingsPanel settings)
        {
            if (settings.placement) return;
            var audio = settings.effects.transform.parent.parent;
            SliderRow(audio,"PlacementVolumeRow","Tile volume",.75f);
            var row = audio.Find("PlacementVolumeRow");
            settings.placement = row.GetComponentInChildren<Slider>();
            Localize(row.GetComponentInChildren<TMP_Text>(),"audio.placement");
        }

        static void AddPlacementAudio(GameAudio audio)
        {
            if (!audio.placements) audio.placements = Source(audio.transform,"TilePlacements",false);
            audio.placementClip = AssetDatabase.LoadAssetAtPath<AudioClip>(
                "Assets/Kenney/Sounds/kenney_casino-audio/Audio/chip-lay-1.ogg");
        }

        static void AddMatchDetails(RectTransform screen,bool tutorial)
        {
            var canvas = screen.GetComponentInParent<Canvas>().transform;
            var overlays = screen.parent.parent.Find("Overlays");
            var area = (RectTransform)screen.Find("BoardArea");
            var nav = ChildComponent<BoardNavigator>(canvas.gameObject,"BoardViewport");
            if (!nav.followArea)
            {
                nav.followArea = area;
                var viewport = (RectTransform)nav.transform;
                viewport.SetParent(canvas,false);
                viewport.SetSiblingIndex(canvas.Find("Background").GetSiblingIndex()+1);
                Stretch(viewport);
            }
            var actions = ChildComponent<RectTransform>(screen.gameObject,"ChoiceActions");
            if (!actions.Find("RandomChoiceButton"))
                AddAction(actions,"RandomChoiceButton","choice.random").transform.SetAsFirstSibling();

            var rivalPanel = overlays.Find("TargetSelectionDialog");
            if (rivalPanel)
            {
                rivalPanel.SetParent(screen,false);
                Stretch((RectTransform)rivalPanel);
                for (int i=1;i<=5;i++)
                {
                    var button = ChildComponent<Button>(rivalPanel.gameObject,"TargetPlayer"+i+"Button");
                    button.GetComponentInChildren<TMP_Text>(true).gameObject.SetActive(false);
                    Size(button,84);
                    var card = Player(button.transform,"RivalCard","",false);
                    Stretch(card,2,2,2,2);
                }
                var content = ChildComponent<RectTransform>(rivalPanel.gameObject,"Content");
                var random = AddAction(content,"RandomRivalButton","swap.random");
                var cancel = ChildComponent<Button>(content.gameObject,"CancelButton");
                random.transform.SetSiblingIndex(cancel.transform.GetSiblingIndex());
            }

            var results = overlays.Find("ResultsDialog");
            var window = ChildComponent<RectTransform>(results.gameObject,"WindowPanel");
            if (!window.Find("ResultsActions"))
            {
                var scroll = ChildComponent<RectTransform>(window.gameObject,"DialogScrollView");
                scroll.offsetMin = new Vector2(scroll.offsetMin.x,80);
                var footer = Node("ResultsActions",window);
                Place(footer,Vector2.zero,Vector2.right,new Vector2(16,12),new Vector2(-16,66));
                Horizontal(footer,10);
                foreach (string name in new[]{"RematchButton","ReturnButton"})
                {
                    var button = ChildComponent<Button>(results.gameObject,name);
                    button.transform.SetParent(footer,false);
                    FitButtonText(button.transform);
                    if (screen.gameObject.scene.name == "OfflineMatch")
                        Localize(button.GetComponentInChildren<TMP_Text>(true),name=="ReturnButton"?"results.menu":"results.restart");
                }
            }

            if (screen.gameObject.scene.name != "OfflineMatch") return;
            var spectator = ChildComponent<RectTransform>(screen.gameObject,"SpectatorPanel");
            if (spectator.Find("SpectatorActions")) return;
            ChildComponent<TMP_Text>(spectator.gameObject,"SpectatorDetailsLabel").gameObject.SetActive(false);
            var layout = spectator.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(16,16,10,10);
            var row = Row("SpectatorActions",spectator);
            AddAction(row,"RestartPracticeButton","results.restart");
            AddAction(row,"PracticeMenuButton","results.menu");
        }

        static Button AddAction(Transform parent,string name,string key)
        {
            var button = Button(name,parent,LocalizedText.Read(key));
            Localize(button.GetComponentInChildren<TMP_Text>(true),key);
            FitButtonText(button.transform);
            return button;
        }
    }
}
