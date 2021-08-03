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
	internal class HierarchyModule : Module
	{
		internal override string Name => "Hierarchy";
		internal override Texture Texture => EditorResources.Load<Texture>(EditorGUIUtility.isProSkin
			? "d_UnityEditor.SceneHierarchyWindow"
			: "UnityEditor.SceneHierarchyWindow");
		internal override bool CanDrag => false;

		private Texture2D _iconPrefab;
		private Texture2D _iconScene;

		private bool _favMode => _mode == 0;

		internal override void Enter()
		{
			_tabContents = new []
			{
				new GUIContent("Favorites", FavIcon),
				new GUIContent("Log", LogIcon),
			};

			_iconScene = EditorResources.Load<Texture2D>(EditorGUIUtility.isProSkin ? "d_Prefab On Icon" : "Prefab On Icon");
			_iconPrefab = EditorResources.Load<Texture2D>(EditorGUIUtility.isProSkin ? "d_Prefab Icon" : "Prefab Icon");

			SelectionLog.UpdateHierarchyLog += Reload;
			FavoriteHierarchySave.UpdateEvent += Reload;
		}

		internal override void Exit()
		{
			SelectionLog.UpdateHierarchyLog -= Reload;
			FavoriteHierarchySave.UpdateEvent -= Reload;
		}

		internal override List<ToolTreeViewItem> GetItems()
		{
			var list = new List<ToolTreeViewItem>();
			var data = _favMode ? FavoriteHierarchySave.Data : SelectionLog.HierarchyLogs;

			foreach (var pair in data.Select((i, index) => new {i, index}))
			{
				var item = pair.i.ToItem(pair.index);
				item.icon = pair.i.InPrefabEditor ? _iconPrefab : _iconScene;

				list.Add(item);
			}

			return list;
		}

		internal override bool DoubleClick(ToolTreeViewItem item)
		{
			return SelectHierarchyObject(item.Data as string);
		}

		internal override void GenerateMenu(TreeViewItem item, ref GenericMenu menu)
		{
			if (_favMode)
			{
				menu.AddItem(new GUIContent("Remove"), false, () =>
				{
					var target = FavoriteHierarchySave.Data.ElementAt(item.id);
					FavoriteHierarchySave.Remove(target);
				});

				menu.AddItem(new GUIContent("Remove All"), false, () => { FavoriteHierarchySave.Clear(); });
				return;
			}
			menu.AddItem(new GUIContent("Add Favorite"), false, () =>
			{
				var target = SelectionLog.HierarchyLogs.ElementAt(item.id);
				FavoriteHierarchySave.Add(target);
			});
		}
	}
}
