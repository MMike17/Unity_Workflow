using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

using static ProcessObj;

[CustomEditor(typeof(ProcessObj))]
class ProcessEditor : Editor
{
	private ProcessObj process;
	private bool editMode;

	private GUIStyle titleStyle;
	private GUIStyle centerStyle;
	private GUIStyle longTextStyle;
	private GUIStyle longInputStyle;

	public override void OnInspectorGUI()
	{
		process = Selection.activeObject as ProcessObj;
		GenerateIfNeeded();

		if (GUILayout.Button(editMode ? "Display mode" : "Edit mode"))
			editMode = !editMode;

		EditorGUILayout.Space();
		EditorGUILayout.Space();

		EditorGUILayout.LabelField("Description", titleStyle);
		EditorGUILayout.Space();

		if (editMode)
		{
			EditorGUILayout.LabelField("Short description", centerStyle);
			process.shortDescription = EditorGUILayout.TextArea(process.shortDescription);

			EditorGUILayout.Space();

			EditorGUILayout.LabelField("Full description", centerStyle);
			process.fullDescription = EditorGUILayout.TextArea(process.fullDescription);
		}
		else
		{
			if (string.IsNullOrEmpty(process.shortDescription))
				EditorGUILayout.HelpBox("No short description", MessageType.Warning);

			if (string.IsNullOrEmpty(process.fullDescription))
				EditorGUILayout.HelpBox("No description", MessageType.Warning);
			else
				EditorGUILayout.LabelField(process.fullDescription, longTextStyle, GUILayout.ExpandWidth(true));
		}

		EditorGUILayout.Space();
		EditorGUILayout.Space();

		EditorGUILayout.LabelField("Tasks", titleStyle);
		EditorGUILayout.Space();

		Task toDelete = null;

		foreach (Task task in process.tasks)
		{
			EditorGUILayout.BeginHorizontal();
			{
				if (editMode)
				{
					if (GUILayout.Button("Remove"))
					{
						toDelete = task;
						break;
					}

					task.title = EditorGUILayout.TextArea(task.title, longInputStyle, GUILayout.MinWidth(100));

					string[] names = ProcessCore.GetScriptNames();
					task.targetScriptIndex = EditorGUILayout.Popup(task.targetScriptIndex, names);
					task.targetScript = task.targetScriptIndex != -1 ? names[task.targetScriptIndex] : "";

					string[] lines = ProcessCore.GetAllLines(task.targetScript);
					task.targetIndex = EditorGUILayout.Popup(task.targetIndex, lines);
				}
				else
				{
					task.state = EditorGUILayout.Toggle(task.state, GUILayout.Width(15));

					if (task.state)
						GUI.color = Color.grey;

					if (GUILayout.Button(task.state ? StrikethroughText(task.title) : task.title, GUI.skin.label))
					{
						AssetDatabase.OpenAsset(
							ProcessCore.GetScriptWithName(task.targetScript),
							ProcessCore.GetLineIndex(task.targetScript, task.targetIndex)
						);
					}

					GUI.color = Color.white;
				}
			}
			EditorGUILayout.EndHorizontal();
		}

		if (toDelete != null)
			process.tasks.Remove(toDelete);

		EditorGUILayout.Space();

		if (GUILayout.Button("Add task"))
		{
			process.tasks.Add(new Task());
			editMode = true;
		}
	}

	private void GenerateIfNeeded()
	{
		if (titleStyle == null)
			titleStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };

		if (centerStyle == null)
			centerStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };

		if (longTextStyle == null)
			longTextStyle = new GUIStyle(GUI.skin.box) { wordWrap = true };

		if (longInputStyle == null)
			longInputStyle = new GUIStyle(GUI.skin.textField) { wordWrap = true };

		if (process.tasks == null)
			process.tasks = new List<Task>();

		process.tasks.ForEach(item => item.targetScriptIndex = ProcessCore.GetScriptIndex(item.targetScript));
	}

	private string StrikethroughText(string text)
	{
		string result = string.Empty;

		foreach (char c in text)
			result += c + "\u0336";

		return result;
	}
}