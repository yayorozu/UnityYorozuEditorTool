using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Yorozu.EditorTools
{
    /// <summary>
    /// お気に入りのHierarchyをPrefabに保存
    /// </summary>
    internal static class FavoriteHierarchySave
    {
        [Serializable]
        private class SaveData
        {
            [SerializeField]
            internal List<HierarchyData> Data = new List<HierarchyData>();
        }

        private static readonly string _key = Application.productName + "FavoriteHierarchySave";

        private static SaveData _saveData;

        internal static event Action UpdateEvent;

        internal static IEnumerable<HierarchyData> Data => _saveData.Data;

        static FavoriteHierarchySave()
        {
            var json = EditorPrefs.GetString(_key);
            _saveData = string.IsNullOrEmpty(json) ?
                new SaveData() :
                JsonUtility.FromJson<SaveData>(json);
        }

        private static void Save()
        {
            EditorPrefs.SetString(_key, JsonUtility.ToJson(_saveData));
            UpdateEvent?.Invoke();
        }

        internal static void Add(params HierarchyData[] data)
        {
            if (data.Length <= 0)
                return;

            // 重複を削除
            _saveData.Data.RemoveAll(data.Contains);
            _saveData.Data.AddRange(data);

            Save();
        }

        internal static void Remove(params HierarchyData[] data)
        {
            var removeCount = _saveData.Data.RemoveAll(data.Contains);
            if (removeCount <= 0)
                return;

            Save();
        }

        /// <summary>
        /// 初期化
        /// </summary>
        internal static void Clear()
        {
            _saveData.Data.Clear();
            Save();
        }
    }
}
