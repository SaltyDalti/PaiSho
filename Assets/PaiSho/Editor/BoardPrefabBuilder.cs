#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace PaiSho.EditorTools
{
    /// <summary>
    /// Builds a standalone Board prefab from the baked GLB so materials/textures survive.
    /// Do not use a Prefab Variant of the GLB — glTFast does not support that pattern.
    /// </summary>
    public static class BoardPrefabBuilder
    {
        private const string GlbPath = "Assets/Models/Board/Board.glb";
        private const string PrefabPath = "Assets/Prefabs/Board/Board.prefab";

        [MenuItem("Pai Sho/Rebuild Board Prefab From GLB")]
        public static void RebuildFromMenu()
        {
            if (Rebuild(out string message))
                Debug.Log(message);
            else
                Debug.LogError(message);
        }

        [InitializeOnLoadMethod]
        private static void AutoRebuildIfBroken()
        {
            EditorApplication.delayCall += () =>
            {
                if (!IsBoardPrefabBroken())
                    return;

                if (Rebuild(out string message))
                    Debug.Log(message);
                else
                    Debug.LogWarning(message);
            };
        }

        public static bool Rebuild(out string message)
        {
            GameObject glb = AssetDatabase.LoadAssetAtPath<GameObject>(GlbPath);
            if (glb == null)
            {
                message = $"Missing board GLB at {GlbPath}. Export Board.glb first.";
                return false;
            }

            // Instantiate and fully unpack so the saved prefab is standalone
            // (not a broken variant of the glTFast asset).
            GameObject instance = PrefabUtility.InstantiatePrefab(glb) as GameObject;
            if (instance == null)
                instance = Object.Instantiate(glb);

            if (instance == null)
            {
                message = $"Could not instantiate {GlbPath}";
                return false;
            }

            try
            {
                instance.name = "Board";

                if (PrefabUtility.IsPartOfPrefabInstance(instance))
                {
                    PrefabUtility.UnpackPrefabInstance(
                        instance,
                        PrefabUnpackMode.Completely,
                        InteractionMode.AutomatedAction);
                }

                EnsureMaterialsPresent(instance);

                string directory = System.IO.Path.GetDirectoryName(PrefabPath);
                if (!string.IsNullOrEmpty(directory) && !AssetDatabase.IsValidFolder(directory))
                    System.IO.Directory.CreateDirectory(directory);

                PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath);
                AssetDatabase.ImportAsset(PrefabPath, ImportAssetOptions.ForceUpdate);

                message =
                    $"Rebuilt {PrefabPath} from {GlbPath} with materials. " +
                    "Drag this prefab into the scene (not Board.blend).";
                return true;
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        private static bool IsBoardPrefabBroken()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
                return true;

            // Broken variant / missing materials.
            if (prefab.name.Contains("Missing") || prefab.name.Contains("Broken"))
                return true;

            var renderers = prefab.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                return true;

            foreach (Renderer renderer in renderers)
            {
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material != null)
                        return false;
                }
            }

            return true;
        }

        private static void EnsureMaterialsPresent(GameObject root)
        {
            int missing = 0;
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] == null)
                        missing++;
                }
            }

            if (missing > 0)
                Debug.LogWarning($"Board GLB instance has {missing} missing material slots after instantiate.");
        }
    }
}
#endif
