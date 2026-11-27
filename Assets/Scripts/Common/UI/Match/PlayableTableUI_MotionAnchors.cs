using DonmniCrash.Game;
using UnityEngine;

namespace DonmniCrash.UI
{
    public abstract partial class PlayableTableUI
    {
        readonly struct FlightAnchor
        {
            public readonly RectTransform rect, clip;
            public readonly bool isTile, top;
            public FlightAnchor(RectTransform rect, bool isTile = false, RectTransform clip = null, bool top = false)
            { this.rect = rect; this.isTile = isTile; this.clip = clip; this.top = top; }
        }

        struct FlightPose
        {
            public Vector2 position, size;
            public Quaternion rotation;
        }

        FlightAnchor MotionAnchor(TileMotion move, bool source)
        {
            var zone = source ? move.From : move.To;
            int seat = source ? move.Sender : move.Receiver;
            int id = move.Tile?.Id ?? -1;
            if (zone == TileZone.Board)
            {
                for (int i = renderedBoard.Count - 1; i >= 0; i--)
                    if (renderedBoard[i].id == id)
                        return new FlightAnchor((RectTransform)renderedBoard[i].view.transform, true);
                return new FlightAnchor(boardArea);
            }
            if (zone == TileZone.Stock)
            {
                if (seat == LocalSeat && move.Choice >= 0 && move.Choice < choiceTiles.Count && choicePanel.activeInHierarchy)
                    return new FlightAnchor((RectTransform)choiceTiles[move.Choice].transform, true, choiceScroll.viewport);
                return new FlightAnchor(stockAnchor);
            }
            if (seat == LocalSeat)
            {
                if (zone == TileZone.Offer && offerPreview.gameObject.activeInHierarchy)
                {
                    var previewTile = offerPreview.GetComponentInChildren<DominoTileView>();
                    if (previewTile) return new FlightAnchor((RectTransform)previewTile.transform, true);
                }
                if (zone == TileZone.Hand && handPanel.activeInHierarchy)
                {
                    if (tileButtons.TryGetValue(id, out var tile))
                        return new FlightAnchor((RectTransform)tile.transform, true, hand.viewport);
                    return new FlightAnchor(hand.viewport);
                }
            }
            var profile = cardBySeat.TryGetValue(seat, out var card) ? card : playerScroll.viewport;
            return new FlightAnchor(profile, clip: playerScroll.viewport, top: source || zone == TileZone.Offer);
        }

        FlightPose Pose(FlightAnchor anchor)
        {
            var rect = anchor.rect;
            if (!rect) rect = flightLayer;
            Vector2 center = rect.rect.center;
            if (anchor.top) center.y = rect.rect.yMax;
            Vector3 world = rect.TransformPoint(center);
            if (anchor.clip)
            {
                var clip = anchor.clip;
                Vector3 inside = clip.InverseTransformPoint(world);
                inside.x = Mathf.Clamp(inside.x, clip.rect.xMin, clip.rect.xMax);
                inside.y = Mathf.Clamp(inside.y, clip.rect.yMin, clip.rect.yMax);
                world = clip.TransformPoint(inside);
            }
            Vector2 position = flightLayer.InverseTransformPoint(world);
            // La mano contraída puede extenderse más allá del área segura.
            if (anchor.clip)
            {
                position.x = Mathf.Clamp(position.x, flightLayer.rect.xMin + 12, flightLayer.rect.xMax - 12);
                position.y = Mathf.Clamp(position.y, flightLayer.rect.yMin + 12, flightLayer.rect.yMax - 12);
            }
            var size = new Vector2(52, 96);
            if (anchor.isTile)
            {
                size.x = flightLayer.InverseTransformVector(rect.TransformVector(Vector3.right * rect.rect.width)).magnitude;
                size.y = flightLayer.InverseTransformVector(rect.TransformVector(Vector3.up * rect.rect.height)).magnitude;
            }
            return new FlightPose { position = position, size = size,
                rotation = anchor.isTile ? Quaternion.Inverse(flightLayer.rotation) * rect.rotation : Quaternion.identity };
        }
    }
}
