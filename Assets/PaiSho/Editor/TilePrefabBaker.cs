#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using PaiSho;
using PaiSho.Pieces;

namespace PaiSho.EditorTools
{
    public static class TilePrefabBaker
    {
        private const float PieceScale = 0.42f;

        private static readonly TileBakeEntry[] Tiles =
        {
            new(PieceType.Jasmine, "Tile_Jasmine"),
            new(PieceType.Rose, "Tile_Rose"),
            new(PieceType.Lily, "Tile_Lily"),
            new(PieceType.Jade, "Tile_Jade"),
            new(PieceType.Chrysanthemum, "Tile_Chrys"),
            new(PieceType.Rhododendron, "Tile_Rhod"),
            new(PieceType.Boat, "Tile_Boat"),
            new(PieceType.Rock, "Tile_Rock"),
            new(PieceType.Knotweed, "Tile_Knot"),
            new(PieceType.Wheel, "Tile_Wheel"),
            new(PieceType.Lotus, "Tile_Lotus"),
            new(PieceType.Orchid, "Tile_Orchid"),
        };

        [InitializeOnLoadMethod]
        private static void RebakeOnLoadIfNeeded()
        {
            EditorApplication.delayCall += () =>
            {
                if (SessionState.GetBool(SessionBakeKey, false))
                    return;

                if (!NeedsRebake())
                    return;

                SessionState.SetBool(SessionBakeKey, true);
                BlenderPathSetup.TrySetupBlender(out _);
                BakeAllFromMenu();
            };
        }

        private static bool NeedsRebake()
        {
            foreach (TileBakeEntry entry in Tiles)
            {
                string prefabPath = $"Assets/Prefabs/Pieces/{entry.PrefabName}.prefab";
                string glbPath = FindGlbSource(entry.PrefabName);
                if (string.IsNullOrEmpty(glbPath))
                    continue;

                if (!File.Exists(prefabPath))
                    return true;

                string text = File.ReadAllText(prefabPath);
                if (text.Contains("PrefabInstance:") && text.Contains("m_SourcePrefab:"))
                    return true;

                if (File.GetLastWriteTimeUtc(glbPath) > File.GetLastWriteTimeUtc(prefabPath))
                    return true;
            }

            return false;
        }

        private const string SessionBakeKey = "PaiSho.TilePrefabBakeAttempted";

        [MenuItem("Pai Sho/Bake All Tile Prefabs (Textured GLB)")]
        public static void BakeAllFromMenu()
        {
            PieceVisualLoader.ClearCache();
            BlenderPathSetup.TrySetupBlender(out _);

            int baked = 0;
            foreach (TileBakeEntry entry in Tiles)
            {
                if (BakeTile(entry))
                    baked++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Baked {baked}/{Tiles.Length} textured tile prefabs.");
        }

        public static void BakeAllFromCommandLine()
        {
            BakeAllFromMenu();
            EditorApplication.Exit(0);
        }

        private static bool BakeTile(TileBakeEntry entry)
        {
            string prefabPath = $"Assets/Prefabs/Pieces/{entry.PrefabName}.prefab";
            RemoveBrokenPrefabVariant(prefabPath);

            string glbPath = FindGlbSource(entry.PrefabName);
            if (string.IsNullOrEmpty(glbPath))
            {
                Debug.LogWarning($"No GLB found for {entry.PrefabName}; run Blender batch export first.");
                return false;
            }

            AssetDatabase.ImportAsset(glbPath, ImportAssetOptions.ForceUpdate);

            if (!PieceVisualLoader.TryLoadVisual(glbPath, out GameObject modelRoot))
            {
                Debug.LogError($"Failed to load GLB for {entry.PrefabName}: {glbPath}");
                return false;
            }

            GameObject instance = Object.Instantiate(modelRoot);
            instance.name = entry.PrefabName;
            instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            instance.transform.localScale = Vector3.one * PieceScale;
            PieceMaterialUtility.EnsureMaterials(instance, glbPath);
            WoodTheme.RecenterPrefabOrigin(instance);
            if (entry.PrefabName == "Tile_Jasmine")
                WoodTheme.ApplyJasmineThicknessCorrection(instance);

            try
            {
                PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
                EmbedRendererMaterialsInPrefab(prefabPath);
                Debug.Log($"Baked {prefabPath} from {glbPath}");
                return true;
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        private static string FindGlbSource(string prefabName)
        {
            string[] candidates =
            {
                $"Assets/Models/Pieces/{prefabName}.glb",
                $"Assets/Resources/PieceVisuals/{prefabName}.glb",
            };

            foreach (string path in candidates)
            {
                if (File.Exists(path))
                    return path;
            }

            return null;
        }

        private static void RemoveBrokenPrefabVariant(string prefabPath)
        {
            if (!File.Exists(prefabPath))
                return;

            string text = File.ReadAllText(prefabPath);
            if (!text.Contains("PrefabInstance:") || !text.Contains("m_SourcePrefab:"))
                return;

            if (PieceVisualLoader.TryLoadVisual(prefabPath, out GameObject existing) &&
                PieceVisualLoader.HasRenderableGeometry(existing))
            {
                return;
            }

            Debug.LogWarning($"Removing broken prefab variant: {prefabPath}");
            AssetDatabase.DeleteAsset(prefabPath);
            PieceVisualLoader.ClearCache();
        }

        private static void EmbedRendererMaterialsInPrefab(string prefabPath)
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                foreach (Renderer renderer in prefabRoot.GetComponentsInChildren<Renderer>(true))
                {
                    Material[] materials = renderer.sharedMaterials;
                    bool changed = false;

                    for (int i = 0; i < materials.Length; i++)
                    {
                        Material material = materials[i];
                        if (material != null && AssetDatabase.Contains(material))
                            continue;

                        Material embedded = material != null
                            ? Object.Instantiate(material)
                            : PieceMaterialUtility.ToUrpMaterial(null);
                        bool isFace = renderer.gameObject.name.IndexOf("face", System.StringComparison.OrdinalIgnoreCase) >= 0;
                        embedded.name = isFace ? "HostBody_Ceramic" : "OpponentBody_Terracotta";
                        if (i > 0)
                            embedded.name += $"_{i}";
                        AssetDatabase.AddObjectToAsset(embedded, prefabPath);
                        materials[i] = embedded;
                        changed = true;
                    }

                    if (changed)
                        renderer.sharedMaterials = materials;
                }

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private readonly struct TileBakeEntry
        {
            public readonly PieceType Type;
            public readonly string PrefabName;

            public TileBakeEntry(PieceType type, string prefabName)
            {
                Type = type;
                PrefabName = prefabName;
            }
        }
    }
}
#endif
