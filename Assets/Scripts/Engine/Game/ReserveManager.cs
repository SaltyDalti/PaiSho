using System.Collections.Generic;
using UnityEngine;
using PaiSho.Pieces;

namespace PaiSho.Game
{
    public class ReserveManager : MonoBehaviour
    {
        public static ReserveManager Instance;

        private Dictionary<Player, List<PieceType>> reservedPieces = new Dictionary<Player, List<PieceType>>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(gameObject);
            else
                Instance = this;

            reservedPieces[Player.Host] = new List<PieceType>();
            reservedPieces[Player.Opponent] = new List<PieceType>();
        }

        public bool HasTile(Player player, PieceType type)
        {
            return reservedPieces[player].Contains(type);
        }

        public void RemoveFromReserve(Player player, PieceType type)
        {
            reservedPieces[player].Remove(type);
        }

        /// <summary>
        /// Add a piece to a player's reserve.
        /// </summary>
        public void AddPieceToReserve(Player player, PieceType type)
        {
            reservedPieces[player].Add(type);
        }

        /// <summary>
        /// Remove a piece from a player's reserve after placement.
        /// </summary>
        public void RemovePieceFromReserve(Player player, PieceType type)
        {
            reservedPieces[player].Remove(type);
        }

        /// <summary>
        /// Get all reserved pieces for a player.
        /// </summary>
        public List<PieceType> GetReservedPieces(Player player)
        {
            return new List<PieceType>(reservedPieces[player]);
        }

        /// <summary>
        /// Check if a player has a specific piece reserved.
        /// </summary>
        public bool HasPiece(Player player, PieceType type)
        {
            return reservedPieces[player].Contains(type);
        }
    }
}
