using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using UnityEngine.Rendering;

#if UNITY_EDITOR
namespace UQImporter
{
    public class UQImporter_Main : EditorWindow
    {
        private static UQImporter_Main _window = null;
        private static Vector2 _windowMinSize = new Vector2(275, 300);
        private static Vector2 _scrollPosition = new Vector2();
        private static Texture2D _moreIcon;
        private static GUIStyle _centeredLabelStyle;
        private string _contextLabel = "";

        private static UserConfig _config;

        private string _selectedFilePath = "";
        private string _assetname = "";
        private string _destinationPath = "";
        private string[] _extractedFilePaths;
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

            _config = UserConfig.LoadUserConfig();

            GetIcons();
            RegisterGUIStyles();
        }

        private void OnGUI()
        {
            if (!_window)
            {
                OpenUQImporter();
            }

            Rect contentsRect = new Rect(0, 8, _window.position.width, _window.position.height - 38);
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
            UnityEngine.Object obj = Selection.activeObject;
            if (CheckValidSelectedObject(obj))
            {
                DrawImporterGUI();
            }
            else
            {
                DrawInvalidObjectGUI();
            }
        }

        private bool CheckValidSelectedObject(UnityEngine.Object o)
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
            var aname = _assetname;
            _contextLabel = "Adjust settings and import.";

            if (String.IsNullOrWhiteSpace(_assetname))
            {
                _assetname = Path.GetFileNameWithoutExtension(_selectedFilePath);
            }
            if (string.IsNullOrWhiteSpace(_destinationPath))
            {
                _destinationPath = _config.defaultDestinationPath;
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
            if (_config.useNameForDestinationFolder && aname != _assetname)
            {
                _destinationPath = _config.defaultDestinationPath;
                _destinationPath += "/" + _assetname;
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label(new GUIContent("Use Name for Folder", "If enabled, a folder will be created at the destination directory using the asset's name."));
            _config.useNameForDestinationFolder = GUILayout.Toggle(_config.useNameForDestinationFolder, "", GUILayout.MinWidth(60), GUILayout.MaxWidth(60));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.Space(20);

            if (GUILayout.Button(new GUIContent("Import Asset", "Extract, build, and import the selected asset to the above destination."), GUILayout.MinHeight(30)))
            {
                ClearCachedTextures();
                ExtractFiles();
                CacheExtractedFiles();
                RenameFiles();
                CreateMaterial();
                // Assign material to model
                // Save model as prefab

                AssetDatabase.Refresh();
            }
        }

        private void ClearCachedTextures()
        {
            _textures.Clear();

            LogContext("Clearing cache...OK");
        }

        private void ExtractFiles()
        {
            ZipFile.ExtractToDirectory(_selectedFilePath, _destinationPath);

            LogContext("Extracting files...OK");
        }

        private void CacheExtractedFiles()
        {
            _extractedFilePaths = Directory.GetFiles(_destinationPath);
            for (int i = 0; i < _extractedFilePaths.Length; i++)
            {
                string relativePath = _extractedFilePaths[i].Replace(Application.dataPath, "Assets").Replace("\\", "/");
                _extractedFilePaths[i] = relativePath;
                AssetDatabase.ImportAsset(_extractedFilePaths[i]);
            }

            LogContext("Caching extracted files...OK");
        }

        private void RenameFiles()
        {
            foreach (string filePath in _extractedFilePaths)
            {
                string newName = Path.GetFileName(filePath);
                string fileExt = Path.GetExtension(filePath);

                if (fileExt.Equals(".fbx", StringComparison.OrdinalIgnoreCase))
                {
                    newName = _assetname + fileExt;
                }
                else
                {
                    foreach (string tkey in _config.textureKeys)
                    {
                        if (filePath.Contains(tkey))
                        {
                            newName = _assetname + "_" + tkey + fileExt;
                            if (newName.Contains("_Normal"))
                            {
                                TextureImporter tImporter = (TextureImporter)AssetImporter.GetAtPath(filePath);
                                tImporter.textureType = TextureImporterType.NormalMap;
                            }
                            CacheTexture(tkey, (Texture2D)AssetDatabase.LoadAssetAtPath(filePath, typeof(Texture2D)));
                        }
                    }
                }

                AssetDatabase.RenameAsset(filePath, newName);

                LogContext("Renaming files...OK");
            }
        }

        private void CacheTexture(string key, Texture2D texture)
        {
            if (!_textures.ContainsKey(key))
            {
                _textures.Add(key, texture);
            }

            LogContext("Caching textures...OK");
        }

        private void CreateMaterial()
        {
            int renderPipeline = 2; //CheckRenderPipelineType();
            switch (renderPipeline)
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

            foreach (KeyValuePair<string, Texture2D> kvp in _textures)
            {
                string matProperty = GetMatProperty(kvp.Key, renderPipeline);
                if (matProperty != null)
                {
                    _assetMat.SetTexture(matProperty, kvp.Value);
                }
                else
                {
                    Debug.LogWarning($"Unkown texture key '{kvp.Key}'");
                }
            }

            AssetDatabase.CreateAsset(_assetMat, $"{_destinationPath}/{_assetname}.mat");
            AssetDatabase.SaveAssets();

            LogContext("Creating material with textures...OK");
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

        private int CheckRenderPipelineType()
        {
            if (GraphicsSettings.currentRenderPipeline.GetType().FullName.Contains("HDRenderPipelineAsset"))
            {
                return 2;
            }
            else if (GraphicsSettings.currentRenderPipeline.GetType().FullName.Contains("UniversalRenderPipelineAsset"))
            {
                return 1;
            }

            return 0;
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
                case "AO": return "_OcclusionMap";
                case "BaseColor": return "_BaseColorMap";
                case "Bump": return "_HeightMap";
                case "Diffuse": return "_BaseMap";
                case "Metalness": return "_MetallicGlossMap";
                case "Normal": return "_NormalMap";
                case "Roughness": return "_SmoothnessMap";
                case "Specular": return "_SpecGlossMap";
                default: return null;
            }
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
            if (_config.logContext)
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
        private static Vector2 _windowMinSize = new Vector2(150, 100);

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
            if (GUILayout.Button(new GUIContent("View repository", "Open UQImporter's repository page.")))
            {
                Application.OpenURL("https://github.com/um-stretch/UQImporter/");
            }
            GUILayout.Space(10);
            if (GUILayout.Button(new GUIContent("Create new config", "Create a new config file if the origianl is missing or deleted.")))
            {
                if (EditorUtility.DisplayDialog("Create new configuration file", "Previous configuration file (config.json) will be lost.\n\nContinue?", "Yes", "Cancel"))
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
        public bool logContext = false;
        public bool doubleSidedMaterial = true;
        public string[] textureKeys = new string[]
        {
            "AO",
            "BaseColor",
            "Bump",
            "Diffuse",
            "Metalness",
            "Normal",
            "Roughness",
            "Specular",
        };

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
            catch (Exception exc)
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
            catch (Exception exc)
            {
                Debug.LogError("Create new config file failed! " + exc);
            }

            AssetDatabase.Refresh();
            UQImporter_Main.OpenUQImporter();
        }
    }
}
#endif