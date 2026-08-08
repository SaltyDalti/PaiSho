using UnityEngine;
using PaiSho.Board;
using PaiSho.Pieces;

namespace PaiSho.Game
{
    public class HarmonyManager : MonoBehaviour
    {
        public static HarmonyManager Instance;

        private void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(gameObject);
            else
                Instance = this;
        }

        public bool IsHarmony(Piece a, Piece b)
        {
            if (a == null || b == null || a.IsGhost || b.IsGhost)
                return false;

            if (IsFlowerBlockedInGate(a) || IsFlowerBlockedInGate(b))
                return false;

            if (IsAdjacentToKnotweed(a) || IsAdjacentToKnotweed(b))
                return false;

            if (!a.CanFormHarmony() || !b.CanFormHarmony())
                return false;

            if (a.Owner != b.Owner)
                return false;

            if (!a.CanHarmonizeWith(b))
                return false;

            if (!a.CanContributeToHarmony() || !b.CanContributeToHarmony())
                return false;

            return AreAlignedWithoutBlockers(a.BoardCoordinate, b.BoardCoordinate);
        }

        private static bool AreAlignedWithoutBlockers(int from, int to)
        {
            if (from == to)
                return false;

            int rowDelta = BoardUtils.GetRow(to) - BoardUtils.GetRow(from);
            int colDelta = BoardUtils.GetColumn(to) - BoardUtils.GetColumn(from);

            if (rowDelta != 0 && colDelta != 0)
                return false;

            int rowStep = rowDelta == 0 ? 0 : (rowDelta > 0 ? 1 : -1);
            int colStep = colDelta == 0 ? 0 : (colDelta > 0 ? 1 : -1);
            int row = BoardUtils.GetRow(from) + rowStep;
            int col = BoardUtils.GetColumn(from) + colStep;

            while (row != BoardUtils.GetRow(to) || col != BoardUtils.GetColumn(to))
            {
                int coordinate = row * BoardUtils.GridSize + col;
                if (BoardManager.Instance.GetPieceAt(coordinate) != null)
                    return false;

                row += rowStep;
                col += colStep;
            }

            return true;
        }

        public bool IsDisharmony(Piece a, Piece b)
        {
            if (a == null || b == null || a.IsGhost || b.IsGhost)
                return false;

            if (IsFlowerBlockedInGate(a) || IsFlowerBlockedInGate(b))
                return false;

            if (!a.CanBeDisharmonized() || !b.CanBeDisharmonized())
                return false;

            if (!a.CanFormDisharmony() || !b.CanFormDisharmony())
                return false;

            if (a.Owner == b.Owner)
                return false;

            var profileA = PieceHarmonyProfiles.Get(a.Type);
            var profileB = PieceHarmonyProfiles.Get(b.Type);
            return profileA.Disharmonic.Contains(b.Type) || profileB.Disharmonic.Contains(a.Type);
        }

        private static bool IsFlowerBlockedInGate(Piece piece)
        {
            return piece != null
                && PieceRules.IsFlower(piece.Type)
                && BoardUtils.IsGate(piece.BoardCoordinate);
        }

        private bool IsAdjacentToKnotweed(Piece piece)
        {
            foreach (int check in BoardUtils.GetAdjacentCoordinates(piece.GetPosition()))
            {
                Piece neighbor = BoardManager.Instance.GetPieceAt(check);
                if (neighbor != null && neighbor.Type == PieceType.Knotweed)
                    return true;
            }

            return false;
        }
    }
}
