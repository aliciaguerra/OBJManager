using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.IO;
using System;

public class ImportObjManager : MonoBehaviour
{

	public int maxPoints = 30000;						// Use this to limit maximum vertices of objects. Clamped to 65534
	public bool combineMultipleGroups = true;			// If true, .obj files containing more than one object will be treated as one object
	public bool useSubmeshesWhenCombining = true;		// If true, combined objects use submeshes, instead of a single mesh with one material
	public bool computeTangents = false;				// Needed if using normal-mapped shaders
	public bool useSuppliedNormals = false;				// If false, normals are calculated by Unity instead of using the normals in the .obj file
	public bool overrideDiffuse = false;				// If true, the color in the material will be used instead of the Kd color in the MTL file
	public bool overrideSpecular = false;				// If true, the specular color in the material will be used instead of the Ks color in the MTL file
	public bool overrideAmbient = false;				// If true, the emissive color in the material will be used instead of the Ka color in the MTL file
	public bool suppressWarnings = false;				// If true, no warnings will be output, although errors will still be printed
	public Vector3 scaleFactor = new Vector3 (1, 1, 1);	// Scale meshes by this amount when they are converted
	public Vector3 objRotation = new Vector3 (0, 0, 0);	// Rotate meshes by this amount when they are converted
	public Vector3 objPosition = new Vector3 (0, 0, 0);	// Move meshes by this amount when they are converted
	
	
	bool useWindowGlassShader;
	bool useWindowGlassBlackShader;
	bool usePaintShader;
	bool useDiffuseShader;
	bool useChromeShader;
	bool useDiffuseBumpedShader;
	bool useTransparentShader;
	bool useLightGlassShader;
	public UISlider sli_Loading;
	public UIPanel pnl_Panel;
	public UIPanel pnl_PanelInstruction;
	public static ImportObjManager _use = null;
	private int indexx;
	private DeformCar _deformCar;
	private SceneManager _sceneManager;
	Texture2D _glassTexture;
	Texture2D _carTexture;
	
	/// <summary>
	/// Awake this instance.
	/// </summary>
	void Awake ()
	{
		Debug.Log ("ImportObjManager awake");
		_deformCar = GetComponent<DeformCar> ();
		_sceneManager = GetComponent<SceneManager> ();
		_use = this;
		
		useWindowGlassShader = false;
		useWindowGlassBlackShader = false;
		usePaintShader = false;
		useDiffuseShader = false;
		useChromeShader = false;
		useDiffuseBumpedShader = false;
		useTransparentShader = false;
		useLightGlassShader = false;
			
		//hide all Instruction and button
		pnl_PanelInstruction.alpha = 0;
		pnl_Panel.alpha = 0;
	}
	
	float TotalSec = 0;

	/// <summary>
	/// Loads the logo by calling StartCoroutine.
	/// </summary>
	/// <param name="url">URL.</param>
	public void LoadLogo (string url)
	{
		Debug.Log ("URL " + url);

		StartCoroutine (SceneManager.share.loadLogoImg (url));

	}

	/// <summary>
	/// Call method _LoadInFolder by StartCoroutine
	/// </summary>
	/// <param name="s_FolderName">S_ folder name.</param>
	public void LoadInFolder (string s_FolderName)
	{
		Debug.Log ("path " + s_FolderName);
		StartCoroutine (_LoadInFolder (s_FolderName));
	}

	/// <summary>
	/// Loads cars the in folder including object, material, tga (texture) 
	/// using multithread
	/// </summary>
	/// <returns>The load in folder.</returns>
	/// <param name="s_FolderName">S_ folder name.</param>
	private IEnumerator _LoadInFolder (string s_FolderName)
	{
		Debug.Log ("LoadInFolder");
		
		float count = 0;
		float percent = 0;
		
		
//		float t = Time.realtimeSinceStartup;	
//		try
//		{
		//////////////////set up all texture using in scene//////////////////
		_sceneManager.SetAllTexture (s_FolderName);
		_glassTexture = _sceneManager.glassTexture;
		_carTexture = _sceneManager.carTexture;
		
		//////////////////set up all texture using in scene//////////////////
		
		List<string> m_Objects = new List<string> ();
		string m_Material = "";
		Hashtable m_hashObj;
		//		init hash list files
		indexx = 0;
		m_hashObj = new Hashtable ();
		
		// create file path of mtl / obj
		DirectoryInfo info = new DirectoryInfo (s_FolderName);
		FileInfo [] fileInfo = info.GetFiles (); 
			
		//check if the folder objects file is empty
		try {
			if (System.IO.Directory.GetFiles (s_FolderName, "*.obj", SearchOption.TopDirectoryOnly).Length == 0
				|| System.IO.Directory.GetFiles (s_FolderName, "*.mtl", SearchOption.TopDirectoryOnly).Length == 0
				|| System.IO.Directory.GetFiles (s_FolderName, "*.tga", SearchOption.TopDirectoryOnly).Length == 0
					) {
				throw new System.Exception ("The folder contain models is empty. Please check!!!!");
			}
		} catch (System.Exception e) {
			Debug.Log ("ERROR IMPORT INSIDE CAR===");	
			Debug.Log (e.Message);
		}
			
		//search obj / mtl files
		foreach (FileInfo f in fileInfo) {
			if (f.Extension.Contains (".obj")) {
				m_Objects.Add (f.FullName);
				indexx++;
				
			} else if (f.Extension.Contains (".mtl")) {
				m_Material = f.FullName;
			}
		}
		
		for (int i = 0; i < m_Objects.Count; i++) {
			m_hashObj.Add (m_Objects [i], m_Material);	
		}
		
		
		float totalParts = m_hashObj.Count + 2;// 1 for init phase, 1 for ending phase

		GameObject carObject = new GameObject ("Car");
		
		//load all material files into materials Dictionary
		Dictionary<string, Material> materials = new Dictionary<string, Material> ();
		foreach (DictionaryEntry d in m_hashObj) {
			string val = d.Value.ToString ();
			string mtlString = File.ReadAllText (val);
			ParseMTL (materials, mtlString, null, null, s_FolderName);
			break;
		}
		
		
		//save all material into Hashtable
		Hashtable hash = new Hashtable ();
		foreach (var n in materials) {
			Material mat = n.Value;

			if (mat.mainTexture != null) {
				hash.Add (mat, mat.mainTexture);
			}
		}
	
		sli_Loading.gameObject.transform.parent.GetComponent<UIPanel> ().alpha = 1;
		
		//percent for init phase
		count++;
		percent = count / totalParts;
		sli_Loading.sliderValue = percent;
		
		foreach (DictionaryEntry d in m_hashObj) {
			string key = d.Key.ToString ();
				
#if (MULTITHREADING) 				
			UnityThreading.ActionThread t1 = null;	
			Debug.Log ("Create thread" + Time.time);
			t1 = UnityThreadHelper.CreateThread(() => {
#endif
			string objString = File.ReadAllText (key);
				
					
			ConvertString (objString, materials, s_FolderName, carObject);
			count++;
				
			percent = count / totalParts;
					
			sli_Loading.sliderValue = percent;
			yield return new WaitForSeconds (0.000001f);

			ExternalFunctions._updatePercentage(percent);
								
#if (MULTITHREADING) 						
			});
#endif			
		}
			

#if !(MULTITHREADING) 		
			
		_sceneManager.setUpCar (carObject);
		//percent for ending phase
		count++;
		percent = count / totalParts;
		sli_Loading.sliderValue = percent;
		yield return new WaitForSeconds (0.5f);
		
		//restore it from Hashtable from above
		foreach (DictionaryEntry entry in hash) {
			Material mat = (Material)entry.Key;
			Texture2D val = (Texture2D)entry.Value;
			mat.mainTexture = val;
		}

		//when load done show all panel instruction and GUI buttons
		if (percent == 1) {
			pnl_PanelInstruction.alpha = 1;
			pnl_Panel.alpha = 1;
			sli_Loading.gameObject.transform.parent.gameObject.GetComponent<Fade> ().enableFade ();
			SceneManager.share.checkLogo ();
		}
		
#endif		

	}	

	/// <summary>
	/// Converts the string to array of gameobjects.
	/// </summary>
	/// <returns>Array of gameobjects.</returns>
	/// <param name="objString">Object string.</param>
	public GameObject[] ConvertString (string objString)
	{
		Dictionary<string, Material> materials = null;
		return ConvertString (ref objString, materials, null, null, "", null);
	}

	/// <summary>
	/// Converts the string to array of gameobjects.
	/// </summary>
	/// <returns>The string.</returns>
	/// <param name="objString">Object string.</param>
	/// <param name="materials">Materials.</param>
	public GameObject[] ConvertString (string objString, Dictionary<string, Material> materials)
	{
		return ConvertString (ref objString, materials, null, null, "", null);
	}

	/// <summary>
	/// Converts the string to array of gameobjects.
	/// </summary>
	/// <returns>The string.</returns>
	/// <param name="objString">Object string.</param>
	/// <param name="materials">Materials.</param>
	/// <param name="filePath">File path.</param>
	/// <param name="carObject">Car object.</param>
	public GameObject[] ConvertString (string objString, Dictionary<string, Material> materials, string filePath, GameObject carObject)
	{
		return ConvertString (ref objString, materials, null, null, filePath, carObject);
	}

	/// <summary>
	/// Converts the string to array of gameobjects.
	/// </summary>
	/// <returns>The string.</returns>
	/// <param name="objString">Object string.</param>
	/// <param name="standardMaterial">Standard material.</param>
	public GameObject[] ConvertString (string objString, Material standardMaterial)
	{
		Dictionary<string, Material> materials = null;
		return ConvertString (ref objString, materials, standardMaterial, null, "", null);
	}

	/// <summary>
	/// Converts the string to array of gameobjects.
	/// </summary>
	/// <returns>The string.</returns>
	/// <param name="objString">Object string.</param>
	/// <param name="materials">Materials.</param>
	/// <param name="standardMaterial">Standard material.</param>
	public GameObject[] ConvertString (string objString, Dictionary<string, Material> materials, Material standardMaterial)
	{
		return ConvertString (ref objString, materials, standardMaterial, null, "", null);
	}

	/// <summary>
	/// Converts the string.
	/// </summary>
	/// <returns>The string.</returns>
	/// <param name="objString">Object string.</param>
	/// <param name="materials">Materials.</param>
	/// <param name="standardMaterial">Standard material.</param>
	/// <param name="transparentMaterial">Transparent material.</param>
	public GameObject[] ConvertString (string objString, Dictionary<string, Material> materials, Material standardMaterial, Material transparentMaterial)
	{
		return ConvertString (ref objString, materials, standardMaterial, transparentMaterial, "", null);
	}

	/// <summary>
	/// Converts the file to array of game objects.
	/// </summary>
	/// <returns>The file.</returns>
	/// <param name="objFilePath">Object file path.</param>
	/// <param name="useMtl">If set to <c>true</c> use mtl.</param>
	public GameObject[] ConvertFile (string objFilePath, bool useMtl)
	{
		return ConvertFile (objFilePath, useMtl, null, null, null);
	}

	/// <summary>
	/// Converts the file to array of game objects.
	/// </summary>
	/// <returns>The file.</returns>
	/// <param name="objFilePath">Object file path.</param>
	/// <param name="useMtl">If set to <c>true</c> use mtl.</param>
	/// <param name="standardMaterial">Standard material.</param>
	public GameObject[] ConvertFile (string objFilePath, bool useMtl, Material standardMaterial)
	{
		return ConvertFile (objFilePath, useMtl, standardMaterial, null, null);
	}

	/// <summary>
	/// Converts the file to array of game objects.
	/// </summary>
	/// <returns>The file.</returns>
	/// <param name="objFilePath">Object file path.</param>
	/// <param name="useMtl">If set to <c>true</c> use mtl.</param>
	/// <param name="standardMaterial">Standard material.</param>
	/// <param name="transparentMaterial">Transparent material.</param>
	/// <param name="carObject">Car object.</param>
	public GameObject[] ConvertFile (string objFilePath, bool useMtl, Material standardMaterial, Material transparentMaterial, GameObject carObject)
	{
#if !UNITY_WEBPLAYER
		objFilePath = objFilePath.Replace ("\\", "/");
		if (!File.Exists (objFilePath)) {
			Debug.Log ("File not found: " + objFilePath);
			return null;
		}
		var objText = File.ReadAllText (objFilePath);
		if (objText == null) {
			Debug.Log ("File not usable: " + objFilePath);
			return null;
		}

		var filePath = objFilePath.Substring (0, objFilePath.LastIndexOf ("/") + 1);
		
		var mtlString = "";		
		if (useMtl) {
			// Extract mtl file name, if there is one
			int idx = objText.IndexOf ("mtllib");
			if (idx != -1) {
				idx += 7;
				int idx2 = objText.IndexOf ('\n', idx);
				if (idx2 != -1) {
					var mtlFileName = objText.Substring (idx, idx2 - idx);
					mtlFileName = mtlFileName.Replace ("\r", "");
					
					// Read in mtl file
					if (File.Exists (filePath + mtlFileName)) {
						mtlString = File.ReadAllText (filePath + mtlFileName);
					} else {
						Debug.LogWarning ("MTL file not found: " + (filePath + mtlFileName));
						useMtl = false;
					}
				} else {
					useMtl = false;
				}
			} else {
				useMtl = false;
			}
		}
		Dictionary<string, Material> materials = null;
		return ConvertString (ref objText, materials, standardMaterial, transparentMaterial, filePath, carObject);
#else
		Debug.LogWarning ("ConvertFile is not supported in the web player");
		return null;
#endif
	}
	
	
	/// <summary>
	/// Extract child object name from .obj files
	/// </summary>
	/// <returns>The child name.</returns>
	/// <param name="str">String.</param>
	string FindChildName (string[] str)
	{
		int max = 0;
		int foundindex = 0;
		int [] a = new int [str.Length];
		
		for (int i = 0; i < str.Length; i++) {
			a [i] = str [i].Length;
		}
		for (int i = 0; i < str.Length; i++) {
			if (a [i] > max) {
				max = a [i];
				foundindex = i;
			}
		}
		a = null;
		return str [foundindex];
	}

	/// <summary>
	/// Converts the string.
	/// </summary>
	/// <returns>The string.</returns>
	/// <param name="objFile">Object file.</param>
	/// <param name="materials">Materials.</param>
	/// <param name="standardMaterial">Standard material.</param>
	/// <param name="transparentMaterial">Transparent material.</param>
	/// <param name="filePath">File path.</param>
	/// <param name="carObject">Car object.</param>
	GameObject[] ConvertString (ref string objFile, Dictionary<string, Material> materials, Material standardMaterial, Material transparentMaterial, string filePath, GameObject carObject)
	{
		var useMtl = true;
		string objectFile = objFile;
		UnityThreading.TaskBase currentTask;
		
//		// Set up obj file
		objectFile = ConvertReturns (objFile);
		var fileLines = objectFile.Split ('\n');
		
		int totalVertices = 0;
		int totalUVs = 0;
		int totalNormals = 0;
	
		var foundV = false;
		int groupCount = 0;
		var originalGroupIndices = new List<int> ();
		int faceCount = 0;
		
#if (MULTITHREADING)		
		currentTask = UnityThreadHelper.Dispatcher.Dispatch(() => {
#endif
		maxPoints = Mathf.Clamp (maxPoints, 0, 65534);						
			
#if (MULTITHREADING)			
		});		
		currentTask.Wait();
#endif
		
//		
//		// Find number of groups in file by looking for face groups
//		// Other methods such as "o " and "g " are too unreliable between different .obj files from different apps
//		// Also find total number of vertices, normals, and uvs in file
		for (int i = 0; i < fileLines.Length; i++) {
			if (fileLines [i].Length < 2)
				continue;
			
			var lineStart = fileLines [i].Substring (0, 2);
			if (lineStart == "f ") {
				if (foundV) {
					groupCount++;
					originalGroupIndices.Add (faceCount);
					foundV = false;	
				}
				faceCount++;
			} else if (lineStart == "v ") {
				foundV = true;
				totalVertices++;
			} else if (lineStart == "vt") {
				totalUVs++;
			} else if (useSuppliedNormals && lineStart == "vn") {
				totalNormals++;
			}
		}
		
//		str_result.Append("2. Iterate object file to determine total vertices/uvs/normal/.. in miliseconds :		" + (Time.realtimeSinceStartup - t)  + "\n");
//		totalmilisec += (Time.realtimeSinceStartup - t);
		originalGroupIndices.Add (-1);
		
		int verticesCount = 0;
		int uvsCount = 0;
		int normalsCount = 0;
		
		
		
		var objVertices = new Vector3[totalVertices];
		var objUVs = new Vector2[totalUVs];
		var objNormals = new Vector3[totalNormals];
		var triData = new List<string> ();
		var quadWarning = false;
		var polyWarning = false;
		var objectNames = new string[groupCount];
		var materialNames = new string[groupCount];
		
		int index = 0;
		var lineInfo = new string[0];
		var groupIndices = new int[groupCount + 1];
		int numberOfGroupsUsed = 0;
		faceCount = 0;
		groupCount = 0;
		int mtlCount = 0;
		int objectNamesCount = 0;
		string parentName = null;

		try {
			while (index < fileLines.Length) {
				var line = fileLines [index++].TrimEnd ();
				
				// Skip over comments and short lines
				if (line.Length < 3 || line [0] == '#')
					continue;
				
				// Remove excess whitespace
				CleanLine (ref line);
				// Skip over short lines (again, in the off chance the above line made this line too short)
				if (line.Length < 3)
					continue;
				
				// If line ends with "\" then combine with the next line, assuming there is one (should be, but just in case)
				while (line[line.Length-1] == '\\' && index < fileLines.Length) {
					line = line.Substring (0, line.Length - 1) + " " + fileLines [index++].TrimEnd ();
				}			
				// find parent object name in *.obj files
				if (line.StartsWith ("mtllib")) {
					lineInfo = line.Split (' ');
					parentName = lineInfo [1].Substring (0, lineInfo [1].IndexOf (".mtl"));
				}
				if (useMtl && line.StartsWith ("usemtl") && mtlCount++ == 0) {
					lineInfo = line.Split (' ');
					if (lineInfo.Length > 1) {
						materialNames [groupCount] = lineInfo [1];
					}
					continue;
				}
				
				var stringStart = line.Substring (0, 2).ToLower ();
				//find child object name in *.obj files
				if ((stringStart == "g " || stringStart == "o ")) {
					
					if (!line.Contains ("g default")) {
						objectNamesCount++;
						objectNames [groupCount] = line.Substring (2, line.Length - 2);
						string [] temp = objectNames [groupCount].Split (' ');
						objectNames [groupCount] = FindChildName (temp);
					}
				}
				// Read vertices into Vector3 array
				else if (stringStart == "v ") {
					lineInfo = line.Split (' ');
					if (lineInfo.Length != 4) {
						throw new System.Exception ("Incorrect number of points while trying to read vertices:\n" + line + "\n");
					} else {
						// Invert x value so it works properly in Unity (left-handed)
						objVertices [verticesCount++] = new Vector3 (-float.Parse (lineInfo [1]), float.Parse (lineInfo [2]), float.Parse (lineInfo [3]));
					}
				}
				// Read UVs into Vector2 array
				else if (stringStart == "vt") {
					lineInfo = line.Split (' ');
					if (lineInfo.Length > 4 || lineInfo.Length < 3) {
						throw new System.Exception ("Incorrect number of points while trying to read UV data:\n" + line + "\n");
					} else {
						objUVs [uvsCount++] = new Vector2 (float.Parse (lineInfo [1]), float.Parse (lineInfo [2]));
					}
				}
				// Read normals into Vector3 array
				else if (useSuppliedNormals && stringStart == "vn") {
					lineInfo = line.Split (' ');
					if (lineInfo.Length != 4) {
						throw new System.Exception ("Incorrect number of points while trying to read normals:\n" + line + "\n");
					} else {
						// Invert x value so it works properly in Unity
						objNormals [normalsCount++] = new Vector3 (-float.Parse (lineInfo [1]), float.Parse (lineInfo [2]), float.Parse (lineInfo [3]));
					}
				}
				// Read triangle face info
				else if (stringStart == "f ") {
					lineInfo = line.Split (' ');
					if (lineInfo.Length >= 4 && lineInfo.Length <= 5) {
						// If data is relative offset, dissect it and replace it with calculated absolute data
						if (lineInfo [1].Substring (0, 1) == "-") {
							for (int i = 1; i < lineInfo.Length; i++) {
								var lineInfoParts = lineInfo [i].Split ('/');
								lineInfoParts [0] = (verticesCount - -int.Parse (lineInfoParts [0]) + 1).ToString ();
								if (lineInfoParts.Length > 1) {
									if (lineInfoParts [1] != "") {
										lineInfoParts [1] = (uvsCount - -int.Parse (lineInfoParts [1]) + 1).ToString ();
									}
									if (lineInfoParts.Length == 3) {
										lineInfoParts [2] = (normalsCount - -int.Parse (lineInfoParts [2]) + 1).ToString ();
									}
								}
								lineInfo [i] = System.String.Join ("/", lineInfoParts);
							}
						}
						// Triangle
						for (int i = 1; i < 4; i++) {
							triData.Add (lineInfo [i]);
						}
						// Quad -- split by adding another triangle
						if (lineInfo.Length == 5) {
							quadWarning = true;
							triData.Add (lineInfo [1]);
							triData.Add (lineInfo [3]);
							triData.Add (lineInfo [4]);
						}
					}
					// Line describes polygon containing more than 4 or fewer than 3 points, which are not supported
					else {
						polyWarning = true;
					}
					// Store index for face group start locations
					if (++faceCount == originalGroupIndices [groupCount + 1]) {
						groupIndices [++groupCount] = triData.Count;
						mtlCount = 0;
						objectNamesCount = 0;
					}
				}
			}
		} catch (System.Exception err) {
			Debug.Log (err.Message);
			return null;
		}
		
		if (combineMultipleGroups && !useSubmeshesWhenCombining) {
			numberOfGroupsUsed = 1;
			groupIndices [1] = triData.Count;
		} else {
			groupIndices [groupCount + 1] = triData.Count;
			numberOfGroupsUsed = groupIndices.Length - 1;
		}
		
		
		// Parse vert/uv/normal index data from triangle face lines
		var triVerts = new int[triData.Count];
		var triUVs = new int[triData.Count];
		var triNorms = new int[triData.Count];
		var lengthCount = 3;

		for (int i = 0; i < triData.Count; i++) {
			string triString = triData [i];
			lineInfo = triString.Split ('/');
			triVerts [i] = int.Parse (lineInfo [0]) - 1;
			if (lineInfo.Length > 1) {
				if (lineInfo [1] != "") {
					triUVs [i] = int.Parse (lineInfo [1]) - 1;
				}
				if (lineInfo.Length == lengthCount && useSuppliedNormals) {
					triNorms [i] = int.Parse (lineInfo [2]) - 1;	
				}
			}
		}
		
		var objVertList = new List<Vector3> (objVertices);
		
		//Create vertices base on number of uvs indcies
		if (totalUVs > 0) {
			SplitOnUVs (triData, triVerts, triUVs, objVertList, objUVs, objVertices, ref verticesCount);
		}
		
		//Create vertices base on number of normal indcies
		SplitOnNormals (triData, triVerts, triNorms, objVertList, objNormals, objVertices, ref verticesCount);
		
		// Warnings
		if (quadWarning && !suppressWarnings) {
			Debug.LogWarning ("At least one object uses quads...automatic triangle conversion is being used, which may not produce best results");
		}
		if (polyWarning && !suppressWarnings) {
			Debug.LogWarning ("Polygons which are not quads or triangles have been skipped");
		}
		if (totalUVs == 0 && !suppressWarnings) {
			Debug.LogWarning ("At least one object does not seem to be UV mapped...any textures used will appear as a solid color");
		}
		if (totalNormals == 0 && !suppressWarnings) {
			Debug.LogWarning ("No normal data found for at least one object...automatically computing normals instead");
		}
		
		// Errors
		if (totalVertices == 0 && triData.Count == 0) {
			Debug.Log ("No objects seem to be present...possibly the .obj file is damaged or could not be read");
			return null;
		} else if (totalVertices == 0) {
			Debug.Log ("The .obj file does not contain any vertices");
			return null;
		} else if (triData.Count == 0) {
			Debug.Log ("The .obj file does not contain any polygons");
			return null;
		}

		GameObject [] gameObjects = null;
		
#if (MULTITHREADING)
	 	currentTask =  UnityThreadHelper.Dispatcher.Dispatch(() => {
#endif			
		// Set up GameObject array...only 1 object if combining groups
		gameObjects = new GameObject[combineMultipleGroups ? 1 : numberOfGroupsUsed];
		for (int i = 0; i < gameObjects.Length; i++) {
			gameObjects [i] = new GameObject (objectNames [i], typeof(MeshFilter), typeof(MeshRenderer));
		}	
#if (MULTITHREADING)			
		});		
		currentTask.Wait();
#endif		
//		// --------------------------------
//		// Create meshes from the .obj data	
		GameObject go = null;
		Mesh mesh = null;
		Vector3[] newVertices = null;
		Vector2[] newUVs = null;
		Vector3[] newNormals = null;
		int[] newTriangles = null;
		var useSubmesh = (combineMultipleGroups && useSubmeshesWhenCombining && numberOfGroupsUsed > 1) ? true : false;
		Material[] newMaterials = null;
		
		
#if (MULTITHREADING)		
		currentTask =  UnityThreadHelper.Dispatcher.Dispatch(() => {
#endif			
		if (useSubmesh) {
			newMaterials = new Material[numberOfGroupsUsed];
		}
#if (MULTITHREADING)		
		});		
		currentTask.Wait();
#endif			
		
		for (int i = 0; i < numberOfGroupsUsed; i++) {
			if (!useSubmesh || (useSubmesh && i == 0)) {
#if (MULTITHREADING)
				currentTask =  UnityThreadHelper.Dispatcher.Dispatch(() =>{
#endif
				go = gameObjects [i];
				mesh = new Mesh ();
					
#if (MULTITHREADING)	
				});				
				currentTask.Wait();
#endif
				
				// Find the number of unique vertices used by this group, also used to map original vertices into 0..thisVertices.Count range
				var vertHash = new Dictionary<int, int> ();
				var thisVertices = new List<Vector3> ();
				int counter = 0;
				int vertHashValue = 0;
				int triStart = groupIndices [i];
				int triEnd = groupIndices [i + 1];
				if (useSubmesh) {
					triStart = groupIndices [0];
					triEnd = groupIndices [numberOfGroupsUsed];
				}
				
				for (int j = triStart; j < triEnd; j++) {
					if (!vertHash.TryGetValue (triVerts [j], out vertHashValue)) {
						vertHash [triVerts [j]] = counter++;
						thisVertices.Add (objVertList [triVerts [j]]);
					}
				}
				
				if (thisVertices.Count > maxPoints) {
					Debug.Log ("The number of vertices in this object exceeds the maximum allowable limit of " + maxPoints);
					return null;
				}
			
				newVertices = new Vector3[thisVertices.Count];
				newUVs = new Vector2[thisVertices.Count];
				newNormals = new Vector3[thisVertices.Count];
				newTriangles = new int[triEnd - triStart];
			
				// Copy .obj mesh data for vertices and triangles to arrays of the correct size
				if (scaleFactor == Vector3.one && objRotation == Vector3.zero && objPosition == Vector3.zero) {
					for (int j = 0; j < thisVertices.Count; j++) {
						newVertices [j] = thisVertices [j];
					}
				} else {
					transform.eulerAngles = objRotation;
					transform.position = objPosition;
					transform.localScale = scaleFactor;
					var thisMatrix = transform.localToWorldMatrix;
					for (int j = 0; j < thisVertices.Count; j++) {
						newVertices [j] = thisMatrix.MultiplyPoint3x4 (thisVertices [j]);
					}
					transform.position = Vector3.zero;
					transform.rotation = Quaternion.identity;
					transform.localScale = Vector3.one;
				}	
				
				// Arrange UVs and normals so they match up with vertices
				if (uvsCount > 0 && normalsCount > 0 && useSuppliedNormals) {
					for (int j = triStart; j < triEnd; j++) {
						newUVs [vertHash [triVerts [j]]] = objUVs [triUVs [j]];
						// Needs to be normalized or lighting is whacked (especially with specular), and some apps don't output normalized normals
						newNormals [vertHash [triVerts [j]]] = objNormals [triNorms [j]].normalized;
					}
				} else {
					// Arrange UVs so they match up with vertices
					if (uvsCount > 0) {
						for (int j = triStart; j < triEnd; j++) {
							newUVs [vertHash [triVerts [j]]] = objUVs [triUVs [j]];
						}
					}
					// Arrange normals so they match up with vertices
					if (normalsCount > 0 && useSuppliedNormals) {
						for (int j = triStart; j < triEnd; j++) {
							newNormals [vertHash [triVerts [j]]] = objNormals [triNorms [j]];
						}
					}
				}
				
				// Since we flipped the normals, swap triangle points 2 & 3
				counter = 0;
				for (int j = triStart; j < triEnd; j += 3) {
					newTriangles [counter] = vertHash [triVerts [j]];
					newTriangles [counter + 1] = vertHash [triVerts [j + 2]];
					newTriangles [counter + 2] = vertHash [triVerts [j + 1]];
					counter += 3;
				}
				
#if (MULTITHREADING)
				currentTask = UnityThreadHelper.Dispatcher.Dispatch( () => {
#endif
				mesh.vertices = newVertices;
				mesh.uv = newUVs;
				if (useSuppliedNormals) {
					mesh.normals = newNormals;
				}
				if (useSubmesh) {
					mesh.subMeshCount = numberOfGroupsUsed;
				}
#if (MULTITHREADING)
				});
				currentTask.Wait();
#endif
			}
				
#if (MULTITHREADING)
				currentTask = UnityThreadHelper.Dispatcher.Dispatch( () => {
#endif
			if (useSubmesh) {
				int thisLength = groupIndices [i + 1] - groupIndices [i];
				var thisTriangles = new int[thisLength];
				System.Array.Copy (newTriangles, groupIndices [i], thisTriangles, 0, thisLength);
				mesh.SetTriangles (thisTriangles, i);
				if (useMtl && materials.ContainsKey (materialNames [i])) {
					newMaterials [i] = materials [materialNames [i]];
				}
			} else {
				mesh.triangles = newTriangles;
			}
//			
			
//			
//			// Stuff that's done for each object, or at the end if using submeshes
			if (!useSubmesh || (useSubmesh && i == numberOfGroupsUsed - 1)) {
				if (normalsCount == 0 || !useSuppliedNormals) {
					mesh.RecalculateNormals ();
					if (computeTangents) {
						newNormals = mesh.normals;
					}
				}
				if (computeTangents) {
					var newTangents = new Vector4[newVertices.Length];
					CalculateTangents (newVertices, newNormals, newUVs, newTriangles, newTangents);
					mesh.tangents = newTangents;
				}
				
				mesh.RecalculateBounds ();
				go.GetComponent<MeshFilter> ().mesh = mesh;
				if (!useSubmesh) {
					if (useMtl && materialNames [i] != null && materials.ContainsKey (materialNames [i])) {
						go.renderer.sharedMaterial = materials [materialNames [i]];
					} else {
						go.renderer.sharedMaterial = standardMaterial;
					}
				} else {
					for (int j = 0; j < newMaterials.Length; j++) {
						if (newMaterials [j] == null) {
							newMaterials [j] = standardMaterial;
						}
					}
					go.renderer.sharedMaterials = newMaterials;
				}
			}
#if (MULTITHREADING)
			});
			currentTask.Wait();
#endif
		}
//		Debug.Log("Break 6	miliSecond :	" + (DateTime.UtcNow - t).Milliseconds + "	parent name	:	" + parentName);
		
#if (MULTITHREADING)
			currentTask = UnityThreadHelper.Dispatcher.Dispatch( () => {
#endif
		GameObject parent = new GameObject (parentName);
		foreach (GameObject gobj in gameObjects) {
			gobj.transform.parent = parent.transform;
		}	
		parent.transform.parent = carObject.transform;
			
		indexx--;
			
		if (indexx == 0) {

#if (MULTITHREADING)
			_sceneManager.setUpCar(carObject);
#endif	
		}
			
#if (MULTITHREADING)
			});
			currentTask.Wait();
#endif
			
		return gameObjects;
			
	}

	/// <summary>
	/// Convert \r character to \n character
	/// </summary>
	/// <returns>The returns.</returns>
	/// <param name="file">File.</param>
	private string ConvertReturns (string file)
	{
		char[] fileChars = file.ToCharArray ();
		for (int i = 0; i < fileChars.Length; i++) {
			if (fileChars [i] == '\r') {
				fileChars [i] = '\n';
			}
		}
		string result = new string (fileChars);
		fileChars = null;
		return result;
	}
	
	
	/// <summary>
	/// Cleans the line. replace 2 spaces by 1 space
	/// </summary>
	/// <param name="line">Line.</param>
	private void CleanLine (ref string line)
	{
		// This fixes lines so that any instance of at least two spaces is replaced with one
		// Using System.Text.RegularExpressions adds 900K to the web player, so instead this is done a little differently
		while (line.IndexOf ("  ") != -1) {
			line = line.Replace ("  ", " ");
		}
	}

	/// <summary>
	/// Calculates the tangents.
	/// </summary>
	/// <param name="vertices">Vertices.</param>
	/// <param name="normals">Normals.</param>
	/// <param name="uv">Uv.</param>
	/// <param name="triangles">Triangles.</param>
	/// <param name="tangents">Tangents.</param>
	private void CalculateTangents (Vector3[] vertices, Vector3[] normals, Vector2[] uv, int[] triangles, Vector4[] tangents)
	{
		Vector3[] tan1 = new Vector3[vertices.Length];
		Vector3[] tan2 = new Vector3[vertices.Length];
		int triCount = triangles.Length;
		int tangentCount = tangents.Length;
		
		for (int i = 0; i < triCount; i += 3) {
			int i1 = triangles [i];
			int i2 = triangles [i + 1];
			int i3 = triangles [i + 2];
			
			Vector3 v1 = vertices [i1];
			Vector3 v2 = vertices [i2];
			Vector3 v3 = vertices [i3];
			
			Vector2 w1 = uv [i1];
			Vector2 w2 = uv [i2];
			Vector2 w3 = uv [i3];
			
			float x1 = v2.x - v1.x;
			float x2 = v3.x - v1.x;
			float y1 = v2.y - v1.y;
			float y2 = v3.y - v1.y;
			float z1 = v2.z - v1.z;
			float z2 = v3.z - v1.z;
			
			float s1 = w2.x - w1.x;
			float s2 = w3.x - w1.x;
			float t1 = w2.y - w1.y;
			float t2 = w3.y - w1.y;
	
			float r = 1.0f / (s1 * t2 - s2 * t1);
			Vector3 sdir = new Vector3 ((t2 * x1 - t1 * x2) * r, (t2 * y1 - t1 * y2) * r, (t2 * z1 - t1 * z2) * r);
			Vector3 tdir = new Vector3 ((s1 * x2 - s2 * x1) * r, (s1 * y2 - s2 * y1) * r, (s1 * z2 - s2 * z1) * r);
			
			tan1 [i1] += sdir;
			tan1 [i2] += sdir;
			tan1 [i3] += sdir;
			
			tan2 [i1] += tdir;
			tan2 [i2] += tdir;
			tan2 [i3] += tdir;
		}
	
		for (int i = 0; i < tangentCount; i++) {
			Vector3 n = normals [i];
			Vector3 t = tan1 [i];
			tangents [i] = (t - n * Vector3.Dot (n, t)).normalized;
			tangents [i].w = (Vector3.Dot (Vector3.Cross (n, t), tan2 [i]) < 0.0f) ? -1.0f : 1.0f;
		}
		
		tan1 = null;
		tan2 = null;
	}


	public class Int2
	{
		public int a;
		public int b;
		
		public Int2 ()
		{
			a = 0;
			b = 0;
		}
		
		public Int2 (int a, int b)
		{
			this.a = a;
			this.b = b;
		}
		
		public override int GetHashCode ()
		{
			string temp = "" + a + "" + b;
			return int.Parse (temp);
		}
		
		public override bool Equals (object o)
		{
			if (o.GetType () != this.GetType ())
				return false;
			Int2 temp = (Int2)o;
			return (this.a == temp.a && this.b == temp.b);
		}
	}
	
	/// <summary>
	/// Create vertices base on number of uvs indcies
	/// </summary>
	/// <param name="triData">Tri data.</param>
	/// <param name="triVerts">Tri verts.</param>
	/// <param name="triUVs">Tri U vs.</param>
	/// <param name="objVertList">Object vert list.</param>
	/// <param name="objUVs">Object U vs.</param>
	/// <param name="objVertices">Object vertices.</param>
	/// <param name="verticesCount">Vertices count.</param>
	private void SplitOnUVs (List<string> triData, int[] triVerts, int[] triUVs, List<Vector3> objVertList, Vector2[] objUVs, Vector3[] objVertices,
							 ref int verticesCount)
	{
		var triHash = new Dictionary<int, Vector2> ();
		var uvHash = new Hashtable ();
		var triIndex = new Int2 ();
		var triHashValue = Vector2.zero;
		var uvHashValue = 0;
		for (int i = 0; i < triData.Count; i++) {
			if (!triHash.TryGetValue (triVerts [i], out triHashValue)) {
				triHash [triVerts [i]] = objUVs [triUVs [i]];
				triIndex = new Int2 (triVerts [i], triUVs [i]);
				uvHash [triIndex] = triVerts [i];
			}
			// If it's the same vertex but the UVs are different...
			else if (triHash [triVerts [i]] != objUVs [triUVs [i]]) {
				triIndex = new Int2 (triVerts [i], triUVs [i]);
				// If the UV index of a previously added vertex already exists, use that vertex
				if (uvHash.ContainsKey (triIndex)) {
					triVerts [i] = (int)uvHash [triIndex];
				}
				// Otherwise make a new vertex and keep the UV reference count to the vertex count
				else {
//					objVertList.Add (objVertices[triVerts[i]]);
					objVertList.Add (objVertList [triVerts [i]]);
					triVerts [i] = verticesCount++;
					triHash [triVerts [i]] = objUVs [triUVs [i]];
					uvHash [triIndex] = triVerts [i];
				}
			}
		}
		triHash = null;
		uvHash = null;
	}
	

	/// <summary>
	/// Create vertices base on number of normal indcies
	/// </summary>
	/// <param name="triData">Tri data.</param>
	/// <param name="triVerts">Tri verts.</param>
	/// <param name="triNormals">Tri normals.</param>
	/// <param name="objVertList">Object vert list.</param>
	/// <param name="objNormals">Object normals.</param>
	/// <param name="objVertices">Object vertices.</param>
	/// <param name="verticesCount">Vertices count.</param>
	private void SplitOnNormals (List<string> triData, int[] triVerts, int[] triNormals, List<Vector3> objVertList, Vector3[] objNormals, Vector3[] objVertices,
							 ref int verticesCount)
	{
		var triHash = new Dictionary<int, Vector3> ();
		var normalHash = new Hashtable ();
		var triIndex = new Int2 ();
		var triHashValue = Vector3.zero;
		var normalHashValue = 0;
		for (int i = 0; i < triData.Count; i++) {
			if (!triHash.TryGetValue (triVerts [i], out triHashValue)) {
				triHash [triVerts [i]] = objNormals [triNormals [i]];
				triIndex = new Int2 (triVerts [i], triNormals [i]);
				normalHash [triIndex] = triVerts [i];
			}
			// If it's the same vertex but the UVs are different...
			else if (triHash [triVerts [i]] != objNormals [triNormals [i]]) {
				triIndex = new Int2 (triVerts [i], triNormals [i]);
				// If the UV index of a previously added vertex already exists, use that vertex
				if (normalHash.ContainsKey (triIndex)) {
					triVerts [i] = (int)normalHash [triIndex];
				}
				// Otherwise make a new vertex and keep the UV reference count to the vertex count
				else {
//					objVertList.Add (objVertices[triVerts[i]]);
					objVertList.Add (objVertList [triVerts [i]]);
					triVerts [i] = verticesCount++;
					triHash [triVerts [i]] = objNormals [triNormals [i]];
					normalHash [triIndex] = triVerts [i];
				}
			}
		}
		triHash = null;
		normalHash = null;
	}
	

	/// <summary>
	/// Read material file including main color texture, texture name and assign them for material Unity object respectively
	/// </summary>
	/// <param name="mtlDictionary">Mtl dictionary.</param>
	/// <param name="mtlFile">Mtl file.</param>
	/// <param name="standardMaterial">Standard material.</param>
	/// <param name="transparentMaterial">Transparent material.</param>
	/// <param name="filePath">File path.</param>
	void ParseMTL (Dictionary<string, Material> mtlDictionary, string mtlFile, Material standardMaterial, Material transparentMaterial, string filePath)
	{
		//convert all \r char into \n char in material file
		string materialFile = ConvertReturns (mtlFile);
		
		try {
			
			//split material lines by \n char save into string array
			var lines = materialFile.Split ('\n');
			var mtlName = "";
			float aR = 0, aG = 0, aB = 0, dR = 0, dG = 0, dB = 0, sR = 0, sG = 0, sB = 0;
			float dVal = 1.0f;
			float specularHighlight = 0.0f;
			int count = 0;
			Texture2D diffuseTexture = null;
			
			Texture2D normalTexture = null;
			
			Cubemap cubemap = null;
			
			bool isNewMtl = false;
			
			bool isHasTexture = false;
			
			for (int i = 0; i < lines.Length; i++) {
				var line = lines [i];
				isNewMtl = false;
				CleanLine (ref line);
				
				if (line.Length < 3 || line [0] == '#') {
					continue;
				}
			
				
				if (line.StartsWith ("newmtl")) {
					isNewMtl = true;
					if (count++ > 0) {
						SetMaterial (mtlDictionary, mtlName, aR, aG, aB, dR, dG, dB, sR, sG, sB, standardMaterial, transparentMaterial, dVal,
									specularHighlight, diffuseTexture, normalTexture, cubemap, isHasTexture);
						isHasTexture = false;
						aR = 0;
						aG = 0;
						aB = 0;
						dR = 0;
						dG = 0;
						dB = 0;
						sR = 0;
						sG = 0;
						sB = 0;
						dVal = 1.0f;
						specularHighlight = 0.0f;
						useWindowGlassShader = false;
						useWindowGlassBlackShader = false;
						usePaintShader = false;
						useDiffuseShader = false;
						useChromeShader = false;
						useDiffuseBumpedShader = false;
						useTransparentShader = false;
						useLightGlassShader = false;
					}
					var lineInfo = line.Split (' ');
					if (lineInfo.Length > 1) {
						mtlName = lineInfo [1];
					
						if (mtlName.Contains ("windowglass_")) {
							useWindowGlassShader = true;
							useWindowGlassBlackShader = false;
							usePaintShader = false;
							useDiffuseShader = false;
							useChromeShader = false;
							useDiffuseBumpedShader = false;
							useTransparentShader = false;
							useLightGlassShader = false;
							
							diffuseTexture = _glassTexture;
							cubemap = (Cubemap)Resources.Load ("FBX&Material/Others/New Cubemap");
						} else if (mtlName.Contains ("windowglassblack_")) {
							useWindowGlassShader = false;
							useWindowGlassBlackShader = true;
							usePaintShader = false;
							useDiffuseShader = false;
							useChromeShader = false;
							useDiffuseBumpedShader = false;
							useTransparentShader = false;
							useLightGlassShader = false;
							
							diffuseTexture = _glassTexture;
							cubemap = (Cubemap)Resources.Load ("FBX&Material/Others/New Cubemap");
						} else if (mtlName.Contains ("paint_")) {
							useWindowGlassShader = false;
							useWindowGlassBlackShader = false;
							usePaintShader = true;
							useDiffuseShader = false;
							useChromeShader = false;
							useDiffuseBumpedShader = false;
							useTransparentShader = false;
							useLightGlassShader = false;
							
							diffuseTexture = _carTexture;
							cubemap = (Cubemap)Resources.Load ("FBX&Material/Others/New Cubemap");
							
						} else if (mtlName.Contains ("diffuse_")) {
							useWindowGlassShader = false;
							useWindowGlassBlackShader = false;
							usePaintShader = false;
							useDiffuseShader = true;
							useChromeShader = false;
							useDiffuseBumpedShader = false;
							useTransparentShader = false;
							useLightGlassShader = false;
							diffuseTexture = null;
						} else if (mtlName.Contains ("chrome_")) {
							useWindowGlassShader = false;
							useWindowGlassBlackShader = false;
							usePaintShader = false;
							useDiffuseShader = false;
							useChromeShader = true;
							useDiffuseBumpedShader = false;
							useTransparentShader = false;
							useLightGlassShader = false;
							
							diffuseTexture = _carTexture;
						
							cubemap = (Cubemap)Resources.Load ("FBX&Material/Others/New Cubemap");
						} else if (mtlName.Contains ("diffusebumped_")) {
							useWindowGlassShader = false;
							useWindowGlassBlackShader = false;
							usePaintShader = false;
							useDiffuseShader = false;
							useChromeShader = false;
							useDiffuseBumpedShader = true;
							useTransparentShader = false;
							useLightGlassShader = false;
							diffuseTexture = null;
						} else if (mtlName.Contains ("transparent_")) {
							useWindowGlassShader = false;
							useWindowGlassBlackShader = false;
							usePaintShader = false;
							useDiffuseShader = false;
							useChromeShader = false;
							useDiffuseBumpedShader = false;
							useTransparentShader = true;
							useLightGlassShader = false;
							diffuseTexture = null;
						} else if (mtlName.Contains ("lightglass_")) {
							useWindowGlassShader = false;
							useWindowGlassBlackShader = false;
							usePaintShader = false;
							useDiffuseShader = false;
							useChromeShader = false;
							useDiffuseBumpedShader = false;
							useTransparentShader = false;
							useLightGlassShader = true;
							
							diffuseTexture = _glassTexture;
						}
					}
					continue;
				}
#if !UNITY_WEBPLAYER
				// Get diffuse texture
				if (line.StartsWith ("map_Kd")) {
					var lineInfo = line.Split (' ');
					if (lineInfo.Length > 1) {
						var textureFilePath = filePath + lineInfo [1];

						if (!File.Exists (textureFilePath)) {
							throw new System.Exception ("Texture file not found: " + textureFilePath);
						}
						
						if (useDiffuseShader) {
							isHasTexture = true;
							diffuseTexture = new Texture2D (1, 1);
							diffuseTexture = TGALoader.LoadTGA (textureFilePath);
							diffuseTexture.name = lineInfo [1] + "";
						} else if (useTransparentShader) {
							diffuseTexture = new Texture2D (1, 1);
							diffuseTexture = TGALoader.LoadTGA (textureFilePath);
							diffuseTexture.name = lineInfo [1] + "";
						} else if (useDiffuseBumpedShader) {//diffuse normal
							diffuseTexture = new Texture2D (1, 1);
							diffuseTexture = TGALoader.LoadTGA (textureFilePath);
							diffuseTexture.name = lineInfo [1] + "";
						}
					}
					continue;
				}
									
#endif
				
				var lineStart = line.Substring (0, 2).ToLower ();
				if (lineStart == "ka") {
					ParseKLine (ref line, ref aR, ref aG, ref aB);
				} else if (lineStart == "kd") {
					ParseKLine (ref line, ref dR, ref dG, ref dB);
				} else if (lineStart == "ks") {
					ParseKLine (ref line, ref sR, ref sG, ref sB);
				} else if (lineStart == "d ") {
					var lineInfo = line.Split (' ');
					if (lineInfo.Length > 1) {
						if (lineInfo [1] == "-halo") {
							if (lineInfo.Length > 2) {
								dVal = float.Parse (lineInfo [2]);
							}
						} else {
							dVal = float.Parse (lineInfo [1]);
						}
					}
				} else if (lineStart == "ns") {
					var lineInfo = line.Split (' ');
					if (lineInfo.Length > 1) {
						specularHighlight = float.Parse (lineInfo [1]);
					}
				}
			}
			SetMaterial (mtlDictionary, mtlName, aR, aG, aB, dR, dG, dB, sR, sG, sB, standardMaterial, transparentMaterial, dVal,
						specularHighlight, diffuseTexture, normalTexture, cubemap, isHasTexture);
		} catch (System.Exception err) {
			Debug.Log (err.Message);
		}
			
	}
	

	/// <summary>
	/// Base on material type set their shader used, their properties value
	/// </summary>
	/// <param name="mtlDictionary">Mtl dictionary.</param>
	/// <param name="mtlName">Mtl name.</param>
	/// <param name="aR">A r.</param>
	/// <param name="aG">A g.</param>
	/// <param name="aB">A b.</param>
	/// <param name="dR">D r.</param>
	/// <param name="dG">D g.</param>
	/// <param name="dB">D b.</param>
	/// <param name="sR">S r.</param>
	/// <param name="sG">S g.</param>
	/// <param name="sB">S b.</param>
	/// <param name="standardMaterial">Standard material.</param>
	/// <param name="transparentMaterial">Transparent material.</param>
	/// <param name="transparency">Transparency.</param>
	/// <param name="specularHighlight">Specular highlight.</param>
	/// <param name="diffuseTexture">Diffuse texture.</param>
	/// <param name="normalTexture">Normal texture.</param>
	/// <param name="cubemap">Cubemap.</param>
	/// <param name="isHasTexture">If set to <c>true</c> is has texture.</param>
	private void SetMaterial (Dictionary<string, Material> mtlDictionary, string mtlName, float aR, float aG, float aB, float dR, float dG, float dB,
						float sR, float sG, float sB, Material standardMaterial, Material transparentMaterial, float transparency, float specularHighlight,
						Texture2D diffuseTexture, Texture2D normalTexture, Cubemap cubemap, bool isHasTexture)
	{
		Material mat = null;
		if (transparency == 1.0f) {
			if (useWindowGlassShader) {


#if UNITY_ANDROID
				if(Screen.width == 480){
					mat = new Material(Shader.Find ("Diffuse"));
					mat.SetColor ("_Color", new Color(.5f, .5f, .5f, 1));
					mat.mainTexture = diffuseTexture;
				} else {
					mat = new Material(Shader.Find ("RedDotGames/Mobile/Car Paint Low Detail"));
					mat.SetColor ("_Color", new Color(0.5f, 0.5f, 0.5f, 1));
					mat.SetColor ("_SpecColor", new Color(0.11f, 0.11f, 0.11f ,0.5f));
					mat.SetFloat ("_Shininess", 2.234f);
					mat.SetFloat ("_Gloss", 1.861702f);
					mat.SetFloat ("_Reflection", 0.3f );// 0.106383f);
					mat.SetFloat ("_FrezPow", 2.0f );//0.8510639f);
					mat.SetFloat ("_FrezFalloff",5.0f );// 7.340425f);
					mat.mainTexture = diffuseTexture;
				}
#else
				mat = new Material (Shader.Find ("RedDotGames/Mobile/Car Paint Low Detail"));
				mat.SetColor ("_Color", new Color (0.5f, 0.5f, 0.5f, 1));
				mat.SetColor ("_SpecColor", new Color (0.11f, 0.11f, 0.11f, 0.5f));
				mat.SetFloat ("_Shininess", 2.234f);
				mat.SetFloat ("_Gloss", 1.861702f);
				mat.SetFloat ("_Reflection", 0.3f);// 0.106383f);
				mat.SetFloat ("_FrezPow", 2.0f);//0.8510639f);
				mat.SetFloat ("_FrezFalloff", 5.0f);// 7.340425f);
				mat.mainTexture = diffuseTexture;
#endif
			} else if (useWindowGlassBlackShader) {


#if UNITY_ANDROID
				if(Screen.width == 480){
					mat = new Material(Shader.Find ("Diffuse"));
					mat.SetColor ("_Color", new Color(.5f, .5f, .5f, 1));
					mat.mainTexture = diffuseTexture;
				} else {
					mat = new Material(Shader.Find ("RedDotGames/Mobile/Car Paint Low Detail"));
					mat.SetColor ("_Color", new Color(0.5f, 0.5f, 0.5f, 1));
					mat.SetColor ("_SpecColor", new Color(0.11f, 0.11f, 0.11f ,0.5f));
					mat.SetFloat ("_Reflection", 0.106383f);
					mat.SetFloat ("_FrezPow",0.8510639f);
					mat.SetFloat ("_FrezFalloff", 7.340425f);				
					mat.SetFloat ("_Shininess", 2.9255f);
					mat.SetFloat ("_Gloss", 1.489362f);
				}
#else
				mat = new Material (Shader.Find ("RedDotGames/Mobile/Car Paint Low Detail"));
				mat.SetColor ("_Color", new Color (0.5f, 0.5f, 0.5f, 1));
				mat.SetColor ("_SpecColor", new Color (0.11f, 0.11f, 0.11f, 0.5f));
				mat.SetFloat ("_Reflection", 0.106383f);
				mat.SetFloat ("_FrezPow", 0.8510639f);
				mat.SetFloat ("_FrezFalloff", 7.340425f);				
				mat.SetFloat ("_Shininess", 2.9255f);
				mat.SetFloat ("_Gloss", 1.489362f);
#endif
			} else if (usePaintShader) {
				
#if UNITY_EDITOR 
				mat = new Material(Shader.Find ("RedDotGames/Mobile/Car Paint Low Detail"));
				mat.SetColor ("_Color", new Color(1, 1, 1, 1));					
				mat.SetFloat ("_Gloss", 1f);//0.2659574f); //4.7f
				mat.SetFloat ("_Shininess",4.468085f);
				mat.SetFloat ("_Reflection", 0.45f);//0.0319149f); // 0.25f
				mat.SetFloat ("_FrezPow", 1.6f );//0.606383f); // 1.6f
				mat.SetFloat ("_FrezFalloff", 3f);//0.117022f); //3f
				mat.mainTexture = diffuseTexture;
				
#elif UNITY_IPHONE 
				if(iPhone.generation == iPhoneGeneration.iPhone4
				   || iPhone.generation == iPhoneGeneration.iPhone3GS
				   || iPhone.generation == iPhoneGeneration.iPhone3G
				   || iPhone.generation == iPhoneGeneration.iPhone
				   || iPhone.generation == iPhoneGeneration.iPad1Gen
				   || iPhone.generation == iPhoneGeneration.iPodTouch3Gen 
				   || iPhone.generation == iPhoneGeneration.iPodTouch2Gen
				   || iPhone.generation == iPhoneGeneration.iPodTouch1Gen)
				{
					// iPhone 4 and others before
					mat = new Material(Shader.Find ("Diffuse"));
					mat.SetColor ("_Color", new Color(.5f, .5f, .5f, 1));
					mat.mainTexture = diffuseTexture;
				}
				else // others after iPhone 4
				{
					mat = new Material(Shader.Find ("RedDotGames/Mobile/Car Paint Low Detail"));
					mat.SetColor ("_Color", new Color(1, 1, 1, 1));					
					mat.SetFloat ("_Gloss", 1f);//0.2659574f); //4.7f
					mat.SetFloat ("_Shininess",4.468085f);
					mat.SetFloat ("_Reflection", 0.45f);//0.0319149f); // 0.25f
					mat.SetFloat ("_FrezPow", 1.6f );//0.606383f); // 1.6f
					mat.SetFloat ("_FrezFalloff", 3f);//0.117022f); //3f
					mat.mainTexture = diffuseTexture;
				}

				/*
				if(iPhone.generation == iPhoneGeneration.iPhone4S
				   || iPhone.generation == iPhoneGeneration.iPhone5 
				   || iPhone.generation == iPhoneGeneration.iPhone5S
				   || iPhone.generation == iPhoneGeneration.iPhone5C
				   || iPhone.generation == iPhoneGeneration.iPhoneUnknown
				   || iPhone.generation == iPhoneGeneration.iPad2Gen) {
					mat = new Material(Shader.Find ("RedDotGames/Mobile/Car Paint Low Detail"));
					mat.SetColor ("_Color", new Color(1, 1, 1, 1));					
					mat.SetFloat ("_Gloss", 1f);//0.2659574f); //4.7f
					mat.SetFloat ("_Shininess",4.468085f);
					mat.SetFloat ("_Reflection", 0.45f);//0.0319149f); // 0.25f
					mat.SetFloat ("_FrezPow", 1.6f );//0.606383f); // 1.6f
					mat.SetFloat ("_FrezFalloff", 3f);//0.117022f); //3f
					mat.mainTexture = diffuseTexture;
				}
				else {
					mat = new Material(Shader.Find ("Diffuse"));//Iphone4
					mat.SetColor ("_Color", new Color(.5f, .5f, .5f, 1));
					mat.mainTexture = diffuseTexture;
				}*/
#elif UNITY_ANDROID
				if(Screen.width == 480){
					mat = new Material(Shader.Find ("Diffuse"));
					mat.SetColor ("_Color", new Color(.5f, .5f, .5f, 1));
					mat.mainTexture = diffuseTexture;
				} else {
					mat = new Material(Shader.Find ("RedDotGames/Mobile/Car Paint Low Detail"));
					mat.SetColor ("_Color", new Color(1, 1, 1, 1));					
					mat.SetFloat ("_Gloss", 1f);//0.2659574f); //4.7f
					mat.SetFloat ("_Shininess",4.468085f);
					mat.SetFloat ("_Reflection", 0.45f);//0.0319149f); // 0.25f
					mat.SetFloat ("_FrezPow", 1.6f );//0.606383f); // 1.6f
					mat.SetFloat ("_FrezFalloff", 3f);//0.117022f); //3f
					mat.mainTexture = diffuseTexture;
				}
#endif
				
				
			} else if (useDiffuseShader) {
				mat = new Material (Shader.Find ("Diffuse"));
				if (isHasTexture) {
					mat.SetColor ("_Color", new Color (1, 1, 1, 1));
					mat.mainTexture = diffuseTexture;
				} else {
					mat.SetColor ("_Color", new Color (dR, dG, dB, transparency));
					mat.mainTexture = null;
				}
			} else if (useChromeShader) {
#if UNITY_ANDROID
				if(Screen.width == 480){
					mat = new Material(Shader.Find ("Diffuse"));
					mat.SetColor ("_Color", new Color(.5f, .5f, .5f, 1));
					mat.mainTexture = diffuseTexture;
				} else {
					mat = new Material(Shader.Find ("RedDotGames/Mobile/Car Paint Low Detail"));
					mat.SetColor ("_Color", new Color(0.4196f, 0.431f, 0.4352f, 1));
					mat.SetColor ("_SpecColor", new Color(0.56f, 0.56f, 0.56f , 1));
					
					mat.SetFloat ("_Shininess", 9.946809f);
					mat.SetFloat ("_Gloss", 0.4787236f);
					mat.SetFloat ("_Reflection", 0.15f);//0.293618f);
					mat.SetFloat ("_FrezPow", 0.9255319f);
					mat.SetFloat ("_FrezFalloff", 10);
					mat.mainTexture = diffuseTexture;
				}
#else
				mat = new Material (Shader.Find ("RedDotGames/Mobile/Car Paint Low Detail"));
				mat.SetColor ("_Color", new Color (0.4196f, 0.431f, 0.4352f, 1));
				mat.SetColor ("_SpecColor", new Color (0.56f, 0.56f, 0.56f, 1));
				
				mat.SetFloat ("_Shininess", 9.946809f);
				mat.SetFloat ("_Gloss", 0.4787236f);
				mat.SetFloat ("_Reflection", 0.15f);//0.293618f);
				mat.SetFloat ("_FrezPow", 0.9255319f);
				mat.SetFloat ("_FrezFalloff", 10);
				mat.mainTexture = diffuseTexture;
#endif

			} else if (useDiffuseBumpedShader) {
				mat = new Material (Shader.Find ("Diffuse"));
				mat.SetColor ("_Color", new Color (1, 1, 1, 1));
				mat.mainTexture = diffuseTexture;
			} else if (useTransparentShader) {
				mat = new Material (Shader.Find ("Transparent/Diffuse"));
				mat.SetColor ("_Color", new Color (1, 1, 1, 1));
				mat.mainTexture = diffuseTexture;
			} else if (useLightGlassShader) {
				mat = new Material (Shader.Find ("RedDotGames/Mobile/Car Glass with Texture"));
				mat.SetColor ("_Color", new Color (1, 1, 1, 1));
				mat.SetFloat ("_Reflection", 0.2465277f);
				mat.SetColor ("_ReflectColor", new Color (0.2705f, 0.2705f, 0.262745f, 0.5f));
				mat.SetFloat ("_FresnelPower", 0.5319149f);
				mat.mainTexture = diffuseTexture;

			} else {
				if (standardMaterial == null) {
					mat = new Material (Shader.Find ("Diffuse"));
					mat.mainTexture = diffuseTexture;
				} else {
					mat = Instantiate (standardMaterial) as Material;
				}				
			}
		} else {
			mat = Instantiate (transparentMaterial) as Material;
		}
		if (mat.HasProperty ("_BumpMap")) {
			mat.SetTexture ("_BumpMap", normalTexture);
		}
		if (mat.HasProperty ("_Cube")) {
			mat.SetTexture ("_Cube", cubemap);
		}
		
		mat.name = mtlName;
		mtlDictionary [mtlName] = mat;
	}
	
	/// <summary>
	/// Parses the K line which read from material files.
	/// </summary>
	/// <param name="line">Line.</param>
	/// <param name="r">The red component.</param>
	/// <param name="g">The green component.</param>
	/// <param name="b">The blue component.</param>
	private void ParseKLine (ref string line, ref float r, ref float g, ref float b)
	{
		if (line.Contains (".rfl") && !suppressWarnings) {
			Debug.LogWarning (".rfl files not supported");
			return;
		}
		if (line.Contains ("xyz") && !suppressWarnings) {
			Debug.LogWarning ("CIEXYZ color not supported");
			return;
		}
		try {
			var lineInfo = line.Split (' ');
			if (lineInfo.Length > 1) {
				r = float.Parse (lineInfo [1]);
			}
			if (lineInfo.Length > 3) {
				g = float.Parse (lineInfo [2]);
				b = float.Parse (lineInfo [3]);
			} else {
				g = r;
				b = r;
			}
		} catch (System.Exception err) {
			Debug.LogWarning ("Incorrect number format when parsing MTL file: " + err.Message);
		}
	}
	

	/// <summary>
	/// Convert texture into texture using normap-mapping texture
	/// </summary>
	/// <param name="tex">Tex.</param>
	void ConvertToNormalMap (ref Texture2D tex)
	{
		
		Color theColour = new Color ();
		for (int x=0; x<tex.width; x++) {
			for (int y=0; y<tex.height; y++) {
				theColour.r = tex.GetPixel (x, y).g;
				theColour.g = theColour.r;
				theColour.b = theColour.r;
				theColour.a = tex.GetPixel (x, y).r;
				tex.SetPixel (x, y, theColour);
			}
		}
		tex.Apply ();
		
	}

}
