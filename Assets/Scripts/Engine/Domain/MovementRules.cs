using System;
using System.Collections.Generic;
using PaiSho.Pieces;

namespace PaiSho.Domain
{
    /// <summary>
    /// Deterministic legal-move generation. Occupancy is injected so Unity
    /// managers and headless tests share the same rules.
    /// </summary>
    public static class MovementRules
    {
        public static List<int> GetLegalMoves(
            PieceType type,
            int currentCoord,
            int seasonalBonus,
            Func<int, bool> isOccupied)
        {
            if (isOccupied == null)
                throw new ArgumentNullException(nameof(isOccupied));

            seasonalBonus = Math.Max(0, seasonalBonus);
            var legalMoves = new List<int>();

            switch (type)
            {
                case PieceType.Jasmine:
                case PieceType.Rose:
                    legalMoves.AddRange(GetStraightLineMoves(currentCoord, 3 + seasonalBonus, canJump: false, isOccupied));
                    break;

                case PieceType.Lily:
                case PieceType.Chrysanthemum:
                    legalMoves.AddRange(GetLShapeMoves(currentCoord, isOccupied));
                    break;

                case PieceType.Jade:
                case PieceType.Rhododendron:
                    legalMoves.AddRange(GetStraightAndDiagonalMoves(currentCoord, 5 + seasonalBonus, isOccupied));
                    break;

                case PieceType.Lotus:
                    legalMoves.AddRange(GetStraightAndDiagonalMoves(currentCoord, 2 + seasonalBonus, isOccupied));
                    break;

                case PieceType.Orchid:
                    legalMoves.AddRange(GetStraightLineMoves(currentCoord, 6 + seasonalBonus, canJump: true, isOccupied));
                    break;

                case PieceType.Rock:
                case PieceType.Knotweed:
                    break;

                case PieceType.Wheel:
                    legalMoves.AddRange(GetStraightLineMoves(currentCoord, 999, canJump: false, isOccupied));
                    break;

                case PieceType.Boat:
                    legalMoves.AddRange(GetBoatMoves(currentCoord, isOccupied));
                    break;
            }

            return legalMoves;
        }

        public static List<int> GetStraightLineMoves(
            int start,
            int range,
            bool canJump,
            Func<int, bool> isOccupied)
        {
            var moves = new List<int>();

            foreach (int dir in BoardCoords.OrthoDirections)
            {
                int current = start;
                for (int step = 0; step < range; step++)
                {
                    current += dir;

                    if (!BoardCoords.IsValidPointCoordinate(current))
                        break;

                    if (isOccupied(current))
                    {
                        if (canJump)
                            continue;
                        break;
                    }

                    moves.Add(current);
                }
            }

            return moves;
        }

        public static List<int> GetStraightAndDiagonalMoves(
            int start,
            int range,
            Func<int, bool> isOccupied)
        {
            var moves = new List<int>();

            foreach (int dir in BoardCoords.AllDirections)
            {
                int current = start;
                for (int step = 0; step < range; step++)
                {
                    current += dir;

                    if (!BoardCoords.IsValidPointCoordinate(current))
                        break;

                    if (isOccupied(current))
                        break;

                    moves.Add(current);
                }
            }

            return moves;
        }

        public static List<int> GetLShapeMoves(int start, Func<int, bool> isOccupied)
        {
            var moves = new List<int>();
            var lPatterns = new (int dx, int dz)[]
            {
                (2, 1), (2, -1), (-2, 1), (-2, -1),
                (1, 2), (1, -2), (-1, 2), (-1, -2)
            };

            GridPos startGrid = BoardCoords.FromCoordinate(start);

            foreach (var (dx, dz) in lPatterns)
            {
                int targetX = startGrid.X + dx;
                int targetZ = startGrid.Z + dz;
                int midX = startGrid.X + (dx / 2);
                int midZ = startGrid.Z + (dz / 2);

                int midCoord = BoardCoords.ToCoordinate(midX, midZ);
                int targetCoord = BoardCoords.ToCoordinate(targetX, targetZ);

                if (!BoardCoords.IsValidPointCoordinate(midCoord) || isOccupied(midCoord))
                    continue;

                if (!BoardCoords.IsValidPointCoordinate(targetCoord) || isOccupied(targetCoord))
                    continue;

                moves.Add(targetCoord);
            }

            return moves;
        }

        public static List<int> GetBoatMoves(int start, Func<int, bool> isOccupied)
        {
            var moves = new List<int>();

            foreach (int dir in BoardCoords.OrthoDirections)
            {
                int current = start;
                bool hasPushed = false;

                for (int step = 0; step < 6; step++)
                {
                    current += dir;

                    if (!BoardCoords.IsValidPointCoordinate(current))
                        break;

                    if (isOccupied(current))
                    {
                        if (hasPushed)
                            break;
                        hasPushed = true;
                        continue;
                    }

                    moves.Add(current);
                }
            }

            return moves;
        }
    }
}
