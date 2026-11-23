using UnityEngine;
using UnityEngine.UI;

namespace DonmniCrash.UI
{
    [ExecuteAlways, RequireComponent(typeof(GridLayoutGroup))]
    public sealed partial class UIResponsiveGrid : MonoBehaviour
    {
        public float minimumCellWidth = 310;
        public float cellHeight = 120;
        public int maximumColumns = 2;
        float previousWidth = -1;
        void OnEnable() => Refresh();
        void OnRectTransformDimensionsChange() => Refresh();
        void Update() { if (!Mathf.Approximately(previousWidth, ((RectTransform)transform).rect.width)) Refresh(); }
        void Refresh()
        {
            var grid = GetComponent<GridLayoutGroup>();
            if (!grid) return;
            previousWidth = ((RectTransform)transform).rect.width;
            float width = Mathf.Max(1, previousWidth - grid.padding.horizontal);
            int columns = Mathf.Clamp(Mathf.FloorToInt((width + grid.spacing.x) / (minimumCellWidth + grid.spacing.x)), 1, maximumColumns);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columns;
            grid.cellSize = new Vector2(Mathf.Max(1, (width - grid.spacing.x * (columns - 1)) / columns), cellHeight);
        }
    }
}
