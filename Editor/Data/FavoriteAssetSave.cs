using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Yorozu.EditorTools
{
	[Serializable]
	internal class FavoriteData
	{
		[SerializeField]
		internal string GUID;

		internal FavoriteData(string guid)
		{
			GUID = guid;
		}
	}

    /// <summary>
    ///     お気に入りのAssetデータをPrefabに保存
    /// </summary>
    internal static class FavoriteAssetSave
	{
		private static readonly string _key = Application.productName + "YorozuTool";
		private static readonly SaveData _saveData;

		static FavoriteAssetSave()
		{
			var json = EditorPrefs.GetString(_key);
			_saveData = string.IsNullOrEmpty(json) ? new SaveData() : JsonUtility.FromJson<SaveData>(json);
		}

		internal static IEnumerable<FavoriteData> Data => _saveData.Data;

		[MenuItem("Assets/Yorozu/AddFavorite")]
		private static void AddFavorite()
		{
			var guids = Selection.assetGUIDs;

			if (guids.Length <= 0)
				return;

			Add(guids);
		}

		internal static event Action UpdateEvent;

		private static void Save()
		{
			EditorPrefs.SetString(_key, JsonUtility.ToJson(_saveData));
			UpdateEvent?.Invoke();
		}

		internal static void Add(params string[] guids)
		{
			var validGuid = guids.Distinct().Where(guid => _saveData.Data.FindIndex(d => d.GUID == guid) < 0).ToArray();

			if (validGuid.Length <= 0)
				return;

			_saveData.Data.AddRange(validGuid.Select(guid => new FavoriteData(guid)));

			Save();
		}

		internal static void Remove(params string[] guids)
		{
			var removeCount = _saveData.Data.RemoveAll(d => guids.Contains(d.GUID));

			if (removeCount <= 0)
				return;

			Save();
		}

        /// <summary>
        ///     存在しないGUIDのものを削除
        /// </summary>
        internal static void RemoveInactive()
		{
			var removeCount =
				_saveData.Data.RemoveAll(d => string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(d.GUID)));

			if (removeCount <= 0)
				return;

			Save();
		}

        /// <summary>
        ///     初期化
        /// </summary>
        internal static void Clear()
		{
			_saveData.Data.Clear();
			Save();
		}

		[Serializable]
		private class SaveData
		{
			[SerializeField]
			internal List<FavoriteData> Data = new List<FavoriteData>();
		}
	}
}
