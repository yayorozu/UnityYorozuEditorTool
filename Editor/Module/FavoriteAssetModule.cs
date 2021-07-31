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
	internal class FavoriteAssetModule : Module
	{
		internal override string Name => "Project Fav";
		internal override Texture Texture =>
			EditorResources.Load<Texture>(EditorGUIUtility.isProSkin ? "d_Favorite" : "Favorite");
		internal override bool CanDrag => true;

		internal override void Enter()
		{
			FavoriteAssetSave.UpdateEvent += Reload;
		}

		internal override void Exit()
		{
			FavoriteAssetSave.UpdateEvent -= Reload;
		}

		internal override bool CanSearchDraw(ToolTreeViewItem item)
		{
			return item.depth >= 1;
		}

		internal override List<ToolTreeViewItem> GetItems()
		{
			return GUIDsToGroup(FavoriteAssetSave.Data.Select(d => d.GUID).ToArray());
		}

		internal override bool DoubleClick(ToolTreeViewItem item)
		{
			OpenAsset(item);

			return false;
		}

		internal override void SingleClick(ToolTreeViewItem item)
		{
			SelectObject(item);
		}

		internal override void GenerateMenu(TreeViewItem item, ref GenericMenu menu)
		{
			menu.AddItem(new GUIContent("Remove"), false, () =>
			{
				if (item.depth == 2)
					return;

				// カテゴリだったら子供を全部削除
				if (item.depth == 0)
				{
					var childGUIDs = item.children
						.Select(c => c.GetGUID())
						.ToArray();

					FavoriteAssetSave.Remove(childGUIDs);

					return;
				}

				FavoriteAssetSave.Remove(item.GetGUID());
			});
			menu.AddSeparator("");
			menu.AddItem(new GUIContent("ExpandAll"), false, () => { _window.ExpandAll(); });
			menu.AddItem(new GUIContent("CollapseAll"), false, () => { _window.CollapseAll(); });
		}

		internal override IEnumerable<Object> GetDragObjects(IList<int> itemIds)
		{
			return itemIds.Select(EditorUtility.InstanceIDToObject)
				.Where(i => i != null);
		}
	}
}
