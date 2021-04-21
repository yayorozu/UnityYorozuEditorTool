using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Yorozu.EditorTools
{
	internal class YorozuToolShareObject : ScriptableObject
	{
		[MenuItem("Assets/Yorozu/AddShare")]
		private static void Menu()
		{
			var guids = Selection.assetGUIDs;
			if(guids.Length <= 0)
				return;

			var data = Load();
			data.Add(guids);
		}

		[SerializeField, HideInInspector]
		internal List<Data> SharaData = new List<Data>();

		internal static YorozuToolShareObject Load()
		{
			var data = AssetDatabase.LoadAssetAtPath<YorozuToolShareObject>("Assets/Editor Default Resources/Yorozu/Share.asset");
			if (data == null)
			{
				data = CreateInstance<YorozuToolShareObject>();
			}

			return data;
		}

		internal void Add(params string[] guids)
		{
			foreach (var guid in guids)
			{
				if (SharaData.Any(d => d.GUID == guid))
					continue;

				SharaData.Add(new Data(guid));
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
		}

		internal void Remove(params string[] guids)
		{
			var removeCount = SharaData.RemoveAll(d => guids.Contains(d.GUID));
			if (removeCount <= 0)
				return;

			EditorUtility.SetDirty(this);
			AssetDatabase.SaveAssets();
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
