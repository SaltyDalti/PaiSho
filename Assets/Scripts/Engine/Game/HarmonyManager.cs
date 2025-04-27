using UnityEngine;
using PaiSho.Pieces;
using PaiSho.Board;

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

        /// <summary>
        /// Determines if two pieces form a harmony.
        /// </summary>
        public bool IsHarmony(Piece a, Piece b)
        {
            if (a.IsGhost || b.IsGhost) return false;
            if (IsAdjacentToKnotweed(a) || IsAdjacentToKnotweed(b)) return false;
            if (!a.CanFormHarmony() || !b.CanFormHarmony()) return false;

            DebugLogger.Log($"Checking harmony between {a.Type} and {b.Type}");
            return a.CanHarmonizeWith(b);
        }

        /// <summary>
        /// Determines if two pieces form a disharmony.
        /// </summary>
        public bool IsDisharmony(Piece a, Piece b)
        {
            if (a.IsGhost || b.IsGhost) return false;
            if (!a.CanBeDisharmonized() || !b.CanBeDisharmonized()) return false;
            if (!a.CanFormDisharmony() || !b.CanFormDisharmony()) return false;

            DebugLogger.Log($"Checking disharmony between {a.Type} and {b.Type}");
            return a.Type != b.Type && a.IsFlower() && b.IsFlower();
        }

        private bool IsAdjacentToKnotweed(Piece piece)
        {
            int[] offsets = { -20, -19, 1, 20, 19, -1, -21, -39 };
            int center = piece.GetPosition();

            foreach (int offset in offsets)
            {
                int check = center + offset;
                Piece neighbor = BoardManager.Instance.GetPieceAt(check);
                if (neighbor != null && neighbor.Type == PieceType.Knotweed)
                    return true;
            }

            return false;
        }
    }
}
