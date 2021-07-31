using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
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
    }

    internal delegate void UpdateProject();
    internal delegate void UpdateHierarchy();

    [InitializeOnLoad]
    internal static class SelectionLog
    {
        private static readonly StringBuilder BuilderCache = new StringBuilder();

        internal static IEnumerable<HierarchyData> HierarchyLogs => HierarchyLogData.instance.Logs;
        internal static IEnumerable<string> ProjectLogs => ProjectLogData.instance.Logs;

        internal static void AddProjectLog(string guid) => ProjectLogData.instance.AddLog(guid);
        private static int _skipCount;

        internal static UpdateHierarchy UpdateHierarchyLog
        {
            get => HierarchyLogData.instance.UpdateLog;
            set => HierarchyLogData.instance.UpdateLog = value;
        }
        internal static UpdateProject UpdateProjectLog
        {
            get => ProjectLogData.instance.UpdateLog;
            set => ProjectLogData.instance.UpdateLog = value;
        }

        static SelectionLog()
        {
            Selection.selectionChanged += SelectionChanged;
        }

        private class HierarchyLogData : ScriptableSingleton<HierarchyLogData>
        {
            internal IEnumerable<HierarchyData> Logs => instance._logs;

            internal UpdateHierarchy UpdateLog;

            [SerializeField]
            private List<HierarchyData> _logs = new List<HierarchyData>(LogMax + 1);
            private const int LogMax = 50;

            internal void AddLog(HierarchyData data)
            {
                _logs.RemoveAll(l => l.Path == data.Path && l.Name == data.Name);
                _logs.Insert(0, data);
                while (_logs.Count > LogMax)
                    _logs.RemoveAt(LogMax);

                UpdateLog?.Invoke();
            }
        }

        private class ProjectLogData : ScriptableSingleton<ProjectLogData>
        {
            internal IEnumerable<string> Logs => instance._logs;
            [SerializeField]
            private List<string> _logs = new List<string>(LogMax + 1);
            private const int LogMax = 50;

            internal UpdateProject UpdateLog;

            internal void AddLog(string data)
            {
                _logs.RemoveAll(l => l.Equals(data));
                _logs.Insert(0, data);
                while (_logs.Count > LogMax)
                    _logs.RemoveAt(LogMax);

                UpdateLog?.Invoke();
            }
        }

        internal static void SkipLog(int count = 1)
        {
            _skipCount += count;
        }

        private static void SelectionChanged()
        {
            if (_skipCount > 0)
            {
                _skipCount--;
                return;
            }

            foreach (var transform in Selection.transforms)
            {
                var prefab = PrefabStageUtility.GetCurrentPrefabStage();
                var data = new HierarchyData
                {
                    SceneName = transform.gameObject.scene.name,
                    Path = HierarchyPath(transform),
                    Name = transform.name,
                    InPrefabEditor = prefab != null,
                };
                HierarchyLogData.instance.AddLog(data);
            }
            foreach (var guid in Selection.assetGUIDs)
            {
                ProjectLogData.instance.AddLog(guid);
            }
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
