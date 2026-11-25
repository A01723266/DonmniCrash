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
        [MenuItem("DonmniCrash/Apply explorable board")]
        public static void ApplyBoardNavigation()
        {
            EnsureMatchScenesSaved();
            LoadTablePolishArt();
            EditMatchScenes((screen,tutorial) => AddBoardNavigation(screen));
        }

        static void AddBoardNavigation(RectTransform screen)
        {
            if (screen.Find("BoardArea")) return;
            var area = (RectTransform)screen.Find("HistoryScroll");
            var scroll = area.GetComponent<ScrollRect>();
            var viewport = scroll.viewport;
            var tiles = scroll.content;
            Object.DestroyImmediate(scroll);
            Object.DestroyImmediate(tiles.GetComponent<ContentSizeFitter>());
            Object.DestroyImmediate(tiles.GetComponent<HorizontalLayoutGroup>());
            area.name = "BoardArea";
            viewport.name = "BoardViewport";
            tiles.name = "BoardTiles";
            tiles.anchorMin = tiles.anchorMax = tiles.pivot = new Vector2(.5f,.5f);
            tiles.sizeDelta = tiles.anchoredPosition = Vector2.zero;
            area.offsetMin = new Vector2(140,area.offsetMin.y);

            var surface = GameObject.Find("BoardRoot").transform.Find("TableSurface");
            var original = surface.GetComponent<Image>();
            var texture = original.sprite.texture;
            var tint = original.color;
            Object.DestroyImmediate(original);
            var wood = surface.gameObject.AddComponent<RawImage>();
            wood.texture = texture;
            wood.color = tint;
            wood.raycastTarget = false;

            var board = viewport.gameObject.AddComponent<BoardNavigator>();
            board.tiles = tiles;
            board.wood = wood;
            var tools = Node("BoardControls",screen);
            var left = new Vector2(0,.5f);
            Place(tools,left,left,new Vector2(8,-72),new Vector2(132,72));
            Vertical(tools,0,6);
            board.followButton = BoardButton(tools,"FollowBoardButton","board.free");
            board.followLabel = board.followButton.GetComponentInChildren<LocalizedText>(true);
            board.zoomIn = BoardButton(tools,"ZoomInButton","board.zoom.in");
            board.zoomOut = BoardButton(tools,"ZoomOutButton","board.zoom.out");
        }

        static Button BoardButton(Transform tools,string name,string key)
        {
            var button = Button(name,tools,LocalizedText.Read(key));
            FitButtonText(button.transform);
            Size(button,44);
            var label = button.GetComponentInChildren<TMP_Text>(true);
            Stretch(label.rectTransform,4,2,4,2);
            Localize(label,key);
            return button;
        }
    }
}
