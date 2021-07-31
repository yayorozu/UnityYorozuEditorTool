using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Yorozu.EditorTools
{
	internal class ToolTreeViewItem : TreeViewItem
	{
		/// <summary>
		/// 各所で利用できるようにオブジェクト型
		/// </summary>
		internal object Data;

		internal string SubLabel;

		internal ToolTreeViewItem(){}

		internal ToolTreeViewItem(int index, int i, string path) : base(index, i, path) { }

		private static class Style
		{
			internal static GUIStyle lineStyle = "TV Line";
		}

		private float _subLabelWidth;
		internal float SubLabelWidth
		{
			get
			{
				if (_subLabelWidth <= 0)
				{
					_subLabelWidth = EditorStyles.miniLabel.CalcSize(new GUIContent(SubLabel)).x + 5f;
				}

				return _subLabelWidth;
			}
		}

		private float _labelWidth;
		internal float LabelWidth
		{
			get
			{
				if (_labelWidth <= 0)
				{
					_labelWidth = Style.lineStyle.CalcSize(new GUIContent(displayName)).x + 30f;
				}

				return _labelWidth;
			}
		}
	}
}
