using System.Collections.Generic;
using UnityEngine;
using PaiSho;
using PaiSho.Board;
using PaiSho.Pieces;

namespace PaiSho.Game
{
    /// <summary>Stacks captured tile visuals west of the grid, compacted by display priority.</summary>
    public class PotVisualManager : MonoBehaviour
    {
        public static PotVisualManager Instance;

        private readonly Dictionary<Player, Transform> potRoots = new();
        private readonly Dictionary<Player, Transform> potTilesRoots = new();
        private readonly Dictionary<Player, List<Piece>> potVisuals = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(gameObject);
            else
                Instance = this;
        }

        public void InitializeAnchors()
        {
            if (BoardManager.Instance == null)
                return;

            BoardLayout layout = BoardManager.Instance.GetBoardLayout();
            if (layout == null)
                return;

            EnsureBoardSetupCapturePots();

            bool preserveScene = ShouldPreserveSceneAnchors();
            EnsurePotRoot(Player.Host, layout, preserveScene);
            EnsurePotRoot(Player.Opponent, layout, preserveScene);
        }

        private static void EnsureBoardSetupCapturePots()
        {
            GameBoardSetup setup = GameBoardSetup.Instance;
            if (setup == null && BoardManager.Instance != null)
                setup = BoardManager.Instance.GetComponent<GameBoardSetup>();

            setup ??= Object.FindAnyObjectByType<GameBoardSetup>();
            setup?.EnsureCapturePotRoots();
        }

        public void SendToPot(Piece piece, Player capturedBy)
        {
            if (piece == null || BoardManager.Instance == null)
                return;

            EnsureAnchorsReady();
            PrepareForPot(piece, capturedBy);
            RelayoutPot(capturedBy);
        }

        public Vector3 PreviewStackPosition(Piece piece, Player capturedBy)
        {
            EnsureAnchorsReady();

            var preview = new List<Piece>();
            if (potVisuals.TryGetValue(capturedBy, out List<Piece> existing))
                preview.AddRange(existing);

            preview.Add(piece);
            var plan = CapturePotLayoutResolver.BuildPlacementPlan(preview);
            return plan.TryGetValue(piece, out CapturePotResolvedSlot resolved)
                ? ComputeStackPosition(capturedBy, plan, resolved)
                : piece.transform.position;
        }

        public void FinalizeInPot(Piece piece, Player capturedBy)
        {
            if (piece == null)
                return;

            EnsureAnchorsReady();
            PrepareForPot(piece, capturedBy);
            RelayoutPot(capturedBy);
        }

        public void ClearAll()
        {
            ClearPot(Player.Host);
            ClearPot(Player.Opponent);
        }

        public void ClearPot(Player capturedBy)
        {
            if (!potVisuals.TryGetValue(capturedBy, out List<Piece> visuals))
                return;

            foreach (Piece piece in visuals)
            {
                if (piece != null)
                    Destroy(piece.gameObject);
            }

            visuals.Clear();
            InvalidatePotCache(capturedBy);
            PotManager.Instance?.ClearCapturedBy(capturedBy);
        }

        public bool TryAddDebugCapture(PieceType type, Player capturedBy)
        {
            if (BoardManager.Instance == null)
                return false;

            Player victim = capturedBy == Player.Host ? Player.Opponent : Player.Host;
            GameObject visual = BoardManager.Instance.CreateHandVisual(victim, type);
            if (visual == null)
                return false;

            Piece piece = visual.GetComponent<Piece>();
            if (piece == null)
            {
                Destroy(visual);
                return false;
            }

            EnsureAnchorsReady();
            PotManager.Instance?.RecordCapture(piece, capturedBy);
            SendToPot(piece, capturedBy);
            return true;
        }

        public bool TryDescribePot(Player capturedBy, out string description)
        {
            description = string.Empty;
            Transform potRoot = EnsurePotRoot(capturedBy, BoardManager.Instance?.GetBoardLayout(), ShouldPreserveSceneAnchors());
            if (potRoot == null)
            {
                description = $"{capturedBy}: no pot root";
                return false;
            }

            bool baked = GameBoardSetup.HasBakedCaptureStackMarkers(potRoot);
            int markers = potRoot.GetComponentsInChildren<CapturePotSlotMarker>(true).Length;
            description = $"{capturedBy} pot='{GetTransformPath(potRoot)}' baked={baked} markers={markers}";
            return baked;
        }

        private void EnsureAnchorsReady()
        {
            if (potRoots.ContainsKey(Player.Host) && potRoots.ContainsKey(Player.Opponent))
                return;

            InitializeAnchors();
        }

        private static bool ShouldPreserveSceneAnchors()
        {
            var setup = Object.FindAnyObjectByType<GameBoardSetup>();
            return setup != null && setup.UsePrebuiltLayout && setup.PreserveSceneAuthored;
        }

        private void PrepareForPot(Piece piece, Player capturedBy)
        {
            BoardManager.Instance.ReleasePieceFromBoard(piece);

            foreach (Collider collider in piece.GetComponentsInChildren<Collider>())
                collider.enabled = false;

            Transform root = EnsurePotRoot(capturedBy, BoardManager.Instance.GetBoardLayout(), ShouldPreserveSceneAnchors());
            Transform tilesRoot = EnsurePotTilesRoot(capturedBy, root);

            if (!potVisuals.TryGetValue(capturedBy, out List<Piece> visuals))
            {
                visuals = new List<Piece>();
                potVisuals[capturedBy] = visuals;
            }

            if (!visuals.Contains(piece))
                visuals.Add(piece);

            if (tilesRoot != null)
                piece.transform.SetParent(tilesRoot, true);
        }

        private void RelayoutPot(Player capturedBy)
        {
            if (!potVisuals.TryGetValue(capturedBy, out List<Piece> visuals) || visuals.Count == 0)
                return;

            Transform potRoot = EnsurePotRoot(capturedBy, BoardManager.Instance.GetBoardLayout(), ShouldPreserveSceneAnchors());
            if (potRoot == null)
                return;

            Transform tilesRoot = EnsurePotTilesRoot(capturedBy, potRoot);
            if (tilesRoot == null)
                return;

            var plan = CapturePotLayoutResolver.BuildPlacementPlan(visuals);
            float cellSpacing = BoardManager.Instance.GetBoardLayout()?.CellSpacing ?? 0.42f;
            bool baked = GameBoardSetup.HasBakedCaptureStackMarkers(potRoot);

            for (int i = 0; i < visuals.Count; i++)
            {
                Piece piece = visuals[i];
                if (piece == null || !plan.TryGetValue(piece, out CapturePotResolvedSlot resolved))
                    continue;

                if (baked &&
                    GameBoardSetup.TryResolveCaptureStackPlacement(
                        potRoot,
                        resolved.Group,
                        resolved.DisplaySlot,
                        resolved.StackIndex,
                        out Transform marker,
                        out int stackLiftDelta))
                {
                    CapturePotLayoutUtility.ApplyRuntimeTileToMarker(
                        piece.gameObject,
                        marker,
                        tilesRoot,
                        cellSpacing,
                        piece.Type,
                        stackLiftDelta);
                    continue;
                }

                piece.transform.SetParent(tilesRoot, true);
                FitPotTile(piece);
                piece.transform.position = ComputeStackPosition(capturedBy, plan, resolved, potRoot, baked);
                piece.transform.rotation = Quaternion.identity;
                CapturePotLayoutUtility.FinalizeRuntimeTile(piece, cellSpacing);
            }
        }

        private Transform EnsurePotTilesRoot(Player capturedBy, Transform potRoot)
        {
            Transform boardRoot = ResolveBoardRoot(potRoot);
            if (boardRoot == null)
                return null;

            if (potTilesRoots.TryGetValue(capturedBy, out Transform existing) &&
                existing != null &&
                existing.parent == boardRoot)
            {
                return existing;
            }

            Transform tilesRoot = CapturePotLayoutUtility.EnsureTilesRoot(boardRoot, capturedBy);
            potTilesRoots[capturedBy] = tilesRoot;
            return tilesRoot;
        }

        private static Transform ResolveBoardRoot(Transform potRoot)
        {
            if (GameBoardSetup.Instance != null)
                return GameBoardSetup.Instance.transform;

            if (potRoot != null)
            {
                GameBoardSetup setup = potRoot.GetComponentInParent<GameBoardSetup>();
                if (setup != null)
                    return setup.transform;
            }

            if (BoardManager.Instance != null)
                return BoardManager.Instance.transform;

            return potRoot;
        }

        private static void FitPotTile(Piece piece)
        {
            if (piece == null)
                return;

            float cellSpacing = BoardManager.Instance.GetBoardLayout()?.CellSpacing ?? 0.42f;
            piece.transform.localScale = Vector3.one;
            WoodTheme.FitPrefabToCellSpacing(piece.gameObject, cellSpacing, alignBottomToSurface: false);
            piece.transform.localScale *= CapturePotAlignmentDefaults.StackTileScale;
        }

        private Vector3 ComputeStackPosition(
            Player capturedBy,
            Dictionary<Piece, CapturePotResolvedSlot> plan,
            CapturePotResolvedSlot resolved,
            Transform potRoot = null,
            bool baked = false)
        {
            potRoot ??= potRoots.TryGetValue(capturedBy, out Transform cached) ? cached : null;
            BoardLayout layout = BoardManager.Instance.GetBoardLayout();
            baked = baked || (potRoot != null && GameBoardSetup.HasBakedCaptureStackMarkers(potRoot));

            if (baked &&
                potRoot != null &&
                GameBoardSetup.TryResolveCaptureStackPlacement(
                    potRoot,
                    resolved.Group,
                    resolved.DisplaySlot,
                    resolved.StackIndex,
                    out Transform marker,
                    out int stackLiftDelta))
            {
                if (stackLiftDelta <= 0)
                    return marker.position;

                float spacing = layout?.CellSpacing ?? 0.42f;
                return marker.position +
                       Vector3.up * (CapturePotLayoutUtility.ResolveStackStepForPreview(marker.parent, spacing) * stackLiftDelta);
            }

            return CapturePotStackLayout.ComputeWorldPosition(
                potRoot,
                layout,
                resolved.Group,
                resolved.DisplaySlot,
                resolved.StackIndex);
        }

        private Transform EnsurePotRoot(Player capturedBy, BoardLayout layout, bool preserveScenePosition)
        {
            if (potRoots.TryGetValue(capturedBy, out Transform existing) && existing != null)
                return existing;

            Transform resolved = ResolveAuthoringPotRoot(capturedBy);
            if (resolved == null)
            {
                DebugLogger.LogWarning(
                    $"Capture pot for {capturedBy} not found under GameBoardSetup " +
                    $"(expected {GameBoardSetup.HostCapturePotName} / {GameBoardSetup.OpponentCapturePotName}).");
                return null;
            }

            potRoots[capturedBy] = resolved;
            CapturePotLayoutUtility.ApplyAnchor(resolved, layout, capturedBy, preserveScenePosition);

            int markerCount = resolved.GetComponentsInChildren<CapturePotSlotMarker>(true).Length;
            DebugLogger.Log(
                $"Capture pot ready for {capturedBy}: {GetTransformPath(resolved)} " +
                $"(markers={markerCount}, baked={GameBoardSetup.HasBakedCaptureStackMarkers(resolved)})");

            return resolved;
        }

        private static Transform ResolveAuthoringPotRoot(Player capturedBy)
        {
            GameBoardSetup setup = GameBoardSetup.Instance;
            if (setup == null && BoardManager.Instance != null)
                setup = BoardManager.Instance.GetComponent<GameBoardSetup>();

            if (setup == null)
                setup = Object.FindAnyObjectByType<GameBoardSetup>();

            if (setup != null)
            {
                Transform pot = setup.GetCapturePotRoot(capturedBy);
                if (pot != null)
                    return pot;

                pot = GameBoardSetup.FindCapturePotRoot(setup.transform, GetPotName(capturedBy));
                if (pot != null)
                    return pot;
            }

            string potName = GetPotName(capturedBy);
            foreach (Transform candidate in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (candidate.name == potName)
                    return candidate;
            }

            return null;
        }

        private static string GetPotName(Player capturedBy)
        {
            return capturedBy == Player.Host
                ? GameBoardSetup.HostCapturePotName
                : GameBoardSetup.OpponentCapturePotName;
        }

        private void InvalidatePotCache(Player capturedBy)
        {
            potRoots.Remove(capturedBy);
            potTilesRoots.Remove(capturedBy);
        }

        private static string GetTransformPath(Transform transform)
        {
            if (transform == null)
                return "<null>";

            var names = new List<string>(8);
            Transform current = transform;
            while (current != null)
            {
                names.Add(current.name);
                current = current.parent;
            }

            names.Reverse();
            return string.Join("/", names);
        }
    }
}
