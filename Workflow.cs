using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System;

/// <summary>Tool to help with agile development of games</summary>
public class Workflow : EditorWindow
{
	const string BASE_FOLDER = "Assets";
	const string TEST_FOLDER_NAME = "TEST";

	readonly static List<string> basePaths = new List<string>(new string[]
	{
		"Scripts", // 0
		"Scripts/Editor",
		"Scripts/Managers",
		"Scripts/Data",
		"Meshes", // 4
		"Shaders", // 5
		"Textures", // 6
		"Materials", // 7
		"Prefabs", // 8
		"Prefabs/UI",
		"Animations", // 10
		"Audios", // 11
		"Audios/SFX",
		"Audios/Music",
		"Scenes", // 14
		"Data"
	});

	readonly static Dictionary<string, int> extentionFolderTarget = new Dictionary<string, int>()
	{
		["cs"] = 0,

		["obj"] = 4,
		["fbx"] = 4,
		["blend"] = 4,

		["shadergraph"] = 5,
		["shader"] = 5,

		["png"] = 6,
		["jpg"] = 6,
		["jpeg"] = 6,
		["psd"] = 6,
		["gif"] = 6,

		["mat"] = 7,

		["prefab"] = 8,

		["anim"] = 10,
		["controller"] = 10,
		["overrideController"] = 10,
		["mask"] = 10,

		["wav"] = 11,
		["mp3"] = 11,
		["ogg"] = 11,

		["unity"] = 14,

		["asset"] = 15
	};

	public static GUIStyle TitleStyle
	{
		get
		{
			if (titleStyle == null)
			{
				titleStyle = new GUIStyle(GUI.skin.label)
				{
					alignment = TextAnchor.MiddleCenter,
					fontStyle = FontStyle.Bold
				};
			}

			return titleStyle;
		}
	}

	static GUIStyle titleStyle;

	Vector2 scroll;

	[MenuItem("Tools/Workflow/Generate folder structure")]
	static void GenerateFolderStructure()
	{
		List<string> paths = new List<string>(basePaths);

		for (int i = 6; i <= 8; i++)
			paths.Add(Path.Combine("Resources", basePaths[i]));

		paths.ForEach(path =>
		{
			string fullPath = GetFullPath(path);

			if (!Directory.Exists(fullPath))
				CreateFolder(fullPath);
		});
	}

	[MenuItem("Tools/Workflow/New feature environment")]
	static void SetupFeatureFolders()
	{
		PopupWindow.Show(new Rect(200, 0, 300, 10), new WorkflowInputPopup());
	}

	static void CreateFolder(string fullPath)
	{
		CreateFolderRecursive(fullPath);
		AssetDatabase.Refresh();
	}

	static void CreateFolderRecursive(string fullPath)
	{
		if (Directory.Exists(fullPath))
			return;
		else
		{
			DirectoryInfo infos = new DirectoryInfo(fullPath);
			string projectPath = MakeProjectRelative(infos.Parent.FullName.Replace('\\', '/'));

			if (!Directory.Exists(infos.Parent.FullName))
				CreateFolderRecursive(infos.Parent.FullName);

			string assetGUID = AssetDatabase.CreateFolder(projectPath, infos.Name);

			if (string.IsNullOrEmpty(assetGUID))
				Debug.LogError("Error while creating folder at path " + fullPath);
		}
	}

	public static void CreateDevFeature(string featureName)
	{
		string fullPath = Path.Combine(BASE_FOLDER, TEST_FOLDER_NAME, featureName);

		if (Directory.Exists(fullPath))
		{
			EditorUtility.DisplayDialog(
				"Already have feature",
				"Feature with name \"" + featureName + "\" already exists",
				"Okay"
			);
		}
		else
		{
			CreateFolder(fullPath);

			EditorUtility.DisplayDialog(
				"Creation suceeded",
				"Folder for feature \"" + featureName + "\" has been created at path " + MakeProjectRelative(fullPath),
				"Okay"
			);
		}
	}

	[MenuItem("Tools/Workflow/Migration window")]
	static void ShowWindow()
	{
		Workflow window = GetWindow<Workflow>();
		window.titleContent = new GUIContent("Workflow");
		window.minSize = new Vector2(300, 200);
		window.Show();
	}

	void OnGUI()
	{
		string rootFolderPath = GetFullPath(TEST_FOLDER_NAME);
		string[] testFolders = Directory.Exists(rootFolderPath) ? Directory.GetDirectories(rootFolderPath) : new string[0];

		scroll = EditorGUILayout.BeginScrollView(scroll);
		{
			EditorGUILayout.BeginVertical();
			{
				EditorGUILayout.Space();

				EditorGUILayout.LabelField("Workflow Migrations", TitleStyle);

				EditorGUILayout.Space();

				foreach (string path in testFolders)
					MakeFlexible(() => DisplayFolder(path));
			}
			EditorGUILayout.EndVertical();
		}
		EditorGUILayout.EndScrollView();
	}

	void MakeFlexible(Action displayCallback)
	{
		EditorGUILayout.BeginHorizontal();
		{
			EditorGUILayout.Space();

			displayCallback?.Invoke();

			EditorGUILayout.Space();
		}
		EditorGUILayout.EndHorizontal();
	}

	void DisplayFolder(string fullPath)
	{
		EditorGUILayout.BeginHorizontal();
		{
			EditorGUILayout.LabelField(new DirectoryInfo(fullPath).Name);
			EditorGUILayout.Space();

			if (GUILayout.Button("Migrate"))
				MigrateFolder(fullPath);
		}
		EditorGUILayout.EndHorizontal();
	}

	static string GetFullPath(string projectPath) => Path.Combine(Application.dataPath, projectPath);

	static string MakeProjectRelative(string fullPath) => fullPath.Replace(
		Application.dataPath,
		BASE_FOLDER
	);

	void MigrateFolder(string fullPath)
	{
		string featureName = new DirectoryInfo(fullPath).Name;
		List<string> testFolders = new List<string>(Directory.GetDirectories(fullPath));
		List<string> allFiles = new List<string>(Directory.GetFiles(fullPath));
		allFiles.RemoveAll(item => item.Contains(".meta"));
		string[] floatingFiles = allFiles.ToArray();

		// detect invalid folders
		List<string> invalidFolders = new List<string>(
			testFolders.FindAll(folder => !basePaths.Contains(new DirectoryInfo(folder).Name))
		);

		// manage floating files
		Dictionary<string, string> floatingFileToFolder = new Dictionary<string, string>();
		List<string> invalidFiles = new List<string>();

		foreach (string floatingFile in floatingFiles)
		{
			if (!floatingFile.Contains('.'))
			{
				Debug.LogError("Couldn't find extension for file with path \"" + floatingFile + "\"");
				continue;
			}

			string[] frags = floatingFile.Split('.');
			string extension = frags[frags.Length - 1];

			if (extentionFolderTarget.ContainsKey(extension))
				floatingFileToFolder.Add(floatingFile, basePaths[extentionFolderTarget[extension]]);
			else
				invalidFiles.Add(floatingFile);
		}

		// move folders
		foreach (string testFolder in testFolders)
		{
			if (invalidFolders.Contains(testFolder))
				continue;

			string targetPath = Path.Combine(BASE_FOLDER, new DirectoryInfo(testFolder).Name, featureName);
			string targetDirectory = new DirectoryInfo(targetPath).Parent.FullName;

			if (!Directory.Exists(targetDirectory))
				CreateFolder(targetDirectory);

			string errorCode = AssetDatabase.MoveAsset(MakeProjectRelative(testFolder), targetPath);

			if (!string.IsNullOrEmpty(errorCode))
				Debug.LogError(errorCode);
		}

		// move files
		foreach (string floatingFile in floatingFiles)
		{
			if (!floatingFileToFolder.ContainsKey(floatingFile))
				continue;

			string fileName = new FileInfo(floatingFile).Name;
			string targetPath = Path.Combine(BASE_FOLDER, floatingFileToFolder[floatingFile], featureName, fileName);
			string folderPath = new FileInfo(targetPath).DirectoryName;

			if (!Directory.Exists(folderPath))
				CreateFolder(folderPath);

			string[] frags = floatingFile.Split('.');

			if (frags[frags.Length - 1] == "cs")
				RemoveTESTNamespace(floatingFile);

			string errorCode = AssetDatabase.MoveAsset(MakeProjectRelative(floatingFile), targetPath);

			if (!string.IsNullOrEmpty(errorCode))
				Debug.LogError(errorCode);
		}

		// delete source folder
		if (invalidFolders.Count != 0 || invalidFiles.Count != 0)
		{
			string filesContent = invalidFiles.Count > 0 ? "Invalid files :\n" : "";
			string foldersContent = invalidFolders.Count > 0 ? "Invalid folders :\n" : "";

			foreach (string invalidFile in invalidFiles)
				filesContent += "\n" + MakeProjectRelative(invalidFile);

			foreach (string invalidFolder in invalidFolders)
				foldersContent += "\n" + MakeProjectRelative(invalidFolder);

			string space = "\n\n";
			filesContent += space;
			foldersContent += space;

			string introContent = "Some files and folders could not be automatically moved." + space;
			string endContent = "They will remain in their source folder (" + MakeProjectRelative(fullPath) + ").";

			EditorUtility.DisplayDialog(
				"Invalid folders or files",
				introContent + filesContent + foldersContent + endContent,
				"Okay"
			);

			Debug.Log(introContent + filesContent + foldersContent + endContent);
		}
		else
		{
			AssetDatabase.DeleteAsset(MakeProjectRelative(fullPath));

			EditorUtility.DisplayDialog(
				"Migration completed",
				"Assets in folder \"" + new DirectoryInfo(fullPath).Name + "\" at path " + MakeProjectRelative(fullPath) + " have been migrated.\n\nSource folder has been deleted.",
				"Okay"
			);
		}

		AssetDatabase.Refresh();
	}

	static void RemoveTESTNamespace(string scriptPath)
	{
		string scriptContent = File.ReadAllText(scriptPath);

		if (!scriptContent.Contains("namespace TEST"))
			return;

		List<string> lines = new List<string>(scriptContent.Split("\r\n"));
		List<int> toRemove = new List<int>();
		bool foundNamespace = false;
		int braketsCount = 0;

		for (int i = 0; i < lines.Count; i++)
		{
			string currentLine = lines[i];

			if (foundNamespace)
			{
				if (currentLine.Contains("{"))
					braketsCount++;
				else if (currentLine.Contains("}"))
					braketsCount--;

				// supposed to fix indentation
				if (braketsCount > 0)
				{
					// tabs
					if (currentLine.StartsWith('\t'))
						lines[i].Remove(0, 1);

					// spaces
					if (currentLine.StartsWith("    "))
						lines[i].Remove(0, 4);
				}

				if (braketsCount == 0)
				{
					toRemove.Add(i);
					break;
				}
			}

			if (currentLine.Contains("namespace TEST"))
			{
				foundNamespace = true;
				braketsCount = 1;

				toRemove.Add(i);
				toRemove.Add(i + 1);
				i++;
			}
		}

		// start from the bottom to not displace indeces
		toRemove.Reverse();
		toRemove.ForEach(item => lines.RemoveAt(item));

		File.WriteAllLines(scriptPath, lines);
		AssetDatabase.ImportAsset(MakeProjectRelative(scriptPath));
	}
}

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