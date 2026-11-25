using System;
using System.Linq;
using DonmniCrash.Game;
using DonmniCrash.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DonmniCrash.EditorTools
{
    public static partial class DonmniCrashUIAuthoring
    {
        [MenuItem("DonmniCrash/Apply table UI polish")]
        public static void ApplyTablePolish()
        {
            EnsureMatchScenesSaved();
            LoadTablePolishArt();
            LoadElementArt();

            UpdatePolishPrefab("Gameplay/PlayerCard", root => PolishPlayerCard((RectTransform)root.transform));
            UpdatePolishPrefab("Gameplay/DominoTileView", root => PolishTile(root.GetComponent<DominoTileView>()));
            UpdatePolishPrefab("Panels/SettingsPanel", root => AddPlayAssist(root.GetComponent<SettingsPanel>()));
            UpdatePolishPrefab("Overlays/ElementHelpDialog", root => BuildElementGuide(ChildComponent<RectTransform>(root,"Content")));
            EditMatchScenes((screen,tutorial) =>
            {
                PolishMatchHud(screen,tutorial);
                PolishOpeningPresentation(screen,tutorial);
            });
            AssetDatabase.SaveAssets();
        }

        static void EnsureMatchScenesSaved()
        {
            for (int index = 0; index < UnityEngine.SceneManagement.SceneManager.sceneCount; index++)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(index).isDirty)
                    throw new InvalidOperationException("Save open scenes before applying the table UI polish.");
        }

        static void LoadTablePolishArt()
        {
            theme = AssetDatabase.LoadAssetAtPath<SessionAppearance>("Assets/Resources/UI/Aspecto.asset").palettes[0];
            rounded = AssetDatabase.LoadAssetAtPath<Sprite>(UI + "/Sprites/RoundedPanel.png");
            regular = Font("Regular");
            semibold = Font("SemiBold");
        }

        static void EditMatchScenes(Action<RectTransform,bool> edit)
        {
            var previous = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach (var name in new[]{"OfflineMatch","OnlineMatch","Tutorial"})
                {
                    var scene = EditorSceneManager.OpenScene("Assets/Scenes/" + name + ".unity");
                    var screen = scene.GetRootGameObjects()
                        .SelectMany(root => root.GetComponentsInChildren<RectTransform>(true))
                        .First(node => node.name == "MatchScreen");
                    edit(screen,name == "Tutorial");
                    foreach (var component in screen.GetComponentsInChildren<Component>(true))
                        if (component && PrefabUtility.IsPartOfPrefabInstance(component))
                            PrefabUtility.RecordPrefabInstancePropertyModifications(component);
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                }
            }
            finally
            {
                if (previous.Any(scene => scene.isActive && scene.isLoaded && !string.IsNullOrEmpty(scene.path)))
                    EditorSceneManager.RestoreSceneManagerSetup(previous);
            }
        }

        static void UpdatePolishPrefab(string path, Action<GameObject> edit)
        {
            path = Prefabs + "/" + path + ".prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                edit(root);
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        static void LoadElementArt()
        {
            foreach (var element in elements)
            {
                elemental[element] = Sprites("Assets/Sprites/" + element + "Domino.png")
                    .OrderByDescending(sprite => sprite.rect.width * sprite.rect.height).Take(2).OrderBy(sprite => sprite.rect.x).ToArray();
                symbols[element] = Sprites("Assets/Sprites/" + element + ".png")
                    .OrderByDescending(sprite => sprite.rect.width * sprite.rect.height).First();
            }
        }

        static void Localize(TMP_Text label,string key) => TextBinding(label).Localize(key);

        static void AddPlayAssist(SettingsPanel settings)
        {
            if (settings.playAssist) return;
            var card = Card("GameplayPanel",settings.transform,"Gameplay");
            card.SetSiblingIndex(1);
            Localize(ChildComponent<TMP_Text>(card.gameObject,"TitleLabel"),"settings.gameplay");
            settings.playAssist = Button("PlayAssistButton",card,"Gameplay assistance: On");
            Localize(settings.playAssist.GetComponentInChildren<TMP_Text>(),"settings.assist.on");
        }

        static void PolishPlayerCard(RectTransform root)
        {
            var existingCard = root.GetComponent<PlayerHudCard>();
            if (existingCard)
            {
                AddOwnSeatBadge(existingCard);
                return;
            }
            var card = root.gameObject.AddComponent<PlayerHudCard>();
            card.size = Size(root,82,168);
            var layout = root.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10,10,5,5);
            layout.spacing = 2;
            root.GetComponent<Image>().raycastTarget = false;
            Bind(root.GetComponent<Image>(),UIThemeRole.Panel).opacity = .64f;
            root.GetComponent<UIThemeBinding>().Apply();

            card.playerName = ChildComponent<TMP_Text>(root.gameObject,"PlayerNameLabel");
            card.playerName.richText = false;
            card.playerName.textWrappingMode = TextWrappingModes.NoWrap;
            card.playerName.margin = new Vector4(0,0,23,0);
            Size(card.playerName,21);
            card.stats = ChildComponent<TMP_Text>(root.gameObject,"PlayerStatsLabel");
            card.stats.text = "HP — / — · Tiles —";
            card.stats.textWrappingMode = TextWrappingModes.NoWrap;
            Size(card.stats,20);
            card.turnIndicator = ChildComponent<Image>(root.gameObject,"TurnIndicator");
            card.turnIndicator.raycastTarget = false;
            card.turnIndicator.gameObject.SetActive(true);
            card.turnIndicator.enabled = false;

            card.emblem = Icon("SeatEmblem",root,symbols["Earth"],20);
            card.emblem.GetComponent<LayoutElement>().ignoreLayout = true;
            Place(card.emblem.rectTransform,Vector2.one,Vector2.one,new Vector2(-30,-28),new Vector2(-10,-8));

            var track = Panel("HealthTrack",root,UIThemeRole.Background);
            track.raycastTarget = false;
            Size(track,9);
            track.transform.SetSiblingIndex(card.turnIndicator.transform.GetSiblingIndex());
            card.healthTrack = track.gameObject;
            var fill = Panel("HealthFill",track.transform,UIThemeRole.Accent);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.raycastTarget = false;
            Stretch(fill.rectTransform,1,1,1,1);
            card.healthFill = fill;
            AddOwnSeatBadge(card);
        }

        static void PolishTile(DominoTileView tile)
        {
            Place(tile.defenseIcon.rectTransform,new Vector2(0,.53f),Vector2.one,new Vector2(13,7),new Vector2(-13,-12));
            Place(tile.attackIcon.rectTransform,Vector2.zero,new Vector2(1,.47f),new Vector2(13,13),new Vector2(-13,-7));
            if (tile.latestOutline) return;
            tile.selectionOutline.GetComponent<Image>().raycastTarget = false;
            var latest = Node("LatestOutline",tile.transform);
            Stretch(latest,-3,-3,-3,-3);
            DrawFrame(latest,UIThemeRole.Accent,3);
            latest.gameObject.SetActive(false);
            tile.latestOutline = latest.gameObject;

            var marker = Node("AssistMarker",tile.transform);
            Stretch(marker,2,2,2,2);
            DrawFrame(marker,UIThemeRole.Accent,2);
            var badge = Panel("AssistBadge",marker,UIThemeRole.Background);
            Place(badge.rectTransform,Vector2.one,Vector2.one,new Vector2(-23,-23),new Vector2(-1,-1));
            badge.raycastTarget = false;
            AddAssistCheck(badge.transform);
            marker.gameObject.SetActive(false);
            tile.assistMarker = marker.gameObject;

            var attack = Panel("CarriedAttack",tile.transform,UIThemeRole.Background);
            Place(attack.rectTransform,Vector2.one,Vector2.one,new Vector2(-35,-35),new Vector2(-2,-2));
            attack.raycastTarget = false;
            tile.carriedAttack = Icon("Sword",attack.transform,elemental["Earth"][0]);
            Stretch(tile.carriedAttack.rectTransform,3,3,3,3);
            attack.gameObject.SetActive(false);
        }

        static void DrawFrame(RectTransform root,UIThemeRole role,float thickness)
        {
            Edge("Top",Vector2.up,Vector2.one,new Vector2(0,-thickness),Vector2.zero);
            Edge("Bottom",Vector2.zero,Vector2.right,Vector2.zero,new Vector2(0,thickness));
            Edge("Left",Vector2.zero,Vector2.up,Vector2.zero,new Vector2(thickness,0));
            Edge("Right",Vector2.right,Vector2.one,new Vector2(-thickness,0),Vector2.zero);

            void Edge(string name,Vector2 min,Vector2 max,Vector2 low,Vector2 high)
            {
                var edge = Panel(name,root,role);
                edge.sprite = null;
                edge.raycastTarget = false;
                Place(edge.rectTransform,min,max,low,high);
            }
        }

        static void BuildElementGuide(RectTransform content)
        {
            var root = content.parent.parent.parent.parent;
            if (root.GetComponent<ElementHelpView>()) return;
            var guide = root.gameObject.AddComponent<ElementHelpView>();
            guide.swords = elements.Select(element => elemental[element][0]).ToArray();
            guide.shields = elements.Select(element => elemental[element][1]).ToArray();
            guide.symbols = elements.Select(element => symbols[element]).ToArray();
            guide.firstShields = new Image[elements.Length];
            guide.secondShields = new Image[elements.Length];
            guide.firstNames = new LocalizedText[elements.Length];
            guide.secondNames = new LocalizedText[elements.Length];
            foreach (var child in content.Cast<Transform>().Where(child => child.name.EndsWith("HelpRow")).ToArray())
                Object.DestroyImmediate(child.gameObject);

            Localize(ChildComponent<TMP_Text>(root.gameObject,"TitleLabel"),"elements.title");
            var description = ChildComponent<TMP_Text>(root.gameObject,"DescriptionLabel");
            Localize(description,"elements.hint");
            Size(description,46);
            content.GetComponent<VerticalLayoutGroup>().spacing = 8;
            var headings = Row("ElementHeadings",content,32);
            Label("AttackHeading",headings,"Attack",19,UIThemeRole.Accent,true);
            Label("DefenseHeading",headings,"Defense",19,UIThemeRole.Accent,true);
            SetColumn(headings.GetChild(0));
            SetColumn(headings.GetChild(1),true);
            Localize(headings.GetChild(0).GetComponent<TMP_Text>(),"element.guide.attack");
            Localize(headings.GetChild(1).GetComponent<TMP_Text>(),"element.guide.defense");
            for (int attack = 0; attack < elements.Length; attack++)
            {
                var row = Row(elements[attack] + "Matchup",content,56);
                AddGuideElement(row,"Attack",guide.swords[attack],attack);
                var defenses = Node("Defenses",row);
                Horizontal(defenses,10);
                SetColumn(defenses,true);
                var matches = Enumerable.Range(0,elements.Length)
                    .Where(defense => TileRules.Beats((Element)defense,(Element)attack)).ToArray();
                (guide.firstShields[attack],guide.firstNames[attack]) = AddGuideElement(defenses,"FirstDefense",guide.shields[matches[0]],matches[0]);
                (guide.secondShields[attack],guide.secondNames[attack]) = AddGuideElement(defenses,"SecondDefense",guide.shields[matches[1]],matches[1]);
            }
            var close = content.Find("CloseButton");
            if (!close) close = Button("CloseButton",content,"Got it",true).transform;
            close.SetAsLastSibling();
        }

        static void SetColumn(Transform column,bool wide = false)
        {
            var size = Size(column,-1,wide ? 206 : 110,wide ? 2 : 1);
            size.preferredWidth = size.minWidth;
        }

        static (Image icon, LocalizedText name) AddGuideElement(Transform parent,string name,Sprite sprite,int index)
        {
            var cell = Node(name,parent);
            var layout = Horizontal(cell,4);
            layout.childForceExpandHeight = false;
            Size(cell,54,98,1);
            var icon = Icon("Icon",cell,sprite,46);
            var label = Label("ElementName",cell,elements[index],16);
            Size(label,-1,48,1);
            label.alignment = TextAlignmentOptions.MidlineLeft;
            var binding = TextBinding(label);
            binding.Localize("game.element." + index);
            return (icon,binding);
        }

        static void PolishMatchHud(RectTransform screen,bool tutorial)
        {
            RectTransform Find(string name) => ChildComponent<RectTransform>(screen.gameObject,name);
            if (screen.GetComponentsInChildren<Button>(true).Any(button => button.name == "ToggleTurnsButton"))
            {
                PolishHandHud(screen,tutorial);
                return;
            }

            var top = Find("TopHUDPanel");
            var hand = Find("HandHUDPanel");
            HidePanelBackground(top);
            HidePanelBackground(hand);
            Place(top,Vector2.up,Vector2.one,new Vector2(16,-144),new Vector2(-16,-12));
            var bar = Find("MatchInfoPanel");
            Place(bar,Vector2.up,Vector2.one,new Vector2(0,-42),Vector2.zero);
            var barLayout = bar.GetComponent<HorizontalLayoutGroup>();
            barLayout.spacing = 8;
            barLayout.childAlignment = TextAnchor.MiddleRight;
            Object.DestroyImmediate(Find("TurnLabel").gameObject);
            var timer = Find("TurnTimerLabel");
            Size(timer,42,105);
            var reserve = Find("ReserveCountLabel");
            reserve.SetParent(bar,false);
            reserve.SetSiblingIndex(1);
            Size(reserve,42,110);
            Object.DestroyImmediate(Find("BoardStatusPanel").gameObject);
            var toggle = Button("ToggleTurnsButton",bar,"Compact turns",false,160);
            toggle.transform.SetSiblingIndex(0);
            Localize(toggle.GetComponentInChildren<TMP_Text>(),"game.turns.collapse");
            Size(toggle,42,160);
            FitButtonText(toggle.transform);
            Size(Find("ElementHelpButton"),42,134);
            FitButtonText(Find("ElementHelpButton"));
            Size(Find("PauseButton"),42,90);

            var players = Find("PlayersScrollView").GetComponent<ScrollRect>();
            Stretch((RectTransform)players.transform,0,0,0,46);
            var playerLayout = players.content.GetComponent<HorizontalLayoutGroup>();
            playerLayout.padding = new RectOffset(1,1,1,1);
            playerLayout.spacing = 8;
            playerLayout.childForceExpandHeight = false;
            foreach (var extra in players.content.GetComponentsInChildren<Image>(true)
                .Where(image => image.name == "SeatEmblem" && !PrefabUtility.GetCorrespondingObjectFromSource(image)).ToArray())
                Object.DestroyImmediate(extra.gameObject);
            foreach (var card in players.content.GetComponentsInChildren<PlayerHudCard>(true))
                card.turnIndicator.gameObject.SetActive(true);

            Place(hand,Vector2.zero,Vector2.right,new Vector2(16,10),new Vector2(-16,181));
            var context = Find("HandTitleLabel");
            context.name = "HandContextLabel";
            var contextText = context.GetComponent<TMP_Text>();
            contextText.text = "";
            Place(context,Vector2.up,Vector2.one,new Vector2(1,0),new Vector2(-204,27));
            context.gameObject.SetActive(false);
            Stretch(Find("HandScrollView"),0,3,202,3);
            var handLayout = Find("HandScrollView").GetComponent<ScrollRect>().content.GetComponent<HorizontalLayoutGroup>();
            handLayout.childForceExpandHeight = false;

            var actions = Find("ActionControlsPanel");
            Place(actions,Vector2.right,Vector2.one,new Vector2(-192,4),new Vector2(-4,-4));
            actions.GetComponent<VerticalLayoutGroup>().spacing = 8;
            foreach (var action in actions.GetComponentsInChildren<Button>(true))
            {
                Size(action,46);
                FitButtonText(action.transform);
            }
            var draw = Find("DrawTileButton");
            var drawMarker = Node("DrawAssistMarker",draw);
            Stretch(drawMarker,-3,-3,-3,-3);
            DrawFrame(drawMarker,UIThemeRole.Accent,3);
            drawMarker.gameObject.SetActive(false);

            var board = Find("HistoryScroll");
            Place(board,Vector2.zero,tutorial ? new Vector2(.45f,1) : Vector2.one,
                new Vector2(tutorial ? 8 : 18,tutorial ? 282 : 212),new Vector2(-12,-152));
            if (tutorial)
            {
                var step = Find("TutorialStepPanel");
                step.offsetMin = new Vector2(step.offsetMin.x,282);
                step.offsetMax = new Vector2(step.offsetMax.x,-152);
            }
            PolishHandHud(screen,tutorial);
        }

        static void FitButtonText(Transform button)
        {
            var label = button.GetComponentInChildren<TMP_Text>(true);
            label.fontSizeMin = 16;
            label.fontSizeMax = 21;
            label.enableAutoSizing = true;
        }

        static void HidePanelBackground(RectTransform root)
        {
            root.GetComponent<Image>().raycastTarget = false;
            root.GetComponent<Image>().enabled = false;
        }
    }
}
