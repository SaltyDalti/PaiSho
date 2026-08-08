using System.Collections.Generic;
using UnityEngine;
using PaiSho;
using PaiSho.Board;
using PaiSho.Pieces;

namespace PaiSho.Game
{
    /// <summary>
    /// Marks a scene or prefab board hierarchy as pre-aligned. Runtime skips
    /// destroy/recreate and gate/stand/tray repositioning when enabled.
    /// </summary>
    public class GameBoardSetup : MonoBehaviour
    {
        public static GameBoardSetup Instance { get; private set; }

        public const string HostTrayName = "HandTrayHost";
        public const string OpponentTrayName = "HandTrayOpponent";
        public const string HostCapturePotName = "CapturePotHost";
        public const string OpponentCapturePotName = "CapturePotOpponent";
        public const string SlotNamePrefix = "Slot_";
        public const string PrefabAssetPath = "Assets/Prefabs/Game/GameBoardSetup.prefab";

        /// <summary>True for baked slot sample tiles and other hand-tray authoring children.</summary>
        public static bool IsHandTrayAuthoringHierarchy(Transform transform)
        {
            while (transform != null)
            {
                if (transform.name == HostTrayName || transform.name == OpponentTrayName)
                    return true;

                transform = transform.parent;
            }

            return false;
        }

        /// <summary>True for baked capture-pot sample tiles and stack marker hierarchies.</summary>
        public static bool IsCapturePotAuthoringHierarchy(Transform transform)
        {
            while (transform != null)
            {
                if (transform.name == HostCapturePotName || transform.name == OpponentCapturePotName)
                    return true;

                if (transform.GetComponent<CapturePotSlotMarker>() != null)
                    return true;

                transform = transform.parent;
            }

            return false;
        }

        [SerializeField] private bool usePrebuiltLayout = true;
        [Tooltip("When enabled, play mode will not re-apply materials, alignment defaults, or move scene-authored transforms.")]
        [SerializeField] private bool preserveSceneAuthored = true;
        [SerializeField] private Transform hostTrayRoot;
        [SerializeField] private Transform opponentTrayRoot;
        [SerializeField] private Transform hostCapturePotRoot;
        [SerializeField] private Transform opponentCapturePotRoot;

        public bool UsePrebuiltLayout => usePrebuiltLayout;
        public bool PreserveSceneAuthored => preserveSceneAuthored;
        public Transform HostTrayRoot
        {
            get
            {
                DiscoverTrayReferences();
                return hostTrayRoot;
            }
        }

        public Transform OpponentTrayRoot
        {
            get
            {
                DiscoverTrayReferences();
                return opponentTrayRoot;
            }
        }
        public Transform HostCapturePotRoot
        {
            get
            {
                DiscoverCapturePotReferences();
                return hostCapturePotRoot;
            }
        }

        public Transform OpponentCapturePotRoot
        {
            get
            {
                DiscoverCapturePotReferences();
                return opponentCapturePotRoot;
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
                return;

            Instance = this;
            DiscoverTrayReferences();
            DiscoverCapturePotReferences();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void InitializeBoard()
        {
            var layout = GetComponent<BoardLayout>();
            var manager = GetComponent<BoardManager>();
            if (layout == null || manager == null)
                return;

            Transform piecesRoot = transform.Find("Pieces");
            manager.SetUsePrebuiltLayout(usePrebuiltLayout);
            manager.SetPreserveSceneAuthored(preserveSceneAuthored);
            manager.Initialize(layout, piecesRoot);
            WoodTheme.HideEmbeddedBoardUnderlays(gameObject);
        }

        public void DiscoverTrayReferences()
        {
            hostTrayRoot = ResolveTrayTransform(HostTrayName, hostTrayRoot);
            opponentTrayRoot = ResolveTrayTransform(OpponentTrayName, opponentTrayRoot);
        }

        public bool HasHandTrayRoots()
        {
            DiscoverTrayReferences();
            return IsLiveTrayRoot(hostTrayRoot, HostTrayName)
                && IsLiveTrayRoot(opponentTrayRoot, OpponentTrayName);
        }

        /// <summary>
        /// Scene boards can lag behind the prefab. Clone missing hand trays from a template,
        /// or bake empty marker trays onto the player stands as a last resort.
        /// </summary>
        public bool EnsureHandTrayRoots(GameBoardSetup templateOverride = null)
        {
            DiscoverTrayReferences();
            if (HasHandTrayRoots())
                return true;

            GameBoardSetup template = templateOverride ?? ResolvePrefabTemplate();
            if (template != null && TryRestoreHandTrayRootsFromTemplate(template))
            {
                DebugLogger.Log($"Restored hand trays on {name} from GameBoardSetup prefab template.");
            }

            DiscoverTrayReferences();
            if (HasHandTrayRoots())
                return true;

            if (TryBakeMissingHandTrays())
            {
                DebugLogger.Log($"Baked missing hand trays onto player stands for {name}.");
            }

            DiscoverTrayReferences();
            if (HasHandTrayRoots())
                return true;

            LogHandTrayDiagnostics();
            return false;
        }

        public bool TryRestoreHandTrayRootsFromTemplate(GameBoardSetup template)
        {
            if (template == null)
                return false;

            template.DiscoverTrayReferences();
            bool restored = false;

            if (!IsLiveTrayRoot(hostTrayRoot, HostTrayName))
            {
                restored |= TryRestoreSingleTray(
                    FindCapturePotRoot(template.transform, HostTrayName),
                    HostTrayName,
                    Player.Host,
                    ref hostTrayRoot);
            }

            if (!IsLiveTrayRoot(opponentTrayRoot, OpponentTrayName))
            {
                restored |= TryRestoreSingleTray(
                    FindCapturePotRoot(template.transform, OpponentTrayName),
                    OpponentTrayName,
                    Player.Opponent,
                    ref opponentTrayRoot);
            }

            DiscoverTrayReferences();
            return restored;
        }

        private bool TryRestoreSingleTray(
            Transform templateTray,
            string trayName,
            Player player,
            ref Transform field)
        {
            if (IsLiveTrayRoot(field, trayName))
                return false;

            Transform existing = FindChildInHierarchy(trayName);
            if (!IsLiveTrayRoot(existing, trayName))
            {
                var named = GameObject.Find(trayName);
                existing = named != null ? named.transform : null;
            }

            if (IsLiveTrayRoot(existing, trayName))
            {
                field = existing;
                return false;
            }

            Transform stand = ResolvePlayerStandRoot(player);
            if (stand == null)
                return false;

            if (templateTray != null)
            {
                var clone = Instantiate(templateTray.gameObject, stand);
                clone.name = trayName;
                clone.transform.localPosition = templateTray.localPosition;
                clone.transform.localRotation = templateTray.localRotation;
                clone.transform.localScale = templateTray.localScale;
                field = clone.transform;
                return true;
            }

            return false;
        }

        private bool TryBakeMissingHandTrays()
        {
            var layout = GetComponent<BoardLayout>();
            if (layout == null)
                return false;

            bool baked = false;

            if (!IsLiveTrayRoot(hostTrayRoot, HostTrayName))
            {
                Transform stand = ResolvePlayerStandRoot(Player.Host);
                Transform tray = HandTrayLayoutUtility.CreateBakedTray(
                    stand, layout, Player.Host, HostTrayName, bakeSlotMarkers: true);
                if (tray != null)
                {
                    hostTrayRoot = tray;
                    baked = true;
                }
            }

            if (!IsLiveTrayRoot(opponentTrayRoot, OpponentTrayName))
            {
                Transform stand = ResolvePlayerStandRoot(Player.Opponent);
                Transform tray = HandTrayLayoutUtility.CreateBakedTray(
                    stand, layout, Player.Opponent, OpponentTrayName, bakeSlotMarkers: true);
                if (tray != null)
                {
                    opponentTrayRoot = tray;
                    baked = true;
                }
            }

            return baked;
        }

        private Transform ResolvePlayerStandRoot(Player player)
        {
            string standName = player == Player.Host ? "PlayerStand" : "PlayerStandOpponent";

            Transform stand = FindChildInHierarchy(standName);
            if (stand != null)
                return stand;

            if (BoardManager.Instance != null)
            {
                stand = BoardManager.Instance.GetPlayerStandTransform(player);
                if (stand != null)
                    return stand;
            }

            var named = GameObject.Find(standName);
            return named != null ? named.transform : null;
        }

        private void LogHandTrayDiagnostics()
        {
            var childNames = new List<string>();
            foreach (Transform child in transform)
                childNames.Add(child.name);

            DebugLogger.LogWarning(
                $"Hand trays missing on '{name}'. Direct children: [{string.Join(", ", childNames)}]. " +
                $"hostRef={(hostTrayRoot != null ? hostTrayRoot.name : "null")}, " +
                $"oppRef={(opponentTrayRoot != null ? opponentTrayRoot.name : "null")}. " +
                "Run Pai Sho > Bake Hand Tray Tile Markers, or Sync Scene Instance From Prefab.");
        }

        private Transform ResolveTrayTransform(string trayName, Transform current)
        {
            if (IsLiveTrayRoot(current, trayName))
                return current;

            Transform found = FindChildInHierarchy(trayName);
            if (IsLiveTrayRoot(found, trayName))
                return found;

            // Stands (and trays parented to them) may be scene siblings, not under GameBoardSetup.
            var named = GameObject.Find(trayName);
            return IsLiveTrayRoot(named != null ? named.transform : null, trayName)
                ? named.transform
                : null;
        }

        private static bool IsLiveTrayRoot(Transform trayRoot, string trayName)
        {
            return trayRoot != null
                && trayRoot
                && trayRoot.name == trayName
                && trayRoot.gameObject.scene.IsValid()
                && trayRoot.gameObject.scene.isLoaded;
        }

        public void DiscoverCapturePotReferences()
        {
            hostCapturePotRoot = ResolveCapturePotTransform(HostCapturePotName, hostCapturePotRoot);
            opponentCapturePotRoot = ResolveCapturePotTransform(OpponentCapturePotName, opponentCapturePotRoot);
        }

        public Transform GetCapturePotRoot(Player player)
        {
            DiscoverCapturePotReferences();
            return player == Player.Host ? hostCapturePotRoot : opponentCapturePotRoot;
        }

        public bool HasCapturePotRoots()
        {
            DiscoverCapturePotReferences();
            return IsLivePotRoot(hostCapturePotRoot, HostCapturePotName)
                && IsLivePotRoot(opponentCapturePotRoot, OpponentCapturePotName);
        }

        /// <summary>True when CapturePotHost/Opponent exist as children in the hierarchy (not just serialized refs).</summary>
        public bool HasCapturePotHierarchy()
        {
            return FindCapturePotRoot(transform, HostCapturePotName) != null
                && FindCapturePotRoot(transform, OpponentCapturePotName) != null;
        }

        public bool HasBakedCapturePotMarkers()
        {
            Transform host = FindCapturePotRoot(transform, HostCapturePotName);
            Transform opponent = FindCapturePotRoot(transform, OpponentCapturePotName);
            return host != null
                && opponent != null
                && HasCompleteCapturePotMarkers(host)
                && HasCompleteCapturePotMarkers(opponent);
        }

        public bool EnsureCapturePotRoots(GameBoardSetup templateOverride = null)
        {
            DiscoverCapturePotReferences();
            if (HasCapturePotRoots())
                return true;

            GameBoardSetup template = templateOverride ?? ResolvePrefabTemplate();
            if (template != null && TryRestoreCapturePotRootsFromTemplate(template))
            {
                DebugLogger.Log($"Restored capture pots on {name} from GameBoardSetup prefab template.");
            }

            DiscoverCapturePotReferences();
            if (HasCapturePotRoots())
                return true;

            LogCapturePotDiagnostics();
            return false;
        }

        /// <summary>
        /// Scene prefab instances can lag behind the asset. Clone missing baked pots from a template setup.
        /// </summary>
        public bool TryRestoreCapturePotRootsFromTemplate(GameBoardSetup template)
        {
            if (template == null)
                return false;

            template.DiscoverCapturePotReferences();
            var layout = GetComponent<BoardLayout>();
            bool restored = false;

            if (!IsLivePotRoot(hostCapturePotRoot, HostCapturePotName))
            {
                restored |= TryRestoreSinglePot(
                    FindCapturePotRoot(template.transform, HostCapturePotName),
                    HostCapturePotName,
                    Player.Host,
                    layout,
                    ref hostCapturePotRoot);
            }

            if (!IsLivePotRoot(opponentCapturePotRoot, OpponentCapturePotName))
            {
                restored |= TryRestoreSinglePot(
                    FindCapturePotRoot(template.transform, OpponentCapturePotName),
                    OpponentCapturePotName,
                    Player.Opponent,
                    layout,
                    ref opponentCapturePotRoot);
            }

            DiscoverCapturePotReferences();
            return restored;
        }

        private bool TryRestoreSinglePot(
            Transform templatePot,
            string potName,
            Player player,
            BoardLayout layout,
            ref Transform field)
        {
            if (IsLivePotRoot(field, potName))
                return false;

            Transform existing = FindChildInHierarchy(potName);
            if (IsLivePotRoot(existing, potName))
            {
                field = existing;
                return false;
            }

            if (templatePot != null)
            {
                var clone = Instantiate(templatePot.gameObject, transform);
                clone.name = potName;
                field = clone.transform;
                return true;
            }

            var empty = new GameObject(potName).transform;
            empty.SetParent(transform, false);
            CapturePotLayoutUtility.ApplyAnchor(empty, layout, player, preserveScenePosition: false);
            field = empty;
            return true;
        }

        private Transform ResolveCapturePotTransform(string potName, Transform current)
        {
            if (IsLivePotRoot(current, potName))
                return current;

            Transform found = FindChildInHierarchy(potName);
            return IsLivePotRoot(found, potName) ? found : null;
        }

        private static bool IsLivePotRoot(Transform potRoot, string potName)
        {
            return potRoot != null
                && potRoot.name == potName
                && potRoot.gameObject.scene.IsValid();
        }

        private void LogCapturePotDiagnostics()
        {
            var childNames = new List<string>();
            foreach (Transform child in transform)
                childNames.Add(child.name);

            DebugLogger.LogWarning(
                $"Capture pots missing on '{name}'. Direct children: [{string.Join(", ", childNames)}]. " +
                $"hostRef={(hostCapturePotRoot != null ? hostCapturePotRoot.name : "null")}, " +
                $"oppRef={(opponentCapturePotRoot != null ? opponentCapturePotRoot.name : "null")}. " +
                "Run Pai Sho > Capture Pot > Sync Scene Instance From Prefab.");
        }

        private static GameBoardSetup ResolvePrefabTemplate()
        {
#if UNITY_EDITOR
            var prefabRoot = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(PrefabAssetPath);
            return prefabRoot != null ? prefabRoot.GetComponent<GameBoardSetup>() : null;
#else
            // Optional player fallback if a copy is placed under Resources/Game/.
            var resourcesRoot = Resources.Load<GameObject>("Game/GameBoardSetup");
            return resourcesRoot != null ? resourcesRoot.GetComponent<GameBoardSetup>() : null;
#endif
        }

        private Transform FindChildInHierarchy(string objectName)
        {
            Transform direct = transform.Find(objectName);
            if (direct != null)
                return direct;

            return FindCapturePotRoot(transform, objectName);
        }

        public static bool TryGetSlotMarker(Transform trayRoot, int slotIndex, out Transform marker)
        {
            marker = null;
            if (trayRoot == null || slotIndex < 0 || slotIndex >= HandTrayAlignmentDefaults.MaxSlots)
                return false;

            string slotName = $"{SlotNamePrefix}{slotIndex}";
            marker = trayRoot.Find(slotName);
            if (marker != null)
                return true;

            foreach (Transform child in trayRoot.GetComponentsInChildren<Transform>(true))
            {
                if (child != null && child.name == slotName && child != trayRoot)
                {
                    marker = child;
                    return true;
                }
            }

            return false;
        }

        public static bool HasBakedSlotMarkers(Transform trayRoot)
        {
            if (trayRoot == null)
                return false;

            for (int i = 0; i < HandTrayAlignmentDefaults.MaxSlots; i++)
            {
                if (!TryGetSlotMarker(trayRoot, i, out _))
                    return false;
            }

            return true;
        }

        public static bool TryGetCaptureStackMarker(
            Transform potRoot,
            CapturePotDisplayGroup group,
            int displaySlot,
            int stackIndex,
            out Transform marker)
        {
            return TryResolveCaptureStackPlacement(
                potRoot,
                group,
                displaySlot,
                stackIndex,
                out marker,
                out _);
        }

        /// <summary>
        /// Resolves a baked stack marker by compact lane (Slot_0..5), not preview/priority type.
        /// </summary>
        public static bool TryResolveCaptureStackPlacement(
            Transform potRoot,
            CapturePotDisplayGroup group,
            int displaySlot,
            int stackIndex,
            out Transform marker,
            out int stackLiftDelta)
        {
            marker = null;
            stackLiftDelta = 0;
            if (potRoot == null)
                return false;

            displaySlot = Mathf.Clamp(displaySlot, 0, CapturePotDisplayOrder.SlotsPerGroup - 1);
            stackIndex = Mathf.Max(0, stackIndex);

            if (!TryFindSlotRoot(potRoot, group, displaySlot, out Transform slotRoot))
                return false;

            string stackName = $"{CapturePotStackCatalog.StackNamePrefix}{stackIndex}";
            marker = FindDirectChild(slotRoot, stackName);
            if (marker != null)
                return true;

            for (int fallbackStack = stackIndex - 1; fallbackStack >= 0; fallbackStack--)
            {
                Transform candidate = FindDirectChild(slotRoot, $"{CapturePotStackCatalog.StackNamePrefix}{fallbackStack}");
                if (candidate == null)
                    continue;

                marker = candidate;
                stackLiftDelta = stackIndex - fallbackStack;
                return true;
            }

            marker = slotRoot;
            stackLiftDelta = stackIndex;
            return true;
        }

        public static bool TryFindSlotRoot(
            Transform potRoot,
            CapturePotDisplayGroup group,
            int displaySlot,
            out Transform slotRoot)
        {
            slotRoot = FindSlotRoot(potRoot, group, displaySlot);
            return slotRoot != null;
        }

        public static Transform FindDirectChild(Transform parent, string childName)
        {
            if (parent == null)
                return null;

            foreach (Transform child in parent)
            {
                if (child.name == childName)
                    return child;
            }

            return null;
        }

        private static Transform FindSlotRoot(Transform potRoot, CapturePotDisplayGroup group, int displaySlot)
        {
            string slotFolder = $"{CapturePotDisplayOrder.SlotNamePrefix}{displaySlot}";

            foreach (string groupFolder in GetGroupFolderCandidates(group))
            {
                Transform groupRoot = FindDirectChild(potRoot, groupFolder);
                if (groupRoot == null)
                    continue;

                Transform slotRoot = FindDirectChild(groupRoot, slotFolder);
                if (slotRoot != null)
                    return slotRoot;
            }

            return null;
        }

        private static IEnumerable<string> GetGroupFolderCandidates(CapturePotDisplayGroup group)
        {
            if (group == CapturePotDisplayGroup.Flowers)
            {
                yield return CapturePotDisplayOrder.FlowersFolder;
                yield return "01_WhiteFlowers";
                yield return "02_RedFlowers";
                yield break;
            }

            yield return CapturePotDisplayOrder.SpecialFolder;
            yield return "03_SpecialFlowers";
            yield return "04_Other";
        }

        public static bool HasBakedCaptureStackMarkers(Transform potRoot)
        {
            if (potRoot == null)
                return false;

            if (potRoot.GetComponentsInChildren<CapturePotSlotMarker>(true).Length > 0)
                return true;

            if (TryFindSlotRoot(potRoot, CapturePotDisplayGroup.Flowers, 0, out Transform slotRoot) &&
                FindDirectChild(slotRoot, $"{CapturePotStackCatalog.StackNamePrefix}0") != null)
            {
                return true;
            }

            return TryFindSlotRoot(potRoot, CapturePotDisplayGroup.SpecialAndOther, 0, out _);
        }

        /// <summary>True when every display lane in a group has at least Stack_0 baked.</summary>
        public static bool HasCompleteCapturePotGroupMarkers(Transform potRoot, CapturePotDisplayGroup group)
        {
            if (potRoot == null)
                return false;

            for (int slot = 0; slot < CapturePotDisplayOrder.SlotsPerGroup; slot++)
            {
                if (!TryFindSlotRoot(potRoot, group, slot, out Transform slotRoot))
                    return false;

                if (FindDirectChild(slotRoot, $"{CapturePotStackCatalog.StackNamePrefix}0") == null)
                    return false;
            }

            return true;
        }

        /// <summary>True when both Flowers and SpecialAndOther groups are fully baked.</summary>
        public static bool HasCompleteCapturePotMarkers(Transform potRoot)
        {
            return HasCompleteCapturePotGroupMarkers(potRoot, CapturePotDisplayGroup.Flowers)
                && HasCompleteCapturePotGroupMarkers(potRoot, CapturePotDisplayGroup.SpecialAndOther);
        }

        public static Transform FindCapturePotGroupRoot(Transform potRoot, CapturePotDisplayGroup group)
        {
            if (potRoot == null)
                return null;

            foreach (string folder in GetGroupFolderCandidates(group))
            {
                Transform found = FindDirectChild(potRoot, folder);
                if (found != null)
                    return found;
            }

            return null;
        }

        public static Transform FindCapturePotRoot(Transform searchRoot, string potName)
        {
            if (searchRoot == null)
                return null;

            if (searchRoot.name == potName)
                return searchRoot;

            foreach (Transform child in searchRoot.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == potName)
                    return child;
            }

            return null;
        }
    }
}
