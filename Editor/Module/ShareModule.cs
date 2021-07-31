using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Yorozu.EditorTools
{
	internal class ShareModule : Module
	{
		internal override string Name => "Share";
		internal override Texture Texture => EditorResources.Load<Texture>(EditorGUIUtility.isProSkin ? "d_UnityEditor.Graphs.AnimatorControllerTool" : "UnityEditor.Graphs.AnimatorControllerTool");
		internal override bool CanDrag => true;

		private YorozuToolShareObject _data;

		internal override void Enter()
		{
			_data = YorozuToolShareObject.Load();
			_data.UpdateShare += Reload;
		}

		internal override void Exit()
		{
			_data.UpdateShare -= Reload;
			_data = null;
		}

		internal override List<ToolTreeViewItem> GetItems()
		{
			if (_data == null)
				return new List<ToolTreeViewItem>();

			return GUIDsToGroup(_data.Share.Select(d => d.GUID).ToArray());
		}

		internal override bool CanSearchDraw(ToolTreeViewItem item)
		{
			return item.depth >= 1;
		}

		internal override bool DoubleClick(ToolTreeViewItem item)
		{
			OpenAsset(item);
			return false;
		}

		internal override void SingleClick(ToolTreeViewItem item) => SelectObject(item);

		internal override void GenerateMenu(TreeViewItem item, ref GenericMenu menu)
		{
			menu.AddItem(new GUIContent("Remove"), false, () =>
			{
				if (item.depth == 2)
					return;

				if (item.depth == 0)
				{
					var childGUIDs = item.children
						.Select(c => c.GetGUID())
						.ToArray();

					_data.Remove(childGUIDs);
					return;
				}

				_data.Remove(item.GetGUID());
				Reload();
			});
		}

		internal override IEnumerable<Object> GetDragObjects(IList<int> itemIds)
		{
			return itemIds.Select(EditorUtility.InstanceIDToObject)
				.Where(i => i != null);
		}
	}
}
