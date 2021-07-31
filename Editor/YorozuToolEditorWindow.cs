using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Yorozu.EditorTools
{
    public class YorozuToolEditorWindow : EditorWindow, IHasCustomMenu
    {
        [MenuItem("Tools/YorozuTool")]
        private static void ShowWindow()
        {
            var window = GetWindow<YorozuToolEditorWindow>("YorozuTool");
            window.Show();
        }

        internal static YorozuToolEditorWindow Window;

        internal Module CurrentModule => _modules[_moduleIndex];

        [SerializeField]
        private TreeViewState _state;
        [SerializeReference]
        private Module[] _modules;

        private ToolTreeView _treeView;
        private SearchField _searchField;
        private int _moduleIndex;
        private GUIContent[] _tabContents;
        private bool _validShare;
        internal bool ValidShare => _validShare;

        private void OnEnable()
        {
            Window = this;
            _validShare = EditorPrefs.GetBool("YorozuTool.ValidShare", false);
            Init();
            CurrentModule.Enter();
        }

        private void OnDisable()
        {
            CurrentModule.Exit();
            Window = null;
        }

        private void Init()
        {
            if (_modules == null)
            {
                _modules = new Module[]
                {
                    new FavoriteAssetModule(),
                    new AssetModule(),
                    new FavoriteHierarchyModule(),
                    new HierarchyModule(),
                    new WindowModule(),
                    new ShareModule(),
                };
            }

            if (_state == null)
            {
                _state = new TreeViewState();
            }
            if (_treeView == null)
            {
                _treeView = new ToolTreeView(_state, this);
            }
            if (_searchField == null)
            {
                _searchField = new SearchField();
                _searchField.downOrUpArrowKeyPressed += _treeView.SetFocusAndEnsureSelectedItem;
            }

            if (_tabContents == null)
            {
                var length = _validShare ? _modules.Length : _modules.Length - 1;
                _tabContents = new GUIContent[length];
                for (var i = 0; i < length; i++)
                {
                    _tabContents[i] = new GUIContent(_modules[i].Name, _modules[i].Texture);
                }
            }
        }

        private void OnGUI()
        {
            Init();
            CheckDrop();

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
            }

            var rect = GUILayoutUtility.GetRect(0, float.MaxValue, 0, float.MaxValue);
            _treeView.OnGUI(rect);
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

        private void CheckDrop()
        {
            var @event = Event.current;
            if (@event.type == EventType.DragUpdated || @event.type == EventType.DragPerform)
            {
                if (mouseOverWindow != this)
                    return;

                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                if (@event.type == EventType.DragPerform)
                {
                    DragAndDrop.activeControlID = 0;

                    var paths = DragAndDrop.paths;
                    if (paths.Length > 0)
                    {
                        FavoriteAssetSave.Add(paths.Select(AssetDatabase.AssetPathToGUID).ToArray());
                    }

                    var objects = DragAndDrop.objectReferences;
                    if (objects.Length > 0)
                    {
                        var data = objects.Select(o => o as GameObject)
                            .Where(g => g != null)
                            .Select(g => g.transform)
                            .Select(HierarchyData.Convert)
                            .ToArray();
                        
                        FavoriteHierarchySave.Add(data);
                    }
                    DragAndDrop.AcceptDrag();
                }
                else
                {
                    DragAndDrop.activeControlID = GUIUtility.GetControlID(FocusType.Passive);
                }

                @event.Use();
            }
        }

        void IHasCustomMenu.AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Asset Share Enable"), _validShare, () =>
            {
                _validShare = !_validShare;
                EditorPrefs.SetBool("YorozuTool.ValidShare", _validShare);
                if (_moduleIndex >= _modules.Length - 1)
                {
                    _moduleIndex = 0;
                }
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
                {
                    FavoriteAssetSave.Clear();
                }
            });
        }
    }
}
