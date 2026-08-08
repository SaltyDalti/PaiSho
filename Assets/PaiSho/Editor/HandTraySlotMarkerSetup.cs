#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using PaiSho;
using PaiSho.Board;
using PaiSho.Game;
using PaiSho.Pieces;

namespace PaiSho.EditorTools
{
    public static class HandTraySlotMarkerSetup
    {

        /// <summary>Fix board materials, create slot anchors, and bake literal piece prefabs on every tray.</summary>
        [MenuItem("Pai Sho/Bake Hand Tray Tile Markers", false, 45)]
        [MenuItem("Pai Sho/Hand Tray/Bake Literal Tile Markers", false, 45)]
        [MenuItem("Pai Sho/Hand Tray/Bake Sample Tiles On Trays", false, 46)]
        public static void BakeHandTrayTileMarkers()
        {
            int trays = AddMarkersInOpenContexts();
            int tiles = RebuildAllSampleTiles();
            GameBoardSetupMaterialFixer.FixFromMenu();

            if (trays == 0 && tiles == 0)
            {
                Debug.LogWarning(
                    "No hand trays found.\n" +
                    "Open Assets/Prefabs/Game/GameBoardSetup.prefab, then run Pai Sho > Bake Hand Tray Tile Markers.");
                return;
            }

            SaveOpenPrefabStage();
            Debug.Log(
                $"Baked {tiles} literal tile marker(s) (Jasmine through Boat). " +
                "Select Slot_0..Slot_6, move with W, save prefab (Ctrl+S).");
        }

        [MenuItem("Pai Sho/Bake All Off-Board Tile Markers", false, 44)]
        public static void BakeAllOffBoardTileMarkers()
        {
            BakeHandTrayTileMarkers();
            CapturePotMarkerSetup.BakeStackMarkers();
        }

        [MenuItem("Pai Sho/Bake All Off-Board Tile Markers", true)]
        private static bool BakeAllOffBoardTileMarkersValidate() => !Application.isPlaying;

        [MenuItem("Pai Sho/Bake Hand Tray Tile Markers", true)]
        private static bool BakeHandTrayTileMarkersValidate() => !Application.isPlaying;

        [MenuItem("Pai Sho/Hand Tray/Add Slot Markers To Selection")]
        public static void AddMarkersToSelection()
        {
            GameBoardSetupMaterialFixer.FixFromMenu();

            int count = 0;

            foreach (Object selected in Selection.objects)
            {
                GameObject gameObject = selected as GameObject;
                if (gameObject == null)
                    continue;

                count += AddMarkersUnder(gameObject.transform);
            }

            count += TryAddMarkersToSelectedPrefabAssets();

            if (count == 0)
            {
                Debug.LogWarning(
                    "No hand trays found from the current selection.\n" +
                    "Try: open Assets/Prefabs/Game/GameBoardSetup.prefab, select the root, run again — " +
                    "or use Pai Sho > Bake Hand Tray Tile Markers.");
                return;
            }

            RebuildAllSampleTiles();
            Debug.Log($"Hand tray slot markers updated on {count} tray(s). Save the prefab if prompted.");
            SaveOpenPrefabStage();
        }

        [MenuItem("Pai Sho/Hand Tray/Add Slot Markers In Scene")]
        public static void AddMarkersInScene()
        {
            BakeHandTrayTileMarkers();
        }

        private static void SaveOpenPrefabStage()
        {
            PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage == null)
                return;

            PrefabUtility.SaveAsPrefabAsset(stage.prefabContentsRoot, stage.assetPath);
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Pai Sho/Hand Tray/Refresh Slot Marker Visuals")]
        public static void RefreshAllMarkerVisuals()
        {
            int count = RebuildAllSampleTiles();

            foreach (HandTraySlotMarker marker in FindAllMarkers())
            {
                marker.RefreshVisibility();
                count++;
            }

            DestroyAllLegacyMarkerMeshes();
            SaveOpenPrefabStage();

            Debug.Log(count > 0
                ? $"Refreshed {count} literal tile marker(s). Save the prefab (Ctrl+S)."
                : "No HandTraySlotMarker components found. Run Bake Literal Tile Markers first.");
        }

        [MenuItem("Pai Sho/Hand Tray/Clean Legacy Marker Meshes")]
        public static void RemovePinkMarkerMeshes()
        {
            int removed = DestroyAllLegacyMarkerMeshes();
            Debug.Log(removed > 0
                ? $"Removed {removed} legacy slot marker mesh object(s). Slots now show as colored gizmo tiles only."
                : "No legacy SlotMarkerVisual meshes found.");
        }

        [MenuItem("Pai Sho/Hand Tray/Adopt Sample Tile Pose To Slot", false, 47)]
        public static void AdoptSampleTilePoseFromSelection()
        {
            int count = 0;

            foreach (Object selected in Selection.objects)
            {
                if (selected is not GameObject gameObject)
                    continue;

                if (!TryResolveSlotAndSample(gameObject.transform, out Transform slot, out Transform sample))
                    continue;

                if (AdoptSampleTilePoseToSlot(slot, sample))
                    count++;
            }

            if (count == 0)
            {
                Debug.LogWarning(
                    "Select Slot_N or its SampleTile child, then run again.\n" +
                    "This moves the slot to the sample tile and resets the sample tile to local origin.");
                return;
            }

            MarkScenesDirty();
            SaveOpenPrefabStage();
            Debug.Log($"Moved {count} slot(s) to their sample tile pose. Sample tiles reset to local origin. Save prefab (Ctrl+S).");
        }

        [MenuItem("Pai Sho/Hand Tray/Adopt Sample Tile Pose To Slot", true)]
        private static bool AdoptSampleTilePoseFromSelectionValidate()
        {
            if (Application.isPlaying)
                return false;

            foreach (Object selected in Selection.objects)
            {
                if (selected is GameObject gameObject &&
                    TryResolveSlotAndSample(gameObject.transform, out _, out _))
                {
                    return true;
                }
            }

            return false;
        }

        [MenuItem("Pai Sho/Hand Tray/Adopt All Sample Tiles To Slots", false, 48)]
        public static void AdoptAllSampleTilePoses()
        {
            int count = 0;

            foreach (HandTraySlotMarker marker in FindAllMarkers())
            {
                Transform sample = marker.transform.Find(HandTraySlotMarker.SampleTileName);
                if (sample == null)
                    continue;

                if (AdoptSampleTilePoseToSlot(marker.transform, sample))
                    count++;
            }

            if (count == 0)
            {
                Debug.LogWarning("No sample tiles found. Run Bake Hand Tray Tile Markers first.");
                return;
            }

            MarkScenesDirty();
            SaveOpenPrefabStage();
            Debug.Log($"Moved {count} slot(s) to their sample tile poses. Save prefab (Ctrl+S).");
        }

        [MenuItem("Pai Sho/Hand Tray/Adopt All Sample Tiles To Slots", true)]
        private static bool AdoptAllSampleTilePosesValidate() => !Application.isPlaying;

        private static bool TryResolveSlotAndSample(Transform transform, out Transform slot, out Transform sample)
        {
            slot = null;
            sample = null;
            if (transform == null)
                return false;

            if (transform.name == HandTraySlotMarker.SampleTileName && transform.parent != null)
            {
                sample = transform;
                slot = transform.parent;
                return slot.GetComponent<HandTraySlotMarker>() != null;
            }

            if (transform.TryGetComponent(out HandTraySlotMarker _))
            {
                slot = transform;
                sample = slot.Find(HandTraySlotMarker.SampleTileName);
                return sample != null;
            }

            return false;
        }

        private static bool AdoptSampleTilePoseToSlot(Transform slot, Transform sample)
        {
            if (slot == null || sample == null)
                return false;

            if (sample.localPosition.sqrMagnitude < 0.00000001f &&
                sample.localRotation == Quaternion.identity)
            {
                return false;
            }

            Undo.RecordObject(slot, "Adopt Sample Tile Pose");
            Undo.RecordObject(sample, "Adopt Sample Tile Pose");

            slot.SetPositionAndRotation(sample.position, sample.rotation);
            sample.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

            EditorUtility.SetDirty(slot.gameObject);
            EditorUtility.SetDirty(sample.gameObject);
            return true;
        }

        private static int DestroyAllLegacyMarkerMeshes()
        {
            int removed = 0;
            foreach (Transform transform in FindAllTransforms())
            {
                if (transform.name != "SlotMarkerVisual" && transform.name != "SlotLabel")
                    continue;

                Undo.DestroyObjectImmediate(transform.gameObject);
                removed++;
            }

            if (removed > 0)
                MarkScenesDirty();

            return removed;
        }

        private static void DestroyMarkerVisualChildren(Transform slotTransform)
        {
            Transform visual = slotTransform.Find("SlotMarkerVisual");
            if (visual != null)
                Undo.DestroyObjectImmediate(visual.gameObject);

            Transform label = slotTransform.Find("SlotLabel");
            if (label != null)
                Undo.DestroyObjectImmediate(label.gameObject);
        }

        private static int AddMarkersInOpenContexts()
        {
            int count = 0;
            var seen = new HashSet<int>();

            PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null)
                count += CollectTrayMarkers(stage.prefabContentsRoot.transform, seen);

            foreach (GameBoardSetup setup in FindAllSetups())
                count += CollectTrayMarkersFromSetup(setup, seen);

            foreach ((Transform tray, Player owner) in FindTrayRootsByName())
            {
                if (tray == null || !seen.Add(tray.GetInstanceID()))
                    continue;

                count += EnsureTrayMarkers(tray, owner);
            }

            if (count > 0)
            {
                MarkScenesDirty();
                SaveOpenPrefabStage();
            }

            return count;
        }

        private static int RebuildAllSampleTiles()
        {
            int count = 0;
            foreach (HandTraySlotMarker marker in FindAllMarkers())
            {
                EnsureSampleTile(
                    marker.transform,
                    marker.SlotIndex,
                    ResolveCellSpacing(marker.transform),
                    rebuild: true);
                marker.RefreshVisibility();
                count++;
            }

            return count;
        }

        private static int TryAddMarkersToSelectedPrefabAssets()
        {
            int count = 0;

            foreach (Object obj in Selection.objects)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                if (string.IsNullOrEmpty(path) || !path.EndsWith(".prefab", System.StringComparison.OrdinalIgnoreCase))
                    continue;

                GameObject contents = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    int added = AddMarkersUnder(contents.transform);
                    if (added <= 0)
                        continue;

                    PrefabUtility.SaveAsPrefabAsset(contents, path);
                    count += added;
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(contents);
                }
            }

            return count;
        }

        private static int AddMarkersUnder(Transform root)
        {
            if (root == null)
                return 0;

            var seen = new HashSet<int>();
            int count = CollectTrayMarkers(root, seen);

            if (count > 0)
            {
                MarkScenesDirty();
                SaveOpenPrefabStage();
            }

            return count;
        }

        private static int CollectTrayMarkers(Transform root, HashSet<int> seen)
        {
            int count = 0;

            Transform trayAncestor = FindTrayRootAncestor(root);
            if (trayAncestor != null && seen.Add(trayAncestor.GetInstanceID()))
                count += EnsureTrayMarkers(trayAncestor, ResolveOwner(trayAncestor));

            GameBoardSetup setup = root.GetComponent<GameBoardSetup>();
            if (setup == null)
                setup = root.GetComponentInChildren<GameBoardSetup>(true);
            if (setup == null)
                setup = root.GetComponentInParent<GameBoardSetup>();

            if (setup != null)
                count += CollectTrayMarkersFromSetup(setup, seen);

            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (!IsTrayRoot(child) || !seen.Add(child.GetInstanceID()))
                    continue;

                count += EnsureTrayMarkers(child, ResolveOwner(child));
            }

            return count;
        }

        private static int CollectTrayMarkersFromSetup(GameBoardSetup setup, HashSet<int> seen)
        {
            if (setup == null)
                return 0;

            setup.DiscoverTrayReferences();
            int count = 0;

            if (setup.HostTrayRoot != null && seen.Add(setup.HostTrayRoot.GetInstanceID()))
                count += EnsureTrayMarkers(setup.HostTrayRoot, Player.Host);

            if (setup.OpponentTrayRoot != null && seen.Add(setup.OpponentTrayRoot.GetInstanceID()))
                count += EnsureTrayMarkers(setup.OpponentTrayRoot, Player.Opponent);

            return count;
        }

        private static IEnumerable<(Transform tray, Player owner)> FindTrayRootsByName()
        {
            foreach (Transform transform in FindAllTransforms())
            {
                if (!IsTrayRoot(transform))
                    continue;

                yield return (transform, ResolveOwner(transform));
            }
        }

        private static IEnumerable<GameBoardSetup> FindAllSetups()
        {
            return Object.FindObjectsByType<GameBoardSetup>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        }

        private static IEnumerable<HandTraySlotMarker> FindAllMarkers()
        {
            return Object.FindObjectsByType<HandTraySlotMarker>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        }

        private static IEnumerable<Transform> FindAllTransforms()
        {
            PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null)
            {
                foreach (Transform transform in stage.prefabContentsRoot.GetComponentsInChildren<Transform>(true))
                    yield return transform;

                yield break;
            }

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded)
                    continue;

                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
                        yield return transform;
                }
            }
        }

        private static Transform FindTrayRootAncestor(Transform transform)
        {
            Transform current = transform;
            while (current != null)
            {
                if (IsTrayRoot(current))
                    return current;

                current = current.parent;
            }

            return null;
        }

        private static int EnsureTrayMarkers(Transform trayRoot, Player owner)
        {
            Undo.RegisterFullObjectHierarchyUndo(trayRoot.gameObject, "Add Hand Tray Slot Markers");

            for (int i = 0; i < HandTrayAlignmentDefaults.MaxSlots; i++)
            {
                string slotName = $"{GameBoardSetup.SlotNamePrefix}{i}";
                Transform slot = trayRoot.Find(slotName);
                if (slot == null)
                {
                    var slotObject = new GameObject(slotName);
                    slot = slotObject.transform;
                    slot.SetParent(trayRoot, false);
                    slot.localPosition = HandTrayAlignmentDefaults.GetSlotPosition(i, owner);
                    slot.localRotation = Quaternion.Euler(HandTrayAlignmentDefaults.GetSlotEuler(i, owner));
                }

                HandTraySlotMarker marker = slot.GetComponent<HandTraySlotMarker>();
                if (marker == null)
                    marker = Undo.AddComponent<HandTraySlotMarker>(slot.gameObject);

                marker.Configure(i, owner);
                DestroyLegacyMarkerVisuals(slot);
                EnsureSampleTile(slot, i, ResolveCellSpacing(slot), rebuild: false);
            }

            EditorUtility.SetDirty(trayRoot);
            return 1;
        }

        private static float ResolveCellSpacing(Transform slot)
        {
            var layout = slot.GetComponentInParent<BoardLayout>();
            if (layout != null)
                return layout.CellSpacing;

            GameBoardSetup setup = slot.GetComponentInParent<GameBoardSetup>();
            if (setup != null)
            {
                layout = setup.GetComponent<BoardLayout>();
                if (layout != null)
                    return layout.CellSpacing;
            }

            return 0.42f;
        }

        private static void EnsureSampleTile(
            Transform slot,
            int slotIndex,
            float cellSpacing,
            bool rebuild)
        {
            PieceType pieceType = HandTraySlotSampleTiles.GetPieceTypeForSlot(slotIndex);
            Transform existing = slot.Find(HandTraySlotMarker.SampleTileName);
            if (existing != null)
            {
                if (rebuild || !SampleTileMatches(existing.gameObject, pieceType))
                {
                    Undo.DestroyObjectImmediate(existing.gameObject);
                }
                else
                {
                    RefreshSampleTile(existing.gameObject, pieceType, cellSpacing);
                    EnsureSampleTileComponent(existing.gameObject);
                    return;
                }
            }

            string prefabPath = HandTraySlotSampleTiles.GetPrefabAssetPath(pieceType);
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (source == null)
            {
                Debug.LogWarning($"Missing piece prefab at {prefabPath}");
                return;
            }

            var instance = PrefabUtility.InstantiatePrefab(source, slot) as GameObject;
            if (instance == null)
                return;

            Undo.RegisterCreatedObjectUndo(instance, "Add Hand Tray Tile Marker");
            instance.name = HandTraySlotMarker.SampleTileName;
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            RefreshSampleTile(instance, pieceType, cellSpacing);
            StripColliders(instance);
            StripRuntimeComponents(instance);
            EnsureSampleTileComponent(instance);
        }

        private static bool SampleTileMatches(GameObject sampleTile, PieceType expectedType)
        {
            var marker = sampleTile.GetComponent<HandTraySlotSampleTile>();
            return marker != null && marker.PieceType == expectedType;
        }

        private static void RefreshSampleTile(GameObject instance, PieceType pieceType, float cellSpacing)
        {
            if (PrefabUtility.IsPartOfPrefabInstance(instance))
                PrefabUtility.RevertPrefabInstance(instance, InteractionMode.AutomatedAction);

            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            string prefabPath = HandTraySlotSampleTiles.GetPrefabAssetPath(pieceType);
            PieceMaterialUtility.EnsureMaterials(instance, prefabPath, liftFlowerFaces: false);

            if (pieceType == PieceType.Jasmine)
                WoodTheme.ApplyJasmineThicknessCorrection(instance);

            WoodTheme.FitPrefabToCellSpacing(instance, cellSpacing, alignBottomToSurface: false);
        }

        private static void StripRuntimeComponents(GameObject root)
        {
            foreach (PaiSho.Pieces.Piece piece in root.GetComponentsInChildren<PaiSho.Pieces.Piece>(true))
            {
                if (piece != null)
                    Undo.DestroyObjectImmediate(piece);
            }
        }

        private static void EnsureSampleTileComponent(GameObject sampleTile)
        {
            HandTraySlotSampleTile marker = sampleTile.GetComponent<HandTraySlotSampleTile>();
            if (marker == null)
                marker = sampleTile.AddComponent<HandTraySlotSampleTile>();

            Transform slot = sampleTile.transform.parent;
            int slotIndex = 0;
            if (slot != null && slot.TryGetComponent(out HandTraySlotMarker slotMarker))
                slotIndex = slotMarker.SlotIndex;

            marker.SetPieceType(HandTraySlotSampleTiles.GetPieceTypeForSlot(slotIndex));
        }

        private static void StripColliders(GameObject root)
        {
            foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
            {
                if (collider != null)
                    Undo.DestroyObjectImmediate(collider);
            }
        }

        private static void DestroyLegacyMarkerVisuals(Transform slotTransform)
        {
            DestroyMarkerVisualChildren(slotTransform);
        }

        private static void MarkScenesDirty()
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

        [MenuItem("Pai Sho/Hand Tray/Sync Scene Instance From Prefab", false, 49)]
        public static void SyncSceneInstanceFromPrefab()
        {
            if (PrefabStageUtility.GetCurrentPrefabStage() != null)
            {
                Debug.LogWarning(
                    "Open the gameplay scene (GamePlay) first — not GameBoardSetup prefab isolation mode — then run Sync again.");
                return;
            }

            var prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(GameBoardSetup.PrefabAssetPath);
            GameBoardSetup template = prefabRoot != null ? prefabRoot.GetComponent<GameBoardSetup>() : null;
            if (template == null)
            {
                Debug.LogWarning($"Missing prefab at {GameBoardSetup.PrefabAssetPath}.");
                return;
            }

            int synced = 0;
            int checkedSetups = 0;
            foreach (GameBoardSetup setup in Object.FindObjectsByType<GameBoardSetup>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (setup == null || !setup.gameObject.scene.IsValid() || PrefabUtility.IsPartOfPrefabAsset(setup.gameObject))
                    continue;

                checkedSetups++;
                setup.DiscoverTrayReferences();
                if (setup.HasHandTrayRoots() &&
                    GameBoardSetup.HasBakedSlotMarkers(setup.HostTrayRoot) &&
                    GameBoardSetup.HasBakedSlotMarkers(setup.OpponentTrayRoot))
                    continue;

                Undo.RegisterFullObjectHierarchyUndo(setup.gameObject, "Sync Hand Trays From Prefab");
                if (setup.EnsureHandTrayRoots(template))
                {
                    EditorUtility.SetDirty(setup);
                    synced++;
                }
            }

            if (checkedSetups == 0)
            {
                Debug.LogWarning(
                    "No GameBoardSetup in the active scene. Open Assets/Scenes/GamePlay.unity, then run Sync again.");
                return;
            }

            if (synced == 0)
            {
                Debug.Log("Hand trays already present on scene GameBoardSetup — no sync needed.");
                return;
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log($"Synced hand trays on {synced} GameBoardSetup instance(s). Save the scene (Ctrl+S).");
        }

        [MenuItem("Pai Sho/Hand Tray/Sync Scene Instance From Prefab", true)]
        private static bool ValidateSyncSceneInstanceFromPrefab() => !Application.isPlaying;

        private static bool IsTrayRoot(Transform transform) =>
            transform != null &&
            (transform.name == GameBoardSetup.HostTrayName ||
             transform.name == GameBoardSetup.OpponentTrayName);

        private static Player ResolveOwner(Transform trayRoot) =>
            trayRoot.name == GameBoardSetup.OpponentTrayName ? Player.Opponent : Player.Host;
    }
}
#endif
