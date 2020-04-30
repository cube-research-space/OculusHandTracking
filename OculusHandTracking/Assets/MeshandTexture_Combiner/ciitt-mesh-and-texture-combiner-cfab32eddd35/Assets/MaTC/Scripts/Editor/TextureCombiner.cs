using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MaTC
{

    /// <summary>
    /// Texture Combiner
    /// </summary>
    public class TextureCombiner : EditorWindow
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

        class CombineInfo
        {

            public string shaderName = "";

            public string meshFolderPath = "Assets/New Meshes Folder";
            public string materialPathToCreate = "Assets/Combined Material.mat";
            public string texturePathToCreate = "Assets/Combined Texture.png";

            /// <summary>
            /// Original texture file path - texture
            /// </summary>
            public Dictionary<string, Texture2D> textureListToCombine = new Dictionary<string, Texture2D>();

            public bool foldout = true;

            public CombineInfo()
            {

            }

            public CombineInfo(string _shaderName, int number)
            {

                string sceneName = EditorSceneManager.GetActiveScene().name;

                this.shaderName = _shaderName;

                this.meshFolderPath = string.Format("Assets/MaTCOutput/{0}/Mesh/{1}/", sceneName, _shaderName);
                this.materialPathToCreate = string.Format("Assets/MaTCOutput/{0}/Material/{1}.mat", sceneName, _shaderName);
                this.texturePathToCreate = string.Format("Assets/MaTCOutput/{0}/Texture/{1}.png", sceneName, _shaderName);

            }

            public string meshFilePathToCreate(Mesh mesh, Texture tex)
            {

                if (!mesh || !tex)
                {
                    return "";
                }

                return
                    this.meshFolderPath +
                    mesh.name +
                    " " +
                    AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(mesh)) +
                    AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(tex)) +
                    ".asset"
                    ;

            }

        }

        class CombineInfoList
        {

            /// <summary>
            /// Shader name - CombineInfo
            /// </summary>
            public Dictionary<string, CombineInfo> infos = new Dictionary<string, CombineInfo>();

            public void clear()
            {
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

                if(denominator <= 0)
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

            if(mode == OpenSceneMode.Single)
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

            this.m_scrollPos = EditorGUILayout.BeginScrollView(this.m_scrollPos);

            // HelpBox
            {
                EditorGUILayout.HelpBox(
                    "This tool will create the things as follows\n\n" +
                    "Combined Materials\n" +
                    "Combined Textures\n" +
                    "Each mesh with changing UV\n" +
                    "(And current scene will be copied for backup with current DateTime)\n",
                    MessageType.Info
                    );
            }

            // HelpBox
            {
                EditorGUILayout.HelpBox(
                    "This tool will replace mesh and material with new ones in each GameObject.\n" +
                    "So this tool will DESTROY your scene.\n",
                    MessageType.Error
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

                GUI.enabled = this.m_gameObjects.Count > 0;

                if (GUILayout.Button("Check Materials", GUILayout.MinHeight(30)))
                {
                    this.checkTags();
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

                        this.drawLineForMeshFolder(ref val.Value.meshFolderPath, "Folder Path for Mesh");
                        this.drawLineForFilePathsToCreate(ref val.Value.materialPathToCreate, "File Path for Material", "mat");
                        this.drawLineForFilePathsToCreate(ref val.Value.texturePathToCreate, "File Path for Texture", "png");

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

                        if (this.checkIfPathsAreValid())
                        {

                            if (this.confirmOverwrite())
                            {

                                this.createBackupCurrentScene();

                                                    
                                this.generate();
                            }

                        }

                    }

                }

            }

            EditorGUILayout.EndScrollView();

            // ClearProgressBar
            {
                EditorUtility.ClearProgressBar();
            }

        }

        /// <summary>
        /// OnInspectorUpdate
        /// </summary>
        // -----------------------------------------------------------------------------------------
        void OnInspectorUpdate()
        {
            Repaint();
        }

        /// <summary>
        /// checkIfPathsAreValid internal
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        // -----------------------------------------------------------------------------------------
        string checkIfPathsAreValidInternal(string filePath)
        {

            string ret = "";

            if (!string.IsNullOrEmpty(filePath))
            {
                
                if (!Path.GetFullPath(filePath).Contains(Path.GetFullPath(Application.dataPath)))
                {
                    ret = filePath + "\n";
                }

            }

            else
            {
                ret = "(Empty path)\n";
            }

            return ret;

        }

        /// <summary>
        /// Check if paths are valid
        /// </summary>
        /// <returns>valid</returns>
        // -----------------------------------------------------------------------------------------
        bool checkIfPathsAreValid()
        {

            StringBuilder sb = new StringBuilder();

            foreach (var val in this.m_infoList.infos)
            {
                sb.Append(this.checkIfPathsAreValidInternal(val.Value.meshFolderPath));
                sb.Append(this.checkIfPathsAreValidInternal(val.Value.materialPathToCreate));
                sb.Append(this.checkIfPathsAreValidInternal(val.Value.texturePathToCreate));
            }

            if (sb.Length > 0)
            {
                EditorUtility.DisplayDialog("Error", "Out of project folder\n\n" + sb.ToString(), "OK");
                return false;
            }

            return true;

        }

        /// <summary>
        /// Confirm overwrite
        /// </summary>
        /// <returns>overwrite</returns>
        // -----------------------------------------------------------------------------------------
        bool confirmOverwrite()
        {

            StringBuilder sb = new StringBuilder();

            MeshFilter tempMeshFilter = null;

            List<Renderer> tempRenderers = null;

            HashSet<string> tempMeshPaths = new HashSet<string>();

            int counter = 0;

            foreach (var gobj in this.m_gameObjects)
            {

                if (!gobj)
                {
                    continue;
                }

                // --------------

                tempRenderers = new List<Renderer>(
                    (this.m_includeChildren) ?
                    gobj.GetComponentsInChildren<Renderer>(true) :
                    new Renderer[] { gobj.GetComponent<Renderer>() }
                    )
                    ;

                foreach (var renderer in tempRenderers)
                {

                    if (!renderer || !this.isTagToCombine(renderer.gameObject))
                    {
                        continue;
                    }

                    // ---------------

                    tempMeshFilter = renderer.GetComponent<MeshFilter>();

                    // ---------------

                    if (!tempMeshFilter || !renderer.sharedMaterial || !tempMeshFilter.sharedMesh)
                    {
                        continue;
                    }

                    string originalShaderName = renderer.sharedMaterial.shader.name;

                    string newMeshFilePath =
                        this.m_infoList.infos[originalShaderName].meshFilePathToCreate(
                            tempMeshFilter.sharedMesh,
                            renderer.sharedMaterial.mainTexture
                            );

                    //
                    {
                        if (string.IsNullOrEmpty(newMeshFilePath))
                        {
                            continue;
                        }
                    }

                    if(tempMeshPaths.Add(newMeshFilePath))
                    {

                        if (File.Exists(newMeshFilePath))
                        {
                            sb.Append(newMeshFilePath);
                            sb.Append("\n");
                            counter++;
                        }

                    }

                }

            }

            foreach (var val in this.m_infoList.infos)
            {

                if (File.Exists(val.Value.materialPathToCreate))
                {
                    sb.Append(val.Value.materialPathToCreate);
                    sb.Append("\n");
                    counter++;
                }

                if (File.Exists(val.Value.texturePathToCreate))
                {
                    sb.Append(val.Value.texturePathToCreate);
                    sb.Append("\n");
                    counter++;
                }

            }

            if (sb.Length <= 0)
            {
                return EditorUtility.DisplayDialog("Confirmation", "Generate?", "Yes", "Cancel");
            }

            string temp = sb.ToString();
            temp = temp.Length <= 500 ? temp : temp.Substring(0, 500) + "...";

            return EditorUtility.DisplayDialog(
                "Confirmation",
                string.Format("Overwrite {0} files?\n\n{1}", counter, temp),
                "Yes",
                "Cancel"
                );

        }

        /// <summary>
        /// Draw line for paths to create
        /// </summary>
        // -----------------------------------------------------------------------------------------
        void drawLineForFilePathsToCreate(ref string path, string label, string extension)
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
        /// Draw line for mesh folder
        /// </summary>
        // -----------------------------------------------------------------------------------------
        void drawLineForMeshFolder(ref string path, string label)
        {

            EditorGUILayout.BeginHorizontal();

            path = EditorGUILayout.TextField(label, path);

            if (GUILayout.Button("...", GUILayout.MaxWidth(22)))
            {

                string tempPath = EditorUtility.SaveFolderPanel(
                    "Save",
                    (string.IsNullOrEmpty(path)) ? "" : Path.GetDirectoryName(path),
                    ""
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

            if(!gobj)
            {
                return false;
            }

            // ------------------

            return !this.m_tagsToExcludeFromCombining.Contains(gobj.tag);

        }

        /// <summary>
        /// Check tag
        /// </summary>
        // -----------------------------------------------------------------------------------------
        void checkTags()
        {

            List<string> tags = new List<string>(UnityEditorInternal.InternalEditorUtility.tags);

            foreach(var val in this.m_tagsToExcludeFromCombining)
            {
                if(!tags.Contains(val))
                {
                    Debug.LogWarning("The project does not contain a tag : " + val);
                }
            }

        }

        /// <summary>
        /// Check Materials
        /// </summary>
        // -----------------------------------------------------------------------------------------
        void checkMaterials()
        {

            this.m_infoList.clear();

            MeshFilter tempMeshFilter = null;

            List<Renderer> tempRenderers = null;

            StringBuilder warningMessageSB = new StringBuilder();

            foreach (var gobj in this.m_gameObjects)
            {

                if (!gobj)
                {
                    continue;
                }

                // --------------

                tempRenderers = new List<Renderer>(
                    (this.m_includeChildren) ?
                    gobj.GetComponentsInChildren<Renderer>(true) :
                    new Renderer[] { gobj.GetComponent<Renderer>() }
                    )
                    ;

                foreach (var renderer in tempRenderers)
                {

                    if (!renderer || !this.isTagToCombine(renderer.gameObject))
                    {
                        continue;
                    }

                    // ---------------

                    tempMeshFilter = renderer.GetComponent<MeshFilter>();

                    // ---------------

                    if (!tempMeshFilter || !renderer.sharedMaterial || !tempMeshFilter.sharedMesh)
                    {
                        continue;
                    }

                    //
                    {

                        int size = tempMeshFilter.sharedMesh.subMeshCount;

                        if (size >= 2)
                        {
                            Debug.LogWarning("Not supported : subMeshCount >= 2 : " + renderer.gameObject.name);
                            continue;
                        }

                    }

                    // ---------------

                    //
                    {

                        Material mat = renderer.sharedMaterial;

                        string texturePath = "";

                        texturePath = AssetDatabase.GetAssetPath(mat.mainTexture);

                        if (!mat.HasProperty("_MainTex"))
                        {
                            Debug.LogWarning("Material's shader does not have [_MainTex] property : " + mat.name);
                            continue;
                        }

                        // -----------------

                        // add CombineInfo
                        {

                            if (!this.m_infoList.infos.ContainsKey(mat.shader.name))
                            {
                                this.m_infoList.infos.Add(mat.shader.name, new CombineInfo(mat.shader.name, this.m_infoList.infos.Count + 1));
                            }

                        }

                        // add mainTexture
                        {

                            if (string.IsNullOrEmpty(texturePath))
                            {
                                warningMessageSB.Append("Empty Texture : " + renderer.gameObject.name + "\n");
                            }

                            else if (!this.m_infoList.infos[mat.shader.name].textureListToCombine.ContainsKey(texturePath))
                            {

                                if (mat.mainTexture)
                                {
                                    this.m_infoList.infos[mat.shader.name].textureListToCombine.Add(texturePath, mat.mainTexture as Texture2D);
                                }

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
        /// Create backup
        /// </summary>
        // -----------------------------------------------------------------------------------------
        bool createBackupCurrentScene()
        {

            string scenePath = EditorSceneManager.GetActiveScene().path;

            string bkScenePath = scenePath.Replace(".unity", "") + "." + System.DateTime.Now.ToString("yyyyMMddHHmmss") + ".unity";

            if(File.Exists(Path.GetFullPath(bkScenePath)))
            {
                EditorUtility.DisplayDialog(
                    "Error",
                    "Failed to create backup scene file.\n\n" + bkScenePath,
                    "Ok"
                    );
                return false;
            }

            FileUtil.CopyFileOrDirectory(scenePath, bkScenePath);

            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Confirmation",
                "Backup scene file was created.\n\n" + bkScenePath,
                "Ok"
                );

            return true;

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

            // shader name - <original texture file path - Rect>
            Dictionary<string, Dictionary<string, Rect>> rectDict = new Dictionary<string, Dictionary<string, Rect>>();

            List<TextureImporter> resume = new List<TextureImporter>();

            // m_progress
            {
                this.m_progress.reset();
            }

            // create texture and material
            {

                foreach (var infoKV in this.m_infoList.infos)
                {

                    rectDict.Add(infoKV.Key, new Dictionary<string, Rect>());

                    // isReadable
                    {

                        // m_progress
                        {
                            this.m_progress.denominator += infoKV.Value.textureListToCombine.Count;
                            this.m_progress.message = "Set Texture's isReadable to true";
                            this.showProgressValues();
                        }

                        foreach (var tex in infoKV.Value.textureListToCombine)
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

                        AssetDatabase.Refresh();

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

                        texList = new List<Texture2D>(infoKV.Value.textureListToCombine.Values);

                        Rect[] rects = atlas.PackTextures(texList.ToArray(), this.m_atlasPaddingSize, maximumAtlasSize);

                        if (rects.Length == infoKV.Value.textureListToCombine.Count)
                        {

                            int i = 0;

                            foreach (var kv in infoKV.Value.textureListToCombine)
                            {
                                rectDict[infoKV.Key].Add(kv.Key, rects[i++]);
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

                        Directory.CreateDirectory(Path.GetDirectoryName(infoKV.Value.texturePathToCreate));
                        File.WriteAllBytes(infoKV.Value.texturePathToCreate, bytes);

                        AssetDatabase.Refresh();

                    }

                    // Material
                    {

                        // m_progress
                        {
                            this.m_progress.numerator++;
                            this.m_progress.denominator++;
                            this.m_progress.message = "Create Material";
                            this.showProgressValues();
                        }

                        Material newMat = new Material(Shader.Find(infoKV.Key));

                        newMat.mainTexture = AssetDatabase.LoadAssetAtPath(infoKV.Value.texturePathToCreate, typeof(Texture)) as Texture;

                        Directory.CreateDirectory(Path.GetDirectoryName(infoKV.Value.materialPathToCreate));
                        AssetDatabase.CreateAsset(newMat, infoKV.Value.materialPathToCreate);

                        AssetDatabase.Refresh();

                    }

                }

            }

            // replace
            {

                MeshFilter tempMeshFilter = null;

                List<Renderer> tempRenderers = null;

                Dictionary<string, Mesh> alreadyCreatedMesh = new Dictionary<string, Mesh>();

                foreach (var gobj in this.m_gameObjects)
                {

                    if (!gobj)
                    {
                        continue;
                    }

                    // --------------

                    tempRenderers = new List<Renderer>(
                        (this.m_includeChildren) ?
                        gobj.GetComponentsInChildren<Renderer>(true) :
                        new Renderer[] { gobj.GetComponent<Renderer>() }
                        )
                        ;

                    // m_progress
                    {
                        this.m_progress.denominator += tempRenderers.Count;
                        this.m_progress.message = "Replace mesh and material in each GameObject";
                        this.showProgressValues();
                    }

                    foreach (var renderer in tempRenderers)
                    {

                        // m_progress
                        {
                            this.m_progress.numerator++;
                            this.m_progress.message = "Replace mesh and material in each GameObject";
                            this.showProgressValues();
                        }

                        if (!renderer || !this.isTagToCombine(renderer.gameObject))
                        {
                            continue;
                        }

                        // ---------------

                        tempMeshFilter = renderer.GetComponent<MeshFilter>();

                        // ---------------

                        if (!tempMeshFilter || !renderer.sharedMaterial || !tempMeshFilter.sharedMesh)
                        {
                            continue;
                        }

                        string originalShaderName = renderer.sharedMaterial.shader.name;

                        if (!this.m_infoList.infos.ContainsKey(originalShaderName))
                        {
                            Debug.LogError("Implementation error : " + originalShaderName);
                            continue;
                        }

                        //
                        {

                            int size = tempMeshFilter.sharedMesh.subMeshCount;

                            if (size >= 2)
                            {
                                continue;
                            }

                            if(!this.m_infoList.infos.ContainsKey(originalShaderName))
                            {
                                Debug.LogError("Implementation error : " + originalShaderName);
                                continue;
                            }

                        }

                        string newMeshFilePath =
                            this.m_infoList.infos[originalShaderName].meshFilePathToCreate(
                                tempMeshFilter.sharedMesh,
                                renderer.sharedMaterial.mainTexture
                                );

                        //
                        {
                            if(string.IsNullOrEmpty(newMeshFilePath))
                            {
                                continue;
                            }
                        }

                        //new mesh
                        {

                            Mesh newMesh = null;

                            if (alreadyCreatedMesh.ContainsKey(newMeshFilePath))
                            {
                                newMesh = alreadyCreatedMesh[newMeshFilePath];
                            }

                            else
                            {

                                newMesh = Instantiate(tempMeshFilter.sharedMesh);

                                alreadyCreatedMesh.Add(newMeshFilePath, newMesh);

                                // uv
                                {

                                    Vector2 tempUv = Vector2.one;
                                    Vector2[] tempUvArray = new Vector2[newMesh.uv.Length]; ;

                                    string texturePath = AssetDatabase.GetAssetPath(renderer.sharedMaterial.mainTexture);

                                    if (rectDict[originalShaderName].ContainsKey(texturePath))
                                    {

                                        for (int j = newMesh.uv.Length - 1; j >= 0; j--)
                                        {

                                            Rect tempRect = rectDict[originalShaderName][texturePath];

                                            tempUv = newMesh.uv[j];

                                            tempUv.x *= tempRect.size.x;
                                            tempUv.y *= tempRect.size.y;
                                            tempUv.x += tempRect.x;
                                            tempUv.y += tempRect.y;

                                            tempUvArray[j] = tempUv;

                                        }

                                    }

                                    if (tempUvArray != null)
                                    {
                                        newMesh.uv = tempUvArray;
                                    }

                                }

                                // CreateAsset mesh
                                {
                                    Directory.CreateDirectory(Path.GetDirectoryName(newMeshFilePath));
                                    AssetDatabase.CreateAsset(newMesh, newMeshFilePath);
                                    AssetDatabase.Refresh();
                                }

                            }

                            //
                            {
                                tempMeshFilter.sharedMesh = newMesh;
                            }

                        }

                        // set sharedMaterial
                        {

                            renderer.sharedMaterial =
                                AssetDatabase.LoadAssetAtPath(
                                    this.m_infoList.infos[renderer.sharedMaterial.shader.name].materialPathToCreate,
                                    typeof(Material)
                                    ) as Material;

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
                "Done.\n\n(Material parameters are reset)",
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
