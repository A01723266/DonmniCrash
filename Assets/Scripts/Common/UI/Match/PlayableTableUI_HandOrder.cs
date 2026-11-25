using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DonmniCrash.UI
{
    public abstract partial class PlayableTableUI
    {
        readonly List<int> handOrder = new();
        HandDrag handDrag;

        protected virtual void OnHandReordered() { }

        void RenderHand()
        {
            var tiles = Display.HandOf(LocalSeat);
            if (tiles.Length == handOrder.Count && tiles.All(f => handOrder.Contains(f.Id))) return;
            handDrag.Cancel();
            var byId = tiles.ToDictionary(f => f.Id);
            handOrder.RemoveAll(id => !byId.ContainsKey(id));
            foreach (var tile in tiles)
                if (!handOrder.Contains(tile.Id)) handOrder.Add(tile.Id);
            tileButtons.Clear();
            ClearChildren(hand.content);
            foreach (int id in handOrder) CreateTile(hand.content,byId[id],id,true);
        }

        void RememberHandOrder()
        {
            handOrder.Sort((a,b) => tileButtons[a].transform.GetSiblingIndex().CompareTo(tileButtons[b].transform.GetSiblingIndex()));
            OnHandReordered();
        }
    }
}
