using System.Collections.Generic;
using PaiSho.Pieces;
using UnityEngine;

namespace PaiSho.Game
{
    /// <summary>
    /// Short-term memory for anti-thrash and piece diversity.
    /// Cleared on match reset.
    /// </summary>
    public static class AiPlanMemory
    {
        private const int HistorySize = 20;

        private struct RecentMove
        {
            public Player Player;
            public int PieceId;
            public int From;
            public int To;
        }

        private static readonly List<RecentMove> history = new(HistorySize);

        public static void Clear() => history.Clear();

        public static void RecordMove(Player player, Piece piece, int from, int to)
        {
            if (piece == null)
                return;

            if (history.Count >= HistorySize)
                history.RemoveAt(0);

            history.Add(new RecentMove
            {
                Player = player,
                PieceId = piece.GetInstanceID(),
                From = from,
                To = to
            });
        }

        public static int CountRecentMovesByPiece(Player player, Piece piece)
        {
            if (piece == null)
                return 0;

            int pieceId = piece.GetInstanceID();
            int count = 0;
            foreach (RecentMove past in history)
            {
                if (past.Player == player && past.PieceId == pieceId)
                    count++;
            }

            return count;
        }

        public static int CountRecentMovesByPlayer(Player player)
        {
            int count = 0;
            foreach (RecentMove past in history)
            {
                if (past.Player == player)
                    count++;
            }

            return count;
        }

        public static int ScoreRepetitionPenalty(GameAction action, AiScorerWeights w)
        {
            if (action.Kind != GameActionKind.Move || action.Piece == null || w == null)
                return 0;

            int pieceId = action.Piece.GetInstanceID();
            int from = action.Piece.BoardCoordinate;
            int to = action.Coordinate;
            int penalty = 0;
            int recentSamePiece = 0;
            int samePieceCoordHits = 0;
            int ownMoves = 0;

            for (int i = history.Count - 1; i >= 0; i--)
            {
                RecentMove past = history[i];
                if (past.Player != action.Player)
                    continue;

                ownMoves++;
                if (past.PieceId == pieceId && past.From == to && past.To == from)
                    penalty += w.reverseMovePenalty;

                // Same piece ↔ destination (or exact same from→to) thrash.
                if (past.PieceId == pieceId && (past.To == to || past.From == to))
                {
                    samePieceCoordHits++;
                    penalty += w.samePieceCoordRepeatPenalty;
                }

                if (past.PieceId == pieceId && past.From == from && past.To == to)
                    penalty += w.samePieceCoordRepeatPenalty;

                if (past.PieceId == pieceId)
                    recentSamePiece++;
            }

            if (recentSamePiece > 0)
                penalty += recentSamePiece * w.samePieceRepeatPenalty;

            if (samePieceCoordHits >= 2)
                penalty += w.samePieceCoordRepeatPenalty;

            // Monopoly: one tile eating the turn history.
            if (ownMoves >= 4 && recentSamePiece * 2 >= ownMoves)
                penalty += w.monopolyMovePenalty * (1 + recentSamePiece / 2);

            for (int i = history.Count - 1; i >= 0; i--)
            {
                if (history[i].Player != action.Player)
                    continue;

                if (history[i].PieceId == pieceId)
                    penalty += w.samePieceRepeatPenalty;
                break;
            }

            return -penalty;
        }

        public static int ScoreDiversityBonus(GameAction action, AiScorerWeights w)
        {
            if (action.Kind != GameActionKind.Move || action.Piece == null || w == null)
                return 0;

            int recent = CountRecentMovesByPiece(action.Player, action.Piece);
            if (recent == 0)
                return w.unusedPieceBonus;

            if (recent == 1)
                return w.unusedPieceBonus / 3;

            return -Mathf.Min(recent, 6) * (w.samePieceRepeatPenalty / 4);
        }
    }
}
