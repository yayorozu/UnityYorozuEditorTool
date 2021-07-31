using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Yorozu.EditorTools
{
	[Serializable]
	internal abstract class Module
	{
		protected YorozuToolEditorWindow _window => YorozuToolEditorWindow.Window;

		protected void Reload() => _window.Reload();

		internal abstract string Name { get; }
		internal abstract Texture Texture { get; }
		internal abstract bool CanDrag { get; }
		internal abstract void Enter();

		internal abstract void Exit();

		internal abstract List<ToolTreeViewItem> GetItems();

		internal virtual bool CanSearchDraw(ToolTreeViewItem item) => true;

		internal virtual bool DoubleClick(ToolTreeViewItem item) => false;

		internal virtual void SingleClick(ToolTreeViewItem item){}

		internal virtual void SelectionChanged(TreeViewItem[] items){}

		internal virtual void GenerateMenu(TreeViewItem item, ref GenericMenu menu) { }

		internal virtual IEnumerable<Object> GetDragObjects(IList<int> itemIds) => new Object[0];

		protected List<ToolTreeViewItem> GUIDsToGroup(string[] guids)
		{
			var group = guids
				.Select(AssetDatabase.GUIDToAssetPath)
				.Where(path => File.Exists(path) || AssetDatabase.IsValidFolder(path))
				.GroupBy(path =>
				{
					// ディレクトリとDefaultAsset を区別するため
					return AssetDatabase.IsValidFolder(path) ?
						typeof(Directory) :
						AssetDatabase.GetMainAssetTypeAtPath(path);
				})
				.Where(pair => pair.Key != null)
				.OrderBy(g => g.Key.Name);

			var list = new List<ToolTreeViewItem>();
			foreach (var g in group)
			{
				var root = new ToolTreeViewItem
				{
					id = list.Count,
					depth = 0,
					displayName = g.Key.Name,
					icon = (Texture2D) AssetDatabase.GetCachedIcon(g.First()),
					Data = g.Key,
				};
				foreach (var path in g)
				{
					var guid = AssetDatabase.AssetPathToGUID(path);
					var item = GUIDToItem(guid);
					if (item != null)
					{
						// フォルダだったら子供を取得する
						if (AssetDatabase.IsValidFolder(path))
						{
							foreach (var file in Directory.GetFiles(path))
							{
								var childGuid = AssetDatabase.AssetPathToGUID(file);
								var childItem = GUIDToItem(childGuid);
								if (childItem != null)
									item.AddChild(childItem);
							}
						}
						root.AddChild(item);
					}
				}

				if (root.hasChildren)
				{
					list.Add(root);
				}
			}

			return list;
		}

		protected ToolTreeViewItem GUIDToItem(string guid)
		{
			var path = AssetDatabase.GUIDToAssetPath(guid);

			if (string.IsNullOrEmpty(path))
				return null;

			var asset = AssetDatabase.LoadAssetAtPath<Object>(path);
			if (asset == null)
				return null;

			var fileName = System.IO.Path.GetFileNameWithoutExtension(path);
			var directory = System.IO.Path.GetDirectoryName(path).Replace("Assets/", "");
			var item = new ToolTreeViewItem
			{
				id = asset.GetInstanceID(),
				depth = 0,
				displayName = fileName,
				icon = (Texture2D) AssetDatabase.GetCachedIcon(path),
				SubLabel = directory,
				Data = path,
			};

			return item;
		}

		protected void OpenAsset(ToolTreeViewItem item)
		{
			if (!(item.Data is string))
				return;

			var path = item.Data as string;
			var guid = AssetDatabase.AssetPathToGUID(path);
			// クリックしたやつはログの上に上げる
			SelectionLog.AddProjectLog(guid);
			if (AssetDatabase.IsValidFolder(path))
			{
				EditorUtility.RevealInFinder(path);
			}
			else
			{
				AssetDatabase.OpenAsset(EditorUtility.InstanceIDToObject(item.id));
			}
		}

		protected void SelectObject(params TreeViewItem[] items)
		{
			SelectionLog.SkipLog();
			Selection.objects = items.Select(i => i as ToolTreeViewItem)
				.Where(i => i.Data is string)
				.Select(i => EditorUtility.InstanceIDToObject(i.id))
				.ToArray();
		}
	}
}
