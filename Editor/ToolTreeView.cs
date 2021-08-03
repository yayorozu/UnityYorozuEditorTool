using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Yorozu.EditorTools
{
	internal class ToolTreeView : TreeView
	{
		private readonly YorozuToolEditorWindow _editor;

		/// <summary>
		/// Editor 内で Drag してるか
		/// TreeViewにもあるけど判定が切り替わるタイミングがお好みではない
		/// </summary>
		private bool _isDragging;

		public ToolTreeView(TreeViewState state, YorozuToolEditorWindow editor) : base(state)
		{
			_editor = editor;
			showAlternatingRowBackgrounds = true;
			showBorder = true;
			Reload();
		}

		public ToolTreeView(TreeViewState state, MultiColumnHeader multiColumnHeader) : base(state, multiColumnHeader)
		{
		}

		protected override TreeViewItem BuildRoot()
		{
			var root = new TreeViewItem(0, -1);
			foreach (var item in _editor.CurrentModule.GetItems())
				root.AddChild(item);

			SetupDepthsFromParentsAndChildren(root);

			// Childrenが無いとエラーになるので仮作成
			if (!root.hasChildren)
				root.AddChild(new ToolTreeViewItem {id = 0, depth = -1});

			return root;
		}

		private TreeViewItem Find(int id)
		{
			return GetRows().First(i => i.id == id);
		}

		protected override bool DoesItemMatchSearch(TreeViewItem item, string search)
		{
			if (!_editor.CurrentModule.CanSearchDraw(item as ToolTreeViewItem))
				return false;

			return base.DoesItemMatchSearch(item, search);
		}

		protected override void SingleClickedItem(int id)
		{
			_editor.CurrentModule.SingleClick(Find(id) as ToolTreeViewItem);
		}

		protected override void DoubleClickedItem(int id)
		{
			if (_editor.CurrentModule.DoubleClick(Find(id) as ToolTreeViewItem))
			{
				var first = GetRows().First();

				// 選択を一番上に
				if (first != null)
					SetSelection(new List<int> {first.id});
			}
		}

		protected override void SelectionChanged(IList<int> selectedIds)
		{
			if (selectedIds.Count <= 0)
				return;

			_editor.CurrentModule.SelectionChanged(selectedIds.Select(Find).ToArray());
		}

		protected override void RenameEnded(RenameEndedArgs args)
		{
			args.acceptedRename = _editor.CurrentModule.RenameEnded(args.itemID, args.originalName, args.newName);
		}

		protected override bool CanRename(TreeViewItem item) => _editor.CurrentModule.CanRename(item);

		protected override bool CanBeParent(TreeViewItem item) => _editor.CurrentModule.CanBeParent(item);

		protected override bool CanMultiSelect(TreeViewItem item) => false;

		protected override bool CanStartDrag(CanStartDragArgs args) => _editor.CurrentModule.CanDrag;

		protected override void SetupDragAndDrop(SetupDragAndDropArgs args)
		{
			if (args.draggedItemIDs.Count <= 0)
				return;

			var dragObjects = new List<Object>(args.draggedItemIDs.Count);
			dragObjects.AddRange(_editor.CurrentModule.GetDragObjects(args.draggedItemIDs));

			if (dragObjects.Count <= 0)
				return;

			DragAndDrop.PrepareStartDrag();
			DragAndDrop.paths = null;
			DragAndDrop.objectReferences = dragObjects.ToArray();
			DragAndDrop.SetGenericData("Tool", new List<int>(args.draggedItemIDs));
			DragAndDrop.StartDrag(dragObjects.Count > 1 ? "<Multiple>" : dragObjects[0].name);
			_isDragging = true;
		}

		protected override DragAndDropVisualMode HandleDragAndDrop(DragAndDropArgs args)
		{
			// ドラッグ終了
			if (args.performDrop)
			{
				// EditorWindow外であれば
				if (!_isDragging)
				{
					AcceptDragItem();
					return DragAndDropVisualMode.Generic;
				}

				switch (args.dragAndDropPosition)
				{
					case DragAndDropPosition.UponItem:
						var item = args.parentItem as ToolTreeViewItem;
						// 親となれるか
						if (item != null && args.parentItem.depth == 0 && item.CanChangeName)
						{
							var draggedObjects = DragAndDrop.objectReferences;
							if (draggedObjects.Length > 0)
							{
								var guid = draggedObjects[0].GetGUID();
								FavoriteAssetSave.Remove(guid);
								FavoriteAssetSave.AddCategoryChild(item.displayName, guid);
								SetSelection(new List<int> {draggedObjects[0].GetInstanceID()});
							}
						}

						break;

					case DragAndDropPosition.BetweenItems:
					case DragAndDropPosition.OutsideItems:
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
				Reload();
				_isDragging = false;
			}
			// Editor 外のオブジェクトをドラッグしてる場合
			else if (!_isDragging && (DragAndDrop.paths.Length > 0 || DragAndDrop.objectReferences.Length > 0))
			{
				return DragAndDropVisualMode.Copy;
			}

			return DragAndDropVisualMode.Move;
		}

		protected override void ContextClickedItem(int id)
		{
			var @event = Event.current;
			@event.Use();

			var menu = new GenericMenu();
			_editor.CurrentModule.GenerateMenu(Find(id), ref menu);

			// 各所で追加
			menu.ShowAsContext();
		}

		protected override void ContextClicked()
		{
			var @event = Event.current;
			@event.Use();

			var menu = new GenericMenu();
			_editor.CurrentModule.GenerateMenu(ref menu);

			// 各所で追加
			menu.ShowAsContext();
		}

		protected override void RowGUI(RowGUIArgs args)
		{
			var item = (ToolTreeViewItem) args.item;

			base.RowGUI(args);

			if (string.IsNullOrEmpty(item.SubLabel))
				return;

			// 右端にsubLabel を表示
			var rect = args.rowRect;
			var width = rect.x + item.LabelWidth + 20f;

			// 残り幅
			var rest = rect.width - width;

			// 幅
			rect.width = Mathf.Min(rest, item.SubLabelWidth);
			rect.x = rect.x + args.rowRect.width - rect.width;
			GUI.Label(rect, item.SubLabel, EditorStyles.miniLabel);
		}

		/// <summary>
		/// Editor 外からのドラッグの受け入れ
		/// </summary>
		private void AcceptDragItem()
		{
			var paths = DragAndDrop.paths;
			if (paths.Length > 0)
			{
				var guids = paths.Select(AssetDatabase.AssetPathToGUID).ToArray();
				if (_editor.CurrentModule.GetType() == typeof(ShareModule))
					YorozuToolShareObject.Load().Add(guids);
				else
					FavoriteAssetSave.Add(true, guids);
			}

			var objects = DragAndDrop.objectReferences;
			if (objects.Length > 0)
			{
				var data = objects.Select(o => o as GameObject)
					.Where(g => g != null)
					.Select(g => g.transform)
					.Select(HierarchyData.Convert)
					.ToArray();

				FavoriteHierarchySave.Add(data);
			}
		}
	}
}
