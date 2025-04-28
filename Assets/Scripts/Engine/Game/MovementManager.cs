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
                    legalMoves.AddRange(GetStraightLineMoves(currentCoord, 6, true));
                    break;

                case PieceType.Rock:
                case PieceType.Knotweed:
                    break; // No movement

                case PieceType.Wheel:
                    legalMoves.AddRange(GetStraightLineMoves(currentCoord, 999)); // Effectively unlimited straight
                    break;

                case PieceType.Boat:
                    legalMoves.AddRange(GetStraightLineMoves(currentCoord, 6)); // Pushing handled elsewhere
                    break;
            }

            return legalMoves;
        }

        private List<int> GetStraightLineMoves(int start, int range, bool canJump = false)
        {
            List<int> moves = new List<int>();
            int[] directions = { -20, 20, -1, 1 }; // Up, Down, Left, Right

            foreach (var dir in directions)
            {
                int current = start;
                for (int step = 0; step < range; step++)
                {
                    current += dir;
                    if (!BoardManager.Instance.IsLegalPosition(current))
                        break;

                    if (!BoardManager.Instance.IsOccupied(current))
                        moves.Add(current);
                    else
                    {
                        if (canJump)
                            continue;
                        else
                            break;
                    }
                }
            }

            return moves;
        }

        private List<int> GetStraightAndDiagonalMoves(int start, int range)
        {
            List<int> moves = new List<int>();
            int[] directions = { -20, 20, -1, 1, -21, -19, 19, 21 }; // All straight + diagonals

            foreach (var dir in directions)
            {
                int current = start;
                for (int step = 0; step < range; step++)
                {
                    current += dir;
                    if (!BoardManager.Instance.IsLegalPosition(current))
                        break;

                    if (!BoardManager.Instance.IsOccupied(current))
                        moves.Add(current);
                    else
                        break;
                }
            }

            return moves;
        }

        private List<int> GetLShapeMoves(int start)
        {
            List<int> moves = new List<int>();

            var lPaths = new (int firstDir, int secondDir)[]
            {
                (-20, -1), (-20, 1),
                (20, -1), (20, 1),
                (-1, -20), (-1, 20),
                (1, -20), (1, 20)
            };

            foreach (var path in lPaths)
            {
                int midStep = start + path.firstDir;
                int finalStep = midStep + path.firstDir;
                int target = finalStep + path.secondDir;

                if (!BoardManager.Instance.IsLegalPosition(midStep))
                    continue;
                if (!BoardManager.Instance.IsLegalPosition(finalStep))
                    continue;
                if (!BoardManager.Instance.IsLegalPosition(target))
                    continue;

                if (BoardManager.Instance.IsOccupied(midStep))
                    continue;
                if (BoardManager.Instance.IsOccupied(finalStep))
                    continue;
                if (BoardManager.Instance.IsOccupied(target))
                    continue;

                moves.Add(target);
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
            movedThisTurn.Clear();
            placedThisTurn.Clear();
        }
    }
}
