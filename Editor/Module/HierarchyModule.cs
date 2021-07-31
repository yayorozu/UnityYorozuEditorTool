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
		private Texture2D _iconPrefab;
		private Texture2D _iconScene;
		internal override string Name => "HierarchyLog";
		internal override Texture Texture => EditorResources.Load<Texture>(EditorGUIUtility.isProSkin
			? "d_UnityEditor.SceneHierarchyWindow"
			: "UnityEditor.SceneHierarchyWindow");
		internal override bool CanDrag => false;

		internal override void Enter()
		{
			SelectionLog.UpdateHierarchyLog += Reload;
			_iconScene =
				EditorResources.Load<Texture2D>(EditorGUIUtility.isProSkin ? "d_Prefab On Icon" : "Prefab On Icon");
			_iconPrefab = EditorResources.Load<Texture2D>(EditorGUIUtility.isProSkin ? "d_Prefab Icon" : "Prefab Icon");
		}

		internal override void Exit()
		{
			SelectionLog.UpdateHierarchyLog -= Reload;
		}

		internal override List<ToolTreeViewItem> GetItems()
		{
			var list = new List<ToolTreeViewItem>();
			foreach (var pair in SelectionLog.HierarchyLogs.Select((i, index) => new {i, index}))
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
			menu.AddItem(new GUIContent("Add Favorite"), false, () =>
			{
				var target = SelectionLog.HierarchyLogs.Skip(item.id).First();
				FavoriteHierarchySave.Add(target);
			});
		}
	}
}
