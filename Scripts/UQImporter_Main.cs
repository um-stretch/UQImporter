using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.IO.Compression;

namespace UQImporter
{
    public class UQImporter_Main : EditorWindow
    {
        private static UQImporter_Main _window = null;
        private static Vector2 _windowMinSize = new Vector2(275, 300);
        private static Vector2 _scrollPosition = new Vector2();

        private string _selectedFilePath = "";
        private string _assetname = "";
        private string _destinationPath = "Assets/";

        private static Texture2D _infoIcon;

        private static GUIStyle _centeredLabelStyle;

        [MenuItem("Tools/Quixel Importer")]
        public static void OpenUQImporter()
        {
            _window = (UQImporter_Main)GetWindow(typeof(UQImporter_Main), false, "Quixel Importer");
            _window.minSize = _windowMinSize;

            if (_window)
            {
                _window.maxSize = _window.minSize;
                _window.maxSize = new Vector2(10000, 10000);
            }

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
            _infoIcon = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/unity-quixel-importer/Icons/info-circle.png", typeof(Texture2D));
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
            if (String.IsNullOrWhiteSpace(_assetname))
            {
                _assetname = Path.GetFileNameWithoutExtension(_selectedFilePath);
            }
            if (string.IsNullOrWhiteSpace(_destinationPath))
            {
                _destinationPath = "Assets/";
            }

            GUILayout.BeginVertical();

            GUILayout.Label(new GUIContent("Asset Name: ", "Leave blank to use original file name."));
            _assetname = EditorGUILayout.TextField(_assetname);

            GUILayout.Label(new GUIContent("Destination Path:", "Location of the asset after importing."));
            GUILayout.BeginHorizontal();
            _destinationPath = EditorGUILayout.TextField(_destinationPath);
            if (GUILayout.Button(new GUIContent("...", "Browse"), GUILayout.MaxWidth(25)))
            {
                _destinationPath = EditorUtility.OpenFolderPanel("Choose a destination for imported files", _destinationPath, "");
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.Space(10);

            if (GUILayout.Button(new GUIContent("Import Asset", "Extract, build, and import the selected asset file to the above destination."), GUILayout.MinHeight(30)))
            {
                try
                {
                    ExtractFiles();
                }
                catch(Exception exc)
                {
                    Debug.Log(exc);
                }

                AssetDatabase.Refresh();
            }
        }

        private void ExtractFiles()
        {
            ZipFile.ExtractToDirectory(_selectedFilePath, _destinationPath);
        }

        private void DrawInvalidObjectGUI()
        {
            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Select a Quixel .zip file to get started.", _centeredLabelStyle);
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();

            _assetname = "";
            _destinationPath = "";
        }

        private void DrawFooter()
        {
            Rect infoRect = new Rect(_window.position.width - 30, _window.position.height - 30, _window.position.width, _window.position.height);
            GUILayout.BeginArea(infoRect);
            if (GUILayout.Button(new GUIContent(_infoIcon, "View repository."), GUIStyle.none))
            {
                Application.OpenURL("https://github.com/um-stretch/unity-quixel-importer/");
            }
            GUILayout.EndArea();
        }
    }
}