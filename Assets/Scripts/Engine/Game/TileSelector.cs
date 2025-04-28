using System.Collections.Generic;
using UnityEngine;
using PaiSho.Pieces;
using PaiSho.Board;

namespace PaiSho.Game
{
    public class TileSelector : MonoBehaviour
    {
        public LayerMask tileLayerMask;
        private Piece selectedPiece;
        private List<Tile> highlightedTiles = new List<Tile>();

        private Tile lastHoveredTile = null;
        private Piece lastHoveredPiece = null;

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, 100f, tileLayerMask))
                {
                    Debug.Log($"Raycast hit: {hit.collider.gameObject.name}");
                    Tile clickedTile = hit.collider.GetComponent<Tile>();
                    if (clickedTile != null)
                    {
                        Debug.Log($"Tile clicked: {clickedTile.GetGridPosition()}");
                        HandleTileClick(clickedTile);
                    }
                }
            }

            HandleHover();
        }


        private void HandleTileClick(Tile tile)
        {
            if (GameManager.Instance.IsSpringPhase())
            {
                // During Spring Phase, immediately place correct flower (Jasmine/Rose)
                PiecePlacementManager.Instance.TryPlacePiece(tile);
                return;
            }

            if (PiecePlacementManager.Instance.IsPlacingPiece())
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
                if (highlightedTiles.Contains(tile))
                {
                    MoveSelectedPiece(tile);
                }
                else
                {
                    DeselectPiece();
                }
            }
        }



        private void SelectPiece(Piece piece)
        {
            selectedPiece = piece;
            HighlightLegalMoves(piece);
        }

        private void DeselectPiece()
        {
            ClearHighlights();
            selectedPiece = null;
        }

        private void HighlightLegalMoves(Piece piece)
        {
            ClearHighlights();
            var legalMoves = MovementManager.Instance.GetLegalMoves(piece);

            foreach (var coord in legalMoves)
            {
                int x = (coord % 20) - 9;
                int z = (coord / 20) - 9;

                Tile tile = BoardManager.Instance.GetTileAt(x, z);
                if (tile != null)
                {
                    tile.EnableHighlight();
                    highlightedTiles.Add(tile);
                }
            }
        }


        private void ClearHighlights()
        {
            foreach (var tile in highlightedTiles)
            {
                tile.DisableHighlight();
            }
            highlightedTiles.Clear();
        }

        private void MoveSelectedPiece(Tile destinationTile)
        {
            BoardManager.Instance.MovePiece(selectedPiece, destinationTile.GetCoordinate());

            CaptureManager.Instance.CheckForCaptures(selectedPiece); // <-- ADD THIS

            DeselectPiece();

            if (!VictoryManager.Instance.CheckForHarmonyRingEnd(GameManager.Instance.GetCurrentPlayer(), BoardManager.Instance.GetAllPieces()))
            {
                GameManager.Instance.EndTurn();
            }
        }

        private void HandleHover()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, tileLayerMask))
            {
                Tile hoveredTile = hit.collider.GetComponent<Tile>();

                if (hoveredTile != null)
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
                                lastHoveredPiece = piece;
                            }
                        }

                        lastHoveredTile = hoveredTile;
                    }
                }
            }
            else
            {
                // Nothing hovered
                if (lastHoveredTile != null)
                {
                    ClearHighlights();
                    lastHoveredTile = null;
                    lastHoveredPiece = null;
                }
            }
        }
    }
}
