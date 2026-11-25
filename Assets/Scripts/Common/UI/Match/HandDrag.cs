using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace DonmniCrash.UI
{
    // Un solo arrastre por mano. El mismo gesto táctil corto sigue perteneciendo al ScrollRect.
    public sealed class HandDrag : MonoBehaviour, IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        ScrollRect scroll;
        RectTransform tile, slot;
        LayoutElement tileLayout;
        PointerEventData pointer;
        Camera pointerCamera;
        Vector2 screenPoint, grabOffset;
        float pressedAt;
        int originalIndex;
        bool scrolling;
        Action moved;

        public void Bind(ScrollRect owner,Action onMoved)
        {
            scroll = owner;
            moved = onMoved;
        }

        public void OnInitializePotentialDrag(PointerEventData data)
        {
            if (pointer != null) return;
            pressedAt = Time.unscaledTime;
            scroll.OnInitializePotentialDrag(data);
        }

        public void OnBeginDrag(PointerEventData data)
        {
            if (pointer != null || data.button != PointerEventData.InputButton.Left) return;
            pointer = data;
            pointerCamera = data.pressEventCamera;
            var hit = data.pointerPressRaycast.gameObject;
            var view = hit ? hit.GetComponentInParent<DominoTileView>() : null;
            bool touch = data is ExtendedPointerEventData input && input.device is Touchscreen;
            scrolling = !view || view.transform.parent != transform || (touch && Time.unscaledTime - pressedAt < .32f);
            if (scrolling) { scroll.OnBeginDrag(data); return; }

            scroll.StopMovement();
            tile = (RectTransform)view.transform;
            originalIndex = tile.GetSiblingIndex();
            tileLayout = tile.GetComponent<LayoutElement>();
            slot = (RectTransform)new GameObject("HandInsertionSlot",typeof(RectTransform),typeof(LayoutElement)).transform;
            slot.SetParent(transform,false);
            var size = slot.GetComponent<LayoutElement>();
            size.preferredWidth = size.minWidth = tile.rect.width;
            size.preferredHeight = size.minHeight = tile.rect.height;
            slot.SetSiblingIndex(originalIndex);
            tileLayout.ignoreLayout = true;
            tile.SetAsLastSibling();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(scroll.content,data.pressPosition,pointerCamera,out var pressed);
            grabOffset = (Vector2)tile.localPosition - pressed;
            screenPoint = data.position;
            data.eligibleForClick = false;
        }

        public void OnDrag(PointerEventData data)
        {
            if (data != pointer) return;
            if (scrolling) scroll.OnDrag(data);
            else screenPoint = data.position;
        }

        void LateUpdate()
        {
            if (!tile) return;
            var viewport = scroll.viewport;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport,screenPoint,pointerCamera,out var cursor);
            float range = scroll.content.rect.width - viewport.rect.width;
            if (range > 0)
            {
                float edge = Mathf.Min(36,viewport.rect.width * .15f);
                float right = Mathf.Clamp01((cursor.x - viewport.rect.xMax + edge) / edge);
                float left = Mathf.Clamp01((viewport.rect.xMin + edge - cursor.x) / edge);
                scroll.horizontalNormalizedPosition = Mathf.Clamp01(scroll.horizontalNormalizedPosition +
                    (right-left) * 520 * Time.unscaledDeltaTime / range);
            }
            PositionSlot();
        }

        void PositionSlot()
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(scroll.content,screenPoint,pointerCamera,out var cursor);
            var position = cursor + grabOffset;
            tile.localPosition = new Vector3(position.x,tile.localPosition.y,0);
            int index = 0;
            foreach (Transform other in transform)
                if (other != slot && other != tile && other.gameObject.activeSelf && other.localPosition.x < position.x) index++;
            slot.SetSiblingIndex(index);
        }

        public void OnEndDrag(PointerEventData data)
        {
            if (data != pointer) return;
            if (scrolling) scroll.OnEndDrag(data);
            if (tile) { screenPoint = data.position; PositionSlot(); }
            bool changed = tile && slot.GetSiblingIndex() != originalIndex;
            Release(true);
            if (changed) moved?.Invoke();
        }

        public void Cancel() => Release(false);
        void OnDisable() => Cancel();

        void Release(bool commit)
        {
            if (tile)
            {
                int index = commit ? slot.GetSiblingIndex() : originalIndex;
                slot.SetAsLastSibling();
                slot.gameObject.SetActive(false);
                Destroy(slot.gameObject);
                tile.SetSiblingIndex(index);
                tileLayout.ignoreLayout = false;
            }
            pointer = null;
            scrolling = false;
            tile = slot = null;
        }
    }
}
