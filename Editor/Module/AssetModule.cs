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
	internal class AssetModule : Module
	{
		internal override string Name => "Asset";
		internal override Texture Texture => EditorResources.Load<Texture>(EditorGUIUtility.isProSkin ? "d_Project" : "Project");
		internal override bool CanDrag => true;

		private bool _favMode => _mode == 0;

		internal override void Enter()
		{
			_tabContents = new []
			{
				new GUIContent("Favorites", FavIcon),
				new GUIContent("Log", LogIcon),
			};

			SelectionLog.UpdateProjectLog += Reload;
			FavoriteAssetSave.UpdateEvent += Reload;
		}

		internal override void Exit()
		{
			SelectionLog.UpdateProjectLog -= Reload;
			FavoriteAssetSave.UpdateEvent -= Reload;
		}

		internal override bool CanSearchDraw(ToolTreeViewItem item)
		{
			return _favMode ? item.depth >= 1 : base.CanSearchDraw(item);
		}

		internal override List<ToolTreeViewItem> GetItems()
		{
			if (_favMode)
			{
				return GUIDsToGroup(FavoriteAssetSave.Data.Select(d => d.GUID).ToArray());
			}

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
			OpenAsset(item, _favMode);

			return true;
		}

		internal override void SelectionChanged(TreeViewItem[] items)
		{
			SelectObject(items);
		}

		internal override void GenerateMenu(TreeViewItem item, ref GenericMenu menu)
		{
			if (_favMode)
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
				return;
			}

			menu.AddItem(new GUIContent("Add Favorite"), false, () => { FavoriteAssetSave.Add(item.GetGUID()); });

			if (_window.ValidShare)
				menu.AddItem(new GUIContent("Add Share"), false, () =>
				{
					var data = YorozuToolShareObject.Load();
					data.Add(item.GetGUID());
				});
		}

		internal override IEnumerable<Object> GetDragObjects(IList<int> itemIds)
		{
			return itemIds.Select(EditorUtility.InstanceIDToObject)
				.Where(i => i != null);;
		}
	}
}
