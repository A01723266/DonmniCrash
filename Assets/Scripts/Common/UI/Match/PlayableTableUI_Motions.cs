using System;
using System.Collections.Generic;
using DonmniCrash.Game;
using UnityEngine;

namespace DonmniCrash.UI
{
    public abstract partial class PlayableTableUI
    {
        sealed class Flight
        {
            public TileMotion move;
            public DominoTileView ghost;
            public FlightPose start;
            public FlightAnchor destination;
            public CanvasGroup landing;
            public float spread;
            public bool landed;
        }

        readonly List<Flight> flights = new();
        RectTransform flightLayer, stockAnchor;
        double flightStart;
        int flightDuration;
        protected virtual double MotionElapsedMilliseconds(int duration) => (Time.unscaledTimeAsDouble - flightStart) * 1000d;
        Action afterFlights;
        protected bool MovingTiles => flights.Count != 0;

        void BindMotions()
        {
            flightLayer = (RectTransform)new GameObject("TileFlights", typeof(RectTransform), typeof(CanvasGroup)).transform;
            flightLayer.SetParent(FindNode("MatchScreen"), false);
            flightLayer.anchorMin = Vector2.zero;
            flightLayer.anchorMax = Vector2.one;
            flightLayer.offsetMin = flightLayer.offsetMax = Vector2.zero;
            var group = flightLayer.GetComponent<CanvasGroup>();
            group.interactable = group.blocksRaycasts = false;
            stockAnchor = ComponentOf<RectTransform>("ReserveCountLabel");
        }

        // Las vistas de origen siguen en su lugar hasta que Render reconstruye la mano y el selector.
        protected void CaptureFlights(IReadOnlyList<TileMotion> moves, int remainingMilliseconds)
        {
            CancelFlights();
            if (moves.Count == 0 || remainingMilliseconds <= 0) return;
            Canvas.ForceUpdateCanvases();
            flightDuration = (moves[moves.Count - 1].Stage + 1) * TileMotion.StageMilliseconds;
            flightStart = Time.unscaledTimeAsDouble - (flightDuration - remainingMilliseconds) / 1000d;
            handDrag.enabled = false;
            var transferCounts = new Dictionary<int, int>();
            foreach (var move in moves)
                if (IsSwap(move)) transferCounts[move.Sender] = transferCounts.GetValueOrDefault(move.Sender) + 1;
            var transferIndices = new Dictionary<int, int>();
            foreach (var movement in moves)
            {
                var move = movement.ForSeat(LocalSeat);
                var origin = MotionAnchor(move, true);
                var ghost = CreateTile(flightLayer, move.Tile, -1, false);
                var rect = (RectTransform)ghost.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
                float spread = 0;
                if (IsSwap(move))
                {
                    int i = transferIndices.GetValueOrDefault(move.Sender);
                    transferIndices[move.Sender] = i + 1;
                    int count = transferCounts[move.Sender];
                    if (count > 1) spread = (i / (count - 1f) - .5f) * Mathf.Min(64, (count - 1) * 10);
                }
                flights.Add(new Flight { move = move, start = Pose(origin), ghost = ghost, spread = spread });
            }
        }

        protected void LandFlights(Action completed = null)
        {
            if (!MovingTiles) { completed?.Invoke(); return; }
            afterFlights = completed;
            Canvas.ForceUpdateCanvases();
            foreach (var flight in flights)
            {
                var target = MotionAnchor(flight.move, false);
                flight.destination = target;
                if (!target.isTile) continue;
                var group = target.rect.GetComponent<CanvasGroup>();
                if (!group) group = target.rect.gameObject.AddComponent<CanvasGroup>();
                group.alpha = 0;
                flight.landing = group;
            }
            AdvanceFlights();
        }

        static bool IsSwap(TileMotion move) => move.From == TileZone.Hand && move.To == TileZone.Hand;

        void AdvanceFlights()
        {
            float elapsed = (float)(MotionElapsedMilliseconds(flightDuration) / TileMotion.StageMilliseconds);
            bool placed = false;
            foreach (var flight in flights)
            {
                if (flight.landed) continue;
                float progress = elapsed - flight.move.Stage;
                if (progress >= 1)
                {
                    if (flight.landing) flight.landing.alpha = 1;
                    flight.ghost.gameObject.SetActive(false);
                    flight.landed = true;
                    placed |= flight.move.To == TileZone.Board;
                    continue;
                }
                // Las fichas propias que saldrán en la segunda etapa siguen a la vista en su origen.
                flight.ghost.gameObject.SetActive(progress >= 0 || flight.move.Sender == LocalSeat);
                var end = Pose(flight.destination);
                float t = Mathf.SmoothStep(0, 1, Mathf.Clamp01(progress));
                var rect = (RectTransform)flight.ghost.transform;
                Vector2 path = end.position - flight.start.position;
                Vector2 position = flight.start.position + path * t;
                if (IsSwap(flight.move))
                {
                    // Los sentidos opuestos describen dos arcos y cada mano se abre en abanico.
                    float arc = 4 * t * (1 - t);
                    position += arc * (Vector2.Perpendicular(path.normalized) * 24 + Vector2.right * flight.spread);
                }
                rect.localPosition = position;
                rect.sizeDelta = Vector2.Lerp(flight.start.size, end.size, t);
                rect.localRotation = Quaternion.Slerp(flight.start.rotation, end.rotation, t);
                if (flight.move.To == TileZone.Board)
                {
                    var upright = Quaternion.Inverse(rect.localRotation);
                    flight.ghost.attackIcon.rectTransform.localRotation = upright;
                    flight.ghost.defenseIcon.rectTransform.localRotation = upright;
                    var symbol = rect.Find("TileSymbol");
                    if (symbol) symbol.localRotation = upright;
                }
            }
            if (placed) GameAudio.Current?.PlayPlacement();
            if (!flights[flights.Count - 1].landed) return;
            var completed = afterFlights;
            CancelFlights();
            RenderActions();
            completed?.Invoke();
        }

        protected void CancelFlights()
        {
            foreach (var flight in flights)
            {
                if (flight.landing) flight.landing.alpha = 1;
                if (flight.ghost)
                {
                    flight.ghost.gameObject.SetActive(false);
                    Destroy(flight.ghost.gameObject);
                }
            }
            flights.Clear();
            afterFlights = null;
            if (handDrag) handDrag.enabled = true;
        }
    }
}
