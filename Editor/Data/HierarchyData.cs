using System;
using System.Text;
using UnityEditor.Experimental.SceneManagement;
using UnityEngine;

namespace Yorozu.EditorTools
{
    [Serializable]
    internal class HierarchyData
    {
        [SerializeField]
        internal string SceneName;
        [SerializeField]
        internal string Path;
        [SerializeField]
        internal string Name;
        [SerializeField]
        internal bool InPrefabEditor;

        private static readonly StringBuilder BuilderCache = new StringBuilder();

        public override bool Equals(object obj)
        {
            var data =  obj as HierarchyData;
            return data.SceneName == SceneName && data.Path == Path && data.Name == Name;
        }

        public override int GetHashCode()
        {
            return SceneName.GetHashCode() ^ Path.GetHashCode() ^ Name.GetHashCode();
        }

        internal ToolTreeViewItem ToItem(int index)
        {
            var item = new ToolTreeViewItem(index, 0, Name)
            {
                SubLabel = string.IsNullOrEmpty(Name) ?
                    $"{Path}" :
                    $"【{SceneName}】{Path}",
                Data = Path + "/" + Name,
            };

            return item;
        }

        internal static HierarchyData Convert(Transform transform)
        {
            var prefab = PrefabStageUtility.GetCurrentPrefabStage();

            return new HierarchyData
            {
                SceneName = transform.gameObject.scene.name,
                Path = HierarchyPath(transform),
                Name = transform.name,
                InPrefabEditor = prefab != null,
            };
        }

        private static string HierarchyPath(Transform transform)
        {
            BuilderCache.Length = 0;
            while (transform.parent != null)
            {
                BuilderCache.Insert(0, transform.parent.name + "/");
                transform = transform.parent;
            }

            var path = BuilderCache.ToString();
            if (path.EndsWith("/"))
            {
                path = path.Substring(0, path.Length - 1);
            }
            return path;
        }
    }
}