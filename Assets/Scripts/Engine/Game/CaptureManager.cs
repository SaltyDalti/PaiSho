using System.Collections.Generic;
using UnityEngine;
using PaiSho.Pieces;
using PaiSho.Board;

namespace PaiSho.Game
{
    public class CaptureManager : MonoBehaviour
    {
        public static CaptureManager Instance;

        private void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(gameObject);
            else
                Instance = this;
        }

        /// <summary>
        /// Checks adjacent pieces around the placed or moved piece, and captures enemy pieces in disharmony.
        /// Merges former PieceCaptureManager behavior (season immunity + pot recording).
        /// </summary>
        public void CheckForCaptures(Piece placedPiece)
        {
            if (placedPiece == null)
                return;

            List<int> neighbors = BoardManager.Instance.GetAdjacentCoordinates(placedPiece.GetPosition());

            foreach (int coord in neighbors)
            {
                Piece neighbor = BoardManager.Instance.GetPieceAt(coord);

                if (neighbor == null)
                    continue;

                if (neighbor.Owner == placedPiece.Owner)
                    continue;

                if (!HarmonyManager.Instance.IsDisharmony(placedPiece, neighbor))
                    continue;

                TryCapture(placedPiece, neighbor);
            }
        }

        public bool TryCapture(Piece attacker, Piece target)
        {
            if (attacker == null || target == null)
                return false;

            if (!target.CanBeCaptured())
            {
                Debug.Log($">>> {target.Type} is immune to capture during {SeasonManager.Instance.GetCurrentSeason()}.");
                return false;
            }

            int targetCoord = target.GetPosition();
            Vector2Int grid = BoardUtils.FromCoordinate(targetCoord);
            Tile tile = BoardManager.Instance.GetTileAt(grid.x, grid.y);
            if (tile != null && tile.GetPiece() == target)
                tile.SetPiece(null);

            PotManager.Instance?.RecordCapture(attacker.Owner, target);
            DebugLogger.Log($">>> {attacker.Type} captured {target.Type} from {target.Owner} at {targetCoord}.");
            BoardManager.Instance.RemovePiece(target);
            return true;
        }
    }
}
