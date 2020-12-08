/****************************************
	Simple MeshCollider Combine
	Copyright Unluck Software	
 	www.chemicalbliss.com																																												
*****************************************/
//Add script to the parent gameObject, then click combine

using UnityEngine;
using System.Collections.Generic;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
[AddComponentMenu("Simple MeshCollider Combine")]

public class SimpleMeshColliderCombine : MonoBehaviour{
    [HideInInspector]	public GameObject[] combinedGameOjects;
	[HideInInspector]	public string meshName = "Combined_Meshes";
	[HideInInspector]	public GameObject boxMeshHolder;
	[Header("Combine Settings")]
	[Tooltip("GameObjects in this list will not be combined.")]
	public MeshCollider[] IgnoreMeshColliders;

	[Header("Optimize Settings")]
	[Tooltip("Distance between vertices to merge.")]
	[Range(0.01f, 2f)]
	public float mergeVerticesThreshold = 0.1f;

    public void ToggleMeshCollider(bool e) {
		for (int i = 0; i < combinedGameOjects.Length; i++){
			if (combinedGameOjects[i] == null) continue;
    		MeshCollider meshCol = combinedGameOjects[i].GetComponent<MeshCollider>();
			if (meshCol != null) {
				meshCol.enabled = e;
			}
		}  
    }

	public bool checkIgnoreList(MeshCollider go) {
		if (IgnoreMeshColliders == null) return false;
		for (int i = 0; i < IgnoreMeshColliders.Length; i++) {
			if (go.transform.localPosition == IgnoreMeshColliders[i].transform.localPosition) {
				return true;
			} else if (go == IgnoreMeshColliders[i]) {
				return true;
			}
		}
		return false;
	}

	public MeshCollider[] FindEnabledMeshes() {
		MeshCollider[] renderers = null;
		renderers = transform.GetComponentsInChildren<MeshCollider>();
		return renderers;
	}

	public BoxCollider[] FindBoxColliders() {
		BoxCollider[] renderers = null;
		renderers = transform.GetComponentsInChildren<BoxCollider>();
		return renderers;
	}

	public Mesh CreateBoxMesh() {	
		Mesh mesh = new Mesh();
		mesh.Clear();
		Vector3 p0 = new Vector3(-.5f,-.5f, .5f);
		Vector3 p1 = new Vector3( .5f,-.5f, .5f);
		Vector3 p2 = new Vector3( .5f,-.5f,-.5f);
		Vector3 p3 = new Vector3(-.5f,-.5f,-.5f);
		Vector3 p4 = new Vector3(-.5f, .5f, .5f);
		Vector3 p5 = new Vector3( .5f, .5f, .5f);
		Vector3 p6 = new Vector3( .5f, .5f,-.5f);
		Vector3 p7 = new Vector3(-.5f, .5f,-.5f);
		Vector3[] vertices = new Vector3[]{
			p0, p1, p2, p3,
			p7, p4, p0, p3,
			p4, p5, p1, p0,
			p6, p7, p3, p2,
			p5, p6, p2, p1,
			p7, p6, p5, p4
		};
		int[] triangles = new int[]{
			3, 1, 0,
			3, 2, 1,			
			3 + 4 * 1, 1 + 4 * 1, 0 + 4 * 1,
			3 + 4 * 1, 2 + 4 * 1, 1 + 4 * 1,
			3 + 4 * 2, 1 + 4 * 2, 0 + 4 * 2,
			3 + 4 * 2, 2 + 4 * 2, 1 + 4 * 2,
			3 + 4 * 3, 1 + 4 * 3, 0 + 4 * 3,
			3 + 4 * 3, 2 + 4 * 3, 1 + 4 * 3,
			3 + 4 * 4, 1 + 4 * 4, 0 + 4 * 4,
			3 + 4 * 4, 2 + 4 * 4, 1 + 4 * 4,
			3 + 4 * 5, 1 + 4 * 5, 0 + 4 * 5,
			3 + 4 * 5, 2 + 4 * 5, 1 + 4 * 5,
		};
		mesh.vertices = vertices;
		mesh.triangles = triangles;
		mesh.RecalculateBounds();
		mesh.Optimize();
		return mesh;
	}

	public void UndoConvertBoxColliders() {
		BoxCollider[] boxColliders = FindBoxColliders();

		for (int i = 0; i < boxColliders.Length; i++) {
			BoxCollider b = boxColliders[i];
			b.enabled = true;
		}
#if UNITY_EDITOR
		DestroyImmediate(boxMeshHolder);
#else
		Destroy(boxMeshHolder);
#endif
	}


	public void ConvertBoxColliders() {
		BoxCollider[] boxColliders = FindBoxColliders();
		if (boxColliders.Length < 1) {
			Debug.LogError("Found no box colliders");
			return;
		}
		boxMeshHolder = new GameObject();
		boxMeshHolder.name = "Converted Box Colliders";
		boxMeshHolder.transform.parent = transform;
		Mesh boxMesh = CreateBoxMesh();
		for (int i = 0; i < boxColliders.Length; i++) {
			BoxCollider b = boxColliders[i];
			b.enabled = false;
			Transform t = boxColliders[i].transform;
			GameObject c = new GameObject();
			c.name = t.name + " (Box Collider)";
			c.transform.position = t.position;
			c.transform.rotation = t.rotation;
			c.transform.localScale = t.localScale;
			c.transform.parent = t;
			c.transform.localScale = b.size;
			c.transform.localPosition = b.center;
			MeshCollider cm = c.AddComponent<MeshCollider>();
			cm.sharedMesh = boxMesh;
			c.transform.parent = boxMeshHolder.transform;
		}
	}


	public void CombineMeshes() {
		MeshCollider meshCol = gameObject.GetComponent<MeshCollider>();
		if(meshCol == null) meshCol = gameObject.AddComponent<MeshCollider>();
		MeshCollider[] meshFilters = null;
    	meshFilters = FindEnabledMeshes();
		combinedGameOjects = new GameObject[meshFilters.Length];
		Mesh meshSprites = new Mesh();
		CombineInstance[] combineInstace = new CombineInstance[meshFilters.Length];
		for (int i = 0; i < combineInstace.Length; i++) {
#if UNITY_EDITOR
			EditorUtility.DisplayProgressBar("Combining", "" + i + " / " + combineInstace.Length, (float)i / combineInstace.Length);
#endif
			if (i != 0 && !checkIgnoreList(meshFilters[i])) {
				combineInstace[i] = new CombineInstance() {
					mesh = meshFilters[i].sharedMesh,
					transform = transform.worldToLocalMatrix * meshFilters[i].transform.localToWorldMatrix
				};
				combinedGameOjects[i] = meshFilters[i].gameObject;
			} else {
				combineInstace[i] = new CombineInstance() {
					mesh = new Mesh(),
					transform = transform.worldToLocalMatrix
				};
			}
		}
		ToggleMeshCollider(false);
#if UNITY_EDITOR
		EditorUtility.ClearProgressBar();
#endif
		meshSprites.name = "MeshCollider Combine Instance";
		meshSprites.Clear();
		meshSprites.CombineMeshes(combineInstace);
		int originalVerts = meshSprites.vertexCount;
		MergeVertices(meshSprites, mergeVerticesThreshold);
		int removed = originalVerts - meshSprites.vertexCount;
		int percentage = (int)(((float)removed / (float)originalVerts) *100);
		meshCol.sharedMesh = meshSprites;
		Debug.Log("Verts reduced from " + originalVerts + " to " + meshCol.sharedMesh.vertexCount + "\nRemoved " + removed   + " Verts" + " (" + percentage + "%)");
	}

	private void MergeVertices(Mesh mesh, float threshold) {

		Vector3[] verts = mesh.vertices;
		List<Vector3> newVerts = new List<Vector3>();
		for (int i = 0; i < verts.Length; ++i) {
			foreach (Vector3 newVert in newVerts)
				if (Vector3.Distance(newVert, verts[i]) <= threshold)
					goto skipToNext;
			newVerts.Add(verts[i]);
			skipToNext:;
		}
		int[] tris = mesh.triangles;
		for (int i = 0; i < tris.Length; ++i) {
			for (int j = 0; j < newVerts.Count; ++j) {
				if (Vector3.Distance(newVerts[j], verts[tris[i]]) <= threshold) {
					tris[i] = j;
					break;
				}
			}
		}
		mesh.Clear();
		mesh.vertices = newVerts.ToArray();
		mesh.triangles = tris;
		mesh.uv = null;
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();
		mesh.Optimize();
	}

	public int Contains(ArrayList l,Material n) {
    	for(int i = 0; i < l.Count; i++) {
    		if ((l[i] as Material) == n) {
    			return i;
    		}
    	}
    	return -1;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(SimpleMeshColliderCombine))]

public class SimpleMeshColliderCombineEditor : Editor {
	public string SaveFile(string folder, string name, string type) {
		string newPath = "";
		string path = EditorUtility.SaveFilePanel("Select Folder ", folder, name, type);
		if (path.Length > 0) {
			if (path.Contains("" + UnityEngine.Application.dataPath)) {
				string s = "" + path + "";
				string d = "" + UnityEngine.Application.dataPath + "/";
				string p = "Assets/" + s.Remove(0, d.Length);
				newPath = p;
				bool cancel = false;
				if (cancel) Debug.Log("Save file canceled");
			} else {
				Debug.LogError("Prefab Save Failed: Can't save outside project: " + path);
			}
		}
		return newPath;
	}

	public override void OnInspectorGUI() {
		DrawDefaultInspector();
		SimpleMeshColliderCombine target_cs = (SimpleMeshColliderCombine)target;
		if (!Application.isPlaying) {
			GUI.enabled = true;
		} else {
			GUI.enabled = false;
		}
		GUILayout.Space(5.0f);
		if (target_cs.combinedGameOjects == null || target_cs.combinedGameOjects.Length == 0) {

			if(target_cs.boxMeshHolder == null) {
			//	if (GUILayout.Button("Convert Box Colliders")) {
			//		if (target_cs.transform.childCount > 1) target_cs.ConvertBoxColliders();
			//	}
			} else {
				if (GUILayout.Button("Undo Convert Box Colliders")) {
					if (target_cs.transform.childCount > 1) target_cs.UndoConvertBoxColliders();
				}
			}
			

			if (GUILayout.Button("Combine (improve collisions)")) {
				if (target_cs.transform.childCount > 1) target_cs.CombineMeshes();
			}
		} else {
			if (GUILayout.Button("Release (to edit)")) {
				target_cs.ToggleMeshCollider(true);
				if (target_cs.GetComponent<MeshCollider>().sharedMesh != null) target_cs.GetComponent<MeshCollider>().sharedMesh = null;// DestroyImmediate(target_cs.GetComponent<MeshCollider>().sharedMesh);
				target_cs.combinedGameOjects = null;
			}
		}
		if (target_cs.GetComponent<MeshCollider>().sharedMesh == null) return;
		if (GUILayout.Button("Save Mesh")) {
			string path = SaveFile("Assets/", target_cs.transform.name + " [SMCC Mesh]", "asset");
			if (path != null && path != "") {
				UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath(path, (Type)typeof(object));
				if (asset == null) {
					Debug.Log(path);
					AssetDatabase.CreateAsset(target_cs.GetComponent<MeshCollider>().sharedMesh, path);
				} else {
					((Mesh)asset).Clear();
					EditorUtility.CopySerialized(target_cs.GetComponent<MeshCollider>().sharedMesh, asset);
					AssetDatabase.SaveAssets();
				}
				target_cs.GetComponent<MeshCollider>().sharedMesh = (Mesh)AssetDatabase.LoadAssetAtPath(path, (Type)typeof(object));

				Debug.Log("Saved mesh asset: " + path);
			}
		}
		GUILayout.Space(5.0f);
		if (GUI.changed) {
			EditorUtility.SetDirty(target_cs);
		}
	}
}
#endif