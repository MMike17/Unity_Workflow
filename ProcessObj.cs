using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Process you can design</summary>
public class ProcessObj : ScriptableObject
{
	public string shortDescription;
	public string fullDescription;
	public List<Task> tasks;

	[Serializable]
	public class Task
	{
		public bool state;
		public string title;
		public int targetScriptIndex;
		public string targetScript;
		public int targetIndex;
	}
}