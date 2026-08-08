using System.Collections.Generic;
using UnityEngine;
using PaiSho.Pieces;
using PaiSho.Domain;

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
            reserves[Player.Host] = StartingReserves.Create();
            reserves[Player.Opponent] = StartingReserves.Create();
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
