using System.Collections.Generic;
using PaiSho.Board;
using PaiSho.Pieces;

namespace PaiSho.Game
{
    public static class ActionGenerator
    {
        public static List<GameAction> GetAllLegalActions(Player player)
        {
            var actions = new List<GameAction>();

            if (BoardManager.Instance == null || PlacementValidator.Instance == null)
                return actions;

            if (GameStateManager.Instance != null && GameStateManager.Instance.IsSpringPhase())
                return GetSpringActions(player);

            AddPlayPhaseActions(player, actions);
            AddBoatAndWheelActions(player, actions);
            AddMomentumActions(player, actions);
            return actions;
        }

        private static List<GameAction> GetSpringActions(Player player)
        {
            var actions = new List<GameAction>();

            if (ReserveManager.Instance == null)
                return actions;

            PieceType? drawn = ReserveManager.Instance.GetSpringDrawnFlower(player);
            if (!drawn.HasValue)
                return actions;

            foreach (int coordinate in PlacementValidator.Instance.GetLegalPlacements(player, drawn.Value))
                actions.Add(GameAction.Place(player, drawn.Value, coordinate));

            return actions;
        }

        private static void AddPlayPhaseActions(Player player, List<GameAction> actions)
        {
            foreach (Piece piece in BoardManager.Instance.GetAllPieces())
            {
                if (piece.Owner != player)
                    continue;

                if (!CanActWithPiece(player, piece))
                    continue;

                foreach (LegalMove move in PlacementValidator.Instance.GetLegalMoves(piece))
                    actions.Add(GameAction.Move(piece, move.Coordinate));
            }

            if (ReserveManager.Instance == null)
                return;

            // Bonus-move turns are move-only — no fresh placements.
            if (GameManager.Instance != null && GameManager.Instance.PendingBonusMove)
                return;

            if (MovementManager.Instance != null &&
                MovementManager.Instance.GetMovedTileCount(player) > 0)
                return;

            foreach (var entry in ReserveManager.Instance.GetHandCounts(player))
            {
                foreach (int coordinate in PlacementValidator.Instance.GetLegalPlacements(player, entry.Key))
                    actions.Add(GameAction.Place(player, entry.Key, coordinate));
            }
        }

        private static void AddBoatAndWheelActions(Player player, List<GameAction> actions)
        {
            if (BoardManager.Instance == null)
                return;

            bool bonusOnly = GameManager.Instance != null && GameManager.Instance.PendingBonusMove;
            if (bonusOnly)
                return;

            if (MovementManager.Instance != null &&
                MovementManager.Instance.GetMovedTileCount(player) > 0)
                return;

            foreach (Piece piece in BoardManager.Instance.GetAllPieces())
            {
                if (piece == null || piece.Owner != player)
                    continue;

                if (piece.Type == PieceType.Wheel && CanActWithPiece(player, piece))
                    actions.Add(GameAction.WheelRotate(piece));

                if (piece.Type != PieceType.Boat || BoatManager.Instance == null)
                    continue;

                if (!CanActWithPiece(player, piece))
                    continue;

                foreach (int neighbor in BoardUtils.GetAdjacentCoordinates(piece.BoardCoordinate))
                {
                    Piece passenger = BoardManager.Instance.GetPieceAt(neighbor);
                    if (passenger != null && BoatManager.Instance.CanLoad(piece, passenger))
                        actions.Add(GameAction.BoatLoad(piece, neighbor));

                    if (BoatManager.Instance.CanUnload(piece, neighbor))
                        actions.Add(GameAction.BoatUnload(piece, neighbor));
                }
            }
        }

        private static void AddMomentumActions(Player player, List<GameAction> actions)
        {
            if (MovementManager.Instance == null || MomentumManager.Instance == null)
                return;

            if (GameManager.Instance != null && GameManager.Instance.PendingBonusMove)
                return;

            if (MovementManager.Instance.PlacedThisTurn(player) ||
                MovementManager.Instance.GetMovedTileCount(player) > 0 ||
                MomentumManager.Instance.GetMomentum(player) <= 0)
                return;

            foreach (Piece piece in BoardManager.Instance.GetAllPieces())
            {
                if (piece.Owner != player)
                    continue;

                if (piece.WiltLevel > 0)
                    actions.Add(GameAction.Revive(player, piece));

                if (piece.WiltLevel > 0 || piece.InHarmony)
                    actions.Add(GameAction.Freeze(player, piece));
            }
        }

        /// <summary>
        /// Peek whether a piece could still act this turn without spending momentum.
        /// </summary>
        private static bool CanActWithPiece(Player player, Piece piece)
        {
            if (piece == null || MovementManager.Instance == null)
                return false;

            if (MovementManager.Instance.HasMoved(piece))
                return false;

            if (piece.Owner != player)
                return false;

            int moved = MovementManager.Instance.GetMovedTileCount();
            if (moved <= 0)
                return true;

            if (moved >= 4)
                return false;

            return MomentumManager.Instance != null && MomentumManager.Instance.HasMomentum(player);
        }
    }
}
