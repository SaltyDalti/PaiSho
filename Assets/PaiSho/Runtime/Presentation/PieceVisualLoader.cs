using System.Collections.Generic;
using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PaiSho
{
    public static class PieceVisualLoader
    {
        private static readonly Dictionary<string, GameObject> BuiltModelCache = new();

        public static bool TryLoadVisual(string assetPath, out GameObject visualRoot)
        {
            visualRoot = null;
            if (string.IsNullOrEmpty(assetPath))
                return false;

            if (BuiltModelCache.TryGetValue(assetPath, out GameObject cached) && HasRenderableGeometry(cached))
            {
                visualRoot = cached;
                return true;
            }

#if UNITY_EDITOR
            if (TryLoadFromAssetDatabase(assetPath, out visualRoot))
            {
                if (IsEphemeralVisual(visualRoot))
                    BuiltModelCache[assetPath] = visualRoot;
                return true;
            }
#endif

            if (TryLoadFromResources(assetPath, out visualRoot))
            {
                if (IsEphemeralVisual(visualRoot))
                    BuiltModelCache[assetPath] = visualRoot;
                return true;
            }

            return false;
        }

#if UNITY_EDITOR
        private static bool TryLoadFromAssetDatabase(string assetPath, out GameObject visualRoot)
        {
            visualRoot = null;
            if (!File.Exists(assetPath))
                return false;

            if (ShouldForceImport(assetPath))
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

            if (IsBrokenPrefabVariant(assetPath))
                return false;

            var main = AssetDatabase.LoadMainAssetAtPath(assetPath) as GameObject;
            if (HasRenderableGeometry(main))
            {
                visualRoot = main;
                return true;
            }

            var direct = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (HasRenderableGeometry(direct))
            {
                visualRoot = direct;
                return true;
            }

            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            GameObject best = FindBestGameObject(assets);
            if (best != null)
            {
                visualRoot = best;
                return true;
            }

            if (TryBuildFromModelAssets(assetPath, assets, out visualRoot))
                return true;

            return false;
        }
#endif

        private static bool ShouldForceImport(string assetPath)
        {
            string ext = Path.GetExtension(assetPath).ToLowerInvariant();
            // glTF is imported by com.unity.cloud.gltfast; forcing ModelImporter causes errors.
            return ext is ".fbx" or ".obj";
        }

#if UNITY_EDITOR
        private static bool IsBrokenPrefabVariant(string assetPath)
        {
            if (!assetPath.EndsWith(".prefab", System.StringComparison.OrdinalIgnoreCase))
                return false;

            string text = File.ReadAllText(assetPath);
            if (!text.Contains("PrefabInstance:") || !text.Contains("m_SourcePrefab:"))
                return false;

            var loaded = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (loaded != null && HasRenderableGeometry(loaded))
                return false;

            if (!TryGetVariantSourceGuid(text, out string sourceGuid))
                return false;

            string sourcePath = AssetDatabase.GUIDToAssetPath(sourceGuid);
            if (string.IsNullOrEmpty(sourcePath))
                return true;

            string ext = Path.GetExtension(sourcePath).ToLowerInvariant();
            return ext is ".glb" or ".gltf";
        }

        private static bool TryGetVariantSourceGuid(string prefabText, out string guid)
        {
            guid = null;
            int markerIndex = prefabText.IndexOf("m_SourcePrefab:", System.StringComparison.Ordinal);
            if (markerIndex < 0)
                return false;

            int guidIndex = prefabText.IndexOf("guid:", markerIndex, System.StringComparison.Ordinal);
            if (guidIndex < 0)
                return false;

            int start = guidIndex + "guid:".Length;
            while (start < prefabText.Length && char.IsWhiteSpace(prefabText[start]))
                start++;

            int end = start;
            while (end < prefabText.Length && prefabText[end] != ',' && prefabText[end] != '}')
                end++;

            guid = prefabText.Substring(start, end - start).Trim();
            return guid.Length == 32;
        }
#endif

        private static bool TryLoadFromResources(string assetPath, out GameObject visualRoot)
        {
            visualRoot = null;

            string resourceKey = ToResourcesKey(assetPath);
            if (string.IsNullOrEmpty(resourceKey))
                return false;

            Object[] assets = Resources.LoadAll(resourceKey);
            GameObject best = FindBestGameObject(assets);
            if (best != null)
            {
                visualRoot = best;
                return true;
            }

            return TryBuildFromModelAssets(assetPath, assets, out visualRoot);
        }

        private static GameObject FindBestGameObject(Object[] assets)
        {
            GameObject best = null;
            int bestRendererCount = 0;

            if (assets == null)
                return null;

            foreach (Object asset in assets)
            {
                if (asset is not GameObject candidate)
                    continue;

                int rendererCount = candidate.GetComponentsInChildren<Renderer>(true).Length;
                if (rendererCount > bestRendererCount)
                {
                    best = candidate;
                    bestRendererCount = rendererCount;
                }
            }

            return best != null && bestRendererCount > 0 ? best : null;
        }

        private static bool TryBuildFromModelAssets(string assetPath, Object[] assets, out GameObject visualRoot)
        {
            visualRoot = null;
            if (assets == null || assets.Length == 0)
                return false;

            var meshes = new List<Mesh>();
            var materials = new List<Material>();

            foreach (Object asset in assets)
            {
                if (asset is Mesh mesh)
                    meshes.Add(mesh);
                else if (asset is Material material)
                    materials.Add(material);
            }

            if (meshes.Count == 0)
                return false;

            string rootName = Path.GetFileNameWithoutExtension(assetPath);
            var root = new GameObject(rootName);
            root.hideFlags = HideFlags.HideAndDontSave;

            for (int i = 0; i < meshes.Count; i++)
            {
                Mesh mesh = meshes[i];
                if (mesh == null)
                    continue;

                var part = new GameObject(string.IsNullOrEmpty(mesh.name) ? $"Mesh_{i}" : mesh.name);
                part.transform.SetParent(root.transform, false);

                var filter = part.AddComponent<MeshFilter>();
                filter.sharedMesh = mesh;

                var renderer = part.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = ResolveMaterial(materials, i);
            }

            if (!HasRenderableGeometry(root))
            {
                Object.DestroyImmediate(root);
                return false;
            }

            visualRoot = root;
            return true;
        }

        private static Material ResolveMaterial(List<Material> materials, int meshIndex)
        {
            if (materials.Count == 0)
                return CreateFallbackMaterial();

            if (meshIndex < materials.Count)
                return materials[meshIndex];

            return materials[0];
        }

        private static Material CreateFallbackMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Universal Render Pipeline/Simple Lit")
                         ?? Shader.Find("Standard");

            var material = new Material(shader);
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", new Color(0.82f, 0.72f, 0.55f));
            else
                material.color = new Color(0.82f, 0.72f, 0.55f);

            return material;
        }

        private static string ToResourcesKey(string assetPath)
        {
            const string resourcesPrefix = "Assets/Resources/";
            if (!assetPath.StartsWith(resourcesPrefix))
                return null;

            string key = assetPath.Substring(resourcesPrefix.Length);
            int extension = key.LastIndexOf('.');
            if (extension > 0)
                key = key.Substring(0, extension);

            return key;
        }

        public static bool HasRenderableGeometry(GameObject obj)
        {
            return obj != null && obj.GetComponentsInChildren<Renderer>(true).Length > 0;
        }

        private static bool IsEphemeralVisual(GameObject go)
        {
            if (go == null)
                return false;

#if UNITY_EDITOR
            return !EditorUtility.IsPersistent(go);
#else
            return (go.hideFlags & HideFlags.HideAndDontSave) != 0;
#endif
        }

        public static void ClearCache()
        {
            foreach (GameObject built in BuiltModelCache.Values)
            {
                if (built != null && IsEphemeralVisual(built))
                    Object.DestroyImmediate(built);
            }

            BuiltModelCache.Clear();
        }

        public static void ClearCacheEntry(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return;

            if (BuiltModelCache.TryGetValue(assetPath, out GameObject built))
            {
                if (built != null && IsEphemeralVisual(built))
                    Object.DestroyImmediate(built);
                BuiltModelCache.Remove(assetPath);
            }
        }
    }
}
