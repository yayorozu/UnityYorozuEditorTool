using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Object = UnityEngine.Object;

#if UNITY_2021_1_OR_NEWER
using UnityEditor.SceneManagement;
#else
using UnityEditor.Experimental.SceneManagement;
#endif

namespace Yorozu.EditorTool
{
	[Serializable]
	internal abstract class Module
	{
		protected YorozuToolEditorWindow _window => YorozuToolEditorWindow.Window;
		internal abstract string Name { get; }
		internal abstract Texture Texture { get; }
		internal abstract bool CanDrag { get; }

		protected GUIContent[] _tabContents;
		protected int _mode;

		protected Texture FavIcon => EditorResources.Load<Texture>("d_Favorite Icon");
		protected Texture LogIcon => EditorResources.Load<Texture>("Profiler.Instrumentation");

		protected void Reload()
		{
			if (_window == null)
				return;
			
			_window.Reload();
		}

		internal abstract void Enter();
		internal abstract void Exit();
		internal abstract List<ToolTreeViewItem> GetItems();

		/// <summary>
		/// ツールバーの右側に機能を追加する場合
		/// </summary>
		internal virtual void OnGUIToolBar()
		{
			if (_tabContents == null)
				return;

			using (var check = new EditorGUI.ChangeCheckScope())
			{
				_mode = GUILayout.Toolbar(
					_mode,
					_tabContents,
					new GUIStyle(EditorStyles.toolbarButton),
					GUI.ToolbarButtonSize.Fixed,
					GUILayout.Width(180f));

				if (check.changed)
				{
					Reload();
				}
			}
		}

		internal virtual bool CanSearchDraw(ToolTreeViewItem item)
		{
			return true;
		}

		internal virtual bool DoubleClick(ToolTreeViewItem item)
		{
			return false;
		}

		internal virtual void SingleClick(ToolTreeViewItem item)
		{
		}

		internal virtual void SelectionChanged(TreeViewItem[] items)
		{
		}

		internal virtual void GenerateMenu(TreeViewItem item, ref GenericMenu menu)
		{
		}

		/// <summary>
		/// アイテム外で右クリック
		/// </summary>
		internal virtual void GenerateMenu(ref GenericMenu menu)
		{
		}

		internal virtual bool CanBeParent(TreeViewItem item)
		{
			return false;
		}

		internal virtual bool CanRename(TreeViewItem item)
		{
			return false;
		}

		internal virtual bool RenameEnded(int itemId, string prev, string current)
		{
			return false;
		}

		internal virtual IEnumerable<Object> GetDragObjects(IList<int> itemIds)
		{
			return new Object[0];
		}

		protected List<ToolTreeViewItem> GUIDsToGroup(string[] guids)
		{
			var group = guids
				.Select(AssetDatabase.GUIDToAssetPath)
				.Where(path => File.Exists(path) || AssetDatabase.IsValidFolder(path))
				.GroupBy(path =>
				{
					// ディレクトリとDefaultAsset を区別するため
					if (AssetDatabase.IsValidFolder(path))
					{
						return typeof(Directory);
					}
					// ScriptableObject
					if (path.EndsWith(".asset"))
					{
						var iconName = AssetDatabase.GetCachedIcon(path);
						if (iconName.name.EndsWith("ScriptableObject Icon"))
						{
							return typeof(ScriptableObject);
						}
					}
					
					return AssetDatabase.GetMainAssetTypeAtPath(path);
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
					Data = g.Key
				};
				
				// ScriptableObjectは作りが別
				if (g.Key == typeof(ScriptableObject))
				{
					var sgroup = g.GroupBy(AssetDatabase.GetMainAssetTypeAtPath);
					foreach (var pair in sgroup)
					{
						var child1 = new ToolTreeViewItem
						{
							depth = 1,
							displayName = pair.Key.Name,
							icon = (Texture2D) AssetDatabase.GetCachedIcon(g.First()),
						};
						
						CreateChild(child1, pair, g.Key);
						if (child1.hasChildren)
						{
							var validPath = pair.FirstOrDefault(p => !string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(p)));
							if (!string.IsNullOrEmpty(validPath))
							{
								var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(validPath);
								var editor = Editor.CreateEditor(asset);
								var script = editor.serializedObject.FindProperty("m_Script");
								child1.id = script.objectReferenceValue.GetInstanceID();
								child1.Data = AssetDatabase.GetAssetPath(script.objectReferenceValue);
							}

							root.AddChild(child1);
						}
					}
				}
				else
				{
					CreateChild(root, g, g.Key);
				}

				if (root.hasChildren)
					list.Add(root);
			}

			return list;
		}

		private ToolTreeViewItem CreateChild(ToolTreeViewItem parent, IEnumerable<string> paths, Type type)
		{
			var child = new List<ToolTreeViewItem>(paths.Count());
			foreach (var path in paths)
			{
				var guid = AssetDatabase.AssetPathToGUID(path);
				var item = GUIDToItem(guid);
				if (item == null) 
					continue;
						
				// フォルダだったら子供を取得する
				if (type == typeof(Directory))
				{
					foreach (var file in Directory.GetFiles(path))
					{
						var childGuid = AssetDatabase.AssetPathToGUID(file);
						var childItem = GUIDToItem(childGuid);
						if (childItem != null)
							item.AddChild(childItem);
					}
				}
				child.Add(item);
			}
			
			// ソートする
			foreach (var item in child.OrderBy(c => c.displayName))
			{
				parent.AddChild(item);
			}

			return parent;
		}

		protected ToolTreeViewItem GUIDToItem(string guid)
		{
			var path = AssetDatabase.GUIDToAssetPath(guid);

			if (string.IsNullOrEmpty(path))
				return null;

			var asset = AssetDatabase.LoadAssetAtPath<Object>(path);

			if (asset == null)
				return null;

			var fileName = Path.GetFileNameWithoutExtension(path);
			var directory = Path.GetDirectoryName(path)?.Replace("Assets/", "");
			var item = new ToolTreeViewItem
			{
				id = asset.GetInstanceID(),
				depth = 0,
				displayName = fileName,
				icon = (Texture2D) AssetDatabase.GetCachedIcon(path),
				SubLabel = directory,
				Data = path
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
				EditorUtility.RevealInFinder(path);
			else
				AssetDatabase.OpenAsset(EditorUtility.InstanceIDToObject(item.id));
		}

		protected void SelectObject(params TreeViewItem[] items)
		{
			SelectionLog.SkipLog();
			Selection.objects = items.Select(i => i as ToolTreeViewItem)
				.Where(i => i.Data is string)
				.Select(i => EditorUtility.InstanceIDToObject(i.id))
				.ToArray();
		}

		/// <summary>
		///     Path から Hierarchy Object を選択
		/// </summary>
		protected bool SelectHierarchyObject(string path)
		{
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
						return false;
				}

				Selection.activeGameObject = findChild.gameObject;

				return true;
			}

			var find = GameObject.Find(path);

			if (find == null)
				return false;

			Selection.activeGameObject = find;

			return true;
		}
	}
}
