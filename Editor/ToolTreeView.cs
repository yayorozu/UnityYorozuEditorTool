using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Yorozu.EditorTools
{
    internal class ToolTreeView : TreeView
    {
        private YorozuToolEditorWindow _editor;

        public ToolTreeView(TreeViewState state, YorozuToolEditorWindow editor) : base(state)
        {
            _editor = editor;
            showAlternatingRowBackgrounds = true;
            showBorder = true;
            Reload();
        }

        public ToolTreeView(TreeViewState state, MultiColumnHeader multiColumnHeader) : base(state, multiColumnHeader) { }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem(0, -1);
            foreach (var item in _editor.CurrentModule.GetItems())
            {
                root.AddChild(item);
            }

            SetupDepthsFromParentsAndChildren(root);
            // Childrenが無いとエラーになるので仮作成
            if (!root.hasChildren)
            {
                root.AddChild(new ToolTreeViewItem{id = 0, depth = -1});
            }
            return root;
        }

        private TreeViewItem Find(int id) => GetRows().First(i => i.id == id);
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
            _editor.CurrentModule.DoubleClick(Find(id) as ToolTreeViewItem);
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            if (selectedIds.Count <= 0)
                return;

            _editor.CurrentModule.SelectionChanged(selectedIds.Select(Find).ToArray());
        }

        protected override bool CanStartDrag(CanStartDragArgs args)
        {
            return _editor.CurrentModule.CanDrag;
        }

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

        protected override void RowGUI(RowGUIArgs args)
        {
            var item = (ToolTreeViewItem) args.item;

            base.RowGUI(args);
            if (string.IsNullOrEmpty(item.subLabel))
                return;

            // 右端にsubLabel を表示
            var rect = args.rowRect;
            var width = rect.x + item.LabelWidth + 20f;
            // 残り幅
            var rest = rect.width - width;
            // 幅
            rect.width = Mathf.Min(rest, item.SubLabelWidth);
            rect.x = rect.x + args.rowRect.width - rect.width;
            GUI.Label(rect, item.subLabel, EditorStyles.miniLabel);
        }
    }

}
