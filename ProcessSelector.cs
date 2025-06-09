using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

using Object = UnityEngine.Object;

/// <summary>Used to load and parse tasks from scripts</summary>
static class ProcessCore
{
	const string SAVE_PATH = "WorkflowSettings.data";

	private static Dictionary<string, Dictionary<int, string>> detectedLines;
	private static List<Object> detectedScripts;

	private static Settings _settings;
	public static Settings settings
	{
		get
		{
			if (_settings == null)
			{
				string[] guids = AssetDatabase.FindAssets(SAVE_PATH.Split('.')[0]);

				if (guids.Length > 0)
					_settings = JsonUtility.FromJson<Settings>(File.ReadAllText(AssetDatabase.GUIDToAssetPath(guids[0])));
				else
					_settings = new Settings();

				RefreshCache();
			}

			return _settings;
		}
	}

	public static void RefreshCache()
	{
		detectedLines = new Dictionary<string, Dictionary<int, string>>();
		detectedScripts = new List<Object>();

		if (string.IsNullOrWhiteSpace(settings.codeMarker))
			return;

		foreach (string guid in AssetDatabase.FindAssets("t:Script"))
		{
			string path = AssetDatabase.GUIDToAssetPath(guid);

			if (!path.StartsWith("Assets"))
				continue;

			string fileName = new FileInfo(path).Name.TrimEnd(".cs".ToCharArray());
			TextAsset script = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
			string[] lines = script.text.Split('\n');
			string marker = "// " + settings.codeMarker + " : ";

			for (int i = 0; i < lines.Length; i++)
			{
				if (lines[i].Contains(marker))
				{
					if (!detectedScripts.Contains(script))
						detectedScripts.Add(script);

					string[] words = lines[i].Split(marker);

					if (!detectedLines.ContainsKey(fileName))
						detectedLines.Add(fileName, new Dictionary<int, string>());

					string line = words[1] == " " ? words[2].TrimStart(' ') : words[1].Trim(' ');
					detectedLines[fileName].Add(i, line);
				}
			}
		}
	}

	public static void SaveSettings()
	{
		File.WriteAllText(Path.Combine(Application.dataPath, SAVE_PATH), JsonUtility.ToJson(settings, true));
	}

	public static string[] GetScriptNames()
	{
		if (detectedScripts == null)
			RefreshCache();

		List<string> names = new List<string>();
		detectedScripts.ForEach(item => names.Add(item.name));
		return names.ToArray();
	}

	public static string[] GetAllLines(string fileName)
	{
		if (detectedLines == null)
			RefreshCache();

		if (detectedLines.ContainsKey(fileName))
		{
			string[] lines = new string[detectedLines[fileName].Values.Count];
			detectedLines[fileName].Values.CopyTo(lines, 0);
			return lines;
		}

		return new string[0];
	}

	public static Object GetScriptWithName(string name) => detectedScripts.Find(item => item.name == name);

	public static int GetLineIndex(string scriptName, int index)
	{
		int count = 0;

		if (detectedLines == null)
			RefreshCache();

		if (detectedLines.ContainsKey(scriptName))
		{
			foreach (KeyValuePair<int, string> pair in detectedLines[scriptName])
			{
				if (count == index)
					return pair.Key + 1;

				count++;
			}
		}

		return count;
	}

	public static int GetScriptIndex(string scriptName)
	{
		if (detectedScripts == null)
			RefreshCache();

		Object selected = detectedScripts.Find(item => item.name == scriptName);
		return selected != null ? detectedScripts.IndexOf(selected) : -1;
	}

	[Serializable]
	public class Settings
	{
		public string codeMarker;
		public string processSavePath;

		public Settings()
		{
			codeMarker = string.Empty;
			processSavePath = string.Empty;
		}
	}
}

/// <summary>Editor window made to select and create processes</summary>
class ProcessSelector : EditorWindow
{
	private List<ProcessObj> processes;
	private string newMarker;
	private string newPath;
	private Vector2 scroll;

	private GUIStyle titleStyle;
	private GUIStyle centerStyle;

	[MenuItem("Tools/Workflow/Process Database")]
	public static void ShowWindow()
	{
		ProcessSelector window = GetWindow<ProcessSelector>();
		window.titleContent = new GUIContent("Process Database");
		window.minSize = new Vector2(250, 230);
		window.Show();
	}

	void OnGUI()
	{
		GenerateIfNeeded();

		EditorGUILayout.Space();
		EditorGUILayout.LabelField("Settings", titleStyle);
		EditorGUILayout.Space();

		newMarker = TextFieldApply("Code marker :", newMarker, ProcessCore.settings.codeMarker, value =>
		{
			ProcessCore.settings.codeMarker = value;
			ProcessCore.SaveSettings();
			ProcessCore.RefreshCache();
		});

		newPath = TextFieldApply("Process save path : ", newPath, ProcessCore.settings.processSavePath, value =>
		{
			ProcessCore.settings.processSavePath = value;
			ProcessCore.SaveSettings();
		});

		EditorGUILayout.Space();

		if (GUILayout.Button("Refresh Processes"))
			RefreshCache();

		EditorGUILayout.Space();
		EditorGUILayout.Space();

		EditorGUILayout.LabelField("Processes", titleStyle);

		if (processes.Count > 0)
		{
			scroll = EditorGUILayout.BeginScrollView(scroll, GUI.skin.box);
			{
				List<int> toRemove = new List<int>();

				for (int i = 0; i < processes.Count; i++)
				{
					if (processes[i] == null)
					{
						toRemove.Add(i);
						continue;
					}

					EditorGUILayout.BeginHorizontal();
					{
						EditorGUILayout.Space();

						if (GUILayout.Button(processes[i].name))
							Selection.activeObject = processes[i];

						EditorGUILayout.Space();
					}
					EditorGUILayout.EndHorizontal();

					if (!string.IsNullOrEmpty(processes[i].shortDescription))
						EditorGUILayout.LabelField(processes[i].shortDescription, centerStyle);

					EditorGUILayout.Space();
				}

				toRemove.Reverse();
				toRemove.ForEach(item => processes.RemoveAt(item));
			}
			EditorGUILayout.EndScrollView();

			EditorGUILayout.Space();
		}

		if (GUILayout.Button("Create process"))
		{
			ProcessObj newProcess = Editor.CreateInstance<ProcessObj>();
			string name = "NewProcess";
			int index = 1;
			bool pathCheck = true;
			string savePath = Path.Combine(ProcessCore.settings.processSavePath, name);

			while (pathCheck)
			{
				if (File.Exists(Path.Combine(Application.dataPath, savePath + ".asset")))
				{
					name = "NewProcess" + index;
					savePath = Path.Combine(ProcessCore.settings.processSavePath, name);
					index++;
				}
				else
					pathCheck = false;
			}

			newProcess.name = name;
			processes.Add(newProcess);

			CreateFolders(Path.Combine("Assets", ProcessCore.settings.processSavePath));
			AssetDatabase.CreateAsset(newProcess, Path.Combine("Assets", savePath + ".asset"));
			AssetDatabase.Refresh();

			Selection.activeObject = newProcess;
		}

		EditorGUILayout.Space();
	}

	private void GenerateIfNeeded()
	{
		if (titleStyle == null)
			titleStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };

		if (centerStyle == null)
			centerStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };

		if (string.IsNullOrEmpty(newMarker))
			newMarker = ProcessCore.settings.codeMarker;

		if (string.IsNullOrEmpty(newPath))
			newPath = ProcessCore.settings.processSavePath;

		if (processes == null)
			RefreshCache();
	}

	private void RefreshCache()
	{
		processes = new List<ProcessObj>();
		string[] guids = AssetDatabase.FindAssets("t:ScriptableObject");

		if (guids.Length > 0)
		{
			foreach (string guid in guids)
			{
				Object obj = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid));

				if (obj is ProcessObj)
					processes.Add(obj as ProcessObj);
			}
		}

		ProcessCore.RefreshCache();
	}

	private string TextFieldApply(string label, string value, string check, Action<string> OnApply)
	{
		string newValue;

		EditorGUILayout.BeginHorizontal();
		{
			newValue = EditorGUILayout.TextField(label, value);

			if (newValue != check && GUILayout.Button("Apply", GUILayout.Width(100)))
				OnApply?.Invoke(newValue);
		}
		EditorGUILayout.EndHorizontal();

		return newValue;
	}

	private void CreateFolders(string path)
	{
		if (!Directory.Exists(path))
		{
			DirectoryInfo info = new DirectoryInfo(path);
			CreateFolders(info.Parent.FullName);
			Directory.CreateDirectory(path);
		}
	}
}