using System.Collections.Generic;
using UnityEngine;
using PaiSho.Board;
using PaiSho.Pieces;
using PaiSho.Game;

namespace PaiSho
{
    public class GameplayVisualizer : MonoBehaviour
    {
        public static GameplayVisualizer Instance;

        private Transform harmonyLinesRoot;
        private Transform zonesRoot;
        private Transform ringRoot;
        private BoardLayout layout;
        private readonly Dictionary<Piece, GameObject> pieceOverlays = new();
        private readonly HashSet<Piece> ringHighlightPieces = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            harmonyLinesRoot = new GameObject("HarmonyLines").transform;
            zonesRoot = new GameObject("GameplayZones").transform;
            ringRoot = new GameObject("VictoryRingMarkers").transform;
        }

        private void Start()
        {
            layout = FindAnyObjectByType<BoardLayout>();
            Refresh();
        }

        public void Refresh()
        {
            if (layout == null)
                layout = FindAnyObjectByType<BoardLayout>();
            if (layout == null || BoardManager.Instance == null)
                return;

            AttachRootsToBoard();

            ClearChildren(harmonyLinesRoot);
            ClearChildren(zonesRoot);
            ClearChildren(ringRoot);

            DrawHarmonyConnections();
            DrawKnotweedZones();
            CacheRingHighlights();
            UpdatePieceOverlays();
        }

        private void CacheRingHighlights()
        {
            ringHighlightPieces.Clear();
            foreach (Player player in new[] { Player.Host, Player.Opponent })
            {
                if (HarmonyRingDetector.TryGetBestEnclosingCycle(player, out List<Piece> cycle))
                {
                    foreach (Piece piece in cycle)
                        ringHighlightPieces.Add(piece);
                }
            }
        }

        private void AttachRootsToBoard()
        {
            Transform boardParent = layout.Origin;
            harmonyLinesRoot.SetParent(boardParent, false);
            zonesRoot.SetParent(boardParent, false);
            ringRoot.SetParent(boardParent, false);
        }

        private Vector3 SurfaceAt(int coordinate, float extraLift = 0.015f)
        {
            return layout.GetSurfaceWorldPosition(coordinate, extraLift);
        }

        private void DrawHarmonyConnections()
        {
            List<Piece> pieces = BoardManager.Instance.GetAllPieces();
            float beamWidth = layout.CellSpacing * 0.042f;

            for (int i = 0; i < pieces.Count; i++)
            {
                for (int j = i + 1; j < pieces.Count; j++)
                {
                    Piece a = pieces[i];
                    Piece b = pieces[j];
                    if (HarmonyManager.Instance == null || !HarmonyManager.Instance.IsHarmony(a, b))
                        continue;

                    Vector3 from = SurfaceAt(a.BoardCoordinate, 0.1f);
                    Vector3 to = SurfaceAt(b.BoardCoordinate, 0.1f);
                    Color lineColor = WoodTheme.GetOwnerHarmonyLineColor(a.Owner);

                    var beam = HarmonyConnectionAnimator.Create(from, to, lineColor, beamWidth);
                    if (beam == null)
                        continue;

                    beam.transform.SetParent(harmonyLinesRoot, true);
                    var anim = beam.GetComponent<HarmonyConnectionAnimator>();
                    anim?.ConfigureReveal(0.03f + (i + j) * 0.006f, 0.48f);
                }
            }
        }

        private void DrawKnotweedZones()
        {
            float discSize = layout.CellSpacing * 0.28f;
            var marked = new HashSet<int>();

            foreach (Piece piece in BoardManager.Instance.GetAllPieces())
            {
                if (piece.Type != PieceType.Knotweed)
                    continue;

                foreach (int neighbor in BoardUtils.GetAdjacentCoordinates(piece.BoardCoordinate))
                {
                    if (!marked.Add(neighbor))
                        continue;

                    var disc = WoodTheme.CreateGlowDisc(
                        SurfaceAt(neighbor, 0.01f),
                        discSize,
                        new Color(0.75f, 0.22f, 0.28f, 0.32f));
                    if (layout.Origin != null)
                        disc.transform.rotation = layout.Origin.rotation;
                    disc.transform.SetParent(zonesRoot, true);
                }
            }
        }

        private void UpdatePieceOverlays()
        {
            // No under-piece glow discs — shading comes from scene lights and PieceStateAnimator on the mesh.
            foreach (var entry in pieceOverlays)
            {
                if (entry.Value != null)
                    Destroy(entry.Value);
            }
            pieceOverlays.Clear();

            foreach (Piece piece in BoardManager.Instance.GetAllPieces())
            {
                if (piece == null)
                    continue;

                var animator = PieceStateAnimator.Ensure(piece);
                if (animator == null)
                    continue;

                if (ringHighlightPieces.Contains(piece) && HarmonyRingDetector.HasCompleteRing(piece.Owner))
                    animator.NotifyVictory();
                else
                    animator.SyncFromPiece(immediate: false);
            }
        }

        private static void ClearChildren(Transform parent)
        {
            if (parent == null)
                return;

            for (int i = parent.childCount - 1; i >= 0; i--)
                Destroy(parent.GetChild(i).gameObject);
        }
    }
}
