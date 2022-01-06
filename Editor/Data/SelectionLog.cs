using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Yorozu.EditorTool
{
	internal delegate void UpdateProject();

	internal delegate void UpdateHierarchy();

	[InitializeOnLoad]
	internal static class SelectionLog
	{
		private static int _skipCount;

		static SelectionLog()
		{
			Selection.selectionChanged += SelectionChanged;
		}

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

		internal static void AddProjectLog(string guid)
		{
			ProjectLogData.instance.AddLog(guid);
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
				var data = HierarchyData.Convert(transform);
				HierarchyLogData.instance.AddLog(data);
			}

			foreach (var guid in Selection.assetGUIDs)
				ProjectLogData.instance.AddLog(guid);
		}

		private class HierarchyLogData : ScriptableSingleton<HierarchyLogData>
		{
			private const int LogMax = 50;
			[SerializeField]
			private List<HierarchyData> _logs = new List<HierarchyData>(LogMax + 1);
			internal UpdateHierarchy UpdateLog;
			internal IEnumerable<HierarchyData> Logs => instance._logs;

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
			private const int LogMax = 50;
			[SerializeField]
			private List<string> _logs = new List<string>(LogMax + 1);
			internal UpdateProject UpdateLog;
			internal IEnumerable<string> Logs => instance._logs;

			internal void AddLog(string data)
			{
				_logs.RemoveAll(l => l.Equals(data));
				_logs.Insert(0, data);
				while (_logs.Count > LogMax)
					_logs.RemoveAt(LogMax);

				UpdateLog?.Invoke();
			}
		}
	}
}
