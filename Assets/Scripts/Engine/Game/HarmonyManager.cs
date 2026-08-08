using System.Collections.Generic;
using UnityEngine;
using PaiSho.Pieces;
using PaiSho.Board;
using PaiSho.Domain;

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

            return HarmonyRules.IsHarmony(
                PlacementValidator.ToSeat(a.Owner),
                PlacementValidator.ToSeat(b.Owner),
                a.Type,
                b.Type,
                a.GetPosition(),
                b.GetPosition(),
                a.IsBlooming(),
                b.IsBlooming());
        }

        /// <summary>
        /// Checks if two pieces are in disharmony.
        /// </summary>
        public bool IsDisharmony(Piece a, Piece b)
        {
            if (a == null || b == null)
                return false;

            return HarmonyRules.IsDisharmony(
                PlacementValidator.ToSeat(a.Owner),
                PlacementValidator.ToSeat(b.Owner),
                a.Type,
                b.Type,
                a.IsBlooming(),
                b.IsBlooming());
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
        /// Distance helper between two coordinates (Chebyshev).
        /// </summary>
        public int GetDistance(int coordA, int coordB) =>
            HarmonyRules.ChebyshevDistance(coordA, coordB);
    }
}
