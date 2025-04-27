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

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, 100f, tileLayerMask))
                {
                    Tile clickedTile = hit.collider.GetComponent<Tile>();
                    if (clickedTile != null)
                    {
                        HandleTileClick(clickedTile);
                    }
                }
            }
        }

        private void HandleTileClick(Tile tile)
        {
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
                Tile tile = BoardManager.Instance.GetTileAt(coord);
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

            DeselectPiece();

            if (!VictoryManager.Instance.CheckForHarmonyRingEnd(GameManager.Instance.GetCurrentPlayer(), BoardManager.Instance.GetAllPieces()))
            {
                GameManager.Instance.EndTurn();
            }
        }
    }
}
