using System.Linq;
using DonmniCrash.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DonmniCrash.EditorTools
{
    public static partial class DonmniCrashUIAuthoring
    {
        const string DiceArt = "Assets/Kenney/UI/kenney_boardgame-pack/PNG/Dice/";
        const string CounterEmojiAsset = "Assets/UI/Sprites/CounterEmoji.asset";
        const string CounterEmojiImage = "Assets/UI/Sprites/CounterEmoji.png";

        [MenuItem("DonmniCrash/Apply opening roll and counters")]
        public static void ApplyOpeningPresentation()
        {
            EnsureMatchScenesSaved();
            LoadTablePolishArt();
            EditMatchScenes(PolishOpeningPresentation);
            AssetDatabase.SaveAssets();
        }

        static void PolishOpeningPresentation(RectTransform screen,bool tutorial)
        {
            var top = ChildComponent<RectTransform>(screen.gameObject,"TopHUDPanel");
            if (!top.Find("MatchCounters"))
            {
                Place(top,Vector2.up,Vector2.one,new Vector2(16,-180),new Vector2(-16,-6));
                var controls = ChildComponent<RectTransform>(top.gameObject,"MatchInfoPanel");
                Place(controls,Vector2.up,Vector2.one,new Vector2(0,-38),Vector2.zero);
                foreach (var button in controls.GetComponentsInChildren<Button>(true)) Size(button,38);
                var players = ChildComponent<RectTransform>(top.gameObject,"PlayersScrollView");
                Stretch(players,0,48,0,42);

                var counters = Node("MatchCounters",top);
                Place(counters,new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(-236,0),new Vector2(236,42));
                var layout = Horizontal(counters,12);
                layout.childAlignment = TextAnchor.MiddleCenter;

                Counter(counters,"ClockCounter",ChildComponent<TMP_Text>(controls.gameObject,"TurnTimerLabel"));
                Counter(counters,"ReserveCounter",ChildComponent<TMP_Text>(controls.gameObject,"ReserveCountLabel"));

                var board = ChildComponent<RectTransform>(screen.gameObject,"HistoryScroll");
                board.offsetMax = new Vector2(board.offsetMax.x,-188);
                if (tutorial)
                {
                    var step = ChildComponent<RectTransform>(screen.gameObject,"TutorialStepPanel");
                    step.offsetMax = new Vector2(step.offsetMax.x,-188);
                }
            }

            var emoji = LoadCounterEmoji();
            CenterCounter(ChildComponent<TMP_Text>(top.gameObject,"TurnTimerLabel"),emoji,"⏱");
            CenterCounter(ChildComponent<TMP_Text>(top.gameObject,"ReserveCountLabel"),emoji,"📦");

            if (tutorial) PolishTutorialGuide(screen);
            else
            {
                var overlays = screen.parent.parent.Find("Overlays");
                if (!overlays.Find("OpeningRollOverlay")) AddOpeningRollOverlay(overlays);
            }
        }

        static void Counter(Transform parent,string name,TMP_Text value)
        {
            var background = Panel(name,parent,UIThemeRole.Elevated);
            background.raycastTarget = false;
            Size(background,42,230);
            value.rectTransform.SetParent(background.transform,false);
        }

        static void CenterCounter(TMP_Text value,TMP_SpriteAsset emoji,string prefix)
        {
            var oldIcon = value.transform.parent.Find("Icon");
            if (oldIcon) Object.DestroyImmediate(oldIcon.gameObject);
            Stretch(value.rectTransform,10,2,10,2);
            value.font = semibold;
            value.fontSize = 25;
            value.fontSizeMin = 19;
            value.fontSizeMax = 25;
            value.enableAutoSizing = true;
            value.textWrappingMode = TextWrappingModes.NoWrap;
            value.alignment = TextAlignmentOptions.Center;
            value.spriteAsset = emoji;
            value.tintAllSprites = false;
            if (!value.text.StartsWith(prefix)) value.text = prefix + " " + value.text;
            Bind(value,UIThemeRole.Text);
        }

        static TMP_SpriteAsset LoadCounterEmoji()
        {
            var asset = AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>(CounterEmojiAsset);
            if (asset) return asset;

            asset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();
            asset.spriteSheet = AssetDatabase.LoadAssetAtPath<Texture2D>(CounterEmojiImage);
            AssetDatabase.CreateAsset(asset,CounterEmojiAsset);
            var serialized = new SerializedObject(asset);
            serialized.FindProperty("m_Version").stringValue = "1.1.0";
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AddEmoji(0,0x23F1,"Stopwatch");
            AddEmoji(1,0x1F4E6,"Package");
            asset.material = new Material(Shader.Find("TextMeshPro/Sprite")) { name = "CounterEmoji Material", mainTexture = asset.spriteSheet };
            AssetDatabase.AddObjectToAsset(asset.material,asset);
            asset.UpdateLookupTables();
            EditorUtility.SetDirty(asset);
            return asset;

            void AddEmoji(uint index,uint unicode,string name)
            {
                var glyph = new TMP_SpriteGlyph(index,new GlyphMetrics(64,64,0,53,66),
                    new GlyphRect((int)index * 64,0,64,64),1,0);
                asset.spriteGlyphTable.Add(glyph);
                asset.spriteCharacterTable.Add(new TMP_SpriteCharacter(unicode,asset,glyph) { name = name, scale = .84f });
            }
        }

        static void AddOpeningRollOverlay(Transform overlays)
        {
            var root = Node("OpeningRollOverlay",overlays);
            Stretch(root);
            var dim = root.gameObject.AddComponent<Image>();
            dim.color = new Color(0,0,0,.76f);

            var panel = Panel("OpeningRollPanel",root);
            Place(panel.rectTransform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(-410,-115),new Vector2(410,115));
            var width = panel.gameObject.AddComponent<UIWidthConstraint>();
            width.maximumWidth = 820;
            width.horizontalMargin = 18;

            var view = root.gameObject.AddComponent<OpeningRollView>();
            view.title = Label("OpeningStageLabel",panel.transform,"Opening dice",30,UIThemeRole.Accent,true);
            Place(view.title.rectTransform,Vector2.up,Vector2.one,new Vector2(16,-67),new Vector2(-16,-15));
            view.title.alignment = TextAlignmentOptions.Center;
            view.summary = Label("OpeningSummaryLabel",panel.transform,"Waiting for the table to load",24);
            Stretch(view.summary.rectTransform,24,22,24,70);
            view.summary.alignment = TextAlignmentOptions.Center;
            view.summary.richText = false;
            view.summary.fontSizeMin = 18;
            view.summary.fontSizeMax = 24;
            view.summary.enableAutoSizing = true;

            var row = Node("OpeningDiceRow",panel.transform);
            Stretch(row,16,19,16,76);
            var layout = Horizontal(row,8);
            layout.childAlignment = TextAnchor.MiddleCenter;
            view.dice = new Image[6];
            view.names = new TMP_Text[6];
            view.diceRow = row.gameObject;
            view.redFaces = LoadDiceFaces("Red");
            view.whiteFaces = LoadDiceFaces("White");

            for (int i = 0; i < view.dice.Length; i++)
            {
                var card = Panel("OpeningDie"+(i+1),row,UIThemeRole.Elevated);
                card.raycastTarget = false;
                Size(card,127,64,1);
                var face = Icon("DieFace",card.transform,view.whiteFaces[0]);
                Place(face.rectTransform,new Vector2(0,.34f),Vector2.one,new Vector2(8,0),new Vector2(-8,-10));
                view.dice[i] = face;
                var name = Label("DiePlayerName",card.transform,"Player",19,UIThemeRole.Text,true);
                Place(name.rectTransform,Vector2.zero,new Vector2(1,.34f),new Vector2(3,5),new Vector2(-3,0));
                name.alignment = TextAlignmentOptions.Center;
                name.richText = false;
                name.textWrappingMode = TextWrappingModes.NoWrap;
                name.fontSizeMin = 13;
                name.fontSizeMax = 19;
                name.enableAutoSizing = true;
                view.names[i] = name;
            }
            row.gameObject.SetActive(false);
            root.gameObject.SetActive(false);
        }

        static Sprite[] LoadDiceFaces(string color) => Enumerable.Range(1,6)
            .Select(face => Sprites(DiceArt + "die" + color + face + ".png")
                .OrderByDescending(sprite => sprite.rect.width * sprite.rect.height).First()).ToArray();
    }
}
