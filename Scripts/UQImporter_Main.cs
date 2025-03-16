using UnityEngine;
using UnityEditor;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using UnityEngine.Rendering;
using System.Linq;

#if UNITY_EDITOR
namespace UQImporter
{
    public class UQImporter_Main : EditorWindow
    {
        private static UQImporter_Main _window = null;
        private static Vector2 _windowMinSize = new Vector2(275, 225);
        private static Vector2 _scrollPosition = new Vector2();
        private static Texture2D _moreIcon;
        private static GUIStyle _centeredLabelStyle;
        private string _contextLabel = "";
        private Object _selection;

        public static UserConfig config { get; private set; }
        private int _renderPipeline = 0;
        private int _matType = 0;

        private string _selectedFilePath = "";
        private string _assetname = "";
        private string _destinationPath = "";
        private string[] _extractedFilePaths;
        private GameObject _modelInstance;
        private Dictionary<string, Texture2D> _textures = new Dictionary<string, Texture2D>();
        private Material _assetMat;

        [MenuItem("Tools/Untiy Quixel Importer")]
        public static void OpenUQImporter()
        {
            _window = (UQImporter_Main)GetWindow(typeof(UQImporter_Main), false, "UQImporter");
            _window.minSize = _windowMinSize;

            if (_window)
            {
                _window.maxSize = _window.minSize;
                _window.maxSize = new Vector2(10000, 10000);
            }

            config = UserConfig.LoadUserConfig();

            GetIcons();
            RegisterGUIStyles();
        }

        private void OnGUI()
        {
            if (!_window)
            {
                OpenUQImporter();
            }

            Rect contentsRect = new Rect(0, 8, _window.position.width, _window.position.height - 32);
            GUILayout.BeginArea(contentsRect);
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

            DrawWindowContents();

            GUILayout.EndScrollView();
            GUILayout.EndArea();

            DrawFooter();

            Repaint();
        }

        private static void GetIcons()
        {
            _moreIcon = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/UQImporter/Icons/moreIcon.png", typeof(Texture2D));
        }

        private static void RegisterGUIStyles()
        {
            _centeredLabelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
            };
        }

        private void DrawWindowContents()
        {
            Object obj = Selection.activeObject;
            if (CheckValidSelectedObject(obj))
            {
                DrawImporterGUI();
            }
            else
            {
                DrawInvalidObjectGUI();
            }
        }

        private bool CheckValidSelectedObject(Object o)
        {
            if (o == null) return false;

            string objectPath = AssetDatabase.GetAssetPath(o);
            if (string.IsNullOrEmpty(objectPath))
            {
                return false;
            }

            string objectFullPath = Path.GetFullPath(objectPath);
            if (Path.GetExtension(objectFullPath).Equals(".zip", System.StringComparison.OrdinalIgnoreCase))
            {
                _selectedFilePath = objectFullPath;
                return true;
            }

            return false;
        }

        private void DrawImporterGUI()
        {
            _contextLabel = "Adjust settings and import.";

            if (System.String.IsNullOrWhiteSpace(_assetname) || _selection != Selection.activeObject)
            {
                _assetname = Path.GetFileNameWithoutExtension(_selectedFilePath);
                _selection = Selection.activeObject;
            }
            if (string.IsNullOrWhiteSpace(_destinationPath))
            {
                _destinationPath = config.defaultDestinationPath;
            }

            GUILayout.BeginVertical();

            GUILayout.Label(new GUIContent("Asset Name: "), EditorStyles.boldLabel);
            _assetname = EditorGUILayout.TextField(_assetname);

            GUILayout.Space(10);

            GUILayout.Label(new GUIContent("Destination Path:", "Location of asset after importing."), EditorStyles.boldLabel);
            GUILayout.BeginHorizontal();
            _destinationPath = EditorGUILayout.TextField(_destinationPath);
            if (GUILayout.Button(new GUIContent("...", "Browse"), GUILayout.MaxWidth(25)))
            {
                _destinationPath = EditorUtility.OpenFolderPanel("Choose a destination for imported files", _destinationPath, "");

            }
            if (config.useNameForDestinationFolder)
            {
                _destinationPath = config.defaultDestinationPath;
                _destinationPath += "/" + _assetname;
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            if (GUILayout.Button(new GUIContent("Import Asset", "Extract, build, and import the selected asset to the above destination."), GUILayout.MinHeight(40)))
            {
                try
                {
                    double s = EditorApplication.timeSinceStartup;

                    PrepareImporter();
                    GetRenderPipelineType();
                    ExtractFiles();
                    CacheExtractedFiles();
                    RenameFiles();
                    CreateMaterial();
                    UpdateModelImporter();
                    AttachMaterial();
                    SavePrefab();
                    CleanDirectory();

                    if (config.logCompletionTime) Debug.Log($"Import successful. Completed in {System.Math.Round(EditorApplication.timeSinceStartup - s, 2)} seconds.");
                }
                catch (System.Exception e)
                {
                    Debug.LogError("Import failed! " + e);
                }

                AssetDatabase.Refresh();
                if (config.pingImportedAsset) EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>($"{_destinationPath}/{_assetname}.prefab"));

                ClearCache();
            }
        }

        private void PrepareImporter()
        {
            _textures.Clear();
            _renderPipeline = 0;
            _matType = 0;

            LogContext("Prepare importer...OK");
        }

        private void GetRenderPipelineType()
        {
            string pipelineAsset = GraphicsSettings.currentRenderPipeline.GetType().FullName;
            if (GraphicsSettings.currentRenderPipeline != null)
            {
                if (pipelineAsset.Contains("HDRenderPipelineAsset"))
                {
                    _renderPipeline = 2;
                }
                else if (pipelineAsset.Contains("UniversalRenderPipelineAsset"))
                {
                    _renderPipeline = 1;
                }
            }
            else
            {
                _renderPipeline = 0;
            }
        }

        private void ExtractFiles()
        {
            ZipFile.ExtractToDirectory(_selectedFilePath, _destinationPath);

            LogContext("Extract files...OK");
        }

        private void CacheExtractedFiles()
        {
            _extractedFilePaths = Directory.GetFiles(_destinationPath);
            for (int i = 0; i < _extractedFilePaths.Length; i++)
            {
                string relativePath = _extractedFilePaths[i].Replace(Application.dataPath, "Assets").Replace("\\", "/");
                _extractedFilePaths[i] = relativePath;
            }

            AssetDatabase.Refresh();

            LogContext("Cache extracted files...OK");
        }

        private void RenameFiles()
        {
            foreach (string filePath in _extractedFilePaths)
            {
                string newName = Path.GetFileName(filePath);
                string fileExt = Path.GetExtension(filePath);

                if (fileExt.Equals(".fbx", System.StringComparison.OrdinalIgnoreCase))
                {
                    newName = _assetname + fileExt;
                }
                else
                {
                    foreach (string tkey in config.textureKeys)
                    {
                        if (filePath.Contains(tkey))
                        {
                            newName = _assetname + "_" + tkey + fileExt;
                            TextureImporter tImporter = (TextureImporter)AssetImporter.GetAtPath(filePath);
                            tImporter.isReadable = true;
                            tImporter.maxTextureSize = 8192;
                            tImporter.textureType = newName.Contains("_Normal") ? TextureImporterType.NormalMap : tImporter.textureType;

                            _matType = newName.Contains("_Opacity") ? 1 : _matType;

                            CacheTexture(tkey, (Texture2D)AssetDatabase.LoadAssetAtPath(filePath, typeof(Texture2D)));
                        }
                    }
                }

                AssetDatabase.RenameAsset(filePath, newName);

                LogContext("Rename files...OK");
            }
        }

        private void CacheTexture(string key, Texture2D texture)
        {
            if (!_textures.ContainsKey(key) && texture != null)
            {
                _textures.Add(key, texture);
            }

            LogContext("Cache textures...OK");
        }

        private void CreateMaterial()
        {
            switch (_renderPipeline)
            {
                case 0:
                    _assetMat = new Material(Shader.Find("Standard"));
                    break;
                case 1:
                    _assetMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    break;
                case 2:
                    _assetMat = new Material(Shader.Find("HDRP/Lit"));
                    break;
            }

            // Populate material properties.
            foreach (KeyValuePair<string, Texture2D> kvp in _textures)
            {
                string matProperty = GetMatProperty(kvp.Key, _renderPipeline);
                if (matProperty != null)
                {
                    _assetMat.SetTexture(matProperty, kvp.Value);
                }
            }

            if (_renderPipeline == 2)
            {
                Texture2D m = _textures.ContainsKey("Metalness") ? _textures["Metalness"] : null;
                Texture2D o = _textures.ContainsKey("AO") ? _textures["AO"] : null;
                Texture2D d = _textures.ContainsKey("Detail") ? _textures["Detail"] : null;
                Texture2D s = _textures.ContainsKey("Roughness") ? _textures["Roughness"] : null;

                GenerateMaskMap(m, o, d, s);
                Texture2D maskMap = AssetDatabase.LoadAssetAtPath<Texture2D>($"{_destinationPath}/{_assetname}_MaskMap.png");

                _assetMat.SetTexture("_MaskMap", maskMap);
            }

            if (_matType == 1)
            {
                _assetMat.SetFloat("_SurfaceType", 1);
                _assetMat.SetFloat("_BlendMode", 0);
                _assetMat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                _assetMat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                _assetMat.renderQueue = (int)RenderQueue.Transparent;

                Texture2D bMap = _textures["BaseColor"];
                Texture2D oMap = _textures["Opacity"];

                Color[] bMapPixels = bMap.GetPixels();
                Color[] oMapPixels = oMap.GetPixels();

                if (config.enableMultithreading)
                {
                    System.Threading.Tasks.Parallel.For(0, bMapPixels.Length - 1, i =>
                    {
                        float r = bMapPixels[i].r;
                        float g = bMapPixels[i].g;
                        float b = bMapPixels[i].b;
                        float a = oMapPixels[i].a;

                        bMapPixels[i] = new Color(r, g, b, a);
                    });
                }
                else
                {
                    for (int i = 0; i < bMapPixels.Length; i++)
                    {
                        bMapPixels[i] = new Color(bMapPixels[i].r, bMapPixels[i].g, bMapPixels[i].b, oMapPixels[i].a);
                    }
                }

                Texture2D boMap = new Texture2D(bMap.width, bMap.height);
                boMap.SetPixels(bMapPixels);
                boMap.Apply();
                _textures["BaseColor"] = boMap;
            }

            if (config.doubleSidedMaterial)
            {
                switch (_renderPipeline)
                {
                    case 0:
                        _assetMat.SetInt("_Cull", (int)CullMode.Off);
                        break;
                    case 1:
                        _assetMat.doubleSidedGI = true;
                        _assetMat.SetInt("_CullMode", (int)CullMode.Off);
                        break;
                    case 2:
                        _assetMat.SetInt("_DoubleSidedEnable", 1);
                        break;
                }
            }

            AssetDatabase.CreateAsset(_assetMat, $"{_destinationPath}/{_assetname}.mat");
            AssetDatabase.SaveAssets();

            LogContext("Create material and assign textures...OK");
        }

        private string GetMatProperty(string tKey, int renderPipeline)
        {
            switch (renderPipeline)
            {
                case 0:
                    return GetStandardMatProperty(tKey);
                case 1:
                    return GetURPMatProperty(tKey);
                case 2:
                    return GetHDRPMatProperty(tKey);
                default:
                    Debug.LogWarning("Unsupported render pipeline.");
                    return null;
            }
        }

        private string GetStandardMatProperty(string textureKey)
        {
            switch (textureKey)
            {
                case "AO": return "_OcclusionMap";
                case "BaseColor": return "_MainTex";
                case "Bump": return "_ParallaxMap";
                case "Diffuse": return "_MainTex";
                case "Metalness": return "_MetallicGlossMap";
                case "Normal": return "_BumpMap";
                default: return null;
            }
        }

        private string GetURPMatProperty(string textureKey)
        {
            switch (textureKey)
            {
                case "AO": return "_OcclusionMap";
                case "BaseColor": return "_BaseMap";
                case "Bump": return "_ParallaxMap";
                case "Diffuse": return "_BaseMap";
                case "Metalness": return "_MetallicGlossMap";
                case "Normal": return "_BumpMap";
                case "Roughness": return "_SmoothnessMap";
                case "Specular": return "_SpecGlossMap";
                default: return null;
            }
        }

        private string GetHDRPMatProperty(string textureKey)
        {
            switch (textureKey)
            {
                case "BaseColor": return "_BaseColorMap";
                case "Bump": return "_HeightMap";
                case "Normal": return "_NormalMap";
                case "Opacity": return "_Opacity";
                default: return null;
            }
        }

        private void GenerateMaskMap(Texture2D metallicMap, Texture2D occlusionMap, Texture2D detailMap, Texture2D smoothnessMap)
        {
            var firstNonNullRef = metallicMap ?? occlusionMap ?? detailMap ?? smoothnessMap;
            if (firstNonNullRef == null)
                return;

            int resolution = firstNonNullRef.width;

            Color[] mPixels = metallicMap != null ? metallicMap.GetPixels() : new Color[resolution * resolution];
            Color[] oPixels = occlusionMap != null ? occlusionMap.GetPixels() : Enumerable.Repeat(Color.white, resolution * resolution).ToArray();
            Color[] dPixels = detailMap != null ? detailMap.GetPixels() : Enumerable.Repeat(Color.white, resolution * resolution).ToArray();
            Color[] sPixels = smoothnessMap != null ? smoothnessMap.GetPixels() : Enumerable.Repeat(Color.white, resolution * resolution).ToArray();

            Texture2D maskMap = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
            Color[] maskPixels = new Color[resolution * resolution];

            if (config.enableMultithreading)
            {
                System.Threading.Tasks.Parallel.For(0, resolution * resolution, i =>
                {
                    float m = mPixels[i].grayscale;
                    float o = oPixels[i].grayscale;
                    float d = dPixels[i].grayscale;
                    float s = sPixels[i].grayscale;

                    maskPixels[i] = new Color(m, o, d, s);
                });
            }
            else
            {
                for (int i = 0; i < resolution * resolution; i++)
                {
                    float m = mPixels[i].grayscale;
                    float o = oPixels[i].grayscale;
                    float d = dPixels[i].grayscale;
                    float s = sPixels[i].grayscale;

                    maskPixels[i] = new Color(m, o, d, s);
                }
            }

            maskMap.SetPixels(maskPixels);
            maskMap.Apply();

            byte[] maskMapBytes = maskMap.EncodeToPNG();
            string path = $"{_destinationPath}/{_assetname}_MaskMap.png";
            File.WriteAllBytes(path, maskMapBytes);
            AssetDatabase.Refresh();

            LogContext("Generate maskMap...OK");
        }

        private void UpdateModelImporter()
        {
            string path = $"{_destinationPath}/{_assetname}.fbx";
            ModelImporter mImporter = (ModelImporter)AssetImporter.GetAtPath(path);
            mImporter.materialImportMode = ModelImporterMaterialImportMode.None;
            AssetDatabase.Refresh();

            LogContext("Update model importer...OK");
        }

        private void AttachMaterial()
        {
            string modelPath = $"{_destinationPath}/{_assetname}.fbx";
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            _modelInstance = (GameObject)PrefabUtility.InstantiatePrefab(model);

            Renderer r = _modelInstance.GetComponent<Renderer>();
            if (r == null) _modelInstance.GetComponentInChildren<Renderer>();
            r.material = _assetMat;

            LogContext("Attach material...OK");
        }

        private void SavePrefab()
        {
            string prefabPath = $"{_destinationPath}/{_assetname}.prefab";
            PrefabUtility.SaveAsPrefabAsset(_modelInstance, prefabPath);
            DestroyImmediate(_modelInstance);

            LogContext("Save prefab...OK");
        }

        private void CleanDirectory()
        {
            string[] allFiles = Directory.GetFiles(_destinationPath);

            foreach (string file in allFiles)
            {
                string fileExt = Path.GetExtension(file).ToLower();

                if (fileExt == ".png" || fileExt == ".jpg")
                {
                    MoveFile(file, "Textures");
                }
                else if (fileExt == ".mat")
                {
                    MoveFile(file, "Materials");
                }
                else if (fileExt == ".fbx")
                {
                    MoveFile(file, "Models");
                }
                else if (fileExt != ".prefab")
                {
                    MoveFile(file, "Other");
                }
            }

            if (config.deleteZipFile)
            {
                string path = _selectedFilePath;
                string kw = Application.dataPath;
                int index = path.IndexOf(kw);

                string relativePath = path.Substring(index + kw.Length + 2);
                relativePath = Path.Combine("Assets", relativePath).Replace("\\", "/");
                AssetDatabase.DeleteAsset(relativePath);
            }

            LogContext("Clean directory...OK");
        }

        private void MoveFile(string file, string folderName)
        {
            string newPath = $"{_destinationPath}/{folderName}";

            if (!Directory.Exists(newPath))
            {
                Directory.CreateDirectory(newPath);
                AssetDatabase.Refresh();
            }

            newPath = $"{newPath}/{Path.GetFileName(file)}";
            AssetDatabase.MoveAsset(file, newPath);
        }

        private void ClearCache()
        {
            _selectedFilePath = "";
            _assetname = "";
            _destinationPath = "";
            _extractedFilePaths = null;
            _modelInstance = null;
            _textures = new Dictionary<string, Texture2D>();
            _assetMat = null;

            LogContext("Clear cache...OK");
        }

        private void DrawInvalidObjectGUI()
        {
            _contextLabel = "Select a Quixel .zip file to get started.";
            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            DrawContextLabel();
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();

            _assetname = "";
            _destinationPath = "";
        }

        private void DrawFooter()
        {
            Rect infoRect = new Rect(_window.position.width - 35, _window.position.height - 30, _window.position.width, _window.position.height);
            GUILayout.BeginArea(infoRect);
            if (GUILayout.Button(new GUIContent(_moreIcon, "More..."), GUIStyle.none))
            {
                UQImporter_More.OpenWindow();
            }
            GUILayout.EndArea();
        }

        private void LogContext(string context)
        {
            if (config.logContext)
            {
                Debug.Log(context);
            }
        }

        private void DrawContextLabel()
        {
            GUILayout.Label(_contextLabel, _centeredLabelStyle);
        }
    }

    public class UQImporter_More : EditorWindow
    {
        private static UQImporter_More _window;
        private static Vector2 _windowMinSize = new Vector2(150, 75);

        public static void OpenWindow()
        {
            _window = (UQImporter_More)GetWindow(typeof(UQImporter_More), true, "UQImporter (More)");

            if (_window)
            {
                _window.Show();
                _window.maxSize = _windowMinSize;
                _window.minSize = _windowMinSize;

                Vector2 mousePos = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
                _window.position = new Rect(mousePos.x, mousePos.y, _window.position.width, _window.position.height);
            }
        }

        private void OnGUI()
        {
            GUILayout.Space(5);
            if (GUILayout.Button(new GUIContent("View repository", "Open UQImporter's repository page.")))
            {
                Application.OpenURL("https://github.com/um-stretch/UQImporter/");
            }
            GUILayout.Space(5);

            if (GUILayout.Button(new GUIContent("Ping config file", "Ping config file in the project panel.")))
            {
                EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>($"{UQImporter_Main.config.pathToUQImporter}/Data/config.json"));
            }
            if (GUILayout.Button(new GUIContent("Create new config file", "Create a new config file if the origianl is missing or deleted.")))
            {
                if (EditorUtility.DisplayDialog("Create new configuration file", "Previous configuration file will be lost.\n\nContinue?", "Yes", "Cancel"))
                {
                    UserConfig.CreateConfigFile();
                }
            }

            if (EditorWindow.focusedWindow != this)
            {
                Close();
            }
        }
    }

    [System.Serializable]
    public class UserConfig
    {
        public string pathToUQImporter = "Assets/UQImporter";
        public string defaultDestinationPath = "Assets/Quixel";
        public bool useNameForDestinationFolder = true;
        public bool doubleSidedMaterial = true;
        public string[] textureKeys = new string[]
        {
            "AO",
            "BaseColor",
            "Bump",
            "Diffuse",
            "Metalness",
            "Normal",
            "Opacity",
            "Roughness",
            "Specular",
        };
        public bool logCompletionTime = true;
        public bool pingImportedAsset = true;
        public bool logContext = false;
        public bool cleanDirectory = true;
        public bool deleteZipFile = false;
        public bool enableMultithreading = false;

        public static UserConfig LoadUserConfig()
        {
            UserConfig config = new UserConfig();

            try
            {
                string configPath = $"Assets\\UQImporter\\Data\\config.json";

                if (File.Exists(configPath))
                {
                    string jsonS = File.ReadAllText(configPath);
                    config = JsonUtility.FromJson<UserConfig>(jsonS);
                }
                else
                {
                    Debug.LogWarning($"UQImporter: No config.json file found at {configPath}. Loading default configuration.");
                }
            }
            catch (System.Exception exc)
            {
                Debug.LogError("Loading config failed! " + exc);
            }

            return config;
        }

        public static void CreateConfigFile()
        {
            try
            {
                string filePath = "Assets\\UQImporter\\Data";

                if (!Directory.Exists(filePath))
                {
                    Directory.CreateDirectory(filePath);
                }

                string configData = JsonUtility.ToJson(new UserConfig(), true);
                File.WriteAllText($"{filePath}\\config.json", configData);
            }
            catch (System.Exception exc)
            {
                Debug.LogError("Create new config file failed! " + exc);
            }

            AssetDatabase.Refresh();
            UQImporter_Main.OpenUQImporter();
        }
    }
}
#endif