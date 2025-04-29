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

            MovementManager.Instance.RegisterMove(selectedPiece);
            GameManager.Instance.MarkTurnComplete();
            CaptureManager.Instance.CheckForCaptures(selectedPiece);

            DeselectPiece(); // Fully reset after move

            if (!VictoryManager.Instance.CheckForHarmonyRingEnd(GameManager.Instance.GetCurrentPlayer(), BoardManager.Instance.GetAllPieces()))
            {
                GameManager.Instance.EndTurn();
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
