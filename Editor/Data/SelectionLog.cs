using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Yorozu.EditorTools
{
    [Serializable]
    internal class HierarchyData
    {
        internal string SceneName;
        internal string Path;
        internal string Name;

        public override bool Equals(object obj)
        {
            var data =  obj as HierarchyData;
            return data.SceneName == SceneName && data.Path == Path;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    internal delegate void  UpdateProject(IEnumerable<string> paths);
    internal delegate void  UpdateHierarchy(IEnumerable<HierarchyData> paths);

    [InitializeOnLoad]
    internal static class SelectionLog
    {
        private static readonly StringBuilder BuilderCache = new StringBuilder();

        internal static IEnumerable<HierarchyData> HierarchyLogs => HierarchyLogData.instance.Logs;
        internal static IEnumerable<string> ProjectLogs => ProjectLogData.instance.Logs;

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

        private class HierarchyLogData : ScriptableSingleton<HierarchyLogData>
        {
            internal IEnumerable<HierarchyData> Logs => instance._logs;

            internal UpdateHierarchy UpdateLog;

            [SerializeField]
            private List<HierarchyData> _logs = new List<HierarchyData>(LogMax + 1);
            private const int LogMax = 50;

            internal void AddLog(HierarchyData data)
            {
                _logs.RemoveAll(l => l.Equals(data));
                _logs.Insert(0, data);
                while (_logs.Count > LogMax)
                    _logs.RemoveAt(LogMax);

                UpdateLog?.Invoke(_logs);
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

                UpdateLog?.Invoke(_logs);
            }
        }

        static SelectionLog()
        {
            Selection.selectionChanged += SelectionChanged;
        }

        private static int _skipCount;

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
                var data = new HierarchyData
                {
                    SceneName = transform.gameObject.scene.name,
                    Path = HierarchyPath(transform),
                    Name = transform.name,
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
