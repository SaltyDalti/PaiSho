using System.Collections.Generic;
using UnityEngine;
using PaiSho.Board;
using PaiSho.Pieces;

namespace PaiSho.Game
{
    public class LegalMoveHighlighter : MonoBehaviour
    {
        public static LegalMoveHighlighter Instance;

        private readonly Dictionary<int, GameObject> markers = new();
        private readonly List<GameObject> contextMarkers = new();
        private Transform root;
        private Transform contextRoot;
        private BoardLayout layout;
        private GameObject selectionRing;
        private Piece currentSelectedPiece;
        private int? hoveredCoordinate;
        private int markerSpawnIndex;
        private GameObject hoverGlow;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            root = new GameObject("LegalMoveMarkers").transform;
            root.SetParent(transform, false);
            contextRoot = new GameObject("MoveContextOverlays").transform;
            contextRoot.SetParent(transform, false);
        }

        private void Start()
        {
            layout = FindAnyObjectByType<BoardLayout>();
            if (layout != null)
            {
                root.SetParent(layout.Origin, false);
                contextRoot.SetParent(layout.Origin, false);
            }
        }

        private void Update()
        {
            PulseHoverGlow();
        }

        public void SetHoveredCoordinate(int? coordinate)
        {
            if (hoveredCoordinate == coordinate)
                return;

            hoveredCoordinate = coordinate;

            if (hoverGlow != null)
            {
                Destroy(hoverGlow);
                hoverGlow = null;
            }

            if (!coordinate.HasValue || layout == null)
                return;

            float size = layout.CellSpacing * 0.4f;
            hoverGlow = WoodTheme.CreateGlowDisc(
                layout.GetSurfaceWorldPosition(coordinate.Value, 0.028f),
                size,
                new Color(0.95f, 0.88f, 0.55f, 0.42f));
            if (layout.Origin != null)
                hoverGlow.transform.rotation = layout.Origin.rotation;
            hoverGlow.transform.SetParent(root, true);
        }

        private void PulseHoverGlow()
        {
            if (hoverGlow == null)
                return;

            float pulse = 1f + 0.05f * Mathf.Sin(Time.time * 5.5f);
            float baseSize = layout != null ? layout.CellSpacing * 0.4f : 1f;
            hoverGlow.transform.localScale = new Vector3(baseSize * pulse, 0.004f, baseSize * pulse);
        }

        public void Clear()
        {
            foreach (var marker in markers.Values)
            {
                if (marker != null)
                    Destroy(marker);
            }

            markers.Clear();
            ClearContext();
            ClearSelectionRing();
            currentSelectedPiece = null;
            hoveredCoordinate = null;
            markerSpawnIndex = 0;
            if (hoverGlow != null)
            {
                Destroy(hoverGlow);
                hoverGlow = null;
            }
        }

        public void ShowSelectedPiece(Piece piece)
        {
            ClearSelectionRing();
            if (piece == null)
                return;

            if (layout == null)
                layout = FindAnyObjectByType<BoardLayout>();
            if (layout == null)
                return;

            selectionRing = WoodTheme.CreateSelectionRing(
                layout,
                piece.BoardCoordinate,
                WoodTheme.GetFlowerAccent(piece.Type));
        }

        public void ClearSelectionRing()
        {
            if (selectionRing != null)
            {
                Destroy(selectionRing);
                selectionRing = null;
            }
        }

        public void ShowMoves(IEnumerable<LegalMove> moves)
        {
            ShowMovesWithContext(null, moves, null, null);
        }

        public void ShowMovesWithContext(
            Piece selectedPiece,
            IEnumerable<LegalMove> moves,
            IEnumerable<int> disharmonyBlocked,
            IEnumerable<int> gardenBlocked,
            IEnumerable<int> unloadTargets = null)
        {
            ClearMoveMarkers();
            currentSelectedPiece = selectedPiece;
            markerSpawnIndex = 0;

            if (layout == null)
                layout = FindAnyObjectByType<BoardLayout>();
            if (layout == null)
                return;

            var moveList = new List<LegalMove>();
            foreach (LegalMove move in moves)
            {
                moveList.Add(move);
                CreateMarker(move.Coordinate, move.IsCapture);
            }

            if (unloadTargets != null)
            {
                foreach (int coordinate in unloadTargets)
                    CreateUnloadMarker(coordinate);
            }

            if (disharmonyBlocked != null)
            {
                foreach (int coordinate in disharmonyBlocked)
                    CreateBlockedMarker(coordinate, isDisharmony: true);
            }

            if (gardenBlocked != null)
            {
                foreach (int coordinate in gardenBlocked)
                    CreateBlockedMarker(coordinate, isDisharmony: false);
            }

            if (selectedPiece != null)
            {
                DrawMoveContext(selectedPiece, moveList);
                DrawDisharmonyThreatRays(selectedPiece);

                if (selectedPiece.Type == PieceType.Wheel)
                    DrawWheelRotationPreview(selectedPiece);
            }
        }

        public void ShowPlacements(IEnumerable<int> coordinates)
        {
            ShowPlacementsWithContext(coordinates, null, null);
        }

        public void ShowPlacementsWithContext(
            IEnumerable<int> coordinates,
            Player? player,
            PieceType? placingType)
        {
            ClearMoveMarkers();
            currentSelectedPiece = null;

            if (layout == null)
                layout = FindAnyObjectByType<BoardLayout>();
            if (layout == null)
                return;

            var legalPorts = new HashSet<int>();
            if (player.HasValue && placingType.HasValue && PieceRules.IsBasicFlower(placingType.Value))
            {
                foreach (int port in PieceRules.GetLegalEntryPorts(player.Value, placingType.Value))
                    legalPorts.Add(port);
            }

            foreach (int coordinate in coordinates)
                CreateMarker(coordinate, false);

            if (placingType.HasValue && PieceRules.IsBasicFlower(placingType.Value))
                DrawPortMarkers(legalPorts);
        }

        public void ShowMomentumTargets(IEnumerable<int> coordinates)
        {
            ClearMoveMarkers();
            currentSelectedPiece = null;

            if (layout == null)
                layout = FindAnyObjectByType<BoardLayout>();
            if (layout == null)
                return;

            foreach (int coordinate in coordinates)
                CreateMarker(coordinate, false, isMomentum: true);
        }

        private void ClearMoveMarkers()
        {
            foreach (var marker in markers.Values)
            {
                if (marker != null)
                    Destroy(marker);
            }

            markers.Clear();
            ClearContext();
            if (hoverGlow != null)
            {
                Destroy(hoverGlow);
                hoverGlow = null;
            }
            hoveredCoordinate = null;
        }

        private void ClearContext()
        {
            foreach (GameObject marker in contextMarkers)
            {
                if (marker != null)
                    Destroy(marker);
            }

            contextMarkers.Clear();
        }

        private void AddBeam(Vector3 from, Vector3 to, Color color, float width)
        {
            var segment = WoodTheme.CreatePathBeam(from, to, color, width);
            if (segment == null)
                return;

            segment.transform.SetParent(contextRoot, true);
            contextMarkers.Add(segment);

            var reveal = segment.AddComponent<LineRevealAnimator>();
            reveal.Configure(0.08f + markerSpawnIndex * 0.012f, 0.32f);
        }

        private void CreateMarker(int coordinate, bool isCapture, bool isMomentum = false)
        {
            if (markers.ContainsKey(coordinate))
                return;

            var marker = WoodTheme.CreateMoveGemMarker(layout, coordinate, isCapture, isMomentum);
            marker.transform.SetParent(root, true);
            markers[coordinate] = marker;
            markerSpawnIndex++;
        }

        private void CreateUnloadMarker(int coordinate)
        {
            if (markers.ContainsKey(coordinate))
                return;

            var marker = WoodTheme.CreateUnloadMarker(layout, coordinate);
            marker.transform.SetParent(root, true);
            markers[coordinate] = marker;
            markerSpawnIndex++;
        }

        private void CreateBlockedMarker(int coordinate, bool isDisharmony)
        {
            if (markers.ContainsKey(coordinate))
                return;

            var marker = WoodTheme.CreateBlockedMoveMarker(layout, coordinate, isDisharmony);
            marker.transform.SetParent(root, true);
            markers[coordinate] = marker;
        }

        private void DrawMoveContext(Piece selected, List<LegalMove> moves)
        {
            float beamWidth = layout.CellSpacing * 0.038f;
            int origin = selected.BoardCoordinate;
            var lPathDrawn = new HashSet<int>();

            foreach (LegalMove move in moves)
            {
                if (move.IsCapture && move.CaptureTarget != null)
                    AddBeam(
                        layout.GetSurfaceWorldPosition(origin, 0.08f),
                        layout.GetSurfaceWorldPosition(move.CaptureTarget.BoardCoordinate, 0.08f),
                        JapaneseTheme.CaptureLine,
                        beamWidth * 1.1f);

                if (move.HasPush)
                {
                    int pushFrom = move.Push.PushedPiece.BoardCoordinate;
                    AddBeam(
                        layout.GetSurfaceWorldPosition(pushFrom, 0.07f),
                        layout.GetSurfaceWorldPosition(move.Push.ToCoordinate, 0.07f),
                        JapaneseTheme.PushLine,
                        beamWidth);

                    if (!markers.ContainsKey(move.Push.ToCoordinate))
                    {
                        var pushMarker = WoodTheme.CreateGlowDisc(
                            layout.GetSurfaceWorldPosition(move.Push.ToCoordinate, 0.045f),
                            layout.CellSpacing * 0.14f,
                            JapaneseTheme.PushLine);
                        if (layout.Origin != null)
                            pushMarker.transform.rotation = layout.Origin.rotation;
                        pushMarker.transform.SetParent(contextRoot, true);
                        contextMarkers.Add(pushMarker);
                    }
                }

                if ((selected.Type == PieceType.Lily || selected.Type == PieceType.Chrysanthemum) &&
                    lPathDrawn.Add(move.Coordinate))
                {
                    var path = new List<int>();
                    if (LegalMoveCalculator.TryGetLMovePath(selected, move.Coordinate, path))
                        DrawPath(path, JapaneseTheme.PathInk, beamWidth * 0.9f);
                }
            }
        }

        private void DrawPath(List<int> path, Color color, float width)
        {
            if (path == null || path.Count == 0 || currentSelectedPiece == null)
                return;

            int previous = currentSelectedPiece.BoardCoordinate;
            foreach (int step in path)
            {
                AddBeam(
                    layout.GetSurfaceWorldPosition(previous, 0.055f),
                    layout.GetSurfaceWorldPosition(step, 0.055f),
                    color,
                    width);
                previous = step;
            }
        }

        private void DrawPortMarkers(HashSet<int> legalPorts)
        {
            if (legalPorts == null || legalPorts.Count == 0)
                return;

            foreach (int port in legalPorts)
            {
                if (!BoardUtils.IsValidPointCoordinate(port))
                    continue;

                var marker = WoodTheme.CreatePortMarker(layout, port, isLegalEntry: true);
                marker.transform.SetParent(contextRoot, true);
                contextMarkers.Add(marker);
            }
        }

        private void DrawWheelRotationPreview(Piece wheel)
        {
            List<int> ring = BoardUtils.GetClockwiseSquareRing(wheel.BoardCoordinate);
            float[] yaws = { 0f, 45f, 90f, 135f, 180f, 225f, 270f, 315f };

            for (int i = 0; i < ring.Count; i++)
            {
                var arrow = WoodTheme.CreateWheelArrow(layout, ring[i], yaws[i % yaws.Length]);
                arrow.transform.SetParent(contextRoot, true);
                contextMarkers.Add(arrow);
            }

            float ringSize = layout.CellSpacing * 0.72f;
            var halo = WoodTheme.CreateGlowDisc(
                layout.GetSurfaceWorldPosition(wheel.BoardCoordinate, 0.025f),
                ringSize,
                new Color(0.95f, 0.8f, 0.34f, 0.48f));
            if (layout.Origin != null)
                halo.transform.rotation = layout.Origin.rotation;
            halo.transform.SetParent(contextRoot, true);
            contextMarkers.Add(halo);

            // Breathing glow makes the rotate affordance read clearly the moment a wheel is selected.
            var haloAnimator = halo.AddComponent<OverlayAnimator>();
            haloAnimator.Configure(OverlayAnimator.Style.RingBreathe, halo.transform, 1.8f, 0.12f);
        }

        private void DrawDisharmonyThreatRays(Piece selected)
        {
            if (selected == null || BoardManager.Instance == null || layout == null)
                return;

            var profile = PieceHarmonyProfiles.Get(selected.Type);
            if (profile.Disharmonic.Count == 0)
                return;

            float rayWidth = layout.CellSpacing * 0.035f;
            float ringSize = layout.CellSpacing * 0.34f;
            var rayColor = JapaneseTheme.DisharmonyMarker;
            var threatRingColor = new Color(0.88f, 0.32f, 0.48f, 0.65f);

            foreach (Piece piece in BoardManager.Instance.GetAllPieces())
            {
                if (piece.Owner != selected.Owner || !profile.Disharmonic.Contains(piece.Type))
                    continue;

                Vector3 threatCenter = layout.GetSurfaceWorldPosition(piece.BoardCoordinate, 0.04f);
                var threatRing = WoodTheme.CreateGlowDisc(threatCenter, ringSize, threatRingColor);
                if (layout.Origin != null)
                    threatRing.transform.rotation = layout.Origin.rotation;
                threatRing.transform.SetParent(contextRoot, true);
                contextMarkers.Add(threatRing);

                foreach (int direction in BoardUtils.CardinalDirections)
                    AddDisharmonyRayFrom(piece.BoardCoordinate, direction, rayColor, rayWidth);
            }
        }

        private void AddDisharmonyRayFrom(int origin, int direction, Color color, float width)
        {
            int coordinate = origin;
            while (true)
            {
                coordinate += direction;
                if (!BoardUtils.IsValidPointCoordinate(coordinate))
                    break;

                if (BoardManager.Instance.GetPieceAt(coordinate) != null)
                    break;

                Vector3 from = layout.GetSurfaceWorldPosition(coordinate - direction, 0.05f);
                Vector3 to = layout.GetSurfaceWorldPosition(coordinate, 0.05f);
                var segment = WoodTheme.CreateDisharmonyRaySegment(from, to, color, width);
                if (segment == null)
                    continue;

                segment.transform.SetParent(contextRoot, true);
                contextMarkers.Add(segment);
            }
        }
    }
}
