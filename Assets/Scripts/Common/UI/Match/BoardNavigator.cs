using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DonmniCrash.UI
{
    /// <summary>Encuadre local de la mesa. Las coordenadas del recorrido nunca dependen del tamaño de pantalla.</summary>
    public sealed class BoardNavigator : MonoBehaviour, IDragHandler
    {
        public enum Gesture { FreeView, Follow, ZoomIn, ZoomOut }
        public event System.Action<Gesture> Navigated;
        public RectTransform tiles, followArea;
        public RawImage wood;
        public Button followButton, zoomIn, zoomOut;
        public LocalizedText followLabel;

        public const float Cell = 72f;
        const float Margin = 4 * Cell, Growth = 6 * Cell, WoodPeriod = 1024f;
        static readonly Rect InitialBounds = Rect.MinMaxRect(-Margin - Cell / 2, -8 * Cell, 24 * Cell, 8 * Cell);
        Rect bounds = InitialBounds;
        RectTransform viewport;
        Vector2 focus, latest;
        bool following = true;
        float zoom = 1;

        void Awake()
        {
            viewport = (RectTransform)transform;
            followButton.onClick.AddListener(ToggleFollow);
            zoomIn.onClick.AddListener(() => ChangeZoom(1.25f));
            zoomOut.onClick.AddListener(() => ChangeZoom(.8f));
        }

        public void ResetBoard(bool resetView)
        {
            bounds = InitialBounds;
            latest = Vector2.zero;
            if (!resetView) return;
            focus = Vector2.zero;
            zoom = 1;
            following = true;
            RefreshLabel();
        }

        public void Include(Vector2 center, Vector2Int direction)
        {
            latest = center * Cell;
            var half = direction.x == 0 ? new Vector2(Cell / 2, Cell) : new Vector2(Cell, Cell / 2);
            var min = latest - half - Vector2.one * Margin;
            var max = latest + half + Vector2.one * Margin;
            bounds.yMin -= Expansion(bounds.yMin - min.y);
            bounds.yMax += Expansion(max.y - bounds.yMax);
            bounds.xMax += Expansion(max.x - bounds.xMax);
        }

        static float Expansion(float overflow) => Mathf.Ceil(Mathf.Max(0, overflow) / Growth) * Growth;

        void ToggleFollow()
        {
            following = !following;
            RefreshLabel();
            Navigated?.Invoke(following ? Gesture.Follow : Gesture.FreeView);
        }

        void RefreshLabel() => followLabel.Localize(following ? "board.free" : "board.follow");

        float MinimumZoom => Mathf.Max(viewport.rect.width / bounds.width, viewport.rect.height / bounds.height);
        float MaximumZoom => Mathf.Max(2f, MinimumZoom);

        void ChangeZoom(float factor)
        {
            float previous = zoom;
            zoom = Mathf.Clamp(zoom * factor, MinimumZoom, MaximumZoom);
            if (!Mathf.Approximately(previous,zoom))
                Navigated?.Invoke(factor > 1 ? Gesture.ZoomIn : Gesture.ZoomOut);
        }

        public void OnDrag(PointerEventData input)
        {
            if (following || input.button != PointerEventData.InputButton.Left) return;
            var camera = input.pressEventCamera;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport,input.position,camera,out var current)
                && RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport,input.position - input.delta,camera,out var before))
                focus = Constrain(focus - (current - before) / zoom);
        }

        Vector2 Constrain(Vector2 position)
        {
            var half = viewport.rect.size * (.5f / zoom);
            return new Vector2(Mathf.Clamp(position.x,bounds.xMin + half.x,bounds.xMax - half.x),
                Mathf.Clamp(position.y,bounds.yMin + half.y,bounds.yMax - half.y));
        }

        void LateUpdate()
        {
            if (viewport.rect.width <= 0 || viewport.rect.height <= 0) return;
            zoom = Mathf.Clamp(zoom, MinimumZoom, MaximumZoom);
            zoomIn.interactable = zoom < MaximumZoom - .001f;
            zoomOut.interactable = zoom > MinimumZoom + .001f;
            focus = Constrain(focus);
            if (following)
            {
                Vector2 clearCenter = viewport.InverseTransformPoint(followArea.TransformPoint(followArea.rect.center));
                var target = Constrain(latest - (clearCenter - viewport.rect.center) / zoom);
                focus = (target - focus).sqrMagnitude < .01f ? target :
                    Vector2.Lerp(focus,target,1 - Mathf.Exp(-10 * Time.unscaledDeltaTime));
            }
            tiles.localScale = new Vector3(zoom,zoom,1);
            tiles.anchoredPosition = -focus * zoom;
            UpdateWood();
        }

        void UpdateWood()
        {
            // La madera ocupa la pantalla completa, también detrás del HUD. Su origen acompaña a las fichas.
            var surface = wood.rectTransform;
            Vector2 lower = viewport.InverseTransformPoint(surface.TransformPoint(surface.rect.min));
            Vector2 upper = viewport.InverseTransformPoint(surface.TransformPoint(surface.rect.max));
            Vector2 origin = (focus + (lower - viewport.rect.center) / zoom) / WoodPeriod;
            wood.uvRect = new Rect(origin,(upper - lower) / (zoom * WoodPeriod));
        }
    }
}
