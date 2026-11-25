using System.Collections.Generic;
using DonmniCrash.Game;
using UnityEngine;

namespace DonmniCrash.UI
{
    public abstract partial class PlayableTableUI
    {
        readonly List<(int id, DominoTileView view)> renderedBoard = new();
        BoardPath boardPath = new();
        protected BoardNavigator boardNavigator { get; private set; }
        RectTransform boardArea;

        void ClearBoard(bool resetView)
        {
            ClearChildren(boardNavigator.tiles);
            renderedBoard.Clear();
            boardPath = new BoardPath();
            boardNavigator.ResetBoard(resetView);
        }

        void RenderBoard()
        {
            var history = Display.BoardTiles;
            // Una revisión de red puede sustituir el historial completo sin cambiar su longitud.
            if (renderedBoard.Count > history.Count) ClearBoard(false);
            for (int i = 0; i < renderedBoard.Count; i++)
                if (renderedBoard[i].id != history[i].Id)
                {
                    ClearBoard(false);
                    break;
                }

            if (renderedBoard.Count < history.Count && renderedBoard.Count > 0)
                MarkLatest(renderedBoard[^1].view,false,null);
            for (int i = renderedBoard.Count; i < history.Count; i++)
            {
                var tile = history[i];
                var (center, direction) = boardPath.Append(tile.Id);
                boardNavigator.Include(center,direction);
                var view = CreateTile(boardNavigator.tiles,tile,-1,false);
                PlaceBoardTile(view,center,direction);
                renderedBoard.Add((tile.Id,view));
            }
            if (history.Count > 0)
                MarkLatest(renderedBoard[^1].view,true,history[^1].Kind == TileKind.Standard ? null : Display.Attack);
        }

        static void PlaceBoardTile(DominoTileView view,Vector2 center,Vector2Int direction)
        {
            var rect = (RectTransform)view.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f,.5f);
            rect.sizeDelta = new Vector2(BoardNavigator.Cell,2 * BoardNavigator.Cell);
            rect.anchoredPosition = center * BoardNavigator.Cell;
            float angle = direction.x == 1 ? 90 : direction.y == 1 ? 180 : 0;
            var rotation = Quaternion.Euler(0,0,angle);
            rect.localRotation = rotation;
            // Gira la piedra y conserva legibles los emblemas y el texto de los comodines.
            var upright = Quaternion.Inverse(rotation);
            view.attackIcon.rectTransform.localRotation = upright;
            view.defenseIcon.rectTransform.localRotation = upright;
            view.carriedAttack.rectTransform.localRotation = upright;
            var symbol = rect.Find("TileSymbol") as RectTransform;
            if (symbol)
            {
                symbol.anchorMin = symbol.anchorMax = rect.pivot;
                symbol.sizeDelta = new Vector2(BoardNavigator.Cell,BoardNavigator.Cell);
                symbol.localRotation = upright;
            }
        }

        void MarkLatest(DominoTileView tile,bool latest,Element? carriedAttack)
        {
            tile.latestOutline.SetActive(latest);
            tile.carriedAttack.transform.parent.gameObject.SetActive(carriedAttack.HasValue);
            if (carriedAttack.HasValue) tile.carriedAttack.sprite = swords[(int)carriedAttack.Value];
        }
    }
}
