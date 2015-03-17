/**
 *	Editor Wizard for easily managing global defines in Unity
 *	Place in Assets/Editor folder, or if you choose to place elsewhere 
 *	be sure to also modify the DEF_MANAGER_PATH constant.
 *	@khenkel
 */

using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;

public class DefineManager : EditorWindow
{
	const string DEF_MANAGER_PATH = "Assets/Editor/DefineManager.cs";
	const string CSCODE_PATH = "Assets/";
	
	enum Compiler
	{
		CSharp,
		Editor,
		UnityScript,
		Boo
	}
	Compiler compiler = Compiler.Editor;

	// http://forum.unity3d.com/threads/93901-global-define/page2
	// Do not modify these paths
	const int COMPILER_COUNT = 4;
	const string CSHARP_PATH 		= "Assets/smcs.rsp";
	const string EDITOR_PATH 		= "Assets/gmcs.rsp";
	const string UNITYSCRIPT_PATH 	= "Assets/us.rsp";
	const string BOO_PATH 			= "Assets/boo.rsp";

	List<string> csDefines = new List<string>(); 
	List<string> csOtherDefines = new List<string>();
	List<string> booDefines = new List<string>(); 
	List<string> booOtherDefines = new List<string>();
	List<string> usDefines = new List<string>(); 
	List<string> usOtherDefines = new List<string>();
	List<string> editorDefines = new List<string>(); 
	List<string> editorOtherDefines = new List<string>();

	[MenuItem("Window/Define Manager")]
	public static void OpenDefManager()
	{
		EditorWindow.GetWindow<DefineManager>(true, "Global Define Manager", true);
	}

	void OnEnable()
	{
		csDefines = ParseRspFile(CSHARP_PATH, ref csOtherDefines);
		usDefines = ParseRspFile(UNITYSCRIPT_PATH, ref usOtherDefines);
		booDefines = ParseRspFile(BOO_PATH, ref booOtherDefines);
		editorDefines = ParseRspFile(EDITOR_PATH, ref editorOtherDefines);
	}

	List<string> defs;
	List<string> otherDefs;
	
	Vector2 scroll = Vector2.zero;
	void OnGUI()
	{
		Color oldColor = GUI.backgroundColor;

		GUILayout.BeginHorizontal();
		for(int i = 0; i < COMPILER_COUNT; i++)	
		{
			if(i == (int)compiler)
				GUI.backgroundColor = Color.gray;

			GUIStyle st;
			switch(i)
			{
				case 0:
					st = EditorStyles.miniButtonLeft;
					break;
				case COMPILER_COUNT-1:
					st = EditorStyles.miniButtonRight;
					break;
				default:
					st = EditorStyles.miniButtonMid;
					break;
			}

			if(GUILayout.Button( ((Compiler)i).ToString(), st))
				compiler = (Compiler)i;

			GUI.backgroundColor = oldColor;
		}
		GUILayout.EndHorizontal();
		
		switch(compiler)
		{
			case Compiler.CSharp:
				defs = csDefines;
				otherDefs = csOtherDefines;
				break;

			case Compiler.Editor:
				defs = editorDefines;
				otherDefs = editorOtherDefines;
				break;

			case Compiler.UnityScript:
				defs = usDefines;
				otherDefs = usOtherDefines;
				break;

			case Compiler.Boo:
				defs = booDefines;
				otherDefs = booOtherDefines;
				break;
		}

		GUILayout.Label(compiler.ToString() + " User Defines");

		scroll = GUILayout.BeginScrollView(scroll);
		for(int i = 0; i < defs.Count; i++)
		{
			GUILayout.BeginHorizontal();
				
				defs[i] = EditorGUILayout.TextField(defs[i]);
				
				GUI.backgroundColor = Color.red;
				if(GUILayout.Button("x", GUIStyle.none, GUILayout.MaxWidth(18)))
					defs.RemoveAt(i);
				GUI.backgroundColor = oldColor;

			GUILayout.EndHorizontal();

		}
		
		GUILayout.Space(4);

		GUI.backgroundColor = Color.cyan;
		if(GUILayout.Button("Add"))	
			defs.Add("NEW_DEFINE");

		GUILayout.EndScrollView();


		GUILayout.BeginHorizontal();
			GUI.backgroundColor = Color.green;
			if( GUILayout.Button("Apply") )
			{
				SetDefines(compiler, defs);
				AssetDatabase.ImportAsset(DEF_MANAGER_PATH, ImportAssetOptions.ForceUpdate);
				AssetDatabase.ImportAsset(CSCODE_PATH, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ImportRecursive);
				OnEnable();
			}

			GUI.backgroundColor = Color.red;
			if(GUILayout.Button("Apply All", GUILayout.MaxWidth(64)))
			{
				for(int i = 0; i < COMPILER_COUNT; i++)
				{
					SetDefines((Compiler)i, defs);
					AssetDatabase.ImportAsset(DEF_MANAGER_PATH, ImportAssetOptions.ForceUpdate);
					OnEnable();
				}
				AssetDatabase.ImportAsset(CSCODE_PATH, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ImportRecursive);
			}
		
		GUILayout.EndHorizontal();
		GUI.backgroundColor = oldColor;
	}

	void SetDefines(Compiler compiler, List<string> defs)
	{
		switch(compiler)
		{
			case Compiler.CSharp:
				WriteDefines(CSHARP_PATH, defs, otherDefs);
				break;

			case Compiler.UnityScript:
				WriteDefines(UNITYSCRIPT_PATH, defs, otherDefs);
				break;
			
			case Compiler.Boo:
				WriteDefines(BOO_PATH, defs, otherDefs);
				break;

			case Compiler.Editor:
				WriteDefines(EDITOR_PATH, defs, otherDefs);
				break;
		}
	}

	List<string> ParseRspFile(string path, ref List<string> otherdefs)
	{
		otherdefs.Clear();
		
		if(!File.Exists(path))
		{
			return new List<string>();
		}

		string[] lines = File.ReadAllLines(path);
		List<string> defs = new List<string>();

		foreach(string cheese in lines)
		{
			if(cheese.StartsWith("-define:"))
			{
				string[] macrodefs = cheese.Replace("-define:", "").Split(';');
				foreach(string macro in macrodefs)
				{
					if(! string.IsNullOrEmpty(macro.Trim()))
						defs.Add(macro.Trim());
				}
			}
			else
			{
				otherdefs.Add(cheese);
			}
		}

		return defs;
	}

	void WriteDefines(string path, List<string> defs, List<string> otherdefs)
	{
		if(defs.Count < 1 && File.Exists(path) && otherdefs.Count < 1)
		{
			File.Delete(path);
		
			if(File.Exists(path + ".meta"))
				File.Delete(path + ".meta");
				
			AssetDatabase.Refresh();
			return;
		}

		StringBuilder sb = new StringBuilder();
		
		for(int i = 0; i < defs.Count; i++)
		{
			sb.AppendLine("-define:" + defs[i] + ";");
		}
		
//		sb.AppendLine("");
		for(int i = 0; i < otherdefs.Count; i++)
		{
			sb.AppendLine(otherdefs[i]);
		}

		using (StreamWriter writer = new StreamWriter(path, false))
		{
			writer.Write(sb.ToString());
		}
	}
}
