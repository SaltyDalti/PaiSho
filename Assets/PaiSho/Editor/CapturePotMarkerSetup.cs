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
    public static class CapturePotMarkerSetup
    {
        [MenuItem("Pai Sho/Capture Pot/Bake Stack Markers", false, 50)]
        [MenuItem("Pai Sho/Capture Pot/Bake All Capture Positions", false, 51)]
        public static void BakeStackMarkers()
        {
            GameBoardSetupMaterialFixer.FixFromMenu();

            int pots = 0;
            int slots = 0;

            foreach (GameBoardSetup setup in FindAllSetups())
            {
                setup.DiscoverCapturePotReferences();
                var layout = setup.GetComponent<BoardLayout>();
                if (layout == null)
                    continue;

                if (setup.HostCapturePotRoot != null)
                {
                    pots++;
                    slots += EnsurePotStackMarkers(setup.HostCapturePotRoot, layout, Player.Host);
                }

                if (setup.OpponentCapturePotRoot != null)
                {
                    pots++;
                    slots += EnsurePotStackMarkers(setup.OpponentCapturePotRoot, layout, Player.Opponent);
                }
            }

            pots += BakeOpenPrefabStage(ref slots);
            RefreshAllCaptureSampleTiles();

            if (pots == 0)
            {
                Debug.LogWarning(
                    "No capture pots found.\n" +
                    "Open Assets/Prefabs/Game/GameBoardSetup.prefab, then run Pai Sho > Capture Pot > Bake Stack Markers.");
                return;
            }

            SaveOpenPrefabStage();
            Debug.Log(
                $"Baked {slots} capture stack marker(s) with sample tiles on {pots} pot(s). " +
                "Layout: 01_Flowers/Slot_0..5 and 02_SpecialAndOther/Slot_0..5. Save prefab (Ctrl+S).");
        }

        [MenuItem("Pai Sho/Capture Pot/Bake Stack Markers", true)]
        private static bool ValidateBake() => !Application.isPlaying;

        [MenuItem("Pai Sho/Capture Pot/Migrate Legacy Folders To Display Slots (Keep Positions)", false, 52)]
        public static void MigrateLegacyFoldersToDisplaySlots()
        {
            int pots = 0;
            int lanes = 0;

            foreach (GameBoardSetup setup in FindAllSetups())
            {
                setup.DiscoverCapturePotReferences();
                var layout = setup.GetComponent<BoardLayout>();
                if (layout == null)
                    continue;

                if (setup.HostCapturePotRoot != null &&
                    CapturePotLegacyMigration.HasLegacyHierarchy(setup.HostCapturePotRoot))
                {
                    lanes += CapturePotLegacyMigration.MigratePot(setup.HostCapturePotRoot, Player.Host);
                    pots++;
                }

                if (setup.OpponentCapturePotRoot != null &&
                    CapturePotLegacyMigration.HasLegacyHierarchy(setup.OpponentCapturePotRoot))
                {
                    lanes += CapturePotLegacyMigration.MigratePot(setup.OpponentCapturePotRoot, Player.Opponent);
                    pots++;
                }

                EditorUtility.SetDirty(setup);
            }

            RefreshAllCaptureSampleTiles();
            SaveOpenPrefabStage();

            if (pots == 0)
            {
                Debug.LogWarning(
                    "No capture pots found, or they are already using 01_Flowers / 02_SpecialAndOther.\n" +
                    "Open GameBoardSetup.prefab and run again if you still have 01_WhiteFlowers-style folders.");
                return;
            }

            Debug.Log(
                $"Migrated {lanes} type lane(s) on {pots} pot(s). Positions preserved — save prefab (Ctrl+S). " +
                "Do not run Bake Stack Markers unless you need missing stacks only.");
        }

        [MenuItem("Pai Sho/Capture Pot/Migrate Legacy Folders To Display Slots (Keep Positions)", true)]
        private static bool ValidateMigrateLegacy() => !Application.isPlaying;

        [MenuItem("Pai Sho/Capture Pot/Add Pot Roots Only", false, 55)]
        public static void AddPotRootsOnly()
        {
            int count = 0;
            foreach (GameBoardSetup setup in FindAllSetups())
            {
                var layout = setup.GetComponent<BoardLayout>();
                if (layout == null)
                    continue;

                count += EnsurePotRoot(setup.transform, GameBoardSetup.HostCapturePotName, layout, Player.Host) ? 1 : 0;
                count += EnsurePotRoot(setup.transform, GameBoardSetup.OpponentCapturePotName, layout, Player.Opponent) ? 1 : 0;
                EditorUtility.SetDirty(setup);
            }

            if (count == 0)
            {
                Debug.LogWarning("No board setup found.");
                return;
            }

            SaveOpenPrefabStage();
            Debug.Log($"Added/updated {count} capture pot root(s). Run Bake Stack Markers for full layout.");
        }

        [MenuItem("Pai Sho/Capture Pot/Sync Scene Instance From Prefab", false, 54)]
        public static void SyncSceneInstanceFromPrefab()
        {
            if (PrefabStageUtility.GetCurrentPrefabStage() != null)
            {
                Debug.LogWarning(
                    "Open the gameplay scene (GamePlay) first — not GameBoardSetup prefab isolation mode — then run Sync again.");
                return;
            }

            GameBoardSetup template = LoadGameBoardSetupPrefabTemplate();
            if (template == null)
            {
                Debug.LogWarning($"Missing prefab at {GameBoardSetup.PrefabAssetPath}.");
                return;
            }

            int synced = 0;
            int checkedSetups = 0;
            foreach (GameBoardSetup setup in FindSceneBoardSetups())
            {
                checkedSetups++;
                if (!NeedsCapturePotSync(setup))
                    continue;

                if (SyncSetupCapturePots(setup, template))
                    synced++;
            }

            if (checkedSetups == 0)
            {
                Debug.LogWarning(
                    "No GameBoardSetup in the active scene. Open Assets/Scenes/GamePlay.unity, then run Sync again.");
                return;
            }

            if (synced == 0)
            {
                bool stillMissing = false;
                foreach (GameBoardSetup setup in FindSceneBoardSetups())
                {
                    if (!NeedsCapturePotSync(setup))
                        continue;

                    stillMissing = true;
                    break;
                }

                if (stillMissing)
                {
                    Debug.LogWarning(
                        "Could not sync capture pots onto scene GameBoardSetup. " +
                        "Select the instance, use Overrides > Revert All, or open GameBoardSetup.prefab and verify CapturePotHost exists.");
                }
                else
                {
                    Debug.Log("Capture pots already present on scene GameBoardSetup — no sync needed.");
                }

                return;
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log($"Synced capture pots on {synced} GameBoardSetup instance(s). Save the scene (Ctrl+S).");
        }

        [MenuItem("Pai Sho/Capture Pot/Sync Scene Instance From Prefab", true)]
        private static bool ValidateSyncSceneInstanceFromPrefab() => !Application.isPlaying;

        private static GameBoardSetup LoadGameBoardSetupPrefabTemplate()
        {
            var prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(GameBoardSetup.PrefabAssetPath);
            return prefabRoot != null ? prefabRoot.GetComponent<GameBoardSetup>() : null;
        }

        private static IEnumerable<GameBoardSetup> FindSceneBoardSetups()
        {
            foreach (GameBoardSetup setup in Object.FindObjectsByType<GameBoardSetup>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                if (setup == null || !setup.gameObject.scene.IsValid())
                    continue;

                if (EditorUtility.IsPersistent(setup))
                    continue;

                // Ignore accidental nested prefab roots spawned under another board setup.
                if (setup.transform.parent != null &&
                    setup.transform.parent.GetComponentInParent<GameBoardSetup>() != null)
                {
                    continue;
                }

                yield return setup;
            }
        }

        private static bool NeedsCapturePotSync(GameBoardSetup setup)
        {
            return !setup.HasBakedCapturePotMarkers();
        }

        private static bool SyncSetupCapturePots(GameBoardSetup setup, GameBoardSetup template)
        {
            Undo.RegisterFullObjectHierarchyUndo(setup.gameObject, "Sync Capture Pots From Prefab");

            int removedDuplicates = RemoveAccidentalNestedBoardDuplicates(setup);
            if (removedDuplicates > 0)
            {
                Debug.LogWarning(
                    $"Removed {removedDuplicates} accidental nested GameBoardSetup object(s) under '{setup.name}'. " +
                    "These are created if InstantiatePrefab is used on a prefab child — use subtree clone instead.");
            }

            if (PrefabUtility.IsPartOfPrefabInstance(setup.gameObject))
            {
                GameObject instanceRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(setup.gameObject) ?? setup.gameObject;
                PrefabUtility.RevertPrefabInstance(instanceRoot, InteractionMode.UserAction);
            }

            setup.DiscoverCapturePotReferences();
            if (!NeedsCapturePotSync(setup))
            {
                EditorUtility.SetDirty(setup);
                return removedDuplicates > 0;
            }

            if (EditorCloneCapturePotsFromTemplate(setup, template))
            {
                setup.DiscoverCapturePotReferences();
                EditorUtility.SetDirty(setup);
                return setup.HasBakedCapturePotMarkers();
            }

            return false;
        }

        private static bool EditorCloneCapturePotsFromTemplate(GameBoardSetup setup, GameBoardSetup template)
        {
            template.DiscoverCapturePotReferences();
            bool changed = false;

            changed |= EditorClonePotSubtree(setup, template, GameBoardSetup.HostCapturePotName);
            changed |= EditorClonePotSubtree(setup, template, GameBoardSetup.OpponentCapturePotName);

            return changed;
        }

        private static bool EditorClonePotSubtree(
            GameBoardSetup setup,
            GameBoardSetup template,
            string potName)
        {
            Transform existing = GameBoardSetup.FindCapturePotRoot(setup.transform, potName);
            Transform templatePot = GameBoardSetup.FindCapturePotRoot(template.transform, potName);
            if (templatePot == null)
                return false;

            if (existing != null)
            {
                if (GameBoardSetup.HasCompleteCapturePotMarkers(existing))
                    return false;

                if (GameBoardSetup.HasBakedCaptureStackMarkers(existing))
                    return EditorSyncMissingPotGroupsFromTemplate(existing, templatePot);

                Undo.DestroyObjectImmediate(existing.gameObject);
            }

            // IMPORTANT: Never use PrefabUtility.InstantiatePrefab on a nested prefab child —
            // Unity instantiates the outermost prefab root (whole GameBoardSetup), not the subtree.
            GameObject clone = Object.Instantiate(templatePot.gameObject, setup.transform);
            clone.name = potName;
            clone.transform.localPosition = templatePot.localPosition;
            clone.transform.localRotation = templatePot.localRotation;
            clone.transform.localScale = templatePot.localScale;
            Undo.RegisterCreatedObjectUndo(clone, $"Clone {potName}");
            return true;
        }

        private static bool EditorSyncMissingPotGroupsFromTemplate(Transform existing, Transform templatePot)
        {
            bool changed = false;

            foreach (CapturePotDisplayGroup group in new[]
                     {
                         CapturePotDisplayGroup.Flowers,
                         CapturePotDisplayGroup.SpecialAndOther
                     })
            {
                if (GameBoardSetup.HasCompleteCapturePotGroupMarkers(existing, group))
                    continue;

                string groupFolder = CapturePotDisplayOrder.GetGroupFolder(group);
                Transform existingGroup = GameBoardSetup.FindCapturePotGroupRoot(existing, group);
                if (existingGroup != null)
                    Undo.DestroyObjectImmediate(existingGroup.gameObject);

                Transform templateGroup = GameBoardSetup.FindCapturePotGroupRoot(templatePot, group);
                if (templateGroup == null)
                {
                    Debug.LogWarning(
                        $"Capture pot template '{templatePot.name}' is missing group folder '{groupFolder}'.");
                    continue;
                }

                GameObject clone = Object.Instantiate(templateGroup.gameObject, existing);
                clone.name = groupFolder;
                clone.transform.localPosition = templateGroup.localPosition;
                clone.transform.localRotation = templateGroup.localRotation;
                clone.transform.localScale = templateGroup.localScale;
                Undo.RegisterCreatedObjectUndo(clone, $"Clone {groupFolder}");
                Debug.Log(
                    $"Synced missing capture-pot group '{groupFolder}' onto '{existing.name}' from prefab template.");
                changed = true;
            }

            return changed;
        }

        private static int RemoveAccidentalNestedBoardDuplicates(GameBoardSetup setup)
        {
            int removed = 0;
            for (int i = setup.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = setup.transform.GetChild(i);
                if (child.GetComponent<GameBoardSetup>() == null)
                    continue;

                Undo.DestroyObjectImmediate(child.gameObject);
                removed++;
            }

            return removed;
        }

        [MenuItem("Pai Sho/Capture Pot/Remove Duplicate Nested GameBoardSetup", false, 53)]
        public static void RemoveDuplicateNestedBoardFromMenu()
        {
            int removed = 0;
            foreach (GameBoardSetup setup in FindSceneBoardSetups())
                removed += RemoveAccidentalNestedBoardDuplicates(setup);

            if (removed == 0)
            {
                Debug.Log("No nested GameBoardSetup duplicates found.");
                return;
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log($"Removed {removed} nested GameBoardSetup duplicate(s). Save the scene (Ctrl+S).");
        }

        [MenuItem("Pai Sho/Capture Pot/Remove Duplicate Nested GameBoardSetup", true)]
        private static bool ValidateRemoveDuplicateNestedBoardFromMenu() => !Application.isPlaying;

        [MenuItem("Pai Sho/Capture Pot/Refresh Sample Tiles", false, 56)]
        [MenuItem("Pai Sho/Capture Pot/Rebuild Sample Tiles", false, 57)]
        public static void RefreshSampleTiles()
        {
            int count = RefreshAllCaptureSampleTiles();
            SaveOpenPrefabStage();
            Debug.Log(count > 0
                ? $"Refreshed {count} capture sample tile(s). One literal piece prefab per Stack_N."
                : "No CapturePotSlotMarker found. Run Bake Stack Markers first.");
        }

        [MenuItem("Pai Sho/Capture Pot/Mirror Host Layout To Opponent (Flip X)", false, 58)]
        public static void MirrorHostCapturePotToOpponent()
        {
            int setups = 0;
            int transforms = 0;

            foreach (GameBoardSetup setup in FindAllSetups())
            {
                setup.DiscoverCapturePotReferences();
                Transform host = setup.HostCapturePotRoot;
                Transform opponent = setup.OpponentCapturePotRoot;

                if (host == null || opponent == null)
                    continue;

                Undo.RegisterFullObjectHierarchyUndo(opponent.gameObject, "Mirror Capture Pot Host To Opponent");
                transforms += MirrorHostToOpponentWithUndo(host, opponent);

                foreach (CapturePotSlotMarker marker in opponent.GetComponentsInChildren<CapturePotSlotMarker>(true))
                    marker.RefreshVisibility();

                EditorUtility.SetDirty(setup);
                setups++;
            }

            if (setups == 0)
            {
                Debug.LogWarning(
                    "Need both CapturePotHost and CapturePotOpponent.\n" +
                    "Open GameBoardSetup.prefab, position the host pot, then run this menu item.");
                return;
            }

            SaveOpenPrefabStage();
            Debug.Log(
                $"Mirrored {transforms} transform(s) from host to opponent (X negated, Y/Z copied). Save prefab (Ctrl+S).");
        }

        [MenuItem("Pai Sho/Capture Pot/Mirror Host Layout To Opponent (Flip X)", true)]
        private static bool MirrorHostCapturePotToOpponentValidate() => !Application.isPlaying;

        private static int MirrorHostToOpponentWithUndo(Transform hostRoot, Transform opponentRoot)
        {
            int count = 0;

            RecordMirroredTransform(hostRoot, opponentRoot);
            count++;

            foreach (Transform hostTransform in hostRoot.GetComponentsInChildren<Transform>(true))
            {
                if (hostTransform == hostRoot || CapturePotMirrorUtility.IsSampleTileTransform(hostTransform))
                    continue;

                string relativePath = CapturePotMirrorUtility.GetRelativePath(hostTransform, hostRoot);
                if (string.IsNullOrEmpty(relativePath))
                    continue;

                Transform opponentTransform = opponentRoot.Find(relativePath);
                if (opponentTransform == null)
                    continue;

                RecordMirroredTransform(hostTransform, opponentTransform);
                count++;
            }

            return count;
        }

        private static void RecordMirroredTransform(Transform source, Transform destination)
        {
            Undo.RecordObject(destination, "Mirror Capture Pot Host To Opponent");
            CapturePotMirrorUtility.ApplyMirroredLocalTransform(source, destination);
        }

        private static int RefreshAllCaptureSampleTiles()
        {
            int count = 0;
            foreach (CapturePotSlotMarker marker in Object.FindObjectsByType<CapturePotSlotMarker>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                float spacing = ResolveCellSpacing(marker.transform);
                RemoveDuplicateSampleTiles(marker.transform, keep: null);
                EnsureSampleTile(marker.transform, marker.PreviewPieceType, spacing, rebuild: false);
                marker.RefreshVisibility();
                count++;
            }

            return count;
        }

        private static int BakeOpenPrefabStage(ref int slotCount)
        {
            PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage == null)
                return 0;

            var setup = stage.prefabContentsRoot.GetComponent<GameBoardSetup>();
            if (setup == null)
                setup = stage.prefabContentsRoot.GetComponentInChildren<GameBoardSetup>(true);

            if (setup == null)
                return 0;

            var layout = setup.GetComponent<BoardLayout>();
            if (layout == null)
                return 0;

            setup.DiscoverCapturePotReferences();
            int pots = 0;

            if (setup.HostCapturePotRoot == null)
                EnsurePotRoot(setup.transform, GameBoardSetup.HostCapturePotName, layout, Player.Host);

            if (setup.OpponentCapturePotRoot == null)
                EnsurePotRoot(setup.transform, GameBoardSetup.OpponentCapturePotName, layout, Player.Opponent);

            setup.DiscoverCapturePotReferences();

            if (setup.HostCapturePotRoot != null)
            {
                pots++;
                slotCount += EnsurePotStackMarkers(setup.HostCapturePotRoot, layout, Player.Host);
            }

            if (setup.OpponentCapturePotRoot != null)
            {
                pots++;
                slotCount += EnsurePotStackMarkers(setup.OpponentCapturePotRoot, layout, Player.Opponent);
            }

            EditorUtility.SetDirty(setup);
            return pots;
        }

        private static bool EnsurePotRoot(Transform boardRoot, string potName, BoardLayout layout, Player player)
        {
            Transform pot = FindChild(boardRoot, potName);
            if (pot == null)
            {
                var potObject = new GameObject(potName);
                pot = potObject.transform;
                pot.SetParent(boardRoot, false);
            }

            CapturePotLayoutUtility.ApplyAnchor(pot, layout, player, preserveScenePosition: false);
            return true;
        }

        private static int EnsurePotStackMarkers(Transform potRoot, BoardLayout layout, Player player)
        {
            if (CapturePotLegacyMigration.HasLegacyHierarchy(potRoot))
            {
                Debug.LogWarning(
                    $"{potRoot.name} still uses legacy folders (01_WhiteFlowers/Jasmine/...). " +
                    "Run Pai Sho > Capture Pot > Migrate Legacy Folders To Display Slots first to keep your alignment.");
                return 0;
            }

            Undo.RegisterFullObjectHierarchyUndo(potRoot.gameObject, "Bake Capture Pot Stack Markers");
            float cellSpacing = layout.CellSpacing;
            int created = 0;

            foreach (CapturePotStackCatalog.SlotDefinition slot in CapturePotStackCatalog.GetAllSlots())
            {
                Transform group = EnsureFolder(potRoot, slot.GroupFolder);
                Transform slotFolder = EnsureFolder(group, slot.SlotFolder);
                Transform stack = slotFolder.Find(slot.StackFolder);
                bool createdStack = stack == null;
                if (createdStack)
                {
                    var stackObject = new GameObject(slot.StackFolder);
                    stack = stackObject.transform;
                    stack.SetParent(slotFolder, false);
                    stack.localPosition = CapturePotStackLayout.ComputeDefaultLocalOffset(layout, slot);
                    stack.localRotation = Quaternion.identity;
                }

                CapturePotSlotMarker marker = stack.GetComponent<CapturePotSlotMarker>();
                if (marker == null)
                    marker = Undo.AddComponent<CapturePotSlotMarker>(stack.gameObject);

                marker.Configure(player, slot);
                EnsureSampleTile(stack, slot.PreviewPieceType, cellSpacing, rebuild: false);
                created++;
            }

            EditorUtility.SetDirty(potRoot);
            return created;
        }

        private static Transform EnsureFolder(Transform parent, string folderName)
        {
            Transform folder = parent.Find(folderName);
            if (folder != null)
                return folder;

            var folderObject = new GameObject(folderName);
            folder = folderObject.transform;
            folder.SetParent(parent, false);
            folder.localPosition = Vector3.zero;
            folder.localRotation = Quaternion.identity;
            folder.localScale = Vector3.one;
            return folder;
        }

        private static void EnsureSampleTile(
            Transform stack,
            PieceType pieceType,
            float cellSpacing,
            bool rebuild)
        {
            List<Transform> existingTiles = CollectSampleTileChildren(stack);
            Transform existing = existingTiles.Count > 0 ? existingTiles[0] : null;
            RemoveDuplicateSampleTiles(stack, keep: rebuild ? null : existing);

            if (existing != null && !rebuild && SampleTileMatches(existing.gameObject, pieceType))
            {
                RefreshSampleTile(existing.gameObject, pieceType, cellSpacing, stack);
                return;
            }

            if (existing != null)
                Undo.DestroyObjectImmediate(existing.gameObject);

            string prefabPath = HandTraySlotSampleTiles.GetPrefabAssetPath(pieceType);
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (source == null)
            {
                Debug.LogWarning($"Missing piece prefab at {prefabPath}");
                return;
            }

            var instance = PrefabUtility.InstantiatePrefab(source, stack) as GameObject;
            if (instance == null)
                return;

            Undo.RegisterCreatedObjectUndo(instance, "Add Capture Pot Sample Tile");
            ApplySampleTileName(instance);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            RefreshSampleTile(instance, pieceType, cellSpacing, stack);
            StripColliders(instance);
            StripRuntimeComponents(instance);
        }

        private static List<Transform> CollectSampleTileChildren(Transform stack)
        {
            var results = new List<Transform>(1);
            foreach (Transform child in stack)
            {
                if (child.name == CapturePotStackCatalog.SampleTileName ||
                    child.GetComponent<CapturePotSampleTile>() != null)
                {
                    results.Add(child);
                }
            }

            return results;
        }

        private static void RemoveDuplicateSampleTiles(Transform stack, Transform keep)
        {
            foreach (Transform child in stack)
            {
                if (child == keep)
                    continue;

                if (child.name != CapturePotStackCatalog.SampleTileName &&
                    child.GetComponent<CapturePotSampleTile>() == null)
                {
                    continue;
                }

                Undo.DestroyObjectImmediate(child.gameObject);
            }
        }

        private static void ApplySampleTileName(GameObject instance)
        {
            instance.name = CapturePotStackCatalog.SampleTileName;
        }

        private static bool SampleTileMatches(GameObject sampleTile, PieceType expectedType)
        {
            var marker = sampleTile.GetComponent<CapturePotSampleTile>();
            return marker != null && marker.PieceType == expectedType;
        }

        private static void RefreshSampleTile(
            GameObject instance,
            PieceType pieceType,
            float cellSpacing,
            Transform stack)
        {
            if (PrefabUtility.IsPartOfPrefabInstance(instance))
                PrefabUtility.RevertPrefabInstance(instance, InteractionMode.AutomatedAction);

            ApplySampleTileName(instance);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            string prefabPath = HandTraySlotSampleTiles.GetPrefabAssetPath(pieceType);
            PieceMaterialUtility.EnsureMaterials(instance, prefabPath, liftFlowerFaces: false);

            if (pieceType == PieceType.Jasmine)
                WoodTheme.ApplyJasmineThicknessCorrection(instance);

            WoodTheme.FitPrefabToCellSpacing(instance, cellSpacing, alignBottomToSurface: false);
            instance.transform.localScale *= CapturePotAlignmentDefaults.StackTileScale;

            CapturePotSampleTile marker = instance.GetComponent<CapturePotSampleTile>();
            if (marker == null)
                marker = instance.AddComponent<CapturePotSampleTile>();

            int stackIndex = 0;
            if (stack.TryGetComponent(out CapturePotSlotMarker slotMarker))
                stackIndex = slotMarker.StackIndex;

            marker.Configure(pieceType, stackIndex);
        }

        private static void StripRuntimeComponents(GameObject root)
        {
            foreach (Piece piece in root.GetComponentsInChildren<Piece>(true))
            {
                if (piece != null)
                    Undo.DestroyObjectImmediate(piece);
            }
        }

        private static void StripColliders(GameObject root)
        {
            foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
            {
                if (collider != null)
                    Undo.DestroyObjectImmediate(collider);
            }
        }

        private static float ResolveCellSpacing(Transform transform)
        {
            var layout = transform.GetComponentInParent<BoardLayout>();
            return layout != null ? layout.CellSpacing : 0.42f;
        }

        private static Transform FindChild(Transform root, string name)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name)
                    return child;
            }

            return null;
        }

        private static System.Collections.Generic.IEnumerable<GameBoardSetup> FindAllSetups()
        {
            PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null)
            {
                var setup = stage.prefabContentsRoot.GetComponent<GameBoardSetup>();
                if (setup == null)
                    setup = stage.prefabContentsRoot.GetComponentInChildren<GameBoardSetup>(true);

                if (setup != null)
                    yield return setup;

                yield break;
            }

            foreach (GameBoardSetup setup in Object.FindObjectsByType<GameBoardSetup>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                yield return setup;
        }

        private static void SaveOpenPrefabStage()
        {
            PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage == null)
                return;

            PrefabUtility.SaveAsPrefabAsset(stage.prefabContentsRoot, stage.assetPath);
            AssetDatabase.SaveAssets();
        }
    }
}
#endif
