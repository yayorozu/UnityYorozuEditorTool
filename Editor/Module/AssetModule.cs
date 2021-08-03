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

		internal override bool CanBeParent(TreeViewItem item)
		{
			return _favMode && ((ToolTreeViewItem) item).CanChangeName;
		}

		internal override bool CanRename(TreeViewItem item)
		{
			return _favMode && ((ToolTreeViewItem) item).CanChangeName;
		}

		internal override bool RenameEnded(int itemId, string prev, string current)
		{
			return FavoriteAssetSave.ChangeCategoryName(prev, current);
		}

		internal override List<ToolTreeViewItem> GetItems()
		{
			var list = new List<ToolTreeViewItem>();
			if (_favMode)
			{
				var a = GUIDsToGroup(FavoriteAssetSave.Data.Select(d => d.GUID).ToArray());
				list.AddRange(a);
				var b = CategoryToItems(FavoriteAssetSave.Categories, list.Count);
				list.AddRange(b);
				return list;
			}

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

			return !_favMode;
		}

		internal override void SelectionChanged(TreeViewItem[] items)
		{
			SelectObject(items);
		}

		internal override void GenerateMenu(TreeViewItem item, ref GenericMenu menu)
		{
			if (_favMode)
			{
				menu.AddItem(new GUIContent("Add Custom Category"), false, () =>
				{
					FavoriteAssetSave.AddCategory();
					_window.Reload();
				});
				var item2 = item.parent as ToolTreeViewItem;
				if (item2 != null && item2.CanChangeName)
				{
					// Custom から戻す
					menu.AddItem(new GUIContent("Back Default Category"), false, () =>
					{
						FavoriteAssetSave.Add(false, item.GetGUID());
					});
				}

				menu.AddSeparator("");
				menu.AddItem(new GUIContent("Remove"), false, () =>
				{
					RemoveItem(item);
				});
				menu.AddSeparator("");
				menu.AddItem(new GUIContent("ExpandAll"), false, () => { _window.ExpandAll(); });
				menu.AddItem(new GUIContent("CollapseAll"), false, () => { _window.CollapseAll(); });
				return;
			}

			menu.AddItem(new GUIContent("Add Favorite"), false, () => { FavoriteAssetSave.Add(true, item.GetGUID()); });

			if (_window.ValidShare)
				menu.AddItem(new GUIContent("Add Share"), false, () =>
				{
					var data = YorozuToolShareObject.Load();
					data.Add(item.GetGUID());
				});
		}

		internal override void GenerateMenu(ref GenericMenu menu)
		{
			if (_favMode)
			{
				menu.AddItem(new GUIContent("Add Custom Category"), false, () =>
				{
					FavoriteAssetSave.AddCategory();
					_window.Reload();
				});
				menu.AddSeparator("");
				menu.AddItem(new GUIContent("ExpandAll"), false, () => { _window.ExpandAll(); });
				menu.AddItem(new GUIContent("CollapseAll"), false, () => { _window.CollapseAll(); });
			}
		}

		private void RemoveItem(TreeViewItem item)
		{
			if (item.depth == 2)
				return;

			var item2 = item as ToolTreeViewItem;
			// Customの場合
			if (item2.CanChangeName)
			{
				FavoriteAssetSave.RemoveCategory(item.displayName);
				return;
			}

			var parent = item.parent as ToolTreeViewItem;
			if (parent != null && parent.CanChangeName)
			{
				FavoriteAssetSave.RemoveCategoryItem(parent.displayName, item.GetGUID());
				return;
			}

			// カテゴリだったら子供を全部削除
			if (item.depth != 0)
			{
				FavoriteAssetSave.Remove(item.GetGUID());
				return;
			}

			var childGUIDs = item.children
				.Select(c => c.GetGUID())
				.ToArray();

			FavoriteAssetSave.Remove(childGUIDs);
		}

		internal override IEnumerable<Object> GetDragObjects(IList<int> itemIds)
		{
			return itemIds.Select(EditorUtility.InstanceIDToObject)
				.Where(i => i != null);;
		}

		/// <summary>
		/// カテゴリ変換
		/// </summary>
		private List<ToolTreeViewItem> CategoryToItems(IEnumerable<FavoriteAssetCategoryData> data, int startIndex)
		{
			var roots = new List<ToolTreeViewItem>();
			foreach (var d in data)
			{
				var root = new ToolTreeViewItem
				{
					id = roots.Count + startIndex,
					depth = 0,
					displayName = d.Name,
					icon = (Texture2D) FavIcon,
					CanChangeName = true,
				};

				foreach (var guid in d.GUIDs)
				{
					root.AddChild(GUIDToItem(guid));
				}

				roots.Add(root);
			}

			return roots;
		}
	}
}
