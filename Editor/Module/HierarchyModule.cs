using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEditor.Experimental.SceneManagement;
using UnityEngine;

namespace Yorozu.EditorTools
{
	[Serializable]
	internal class HierarchyModule : Module
	{
		internal override string Name => "HierarchyLog";
		internal override Texture Texture => EditorResources.Load<Texture>(EditorGUIUtility.isProSkin ? "d_UnityEditor.SceneHierarchyWindow" : "UnityEditor.SceneHierarchyWindow");
		internal override bool CanDrag => false;

		private Texture2D _icon;

		internal override void Enter()
		{
			SelectionLog.UpdateHierarchyLog += i => Reload();
			_icon = EditorResources.Load<Texture2D>(EditorGUIUtility.isProSkin ? "d_Prefab Icon" : "Prefab Icon");

		}

		internal override void Exit()
		{
			SelectionLog.UpdateHierarchyLog -= i => Reload();
		}

		internal override List<ToolTreeViewItem> GetItems()
		{
			var list = new List<ToolTreeViewItem>();
			foreach (var pair in SelectionLog.HierarchyLogs.Select((i, index) => new{i, index}))
			{
				var item = new ToolTreeViewItem(pair.index, 0, pair.i.Name)
				{
					subLabel = string.IsNullOrEmpty(pair.i.Name) ?
						$"{pair.i.Path}" :
						$"【{pair.i.SceneName}】{pair.i.Path}",
					Data = pair.i.Path + "/" + pair.i.Name,
					icon = _icon,
				};

				list.Add(item);
			}

			return list;
		}

		internal override void DoubleClick(ToolTreeViewItem item)
		{
			var path = item.Data as string;

			// PrefabModeだと処理が違う
			var stage = PrefabStageUtility.GetCurrentPrefabStage();
			if (stage != null)
			{
				var root = stage.prefabContentsRoot;
				path = path.Substring(path.IndexOf("/", StringComparison.Ordinal) + 1);
				var findChild = root.transform.Find(path);
				// Editing Environment が設定されていた場合パスがずれるので再度取得
				if (findChild == null)
				{
					path = path.Substring(path.IndexOf("/", StringComparison.Ordinal) + 1);
					findChild = root.transform.Find(path);
					if (findChild == null)
						return;
				}

				SelectionLog.SkipLog();
				Selection.activeGameObject = findChild.gameObject;
				return;
			}

			var find = GameObject.Find(path);
			if (find == null)
				return;

			SelectionLog.SkipLog();
			Selection.activeGameObject = find;
		}
	}
}
