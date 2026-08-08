using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PaiSho.Board
{
    public static class PlayerStandLoader
    {
        public const string ModelAssetPath = "Assets/Models/Board/PlayerStand.glb";
        public const string ModelResourcesPath = "Assets/Resources/Board/PlayerStand.glb";
        public const string ResourcesKey = "Board/PlayerStand";

        public static bool TryLoadModel(out GameObject modelRoot, out string sourcePath)
        {
            sourcePath = null;
            modelRoot = null;

#if UNITY_EDITOR
            if (TryLoadFromAssetPath(ModelAssetPath, out modelRoot, out sourcePath))
                return true;

            if (TryLoadFromAssetPath(ModelResourcesPath, out modelRoot, out sourcePath))
                return true;
#endif

            return TryLoadFromResources(out modelRoot, out sourcePath);
        }

#if UNITY_EDITOR
        private static bool TryLoadFromAssetPath(string path, out GameObject modelRoot, out string sourcePath)
        {
            modelRoot = null;
            sourcePath = null;
            if (!File.Exists(path))
                return false;

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
                return false;

            sourcePath = path;
            modelRoot = prefab;
            return true;
        }
#endif

        private static bool TryLoadFromResources(out GameObject modelRoot, out string sourcePath)
        {
            modelRoot = Resources.Load<GameObject>(ResourcesKey);
            if (modelRoot == null)
            {
                sourcePath = null;
                return false;
            }

            sourcePath = ModelResourcesPath;
            return true;
        }

        public static bool IsAvailable() => TryLoadModel(out _, out _);
    }
}
