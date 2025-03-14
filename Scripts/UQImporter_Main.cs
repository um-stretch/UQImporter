using UnityEngine;
using UnityEditor;
using System.IO;
using System.IO.Compression;

namespace UQImporter
{
    public class UQImporter_Main : EditorWindow
    {
        private static UQImporter_Main _window;
        private static Vector2 _windowMinSize = new Vector2(275, 300);
        private static Vector2 _scrollPosition = new Vector2();

        private static string _selectedFilePath = "";
        private static string _extractedFilePath = "";

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
        }

        private void OnGUI()
        {
            GUILayout.BeginScrollView(_scrollPosition);

            Object selection = Selection.activeObject;
            if (ValidateSelectedObject(selection))
            {
                GUILayout.Label("Selection is valid.");
            }
            else
            {
                GUILayout.Label("Select a valid .zip file.");
            }

            GUILayout.EndScrollView();
        }

        private bool ValidateSelectedObject(Object o)
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
                return true;
            }

            return false;
        }
    }
}