using System.Collections.Generic;
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
        /// Checks if two pieces are in harmony.
        /// </summary>
        public bool IsHarmony(Piece a, Piece b)
        {
            if (a == null || b == null)
                return false;

            if (a.Owner != b.Owner)
                return false;

            if (!a.CanHarmonizeWith(b))
                return false;

            int distance = GetDistance(a.GetPosition(), b.GetPosition());

            return distance == 1 || distance == 19 || distance == 20 || distance == 21;
        }

        /// <summary>
        /// Checks if two pieces are in disharmony.
        /// </summary>
        public bool IsDisharmony(Piece a, Piece b)
        {
            if (a == null || b == null)
                return false;

            if (a.Owner == b.Owner)
                return false;

            return !a.CanHarmonizeWith(b);
        }

        /// <summary>
        /// Called when a piece moves. Re-evaluate harmonies.
        /// </summary>
        public void UpdateHarmoniesFor(Piece movedPiece)
        {
            if (movedPiece == null)
                return;

            List<Piece> allPieces = BoardManager.Instance.GetAllPieces();

            foreach (var other in allPieces)
            {
                if (other == movedPiece)
                    continue;

                bool wasInHarmony = movedPiece.IsInHarmonyWith(other);
                bool nowInHarmony = IsHarmony(movedPiece, other);

                if (nowInHarmony && !wasInHarmony)
                {
                    movedPiece.AddHarmony(other);
                    other.AddHarmony(movedPiece);
                    DebugLogger.Log($"[Harmony] Formed: {movedPiece.Type} and {other.Type}");
                }
                else if (!nowInHarmony && wasInHarmony)
                {
                    movedPiece.RemoveHarmony(other);
                    other.RemoveHarmony(movedPiece);
                    DebugLogger.Log($"[Harmony] Broken: {movedPiece.Type} and {other.Type}");
                }
            }
        }

        /// <summary>
        /// Distance helper between two coordinates.
        /// </summary>
        private int GetDistance(int coordA, int coordB)
        {
            Vector2Int a = BoardUtils.FromCoordinate(coordA);
            Vector2Int b = BoardUtils.FromCoordinate(coordB);

            int dx = Mathf.Abs(a.x - b.x);
            int dz = Mathf.Abs(a.y - b.y);

            return Mathf.Max(dx, dz);
        }
    }
}
