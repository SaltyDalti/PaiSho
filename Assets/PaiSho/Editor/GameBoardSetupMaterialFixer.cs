#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using PaiSho;
using PaiSho.Board;
using PaiSho.Game;

namespace PaiSho.EditorTools
{
    /// <summary>Repairs missing glTF materials on scene-authored GameBoardSetup prefabs.</summary>
    public static class GameBoardSetupMaterialFixer
    {
        private const string GameBoardSetupPrefabPath = "Assets/Prefabs/Game/GameBoardSetup.prefab";

        [InitializeOnLoadMethod]
        private static void HookPrefabStage()
        {
            PrefabStage.prefabStageOpened += stage =>
            {
                if (!IsGameBoardSetupPath(stage.assetPath))
                    return;

                EditorApplication.delayCall += () =>
                {
                    if (HasBrokenMaterials(stage.prefabContentsRoot))
                        FixRoot(stage.prefabContentsRoot, saveAssetPath: stage.assetPath);
                };
            };
        }

        [MenuItem("Pai Sho/Fix Board & Stand Materials")]
        public static void FixFromMenu()
        {
            int fixedRoots = 0;

            PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null && IsGameBoardSetupPath(stage.assetPath))
            {
                if (FixRoot(stage.prefabContentsRoot, stage.assetPath))
                    fixedRoots++;
            }
            else
            {
                foreach (GameObject selected in Selection.gameObjects)
                {
                    GameBoardSetup setup = selected.GetComponentInParent<GameBoardSetup>();
                    if (setup != null && FixRoot(setup.gameObject, null))
                        fixedRoots++;
                }

                if (AssetDatabase.LoadAssetAtPath<GameObject>(GameBoardSetupPrefabPath) != null)
                {
                    GameObject contents = PrefabUtility.LoadPrefabContents(GameBoardSetupPrefabPath);
                    try
                    {
                        if (FixRoot(contents, GameBoardSetupPrefabPath))
                            fixedRoots++;
                    }
                    finally
                    {
                        PrefabUtility.UnloadPrefabContents(contents);
                    }
                }
            }

            if (fixedRoots == 0)
                Debug.LogWarning("No GameBoardSetup found to repair. Open GameBoardSetup.prefab or select it in the hierarchy.");
            else
                Debug.Log($"Repaired materials on {fixedRoots} GameBoardSetup root(s).");
        }

        public static bool FixRoot(GameObject root, string saveAssetPath)
        {
            if (root == null)
                return false;

            BoardLayout layout = root.GetComponent<BoardLayout>();
            bool changed = false;

            Transform boardSurface = root.transform.Find("BoardModelSurface");
            GameObject boardModel = FindBoardModelRoot(boardSurface != null ? boardSurface : root.transform);
            if (boardModel != null)
            {
                changed |= RepairBoardModel(boardModel, layout);
            }

            changed |= RepairStand(root.transform, "PlayerStand");
            changed |= RepairStand(root.transform, "PlayerStandOpponent");

            if (!changed)
                return false;

            EditorUtility.SetDirty(root);

            if (!string.IsNullOrEmpty(saveAssetPath))
            {
                PrefabUtility.SaveAsPrefabAsset(root, saveAssetPath);
                AssetDatabase.SaveAssets();
            }
            else
            {
                MarkOpenScenesDirty();
            }

            return true;
        }

        public static bool HasBrokenMaterials(GameObject root)
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null)
                    continue;

                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material == null)
                        return true;

                    if (material.shader == null)
                        return true;

                    string shaderName = material.shader.name;
                    if (shaderName.Contains("Error") || shaderName.Contains("InternalError"))
                        return true;

                    if (!material.shader.isSupported)
                        return true;
                }
            }

            return false;
        }

        private static bool RepairBoardModel(GameObject boardModel, BoardLayout layout)
        {
            WoodTheme.PreloadVisualMaterials(boardModel);

            if (layout != null)
            {
                WoodTheme.RefreshPrebuiltBoardVisuals(layout, boardModel, applyMaterialTweaks: false);
                return true;
            }

            foreach (string path in new[]
                     {
                         BoardVisualLoader.ModelAssetPath,
                         BoardVisualLoader.ModelResourcesPath,
                         BoardVisualLoader.BlendAssetPath
                     })
            {
                if (string.IsNullOrEmpty(path))
                    continue;

                PieceMaterialUtility.EnsureMaterials(boardModel, path);
                if (HasAnyAssignedMaterial(boardModel))
                    return true;
            }

            return true;
        }

        private static bool HasAnyAssignedMaterial(GameObject root)
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null)
                    continue;

                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material != null)
                        return true;
                }
            }

            return false;
        }

        private static bool RepairStand(Transform root, string standName)
        {
            Transform stand = FindDeepChild(root, standName);
            if (stand == null)
                return false;

            bool SkipHandTray(Transform transform) => GameBoardSetup.IsHandTrayAuthoringHierarchy(transform);

            WoodTheme.PreloadVisualMaterials(stand.gameObject, SkipHandTray);
            WoodTheme.RefreshPrebuiltStandVisuals(stand.gameObject, SkipHandTray);
            return true;
        }

        private static GameObject FindBoardModelRoot(Transform searchRoot)
        {
            Transform direct = searchRoot.Find("BoardModel");
            if (direct != null)
                return direct.gameObject;

            foreach (Transform child in searchRoot.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == "BoardModel" || child.name == "Board")
                    return child.gameObject;
            }

            return null;
        }

        private static Transform FindDeepChild(Transform root, string name)
        {
            if (root.name == name)
                return root;

            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name)
                    return child;
            }

            return null;
        }

        private static bool IsGameBoardSetupPath(string assetPath) =>
            !string.IsNullOrEmpty(assetPath) &&
            assetPath.Replace('\\', '/').EndsWith("GameBoardSetup.prefab");

        private static void MarkOpenScenesDirty()
        {
            PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null)
            {
                EditorSceneManager.MarkSceneDirty(stage.scene);
                return;
            }

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded)
                    EditorSceneManager.MarkSceneDirty(scene);
            }
        }
    }
}
#endif
