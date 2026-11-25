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
        [MenuItem("DonmniCrash/Apply tile selection tables")]
        public static void ApplySelectionTables()
        {
            EnsureMatchScenesSaved();
            LoadTablePolishArt();
            EditMatchScenes(AddSelectionTable);
        }

        static void AddSelectionTable(RectTransform screen,bool tutorial)
        {
            if (screen.Find("TileChoicePanel")) return;
            var shade = Panel("TileChoicePanel",screen,UIThemeRole.Background);
            Stretch(shade.rectTransform);
            shade.GetComponent<UIThemeBinding>().opacity = .86f;
            shade.GetComponent<UIThemeBinding>().Apply();
            var card = Panel("ChoiceTable",shade.transform);
            Place(card.rectTransform,new Vector2(.08f,.06f),new Vector2(.92f,.94f),Vector2.zero,Vector2.zero);
            var title = Label("ChoiceTitle",card.transform,"",25,UIThemeRole.Text,true);
            Place(title.rectTransform,Vector2.up,Vector2.one,new Vector2(20,-46),new Vector2(-160,-5));
            var clock = Label("ChoiceClock",card.transform,"",20,UIThemeRole.Accent,true);
            Place(clock.rectTransform,Vector2.one,Vector2.one,new Vector2(-158,-46),new Vector2(-20,-5));
            clock.alignment = TextAlignmentOptions.Right;
            var hint = Label("ChoiceHint",card.transform,"",19);
            Place(hint.rectTransform,Vector2.up,Vector2.one,new Vector2(20,-94),new Vector2(-20,-45));
            hint.enableAutoSizing = true;
            hint.fontSizeMin = 15;
            hint.fontSizeMax = 19;

            var wood = Node("ChoiceSurface",card.transform);
            Stretch(wood,12,68,12,98);
            var texture = wood.gameObject.AddComponent<RawImage>();
            texture.texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Sprites/WoodenTable.jpg");
            texture.color = new Color(.56f,.53f,.5f);
            var grid = Scroll("ChoiceScroll",wood);
            Stretch((RectTransform)grid.parent.parent,8,6,8,6);
            Object.DestroyImmediate(grid.GetComponent<VerticalLayoutGroup>());
            var layout = grid.gameObject.AddComponent<GridLayoutGroup>();
            layout.padding = new RectOffset(5,5,5,5);
            layout.spacing = new Vector2(6,8);
            var columns = grid.gameObject.AddComponent<UIResponsiveGrid>();
            columns.minimumCellWidth = 58;
            columns.maximumColumns = 15;
            columns.cellHeight = 92;
            var preview = Node("OfferPreview",wood);
            Place(preview,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(-44,-72),new Vector2(44,72));

            var actions = Node("ChoiceActions",card.transform);
            Place(actions,Vector2.zero,Vector2.right,new Vector2(16,10),new Vector2(-16,58));
            Horizontal(actions,12);
            foreach (var pair in new[]{("ChoiceContinue","choice.continue"),("OfferPlay","game.play"),("OfferKeep","choice.keep")})
            {
                var action = Button(pair.Item1,actions,LocalizedText.Read(pair.Item2),pair.Item1!="OfferKeep");
                FitButtonText(action.transform);
                Localize(action.GetComponentInChildren<TMP_Text>(true),pair.Item2);
            }
            shade.gameObject.SetActive(false);
            if (!tutorial) return;
            var progress = ChildComponent<RectTransform>(screen.gameObject,"TutorialProgressPanel");
            for (int i=progress.childCount;i<11;i++)
                Object.Instantiate(progress.GetChild(0).gameObject,progress).name = "Step"+(i+1)+"Indicator";
        }
    }
}
