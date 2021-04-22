using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Yorozu.EditorTools
{
    [Serializable]
    internal class FavoriteModule : Module
    {
        internal override string Name => "Favorite";
        internal override Texture Texture => EditorResources.Load<Texture>(EditorGUIUtility.isProSkin ? "d_Favorite" : "Favorite");
        internal override bool CanDrag => true;

        internal override void Enter()
        {
            FavoriteSave.UpdateEvent += Reload;
        }

        internal override void Exit()
        {
            FavoriteSave.UpdateEvent -= Reload;
        }

        internal override bool CanSearchDraw(ToolTreeViewItem item)
        {
            return item.depth >= 1;
        }

        internal override List<ToolTreeViewItem> GetItems() => GUIDsToGroup(FavoriteSave.Data.Select(d => d.GUID).ToArray());

        internal override void DoubleClick(ToolTreeViewItem item) => OpenAsset(item);

        internal override void SingleClick(ToolTreeViewItem item) => SelectObject(item);

        internal override void GenerateMenu(TreeViewItem item, ref GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Remove"), false, () =>
            {
                var path = AssetDatabase.GetAssetPath(EditorUtility.InstanceIDToObject(item.id));
                FavoriteSave.Remove(AssetDatabase.AssetPathToGUID(path));
            });
        }

        internal override IEnumerable<Object> GetDragObjects(IList<int> itemIds)
        {
            return itemIds.Select(EditorUtility.InstanceIDToObject)
                .Where(i => i != null);
        }
    }
}
