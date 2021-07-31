using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Yorozu.EditorTools
{
	internal delegate void UpdateShare();

	internal class YorozuToolShareObject : ScriptableObject
	{
		[SerializeField]
		[HideInInspector]
		private List<Data> _sharaData = new List<Data>();
		internal UpdateShare UpdateShare;
		internal IEnumerable<Data> Share => _sharaData;

		[MenuItem("Assets/Yorozu/AddShare")]
		private static void Menu()
		{
			var guids = Selection.assetGUIDs;

			if (guids.Length <= 0)
				return;

			var data = Load();
			data.Add(guids);
		}

		internal static YorozuToolShareObject Load()
		{
			var data = AssetDatabase.LoadAssetAtPath<YorozuToolShareObject>(
				"Assets/Editor Default Resources/Yorozu/Share.asset");
			if (data == null)
				data = CreateInstance<YorozuToolShareObject>();

			return data;
		}

		internal void Add(params string[] guids)
		{
			foreach (var guid in guids)
			{
				if (_sharaData.Any(d => d.GUID == guid))
					continue;

				_sharaData.Add(new Data(guid));
			}

			// インスタンスがなかったら作成
			if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(this)))
			{
				// 保存先のディレクトリ作成
				if (!AssetDatabase.IsValidFolder("Assets/Editor Default Resources"))
					AssetDatabase.CreateFolder("Assets", "Editor Default Resources");

				if (!AssetDatabase.IsValidFolder("Assets/Editor Default Resources/Yorozu/"))
					AssetDatabase.CreateFolder("Assets/Editor Default Resources", "Yorozu");

				AssetDatabase.CreateAsset(this, "Assets/Editor Default Resources/Yorozu/Share.asset");
			}

			EditorUtility.SetDirty(this);
			AssetDatabase.SaveAssets();

			UpdateShare?.Invoke();
		}

		internal void Remove(params string[] guids)
		{
			var removeCount = _sharaData.RemoveAll(d => guids.Contains(d.GUID));

			if (removeCount <= 0)
				return;

			EditorUtility.SetDirty(this);
			AssetDatabase.SaveAssets();

			UpdateShare?.Invoke();
		}

		[Serializable]
		internal class Data
		{
			[SerializeField]
			internal string GUID;

			internal Data(string guid)
			{
				GUID = guid;
			}
		}
	}
}