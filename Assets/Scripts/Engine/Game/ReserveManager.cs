using System.Collections.Generic;
using UnityEngine;
using PaiSho.Pieces;

namespace PaiSho.Game
{
    public class ReserveManager : MonoBehaviour
    {
        public static ReserveManager Instance;

        private Dictionary<Player, Dictionary<PieceType, int>> reserves = new Dictionary<Player, Dictionary<PieceType, int>>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(gameObject);
            else
                Instance = this;

            InitializeReserves();
        }

        private void InitializeReserves()
        {
            reserves[Player.Host] = new Dictionary<PieceType, int>();
            reserves[Player.Opponent] = new Dictionary<PieceType, int>();

            // Flowers - 6 each
            PieceType[] flowers = { PieceType.Jasmine, PieceType.Lily, PieceType.Jade, PieceType.Rose, PieceType.Rhododendron, PieceType.Chrysanthemum };
            foreach (var flower in flowers)
            {
                reserves[Player.Host][flower] = 6;
                reserves[Player.Opponent][flower] = 6;
            }

            // Non-Flowers and Special Flowers - 3 each
            PieceType[] nonFlowers = { PieceType.Boat, PieceType.Rock, PieceType.Knotweed, PieceType.Wheel, PieceType.Lotus, PieceType.Orchid };
            foreach (var type in nonFlowers)
            {
                reserves[Player.Host][type] = 3;
                reserves[Player.Opponent][type] = 3;
            }
        }

        public bool HasPieceAvailable(Player player, PieceType type)
        {
            return reserves[player].ContainsKey(type) && reserves[player][type] > 0;
        }

        public void UsePiece(Player player, PieceType type)
        {
            if (HasPieceAvailable(player, type))
            {
                reserves[player][type]--;
            }
            else
            {
                Debug.LogError($"Player {player} tried to use unavailable piece {type}");
            }
        }

        public void ReturnPiece(Player player, PieceType type)
        {
            if (reserves[player].ContainsKey(type))
            {
                reserves[player][type]++;
            }
        }
    }
}
