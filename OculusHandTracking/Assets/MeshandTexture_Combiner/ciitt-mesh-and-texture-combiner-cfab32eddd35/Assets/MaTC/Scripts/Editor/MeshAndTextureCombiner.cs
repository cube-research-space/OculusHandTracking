using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MaTC
{

    /// <summary>
    /// MeshAndTextureCombiner
    /// </summary>
    public class MeshAndTextureCombiner : EditorWindow
    {

        enum MaximumAtlasSize
        {

            _64 = 64,
            _128 = 128,
            _256 = 256,
            _512 = 512,
            _1024 = 1024,
            _2048 = 2048,
            _4096 = 4096,
            _8192 = 8192

        }

        class MeshAndTexturePath
        {

            public Mesh mesh = null;
            public string texturePath = "";
            public Matrix4x4 matrix = Matrix4x4.identity;

            public MeshAndTexturePath(Mesh _mesh, string _texturePath, Matrix4x4 _matrix)
            {
                this.mesh = _mesh;
                this.texturePath = _texturePath;
                this.matrix = _matrix;
            }

            public void clear()
            {

                if (this.mesh)
                {
                    DestroyImmediate(this.mesh);
                }

            }

        }

        class CombineInfo
        {

            public string shaderName = "";

            public string meshPathToCreate = "Assets/Combined Mesh.asset";
            public string materialPathToCreate = "Assets/Combined Material.mat";
            public string texturePathToCreate = "Assets/Combined Texture.png";

            public List<MeshAndTexturePath> meshListToCombine = new List<MeshAndTexturePath>();
            public Dictionary<string, Texture2D> textureListToCombine = new Dictionary<string, Texture2D>();

            public bool foldout = true;

            public CombineInfo()
            {

            }

            public CombineInfo(string _shaderName, int number)
            {

                string sceneName = EditorSceneManager.GetActiveScene().name;

                this.shaderName = _shaderName;

                this.meshPathToCreate = string.Format("Assets/MaTCOutput/{0}/Mesh/{1}/Combined Mesh {2}.asset", sceneName, _shaderName, number);
                this.materialPathToCreate = string.Format("Assets/MaTCOutput/{0}/Material/{1}/Combined Material {2}.mat", sceneName, _shaderName, number);
                this.texturePathToCreate = string.Format("Assets/MaTCOutput/{0}/Texture/{1}/Combined Texture {2}.png", sceneName, _shaderName, number);

            }

        }

        class CombineInfoList
        {

            public Dictionary<string, CombineInfo> infos = new Dictionary<string, CombineInfo>();

            public void clear()
            {

                foreach (var info in this.infos)
                {

                    foreach (var mesh in info.Value.meshListToCombine)
                    {
                        mesh.clear();
                    }

                }

                this.infos.Clear();

            }

        }

        class ProgressValues
        {

            public string message = "";
            public int denominator = 1;
            public int numerator = 0;

            public void reset()
            {
                this.message = "";
                this.denominator = 1;
                this.numerator = 0;
            }

            public float progress()
            {

                if (denominator <= 0)
                {
                    return 0.0f;
                }

                return (float)this.numerator / (float)this.denominator;

            }

        }

        /// <summary>
        /// Scroll pos
        /// </summary>
        Vector2 m_scrollPos = Vector2.zero;

        /// <summary>
        /// GameObject list
        /// </summary>
        [SerializeField]
        List<GameObject> m_gameObjects = new List<GameObject>();

        /// <summary>
        /// SerializedObject
        /// </summary>
        SerializedObject m_serializedObject = null;

        /// <summary>
        /// Include children
        /// </summary>
        bool m_includeChildren = true;

        /// <summary>
        /// Tag to exclude list
        /// </summary>
        [SerializeField]
        List<string> m_tagsToExcludeFromCombining = new List<string> { "EditorOnly", "NotCombine" };

        /// <summary>
        /// Maximum atlas size
        /// </summary>
        MaximumAtlasSize m_maximumAtlasSize = MaximumAtlasSize._2048;

        /// <summary>
        /// Texture atlas padding size
        /// </summary>
        int m_atlasPaddingSize = 0;

        /// <summary>
        /// Info list
        /// </summary>
        CombineInfoList m_infoList = new CombineInfoList();

        /// <summary>
        /// Progress
        /// </summary>
        ProgressValues m_progress = new ProgressValues();

        /// <summary>
        /// Awake
        /// </summary>
        // -----------------------------------------------------------------------------------------
        void Awake()
        {
            EditorSceneManager.sceneOpened -= this.onSceneOpened;
            EditorSceneManager.sceneOpened += this.onSceneOpened;
        }

        /// <summary>
        /// onSceneOpened
        /// </summary>
        /// <param name="scene">scene</param>
        /// <param name="mode">OpenSceneMode</param>
        // -----------------------------------------------------------------------------------------
        void onSceneOpened(Scene scene, UnityEditor.SceneManagement.OpenSceneMode mode)
        {

            if (mode == OpenSceneMode.Single)
            {
                this.Close();
            }

        }

        /// <summary>
        /// OnDestroy
        /// </summary>
        // -----------------------------------------------------------------------------------------
        void OnDestroy()
        {
            EditorSceneManager.sceneOpened -= this.onSceneOpened;
            this.m_infoList.clear();
        }

        /// <summary>
        /// OnGUI
        /// </summary>
        // -----------------------------------------------------------------------------------------
        void OnGUI()
        {

            if (this.m_serializedObject == null)
            {
                this.m_serializedObject = new SerializedObject(this);
            }

            EditorGUIUtility.labelWidth = 200;

            this.m_scrollPos = EditorGUILayout.BeginScrollView(this.m_scrollPos);

            // HelpBox
            {
                EditorGUILayout.HelpBox(
                    "Mesh and Texture Combiner",
                    MessageType.Info
                    );
            }

            GUILayout.Space(20.0f);

            // m_textureAtlasSize
            {
                this.m_maximumAtlasSize = (MaximumAtlasSize)EditorGUILayout.EnumPopup("Maximum Atlas Size", this.m_maximumAtlasSize);
            }

            // m_atlasPaddingSize
            {
                this.m_atlasPaddingSize = EditorGUILayout.IntSlider("Atlas Padding Size", this.m_atlasPaddingSize, 0, 16);
            }

            GUILayout.Space(20.0f);

            // m_includeChildren
            {
                this.m_includeChildren = EditorGUILayout.Toggle("Use GetComponentsInChildren", this.m_includeChildren);
            }

            // m_tagsToExcludeFromCombining
            {

                EditorGUILayout.PropertyField(this.m_serializedObject.FindProperty("m_tagsToExcludeFromCombining"), true);

                this.m_serializedObject.ApplyModifiedProperties();

            }

            // m_gobjs
            {

                EditorGUILayout.PropertyField(this.m_serializedObject.FindProperty("m_gameObjects"), true);

                this.m_serializedObject.ApplyModifiedProperties();

            }

            GUILayout.Space(20.0f);

            //
            {

                EditorGUILayout.HelpBox(
                    "Please click [Check Materials] button again if you changed some Transform parameters.",
                    MessageType.Info
                    );

                GUI.enabled = this.m_gameObjects.Count > 0;

                if (GUILayout.Button("Check Materials", GUILayout.MinHeight(30)))
                {
                    this.checkMaterials();
                }

                GUI.enabled = true;

            }

            GUILayout.Space(20.0f);

            //
            {

                foreach (var val in this.m_infoList.infos)
                {

                    val.Value.foldout = EditorGUILayout.Foldout(val.Value.foldout, val.Key, true);

                    if (val.Value.foldout)
                    {

                        this.drawLineForPathsToCreate(ref val.Value.meshPathToCreate, "File Path for Mesh", "asset");
                        this.drawLineForPathsToCreate(ref val.Value.materialPathToCreate, "File Path for Material", "mat");
                        this.drawLineForPathsToCreate(ref val.Value.texturePathToCreate, "File Path for Texture", "png");

                    }

                    GUILayout.Space(20.0f);

                }

            }

            GUILayout.Space(20.0f);

            //
            {

                if (this.m_infoList.infos.Count > 0)
                {

                    if (GUILayout.Button("Generate", GUILayout.MinHeight(30)))
                    {

                        if (this.confirmOverwrite())
                        {
                            this.generate();
                        }

                    }

                }

            }

            EditorGUILayout.EndScrollView();

        }

        /// <summary>
        /// Confirm overwrite
        /// </summary>
        /// <returns>overwrite</returns>
        // -----------------------------------------------------------------------------------------
        bool confirmOverwrite()
        {

            StringBuilder sb = new StringBuilder();

            foreach (var val in this.m_infoList.infos)
            {

                if (File.Exists(val.Value.meshPathToCreate))
                {
                    sb.Append(val.Value.meshPathToCreate);
                    sb.Append("\n");
                }

                if (File.Exists(val.Value.materialPathToCreate))
                {
                    sb.Append(val.Value.materialPathToCreate);
                    sb.Append("\n");
                }

                if (File.Exists(val.Value.texturePathToCreate))
                {
                    sb.Append(val.Value.texturePathToCreate);
                    sb.Append("\n");
                }

            }

            if (sb.Length <= 0)
            {
                return EditorUtility.DisplayDialog("Confirmation", "Generate?", "Yes", "Cancel");
            }

            return EditorUtility.DisplayDialog("Confirmation", "Overwrite the following files?\n\n" + sb.ToString(), "Yes", "Cancel");

        }

        /// <summary>
        /// Draw line for paths to create
        /// </summary>
        // -----------------------------------------------------------------------------------------
        void drawLineForPathsToCreate(ref string path, string label, string extension)
        {

            EditorGUILayout.BeginHorizontal();

            path = EditorGUILayout.TextField(label, path);

            if (GUILayout.Button("...", GUILayout.MaxWidth(22)))
            {

                string tempPath = EditorUtility.SaveFilePanelInProject(
                    "Save",
                    Path.GetFileNameWithoutExtension(path),
                    extension,
                    "Please enter a file name",
                    path
                    );

                if (tempPath.Length > 0)
                {
                    path = tempPath;
                }

            }

            EditorGUILayout.EndHorizontal();

        }

        /// <summary>
        /// Is tag to combine
        /// </summary>
        /// <param name="gobj">GameObject</param>
        /// <returns>combine</returns>
        // -----------------------------------------------------------------------------------------
        bool isTagToCombine(GameObject gobj)
        {

            if (!gobj)
            {
                return false;
            }

            // ------------------

            return !this.m_tagsToExcludeFromCombining.Contains(gobj.tag);

        }

        /// <summary>
        /// Check Materials
        /// </summary>
        // -----------------------------------------------------------------------------------------
        void checkMaterials()
        {

            this.m_infoList.clear();

            MeshFilter tempMeshFilter = null;

            Renderer[] tempRenderers = null;

            StringBuilder warningMessageSB = new StringBuilder();

            foreach (var gobj in this.m_gameObjects)
            {

                if (!gobj)
                {
                    continue;
                }

                // --------------

                tempRenderers =
                    (this.m_includeChildren) ?
                    gobj.GetComponentsInChildren<Renderer>(true) :
                    new Renderer[] { gobj.GetComponent<Renderer>() };

                int numberCounter = 0;

                foreach (var renderer in tempRenderers)
                {

                    if (!renderer || !this.isTagToCombine(renderer.gameObject))
                    {
                        continue;
                    }

                    // ---------------

                    tempMeshFilter = renderer.GetComponent<MeshFilter>();

                    // ---------------

                    if (!tempMeshFilter || renderer.sharedMaterials.Length <= 0)
                    {
                        continue;
                    }

                    if (tempMeshFilter.sharedMesh.subMeshCount != renderer.sharedMaterials.Length)
                    {
                        Debug.LogError("sharedMesh.subMeshCount != sharedMaterials.Length : " + renderer.gameObject.name);
                        continue;
                    }

                    // ---------------

                    // 
                    {

                        int size = tempMeshFilter.sharedMesh.subMeshCount;

                        for (int i = 0; i < size; i++)
                        {

                            Material mat = renderer.sharedMaterials[i];

                            string texturePath = AssetDatabase.GetAssetPath(mat.mainTexture);

                            if (!mat.HasProperty("_MainTex"))
                            {
                                Debug.LogWarning("Material's shader does not have [_MainTex] property");
                                continue;
                            }

                            // --------------

                            // add CombineInfo
                            {

                                if (!this.m_infoList.infos.ContainsKey(mat.shader.name))
                                {
                                    this.m_infoList.infos.Add(mat.shader.name, new CombineInfo(mat.shader.name, ++numberCounter));
                                }

                            }

                            // add mainTexture
                            {

                                if (string.IsNullOrEmpty(texturePath))
                                {
                                    warningMessageSB.Append("Empty Texture : " + renderer.gameObject.name + "\n");
                                    continue;
                                }

                                else if (!this.m_infoList.infos[mat.shader.name].textureListToCombine.ContainsKey(texturePath))
                                {
                                    this.m_infoList.infos[mat.shader.name].textureListToCombine.Add(texturePath, mat.mainTexture as Texture2D);
                                }

                            }

                            // add mesh
                            {

                                Mesh newMesh = new Mesh();

                                List<Vector3> vertices = new List<Vector3>();
                                List<Vector2> uv = new List<Vector2>();
                                List<int> triangles = new List<int>();
                                List<Color> colors = new List<Color>();

                                int uvLength = tempMeshFilter.sharedMesh.uv.Length;
                                int colorLength = tempMeshFilter.sharedMesh.colors.Length;

                                Dictionary<int, int> link = new Dictionary<int, int>();

                                {

                                    int counter = 0;

                                    foreach (var index in tempMeshFilter.sharedMesh.GetIndices(i))
                                    {

                                        if (!link.ContainsKey(index))
                                        {

                                            link.Add(index, counter++);

                                            vertices.Add(tempMeshFilter.sharedMesh.vertices[index]);

                                            if (uvLength > index)
                                            {
                                                uv.Add(tempMeshFilter.sharedMesh.uv[index]);
                                            }

                                            if (colorLength > index)
                                            {
                                                colors.Add(tempMeshFilter.sharedMesh.colors[index]);
                                            }

                                        }

                                    }

                                    foreach (var index in tempMeshFilter.sharedMesh.GetTriangles(i))
                                    {
                                        triangles.Add(link[index]);
                                    }

                                }

                                newMesh.name = tempMeshFilter.sharedMesh.name + "_" + i;
                                newMesh.vertices = vertices.ToArray();
                                newMesh.uv = uv.ToArray();
                                newMesh.triangles = triangles.ToArray();
                                newMesh.colors = colors.ToArray();
                                newMesh.RecalculateNormals();

                                this.m_infoList.infos[mat.shader.name].meshListToCombine.Add(
                                    new MeshAndTexturePath(newMesh, texturePath, renderer.transform.localToWorldMatrix)
                                    );

                            }

                        }

                    }

                }

            }

            //
            {

                string result = string.Format("Found {0} Shaders", this.m_infoList.infos.Count);
                string warningMessage = warningMessageSB.ToString();

                if (this.m_infoList.infos.Count <= 0)
                {
                    result += "\n\n" + "(There's nothing to do)";
                }

                else if (warningMessage.Length > 0)
                {

                    if (warningMessage.Length < 300)
                    {
                        result += "\n\n" + "*Warnings\n" + warningMessage;
                    }

                    else
                    {
                        result += "\n\n" + "*Warnings\n" + warningMessage.Substring(0, 300) + "...";
                    }

                    Debug.LogWarning(warningMessage);

                }

                EditorUtility.DisplayDialog("Confirmation", result, "OK");

            }

        }

        /// <summary>
        /// Generate
        /// </summary>
        // -----------------------------------------------------------------------------------------
        void generate()
        {

            Texture2D atlas = null;
            Texture2D png = null;
            int maximumAtlasSize = (int)this.m_maximumAtlasSize;
            List<Texture2D> texList = new List<Texture2D>(); ;

            List<TextureImporter> resume = new List<TextureImporter>();
            Dictionary<string, Rect> rectDict = new Dictionary<string, Rect>();

            int generatedCounter = 0;

            // m_progress
            {
                this.m_progress.reset();
            }

            foreach (var val in this.m_infoList.infos)
            {

                texList.Clear();
                rectDict.Clear();

                // isReadable
                {

                    // m_progress
                    {
                        this.m_progress.denominator += val.Value.textureListToCombine.Count;
                        this.m_progress.message = "Set Texture's isReadable to true";
                        this.showProgressValues();
                    }

                    foreach (var tex in val.Value.textureListToCombine)
                    {

                        // m_progress
                        {
                            this.m_progress.numerator++;
                            this.m_progress.message = "Set Texture's isReadable to true";
                            this.showProgressValues();
                        }

                        TextureImporter ti = AssetImporter.GetAtPath(tex.Key) as TextureImporter;

                        bool oriReadable = ti.isReadable;

                        if (!ti.isReadable)
                        {
                            ti.isReadable = true;
                            AssetDatabase.ImportAsset(tex.Key, ImportAssetOptions.ForceSynchronousImport);
                            resume.Add(ti);
                        }

                    }

                }

                // PackTextures
                {

                    // m_progress
                    {
                        this.m_progress.numerator++;
                        this.m_progress.denominator++;
                        this.m_progress.message = "PackTextures";
                        this.showProgressValues();
                    }

                    atlas = new Texture2D(maximumAtlasSize, maximumAtlasSize);

                    texList = new List<Texture2D>(val.Value.textureListToCombine.Values);

                    Rect[] rects = atlas.PackTextures(texList.ToArray(), this.m_atlasPaddingSize, maximumAtlasSize);

                    int i = 0;

                    if (rects.Length == val.Value.textureListToCombine.Count)
                    {

                        foreach (var kv in val.Value.textureListToCombine)
                        {

                            if (!rectDict.ContainsKey(kv.Key))
                            {
                                rectDict.Add(kv.Key, rects[i++]);
                            }

                        }

                    }

                    else
                    {
                        Debug.LogError("Implementation Error : rects.Length != val.Value.textureListToCombine.Count");
                        continue;
                    }

                    png = new Texture2D(atlas.width, atlas.height, TextureFormat.ARGB32, false);

                    png.SetPixels(atlas.GetPixels());

                }

                // png
                {

                    // m_progress
                    {
                        this.m_progress.numerator++;
                        this.m_progress.denominator++;
                        this.m_progress.message = "Create PNG file";
                        this.showProgressValues();
                    }

                    byte[] bytes = png.EncodeToPNG();

                    Object.DestroyImmediate(atlas);
                    Object.DestroyImmediate(png);

                    Directory.CreateDirectory(Path.GetDirectoryName(val.Value.texturePathToCreate));
                    File.WriteAllBytes(val.Value.texturePathToCreate, bytes);

                    AssetDatabase.Refresh();

                }

                // mesh
                {

                    // m_progress
                    {
                        this.m_progress.numerator++;
                        this.m_progress.denominator++;
                        this.m_progress.message = "Create Mesh and Material";
                        this.showProgressValues();
                    }

                    int size = val.Value.meshListToCombine.Count;
                    CombineInstance[] combine = new CombineInstance[size];
                    MeshAndTexturePath matp = null;

                    Vector2 tempUv = Vector2.one;
                    Vector2[] tempUvArray = null;

                    for (int i = 0; i < size; i++)
                    {

                        matp = val.Value.meshListToCombine[i];

                        // uv
                        {

                            tempUvArray = new Vector2[matp.mesh.uv.Length];

                            for (int j = matp.mesh.uv.Length - 1; j >= 0; j--)
                            {

                                if (rectDict.ContainsKey(matp.texturePath))
                                {

                                    tempUv = matp.mesh.uv[j];

                                    tempUv.x *= rectDict[matp.texturePath].size.x;
                                    tempUv.y *= rectDict[matp.texturePath].size.y;
                                    tempUv.x += rectDict[matp.texturePath].x;
                                    tempUv.y += rectDict[matp.texturePath].y;

                                    tempUvArray[j] = tempUv;

                                }

                            }

                            if (tempUvArray != null)
                            {
                                matp.mesh.uv = tempUvArray;
                            }

                        }

                        combine[i].mesh = matp.mesh;
                        combine[i].transform = matp.matrix;

                    }

                    //
                    {

                        Mesh newMesh = new Mesh();
                        Material newMat = new Material(Shader.Find(val.Key));

                        newMesh.CombineMeshes(combine);
                        newMat.mainTexture = AssetDatabase.LoadAssetAtPath(val.Value.texturePathToCreate, typeof(Texture)) as Texture;

                        Unwrapping.GenerateSecondaryUVSet(newMesh);

                        Directory.CreateDirectory(Path.GetDirectoryName(val.Value.meshPathToCreate));
                        Directory.CreateDirectory(Path.GetDirectoryName(val.Value.materialPathToCreate));
                        AssetDatabase.CreateAsset(newMesh, val.Value.meshPathToCreate);
                        AssetDatabase.CreateAsset(newMat, val.Value.materialPathToCreate);

                        AssetDatabase.Refresh();

                        // GameObject
                        {
                            
                            GameObject combinedGobj = new GameObject(Path.GetFileNameWithoutExtension(val.Value.meshPathToCreate));

                            combinedGobj.AddComponent<MeshFilter>().sharedMesh = newMesh;
                            combinedGobj.AddComponent<MeshRenderer>().sharedMaterial = newMat;

                            generatedCounter++;

                        }

                    }

                }

            }


            // resume
            {

                // m_progress
                {
                    this.m_progress.denominator += resume.Count;
                    this.m_progress.message = "Resume Texture's isReadable";
                    this.showProgressValues();
                }

                foreach (var ti in resume)
                {

                    // m_progress
                    {
                        this.m_progress.numerator++;
                        this.m_progress.message = "Resume Texture's isReadable";
                        this.showProgressValues();
                    }

                    ti.isReadable = false;
                    AssetDatabase.ImportAsset(ti.assetPath, ImportAssetOptions.ForceSynchronousImport);

                }

            }

            // m_progress
            {
                this.closeProgressValues();
            }

            AssetDatabase.Refresh();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog(
                "Confirmation",
                string.Format("Done.\n\n{0} GameObjects will be generated.\n(Material parameters are reset)", generatedCounter),
                "Ok"
                );

        }

        /// <summary>
        /// Show progress values
        /// </summary>
        // -----------------------------------------------------------------------------------------
        void showProgressValues()
        {

            EditorUtility.DisplayProgressBar(
                "Progress",
                this.m_progress.message + " : " + this.m_progress.numerator + " / " + this.m_progress.denominator,
                this.m_progress.progress()
                );

        }

        /// <summary>
        /// Close progress values
        /// </summary>
        // -----------------------------------------------------------------------------------------
        void closeProgressValues()
        {
            this.m_progress.reset();
            EditorUtility.ClearProgressBar();
        }

    }

}
