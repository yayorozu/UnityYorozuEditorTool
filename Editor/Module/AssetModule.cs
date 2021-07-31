using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Yorozu.EditorTools
{
    [Serializable]
    internal class AssetModule : Module
    {
        internal override string Name => "AssetLog";
        internal override Texture Texture => EditorResources.Load<Texture>(EditorGUIUtility.isProSkin ? "d_Project" : "Project");
        internal override bool CanDrag => true;

        internal override void Enter()
        {
            SelectionLog.UpdateProjectLog += Reload;
        }

        internal override void Exit()
        {
            SelectionLog.UpdateProjectLog -= Reload;
        }

        internal override List<ToolTreeViewItem> GetItems()
        {
            var list = new List<ToolTreeViewItem>();
            foreach (var guid in SelectionLog.ProjectLogs)
            {
                var item = GUIDToItem(guid);
                if (item != null)
                    list.Add(item);
            }

            return list;
        }

        internal override bool DoubleClick(ToolTreeViewItem item)
        {
            OpenAsset(item);
            return true;
        }

        internal override void SelectionChanged(TreeViewItem[] items) => SelectObject(items);

        internal override void GenerateMenu(TreeViewItem item, ref GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Add Favorite"), false, () =>
            {
                FavoriteAssetSave.Add(item.GetGUID());
            });

            if (_window.ValidShare)
            {
                menu.AddItem(new GUIContent("Add Share"), false, () =>
                {
                    var data = YorozuToolShareObject.Load();
                    data.Add(item.GetGUID());
                });
            }
        }

        internal override IEnumerable<UnityEngine.Object> GetDragObjects(IList<int> itemIds)
        {
            return itemIds.Select(EditorUtility.InstanceIDToObject);
        }
    }
}
