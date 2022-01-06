using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Yorozu.EditorTools
{
	public class YorozuToolEditorWindow : EditorWindow, IHasCustomMenu
	{
		internal static YorozuToolEditorWindow Window;
		[SerializeField]
		private TreeViewState _state;
		[SerializeReference]
		private Module[] _modules;
		private int _moduleIndex;
		private SearchField _searchField;
		private GUIContent[] _tabContents;
		private ToolTreeView _treeView;
		internal Module CurrentModule => _modules[_moduleIndex];
		internal bool ValidShare { get; private set; }

		private static readonly string ShareKay = "YorozuTool.ValidShare";

		private void OnEnable()
		{
			Window = this;
			ValidShare = EditorPrefs.GetBool(ShareKay, false);
			Init();
			CurrentModule.Enter();
		}

		private void OnDisable()
		{
			CurrentModule.Exit();
			Window = null;
		}

		private void OnGUI()
		{
			Init();

			using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
			{
				using (var check = new EditorGUI.ChangeCheckScope())
				{
					var prev = _moduleIndex;
					_moduleIndex = GUILayout.Toolbar(
						_moduleIndex,
						_tabContents,
						new GUIStyle(EditorStyles.toolbarButton),
						GUI.ToolbarButtonSize.Fixed);

					if (check.changed)
					{
						_modules[prev].Exit();
						CurrentModule.Enter();
						Reload();
					}
				}
			}

			using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
			{
				_treeView.searchString = _searchField.OnToolbarGUI(_treeView.searchString);
				CurrentModule.OnGUIToolBar();
			}

			var rect = GUILayoutUtility.GetRect(0, float.MaxValue, 0, float.MaxValue);
			_treeView.OnGUI(rect);
		}

		void IHasCustomMenu.AddItemsToMenu(GenericMenu menu)
		{
			menu.AddItem(new GUIContent("Asset Share Enable"), ValidShare, () =>
			{
				ValidShare = !ValidShare;
				EditorPrefs.SetBool(ShareKay, ValidShare);
				if (_moduleIndex >= _modules.Length - 1)
					_moduleIndex = 0;
				_tabContents = null;
			});
			menu.AddSeparator("");
			menu.AddItem(new GUIContent("Remove Invalid Favorite Asset"), false, () =>
			{
				FavoriteAssetSave.RemoveInactive();
			});

			menu.AddItem(new GUIContent("Remove All Favorite Asset"), false, () =>
			{
				if (EditorUtility.DisplayDialog("Info", "Clean All Favorite Asset?", "OK", "Cancel"))
					FavoriteAssetSave.Clear();
			});

			menu.AddItem(new GUIContent("Remove All Favorite Hierarchy GameObjects"), false, () =>
			{
				if (EditorUtility.DisplayDialog("Info", "Clean All Favorite Hierarchy GameObjects?", "OK", "Cancel"))
					FavoriteHierarchySave.Clear();
			});
		}

		[MenuItem("Tools/YorozuTool")]
		private static void ShowWindow()
		{
			var window = GetWindow<YorozuToolEditorWindow>("YorozuTool");
			window.Show();
		}

		private void Init()
		{
			if (_modules == null)
				_modules = new Module[]
				{
					new AssetModule(),
					new HierarchyModule(),
					new WindowModule(),
					new ShareModule()
				};

			if (_state == null)
				_state = new TreeViewState();
			if (_treeView == null)
				_treeView = new ToolTreeView(_state, this);
			if (_searchField == null)
			{
				_searchField = new SearchField();
				_searchField.downOrUpArrowKeyPressed += _treeView.SetFocusAndEnsureSelectedItem;
			}

			if (_tabContents == null)
			{
				var length = ValidShare ? _modules.Length : _modules.Length - 1;
				_tabContents = new GUIContent[length];
				for (var i = 0; i < length; i++)
					_tabContents[i] = new GUIContent(_modules[i].Name, _modules[i].Texture);
			}
		}

		internal void Reload()
		{
			_treeView.Reload();
			Repaint();
		}

		internal void ExpandAll()
		{
			_treeView.ExpandAll();
		}

		internal void CollapseAll()
		{
			_treeView.CollapseAll();
		}
	}
}
