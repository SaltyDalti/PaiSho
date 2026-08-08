using System.Collections.Generic;
using UnityEngine;
using PaiSho.Pieces;
using PaiSho.Board;
using PaiSho.Domain;

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
        /// Targeting legality is Domain <see cref="CaptureRules"/>; pot/remove stay here.
        /// </summary>
        public void CheckForCaptures(Piece placedPiece)
        {
            if (placedPiece == null || BoardManager.Instance == null)
                return;

            BoardPiece attacker = ToBoardPiece(placedPiece);
            List<BoardPiece> snapshot = SnapshotBoard();
            GardenSeason season = SeasonMapping.Current();

            foreach (CaptureOpportunity opportunity in CaptureRules.FindCaptureTargets(attacker, snapshot, season))
            {
                Piece neighbor = BoardManager.Instance.GetPieceAt(opportunity.Target.Coordinate);
                if (neighbor != null)
                    TryCapture(placedPiece, neighbor);
            }
        }

        public bool TryCapture(Piece attacker, Piece target)
        {
            if (attacker == null || target == null)
                return false;

            CaptureDenyReason reason = CaptureRules.EvaluatePair(
                ToBoardPiece(attacker),
                ToBoardPiece(target),
                SeasonMapping.Current());

            if (reason == CaptureDenyReason.SeasonImmune)
            {
                Debug.Log($">>> {target.Type} is immune to capture during {SeasonMapping.Current()}.");
                return false;
            }

            if (reason != CaptureDenyReason.None)
                return false;

            int targetCoord = target.GetPosition();
            Vector2Int grid = BoardUtils.FromCoordinate(targetCoord);
            Tile tile = BoardManager.Instance.GetTileAt(grid.x, grid.y);
            if (tile != null && tile.GetPiece() == target)
                tile.SetPiece(null);

            PotManager.Instance?.RecordCapture(attacker.Owner, target);
            BloomingManager.Instance?.AddToPot(target);
            DebugLogger.Log($">>> {attacker.Type} captured {target.Type} from {target.Owner} at {targetCoord}.");
            BoardManager.Instance.RemovePiece(target);
            return true;
        }

        private static BoardPiece ToBoardPiece(Piece piece)
        {
            return new BoardPiece(
                PlacementValidator.ToSeat(piece.Owner),
                piece.Type,
                piece.GetPosition(),
                piece.IsGhost,
                piece.IsBlooming());
        }

        private static List<BoardPiece> SnapshotBoard()
        {
            List<Piece> all = BoardManager.Instance.GetAllPieces();
            var snapshot = new List<BoardPiece>(all.Count);
            foreach (Piece piece in all)
            {
                if (piece == null)
                    continue;
                snapshot.Add(ToBoardPiece(piece));
            }
            return snapshot;
        }
    }
}
