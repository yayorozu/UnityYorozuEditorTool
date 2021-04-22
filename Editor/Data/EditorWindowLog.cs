using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Yorozu.EditorTools
{
	[Serializable]
	internal class WindowData
	{
		[SerializeField]
		internal string TypeName;
		[SerializeField]
		internal string TypeNameSpace;
		[SerializeField]
		internal Texture2D Icon;
		[SerializeField]
		internal string Title;

		internal WindowData(EditorWindow window)
		{
			var type = window.GetType();
			TypeNameSpace = type.Namespace;
			TypeName = type.Name;
			Icon = window.titleContent.image as Texture2D;
			Title = window.titleContent.text;
		}
	}

	internal delegate void UpdateWindow(IEnumerable<WindowData> paths);

	[InitializeOnLoad]
	internal static class EditorWindowLog
	{
		private static EditorWindow _currentWindow;

		internal static IEnumerable<WindowData> Logs => EditorWindowLogData.instance.Logs;

		private static int _count;
		private static int _skipCount;

		static EditorWindowLog()
		{
			EditorApplication.update += Update;
		}

		internal static void SkipLog(int count = 1)
		{
			_skipCount += count;
		}

		private static void Update()
		{
			if (_count-- > 0)
				return;

			// 毎フレームやる必要はないので
			if (_currentWindow != EditorWindow.focusedWindow)
			{
				_currentWindow = EditorWindow.focusedWindow;
				if (_currentWindow != null && _currentWindow.GetType() != typeof(YorozuToolEditorWindow))
				{
					if (_skipCount > 0)
					{
						_skipCount--;
					}
					else
					{
						EditorWindowLogData.instance.AddLog(new WindowData(_currentWindow));
					}
				}

			}

			_count = 100;
		}

		internal static UpdateWindow UpdateLog
		{
			get => EditorWindowLogData.instance.UpdateLog;
			set => EditorWindowLogData.instance.UpdateLog = value;
		}

		private class EditorWindowLogData : ScriptableSingleton<EditorWindowLogData>
		{
			internal IEnumerable<WindowData> Logs => instance._logs;

			internal UpdateWindow UpdateLog;

			[SerializeField]
			private List<WindowData> _logs = new List<WindowData>(LogMax + 1);
			private const int LogMax = 50;


			internal void AddLog(WindowData data)
			{
				_logs.RemoveAll(l => l.Title == data.Title);
				_logs.Insert(0, data);
				while (_logs.Count > LogMax)
					_logs.RemoveAt(LogMax);

				UpdateLog?.Invoke(_logs);
			}
		}
	}
}
