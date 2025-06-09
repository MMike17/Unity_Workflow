using UnityEditor;
using UnityEngine;

/// <summary>Popup for creation of feature development environment</summary>
class WorkflowInputPopup : PopupWindowContent
{
	string folderName;

	public override void OnOpen()
	{
		base.OnOpen();
		folderName = "";
	}

	public override void OnGUI(Rect rect)
	{
		EditorGUILayout.LabelField("Feature name", Workflow.TitleStyle);
		EditorGUILayout.Space();

		folderName = EditorGUILayout.TextField(folderName);

		EditorGUILayout.Space();

		if (GUILayout.Button("Create"))
			Workflow.CreateDevFeature(folderName);
	}
}