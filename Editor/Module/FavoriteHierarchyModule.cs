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
	internal class FavoriteHierarchyModule : Module
	{
		internal override string Name => "Hierarchy";
		internal override Texture Texture => EditorResources.Load<Texture>("Favorite Icon");
		internal override bool CanDrag => false;

		private Texture2D _iconScene;
		private Texture2D _iconPrefab;

		internal override void Enter()
		{
			_iconScene = EditorResources.Load<Texture2D>(EditorGUIUtility.isProSkin ? "d_Prefab On Icon" : "Prefab On Icon");
			_iconPrefab = EditorResources.Load<Texture2D>(EditorGUIUtility.isProSkin ? "d_Prefab Icon" : "Prefab Icon");
			FavoriteHierarchySave.UpdateEvent += Reload;
		}

		internal override void Exit()
		{
			FavoriteHierarchySave.UpdateEvent -= Reload;
		}

		internal override List<ToolTreeViewItem> GetItems()
		{
			var list = new List<ToolTreeViewItem>();
			foreach (var pair in FavoriteHierarchySave.Data.Select((i, index) => new{i, index}))
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
			menu.AddItem(new GUIContent("Remove"), false, () =>
			{
				var target = SelectionLog.HierarchyLogs.Skip(item.id).First();
				FavoriteHierarchySave.Remove(target);
			});

			menu.AddItem(new GUIContent("Remove All"), false, () =>
			{
				FavoriteHierarchySave.Clear();
			});
		}
	}
}
