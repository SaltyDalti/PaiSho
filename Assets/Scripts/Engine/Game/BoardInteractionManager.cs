using System.Collections.Generic;
using UnityEngine;
using PaiSho.Pieces;
using PaiSho.Board;

namespace PaiSho.Game
{
    public class BoardInteractionManager : MonoBehaviour
    {
        public LayerMask tileLayerMask;
        private Piece selectedPiece;
        private List<Tile> highlightedTiles = new List<Tile>();
        private HashSet<int> legalMoveCoordinates = new HashSet<int>();

        private Tile lastHoveredTile = null;

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                HandleClick();
            }
            else if (selectedPiece == null)
            {
                HandleHover();
            }
        }

        private void HandleClick()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit, 100f, tileLayerMask))
            {
                if (hit.collider.TryGetComponent(out Piece clickedPiece))
                {
                    HandlePieceClick(clickedPiece);
                }
                else if (hit.collider.TryGetComponent(out Tile clickedTile))
                {
                    HandleTileClick(clickedTile);
                }
            }
        }

        private void HandleTileClick(Tile tile)
        {
            if (tile == null)
                return;

            if (GameManager.Instance.IsSpringPhase() || PiecePlacementManager.Instance.IsPlacingPiece())
            {
                PiecePlacementManager.Instance.TryPlacePiece(tile);
                return;
            }

            if (selectedPiece == null)
            {
                if (tile.HasPiece() && tile.GetPiece().Owner == GameManager.Instance.GetCurrentPlayer())
                {
                    SelectPiece(tile.GetPiece());
                }
            }
            else
            {
                if (legalMoveCoordinates.Contains(tile.GetCoordinate()))
                {
                    MoveSelectedPiece(tile);
                }
                else
                {
                    DeselectPiece();
                }
            }
        }

        private void HandlePieceClick(Piece piece)
        {
            if (piece == null || piece.Owner != GameManager.Instance.GetCurrentPlayer())
                return;

            SelectPiece(piece);
        }

        private void SelectPiece(Piece piece)
        {
            ClearHighlights();
            selectedPiece = piece;

            HighlightLegalMoves(piece);
        }

        private void HighlightLegalMoves(Piece piece)
        {
            ClearHighlights();
            legalMoveCoordinates.Clear();

            if (piece == null)
                return;

            List<int> legalMoves = MovementManager.Instance.GetLegalMoves(piece);

            foreach (int coord in legalMoves)
            {
                Vector2Int gridPos = BoardUtils.FromCoordinate(coord);
                Tile tile = BoardManager.Instance.GetTileAt(gridPos.x, gridPos.y);

                if (tile != null)
                {
                    tile.EnableHighlight();
                    highlightedTiles.Add(tile);
                    legalMoveCoordinates.Add(tile.GetCoordinate());
                }
            }
        }

        private void ClearHighlights()
        {
            foreach (Tile tile in highlightedTiles)
            {
                if (tile != null)
                    tile.DisableHighlight();
            }
            highlightedTiles.Clear();
            legalMoveCoordinates.Clear();
        }

        private void DeselectPiece()
        {
            ClearHighlights();
            selectedPiece = null;
            lastHoveredTile = null;
        }

        private void MoveSelectedPiece(Tile destinationTile)
        {
            if (selectedPiece == null || destinationTile == null)
                return;

            int startCoord = selectedPiece.GetPosition();
            int destCoord = destinationTile.GetCoordinate();

            BoardManager.Instance.MovePiece(selectedPiece, destCoord);

            HarmonyManager.Instance.UpdateHarmoniesFor(selectedPiece);

            selectedPiece.transform.position = destinationTile.transform.position + Vector3.up * 0.1f;

            Tile startTile = BoardManager.Instance.GetTileAt((startCoord % 20) - 9, (startCoord / 20) - 9);
            if (startTile != null)
                startTile.SetPiece(null);

            destinationTile.SetPiece(selectedPiece);

            if (selectedPiece.Type == PieceType.Boat)
                TryBoatPush(startCoord, destCoord, selectedPiece);

            MovementManager.Instance.RegisterMove(selectedPiece);
            if (GameLogManager.Instance != null)
                GameLogManager.Instance.LogMove(selectedPiece.Owner, selectedPiece.Type, startCoord, destCoord);

            GameManager.Instance.MarkTurnComplete();
            CaptureManager.Instance.CheckForCaptures(selectedPiece);

            if (selectedPiece.CausesRotation())
                WheelRotationManager.Instance?.RotateAdjacentTiles(selectedPiece);

            Piece moved = selectedPiece;
            DeselectPiece();

            if (!VictoryManager.Instance.CheckForHarmonyRingEnd(moved.Owner, BoardManager.Instance.GetAllPieces()))
            {
                GameManager.Instance.EndTurn();
            }
        }

        /// <summary>
        /// If the boat path crossed an occupied tile, push that piece one step further
        /// in the same direction (when the landing square is empty and legal).
        /// </summary>
        private void TryBoatPush(int startCoord, int destCoord, Piece boat)
        {
            Vector2Int start = BoardUtils.FromCoordinate(startCoord);
            Vector2Int dest = BoardUtils.FromCoordinate(destCoord);
            int dx = Mathf.Clamp(dest.x - start.x, -1, 1);
            int dz = Mathf.Clamp(dest.y - start.y, -1, 1);
            if (dx == 0 && dz == 0)
                return;

            // Walk from start toward dest; the first occupied cell (not the boat) is the push target.
            int cur = startCoord;
            int guard = 0;
            while (cur != destCoord && guard++ < 12)
            {
                Vector2Int g = BoardUtils.FromCoordinate(cur);
                g.x += dx;
                g.y += dz;
                int next = BoardUtils.ToCoordinate(g.x, g.y);
                Piece blocker = BoardManager.Instance.GetPieceAt(next);
                if (blocker != null && blocker != boat)
                {
                    Vector2Int pushGrid = new Vector2Int(g.x + dx, g.y + dz);
                    int pushCoord = BoardUtils.ToCoordinate(pushGrid.x, pushGrid.y);
                    if (!BoardUtils.LegalPoints.Contains(pushCoord) || BoardManager.Instance.IsOccupied(pushCoord))
                    {
                        Debug.Log($"[Boat] Cannot push {blocker.Type}; destination blocked.");
                        return;
                    }

                    Tile fromTile = BoardManager.Instance.GetTileAt(g.x, g.y);
                    Tile toTile = BoardManager.Instance.GetTileAt(pushGrid.x, pushGrid.y);
                    BoardManager.Instance.MovePiece(blocker, pushCoord);
                    if (fromTile != null) fromTile.SetPiece(null);
                    if (toTile != null)
                    {
                        toTile.SetPiece(blocker);
                        blocker.transform.position = toTile.transform.position + Vector3.up * 0.1f;
                    }
                    HarmonyManager.Instance.UpdateHarmoniesFor(blocker);
                    Debug.Log($"[Boat] Pushed {blocker.Type} to {pushGrid}");
                    return;
                }
                cur = next;
            }
        }

        private void HandleHover()
        {
            if (selectedPiece != null)
                return;

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit, 100f, tileLayerMask))
            {
                if (hit.collider.TryGetComponent(out Tile hoveredTile))
                {
                    if (hoveredTile != lastHoveredTile)
                    {
                        ClearHighlights();

                        if (hoveredTile.HasPiece())
                        {
                            Piece piece = hoveredTile.GetPiece();

                            if (piece.Owner == GameManager.Instance.GetCurrentPlayer())
                            {
                                HighlightLegalMoves(piece);
                            }
                        }

                        lastHoveredTile = hoveredTile;
                    }
                }
            }
            else
            {
                if (lastHoveredTile != null)
                {
                    ClearHighlights();
                    lastHoveredTile = null;
                }
            }
        }
    }
}
