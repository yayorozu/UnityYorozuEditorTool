using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEngine;

namespace Yorozu.EditorTools
{
	internal class WindowModule : Module
	{
		internal override string Name => "WindowLog";
		internal override Texture Texture => EditorResources.Load<Texture>("winbtn_win_max");
		internal override bool CanDrag => false;

		internal override void Enter()
		{
			EditorWindowLog.UpdateLog += i => Reload();
		}

		internal override void Exit()
		{
			EditorWindowLog.UpdateLog -= i => Reload();
		}

		internal override List<ToolTreeViewItem> GetItems()
		{
			var list = new List<ToolTreeViewItem>();
			foreach (var pair in EditorWindowLog.Logs.Select((log, index) => new {log, index}))
			{
				var item = new ToolTreeViewItem()
				{
					id = pair.index,
					depth = 0,
					displayName = pair.log.Title,
					icon = pair.log.Icon,
					Data = pair.log.TypeNameSpace + ":" + pair.log.TypeName,
				};
				list.Add(item);
			}

			return list;
		}

		internal override void DoubleClick(ToolTreeViewItem item)
		{
			var typeString = item.Data as string;
			if (typeString == null)
			{
				return;
			}

			var split = typeString.Split(':');
			var findType = AppDomain.CurrentDomain
				.GetAssemblies()
				.SelectMany(a => a.GetTypes())
				.FirstOrDefault(t => t.Namespace == split[0] && t.Name == split[1]);

			if (findType == null)
			{
				Debug.LogWarning(typeString + " is not found");
				return;
			}

			EditorWindowLog.SkipLog();
			var findObjects = Resources.FindObjectsOfTypeAll(findType);
			// すでにOpen
			if (findObjects.Length > 0)
			{
				var window = findObjects[0] as EditorWindow;
				window.Focus();
			}
			else
			{
				var window = EditorWindow.GetWindow(findType);
				window.titleContent = new GUIContent(item.displayName, item.icon);
			}
		}
	}
}
