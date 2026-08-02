#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.Editor.Utilities.Messaging
{
	public class SimpleEditorTableView<TData>
	{
		private MultiColumnHeaderState _multiColumnHeaderState;
		private MultiColumnHeader _multiColumnHeader;
		private MultiColumnHeaderState.Column[] _columns;
		private readonly Color _lighterColor = Color.white * 0.7f;
		private readonly Color _darkerColor = Color.white * 0.5f;

		private Vector2 _scrollPosition;
		private bool _columnResized;
		private bool _isSettingSortProgrammatically;

		public delegate void DrawItem(Rect rect, TData item);
		public delegate void OnRowClick(TData item);
		public delegate Color GetRowBackgroundColor(TData item, int rowIndex);
		public delegate void OnSortingChanged(int columnIndex, bool ascending);
		public delegate void OnContextMenu(TData item, int columnIndex, Rect cellRect);

		public class ColumnDef
		{
			internal MultiColumnHeaderState.Column column;
			internal DrawItem onDraw;
			internal Comparison<TData> onSort;

			public ColumnDef SetMaxWidth(float maxWidth)
			{
				column.maxWidth = maxWidth;
				return this;
			}

			public ColumnDef SetTooltip(string tooltip)
			{
				column.headerContent.tooltip = tooltip;
				return this;
			}

			public ColumnDef SetAutoResize(bool autoResize)
			{
				column.autoResize = autoResize;
				return this;
			}

			public ColumnDef SetAllowToggleVisibility(bool allow)
			{
				column.allowToggleVisibility = allow;
				return this;
			}

			public ColumnDef SetSorting(Comparison<TData> onSort)
			{
				this.onSort = onSort;
				column.canSort = true;
				return this;
			}
		}

		private readonly List<ColumnDef> _columnDefs = new List<ColumnDef>();
		private OnRowClick _rowClickCallback;
		private GetRowBackgroundColor _rowBackgroundColorCallback;
		private OnSortingChanged _sortingChangedCallback;
		private OnContextMenu _contextMenuCallback;

		public void ClearColumns()
		{
			_columnDefs.Clear();
			_columnResized = true;
		}

		public ColumnDef AddColumn(string title, int minWidth, DrawItem onDrawItem)
		{
			var columnDef = new ColumnDef
			{
				column = new MultiColumnHeaderState.Column
				{
					allowToggleVisibility = false,
					autoResize = true,
					minWidth = minWidth,
					canSort = false,
					sortingArrowAlignment = TextAlignment.Right,
					headerContent = new GUIContent(title),
					headerTextAlignment = TextAlignment.Left,
				},
				onDraw = onDrawItem
			};

			_columnDefs.Add(columnDef);
			_columnResized = true;
			return columnDef;
		}

		public void SetRowClickCallback(OnRowClick callback)
		{
			_rowClickCallback = callback;
		}

		public void SetRowBackgroundColorCallback(GetRowBackgroundColor callback)
		{
			_rowBackgroundColorCallback = callback;
		}

		public void SetSortingChangedCallback(OnSortingChanged callback)
		{
			_sortingChangedCallback = callback;
		}

		public void SetContextMenuCallback(OnContextMenu callback)
		{
			_contextMenuCallback = callback;
		}

		private void ReBuild()
		{
			_columns = _columnDefs.Select(def => def.column).ToArray();
			_multiColumnHeaderState = new MultiColumnHeaderState(_columns);
			_multiColumnHeader = new MultiColumnHeader(_multiColumnHeaderState);
			_multiColumnHeader.visibleColumnsChanged += multiColumnHeader => multiColumnHeader.ResizeToFit();
			_multiColumnHeader.sortingChanged += OnHeaderSortingChanged;
			_multiColumnHeader.ResizeToFit();
			_columnResized = false;
		}

		private void OnHeaderSortingChanged(MultiColumnHeader header)
		{
			if (_isSettingSortProgrammatically)
				return;

			if (_sortingChangedCallback == null)
				return;

			int sortIndex = header.sortedColumnIndex;
			if (sortIndex >= 0)
			{
				bool ascending = header.IsSortedAscending(sortIndex);
				_sortingChangedCallback.Invoke(sortIndex, ascending);
			}
		}

		/// <summary>Draws with no height cap and auto row height.</summary>
		public void DrawTableGUI(TData[] data) => DrawTableGUI(data, float.MaxValue, -1);

		/// <summary>Draws with auto row height.</summary>
		public void DrawTableGUI(TData[] data, float maxHeight) => DrawTableGUI(data, maxHeight, -1);

		public void DrawTableGUI(TData[] data, float maxHeight, float rowHeight)
{
    if (_multiColumnHeader == null || _columnResized)
        ReBuild();

    if (rowHeight < 0)
        rowHeight = EditorGUIUtility.singleLineHeight;

    // Get the actual raw width demanded by your active columns
    float totalColumnsWidth = _multiColumnHeaderState.widthOfAllVisibleColumns;

    // FIX 1: Allow the header rect to dynamically take up the available layout width (0 to 10000)
    // This stops the layout system from fighting with the parent container's width.
    Rect headerRect = GUILayoutUtility.GetRect(0, 10000, rowHeight, rowHeight);
    
    // FIX 2: Pass '_scrollPosition.x' to xScroll so column headers shift correctly when scrolling horizontally
    _multiColumnHeader!.OnGUI(headerRect, xScroll: _scrollPosition.x);

    float desiredHeight = rowHeight * data.Length + GUI.skin.horizontalScrollbar.fixedHeight;
    float displayHeight = Mathf.Min(desiredHeight, maxHeight);
    
    // FIX 3: Let the viewport rect naturally fill the container width
    Rect scrollViewPos = GUILayoutUtility.GetRect(0, 10000, 0, displayHeight);
    Rect viewRect = new Rect(0, 0, totalColumnsWidth, desiredHeight);

    _scrollPosition = GUI.BeginScrollView(
        position: scrollViewPos,
        scrollPosition: _scrollPosition,
        viewRect: viewRect,
        alwaysShowHorizontal: false,
        alwaysShowVertical: false);

    // FIX 4: REMOVED EditorGUILayout.BeginVertical() / EndVertical() from this section.
    // We are using absolute coordinate drawing via rowRect/cellRect here, so layout blocks must not be mixed in.

    // PERF: Row virtualization. Only iterate rows whose rowRect intersects the visible
    // viewport [_scrollPosition.y, _scrollPosition.y + displayHeight]. Rows outside this
    // range are clipped by the scroll view and cannot receive mouse events anyway, so
    // skipping them is behavior-preserving while avoiding per-row onDraw/cell work.
    int firstVisibleRow = Mathf.Max(0, Mathf.FloorToInt(_scrollPosition.y / rowHeight) - 1);
    int lastVisibleRow = Mathf.Min(data.Length - 1, Mathf.CeilToInt((_scrollPosition.y + displayHeight) / rowHeight) + 1);

    for (int row = firstVisibleRow; row <= lastVisibleRow; row++)
    {
        Rect rowRect = new Rect(0, rowHeight * row, totalColumnsWidth, rowHeight);

        Color rowColor = _rowBackgroundColorCallback != null
            ? _rowBackgroundColorCallback.Invoke(data[row], row)
            : row % 2 == 0 ? _darkerColor : _lighterColor;
        
        // Only draw the rect background if it isn't completely transparent
        if (rowColor != Color.clear)
        {
            EditorGUI.DrawRect(rowRect, rowColor);
        }

        for (int col = 0; col < _columns.Length; col++)
        {
            if (!_multiColumnHeader.IsColumnVisible(col))
                continue;

            int visibleColumnIndex = _multiColumnHeader.GetVisibleColumnIndex(col);
            Rect cellRect = _multiColumnHeader.GetCellRect(visibleColumnIndex, rowRect);
            
            _columnDefs[col].onDraw(cellRect, data[row]);

            if (_contextMenuCallback != null
                && Event.current.type == EventType.MouseDown
                && Event.current.button == 1
                && cellRect.Contains(Event.current.mousePosition))
            {
                _contextMenuCallback.Invoke(data[row], col, cellRect);
                Event.current.Use();
            }
        }

        if (_rowClickCallback != null
            && Event.current.type == EventType.MouseUp
            && Event.current.button == 0
            && GUIUtility.hotControl == 0
            && rowRect.Contains(Event.current.mousePosition))
        {
            _rowClickCallback.Invoke(data[row]);
            Event.current.Use();
        }
    }

    GUI.EndScrollView(handleScrollWheel: true);
}

		public void SetSortedColumn(int columnIndex, bool ascending)
		{
			if (_multiColumnHeader == null || _columnResized)
				ReBuild();

			if (columnIndex < 0 || columnIndex >= _columnDefs.Count || _multiColumnHeader == null)
				return;

			_isSettingSortProgrammatically = true;
			try
			{
				_columnDefs[columnIndex].column.sortedAscending = ascending;

				if (columnIndex < _columns.Length)
					_columns[columnIndex].sortedAscending = ascending;

				_multiColumnHeader.sortedColumnIndex = columnIndex;
			}
			finally
			{
				_isSettingSortProgrammatically = false;
			}
		}
	}
}
#endif
