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

	[Serializable]
	internal class FavoriteAssetCategoryData
	{
		[SerializeField]
		internal string Name;

		[SerializeField]
		internal List<string> GUIDs = new List<string>();

		internal FavoriteAssetCategoryData(string name)
		{
			Name = name;
		}
	}

    /// <summary>
    ///     お気に入りのAssetデータをPrefabに保存
    /// </summary>
    internal static class FavoriteAssetSave
	{
		[Serializable]
		private class SaveData
		{
			[SerializeField]
			internal List<FavoriteData> Data = new List<FavoriteData>();

			[SerializeField]
			internal List<FavoriteAssetCategoryData> Categories = new List<FavoriteAssetCategoryData>();
		}

		private static readonly string _key = Application.productName + "YorozuTool";
		private static readonly SaveData _saveData;

		internal static IEnumerable<FavoriteData> Data => _saveData.Data;
		internal static IEnumerable<FavoriteAssetCategoryData> Categories => _saveData.Categories;

		static FavoriteAssetSave()
		{
			var json = EditorPrefs.GetString(_key);
			_saveData = string.IsNullOrEmpty(json) ? new SaveData() : JsonUtility.FromJson<SaveData>(json);
		}

		[MenuItem("Assets/Yorozu/AddFavorite")]
		private static void AddFavorite()
		{
			var guids = Selection.assetGUIDs;

			if (guids.Length <= 0)
				return;

			Add(true, guids);
		}

		internal static event Action UpdateEvent;

		private static void Save()
		{
			EditorPrefs.SetString(_key, JsonUtility.ToJson(_saveData));
			UpdateEvent?.Invoke();
		}

		internal static void Add(bool isCategoryCheck, params string[] guids)
		{
			var validGuid = guids.Distinct()
				.Where(guid => _saveData.Data.FindIndex(d => d.GUID == guid) < 0)
				// カテゴリ内にあれば追加しない
				.Where(guid => (isCategoryCheck && !ContainsCategoryInItem(guid)) || !isCategoryCheck)
				.ToArray();

			if (validGuid.Length <= 0)
				return;

			if (!isCategoryCheck)
			{
				RemoveAllCategoryItem(validGuid);
			}
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

        internal static void AddCategory()
        {
	        var name = "Custom";
	        // デフォ名あれば追加はしない
	        if (_saveData.Categories.Any(c => c.Name == name))
	        {
		        return;
	        }
	        _saveData.Categories.Add(new FavoriteAssetCategoryData(name));
	        Save();
        }

        internal static void AddCategoryChild(string name, params string[] guids)
        {
	        var index = _saveData.Categories.FindIndex(c => c.Name == name);
	        if (index < 0)
		        return;

	        foreach (var guid in guids)
	        {
		        RemoveAllCategoryItem(guid);
		        if (_saveData.Categories[index].GUIDs.Contains(guid))
					continue;

				_saveData.Categories[index].GUIDs.Add(guid);
	        }

	        Save();
        }

        /// <summary>
        /// カテゴリ名を変更
        /// </summary>
        internal static bool ChangeCategoryName(string prevName, string newName)
        {
	        if (string.IsNullOrEmpty(newName))
		        return false;

	        // かぶったら
	        if (_saveData.Categories.All(c => c.Name == newName))
		        return false;

	        var index = _saveData.Categories.FindIndex(c => c.Name == prevName);
	        if (index < 0)
		        return false;

	        _saveData.Categories[index].Name = newName;
	        Save();
	        return true;
        }

        /// <summary>
        /// 削除
        /// </summary>
        internal static void RemoveCategory(string name)
        {
	        if (_saveData.Categories.RemoveAll(c => c.Name == name) > 0)
	        {
		        Save();
	        }
        }

        /// <summary>
        /// カテゴリない削除
        /// </summary>
        /// <param name="parentName"></param>
        /// <param name="guid"></param>
        internal static void RemoveCategoryItem(string parentName, string guid)
        {
	        var index = _saveData.Categories.FindIndex(c => c.Name == parentName);
	        if (index < 0)
		        return;

	        if (_saveData.Categories[index].GUIDs.RemoveAll(g => g == guid) > 0)
	        {
		        Save();
	        }
        }

        /// <summary>
        /// 全カテゴリから該当する GUID を削除
        /// </summary>
        private static void RemoveAllCategoryItem(params string[] guids)
        {
	        foreach (var guid in guids)
	        {
		        // 他のやつで使ってる場合は削除
		        foreach (var category in _saveData.Categories)
		        {
			        category.GUIDs.RemoveAll(g => g == guid);
		        }
	        }
        }

        /// <summary>
        /// カテゴリ内にGUIDが登録されているか
        /// </summary>
        private static bool ContainsCategoryInItem(string guid)
        {
	        // 他のやつで使ってる場合は削除
	        foreach (var category in _saveData.Categories)
	        {
		        if (category.GUIDs.Contains(guid))
			        return true;
	        }

	        return false;
        }
	}
}
