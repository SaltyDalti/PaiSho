using System.Collections.Generic;
using UnityEngine;
using PaiSho.Pieces;
using PaiSho.Board;

namespace PaiSho.Game
{
    public class MovementManager : MonoBehaviour
    {
        public static MovementManager Instance;

        private HashSet<Piece> movedThisTurn = new HashSet<Piece>();
        private HashSet<Piece> placedThisTurn = new HashSet<Piece>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(gameObject);
            else
                Instance = this;
        }

        public void RegisterMove(Piece piece)
        {
            movedThisTurn.Add(piece);
            piece.HasMovedThisTurn = true;
        }

        public void RegisterPlacement(Piece piece)
        {
            placedThisTurn.Add(piece);
        }

        public List<int> GetLegalMoves(Piece piece)
        {
            List<int> legalMoves = new List<int>();

            if (piece == null)
                return legalMoves;

            int currentCoord = piece.GetPosition();

            switch (piece.Type)
            {
                case PieceType.Jasmine:
                case PieceType.Rose:
                    legalMoves.AddRange(GetStraightLineMoves(currentCoord, 3));
                    break;

                case PieceType.Lily:
                case PieceType.Chrysanthemum:
                    legalMoves.AddRange(GetLShapeMoves(currentCoord));
                    break;

                case PieceType.Jade:
                case PieceType.Rhododendron:
                    legalMoves.AddRange(GetStraightAndDiagonalMoves(currentCoord, 5));
                    break;

                case PieceType.Lotus:
                    legalMoves.AddRange(GetStraightAndDiagonalMoves(currentCoord, 2));
                    break;

                case PieceType.Orchid:
                    legalMoves.AddRange(GetStraightLineMoves(currentCoord, 6, canJump: true));
                    break;

                case PieceType.Rock:
                case PieceType.Knotweed:
                    // No movement
                    break;

                case PieceType.Wheel:
                    legalMoves.AddRange(GetStraightLineMoves(currentCoord, 999));
                    break;

                case PieceType.Boat:
                    legalMoves.AddRange(GetBoatMoves(currentCoord));
                    break;
            }

            return legalMoves;
        }

        private List<int> GetStraightLineMoves(int start, int range, bool canJump = false)
        {
            List<int> moves = new List<int>();
            int[] directions = { -20, 20, -1, 1 }; // Vertical and horizontal directions only

            foreach (var dir in directions)
            {
                int current = start;
                for (int step = 0; step < range; step++)
                {
                    current += dir;

                    if (!BoardUtils.LegalPoints.Contains(current))
                        break;

                    if (BoardManager.Instance.IsOccupied(current))
                    {
                        if (canJump)
                            continue;
                        else
                            break;
                    }

                    moves.Add(current);

                    if (!canJump && BoardManager.Instance.IsOccupied(current))
                        break;
                }
            }

            return moves;
        }

        private List<int> GetStraightAndDiagonalMoves(int start, int range)
        {
            List<int> moves = new List<int>();
            int[] directions = { -20, 20, -1, 1, -21, -19, 19, 21 }; // Straight and diagonal directions

            foreach (var dir in directions)
            {
                int current = start;
                for (int step = 0; step < range; step++)
                {
                    current += dir;

                    if (!BoardUtils.LegalPoints.Contains(current))
                        break;

                    if (BoardManager.Instance.IsOccupied(current))
                        break;

                    moves.Add(current);
                }
            }

            return moves;
        }

        private List<int> GetLShapeMoves(int start)
        {
            List<int> moves = new List<int>();

            var lPatterns = new (int dx, int dz)[]
            {
                (2, 1), (2, -1), (-2, 1), (-2, -1),
                (1, 2), (1, -2), (-1, 2), (-1, -2)
            };

            Vector2Int startGrid = BoardUtils.FromCoordinate(start);

            foreach (var (dx, dz) in lPatterns)
            {
                int targetX = startGrid.x + dx;
                int targetZ = startGrid.y + dz;

                int midX = startGrid.x + (dx / 2);
                int midZ = startGrid.y + (dz / 2);

                int midCoord = BoardUtils.ToCoordinate(midX, midZ);
                int targetCoord = BoardUtils.ToCoordinate(targetX, targetZ);

                if (!BoardUtils.LegalPoints.Contains(midCoord) || BoardManager.Instance.IsOccupied(midCoord))
                    continue;

                if (!BoardUtils.LegalPoints.Contains(targetCoord) || BoardManager.Instance.IsOccupied(targetCoord))
                    continue;

                moves.Add(targetCoord);
            }

            return moves;
        }

        private List<int> GetBoatMoves(int start)
        {
            List<int> moves = new List<int>();
            int[] directions = { -20, 20, -1, 1 }; // Boat moves straight only

            foreach (var dir in directions)
            {
                int current = start;
                bool hasPushed = false;

                for (int step = 0; step < 6; step++)
                {
                    current += dir;

                    if (!BoardUtils.LegalPoints.Contains(current))
                        break;

                    if (BoardManager.Instance.IsOccupied(current))
                    {
                        if (hasPushed)
                            break; // Cannot push more than one piece
                        else
                        {
                            hasPushed = true;
                            continue;
                        }
                    }

                    moves.Add(current);
                }
            }

            return moves;
        }

        public bool PlacedThisTurn(Player player)
        {
            foreach (var piece in placedThisTurn)
            {
                if (piece.Owner == player)
                    return true;
            }
            return false;
        }

        public int GetMovedTileCount()
        {
            return movedThisTurn.Count;
        }

        public void ClearTurnData()
        {
            foreach (var piece in movedThisTurn)
            {
                piece.HasMovedThisTurn = false;
            }
            movedThisTurn.Clear();
            placedThisTurn.Clear();
        }
    }
}
