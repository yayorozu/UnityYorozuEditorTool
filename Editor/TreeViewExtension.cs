using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Yorozu.EditorTool
{
	internal static class TreeViewExtension
	{
		/// <summary>
		///     GUID に変換
		/// </summary>
		internal static string GetGUID(this TreeViewItem self)
		{
			var obj = EditorUtility.InstanceIDToObject(self.id);

			if (obj == null)
				return null;

			var path = AssetDatabase.GetAssetPath(obj);

			return AssetDatabase.AssetPathToGUID(path);
		}

		internal static string GetGUID(this Object self)
		{
			var obj = EditorUtility.InstanceIDToObject(self.GetInstanceID());

			if (obj == null)
				return null;

			var path = AssetDatabase.GetAssetPath(obj);

			return AssetDatabase.AssetPathToGUID(path);
		}
	}
}
