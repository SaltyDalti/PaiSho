using System.Collections.Generic;
using UnityEngine;
using PaiSho.Pieces;

namespace PaiSho.Game
{
    public class MomentumManager : MonoBehaviour
    {
        public static MomentumManager Instance;

        private Dictionary<Player, int> momentumTokens = new Dictionary<Player, int>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(gameObject);
            else
                Instance = this;

            momentumTokens[Player.Host] = 0;
            momentumTokens[Player.Opponent] = 0;
        }

        /// <summary>
        /// Award a momentum token to a player.
        /// </summary>
        public void AwardMomentum(Player player, string reason)
        {
            momentumTokens[player]++;
            Debug.Log($"{player} gained a Momentum token for: {reason}");
        }

        /// <summary>
        /// Attempt to spend a momentum token for a special action.
        /// </summary>
        public bool TrySpendMomentum(Player player, string reason)
        {
            if (momentumTokens[player] > 0)
            {
                momentumTokens[player]--;
                DebugLogger.Log($">>> {player} spent a Momentum Token for: {reason}");
                return true;
            }

            DebugLogger.LogWarning($">>> {player} tried to spend Momentum but has none.");
            return false;
        }

        /// <summary>
        /// Spend a momentum token to revive a wilted tile.
        /// </summary>
        public bool SpendReviveTile(Player player, Piece piece)
        {
            if (!TrySpendMomentum(player, "Revive Wilted Tile")) return false;

            piece.WiltLevel = 0;
            piece.PointValue = 1;
            piece.TurnsSinceMoved = 0;
            piece.TurnsSinceHarmonized = 0;
            piece.SetVisualState("vibrant");

            DebugLogger.Log($">>> {player} revived {piece.Type} using momentum!");
            return true;
        }

        /// <summary>
        /// Spend a momentum token to prevent wilting decay next turn.
        /// </summary>
        public bool SpendFreezeWilt(Player player, Piece piece)
        {
            if (!TrySpendMomentum(player, "Freeze Wilt Decay")) return false;

            piece.FreezeWiltNextTurn = true;
            DebugLogger.Log($">>> {player} protected {piece.Type} from wilting this turn.");
            return true;
        }

        public bool HasMomentum(Player player)
        {
            return momentumTokens.ContainsKey(player) && momentumTokens[player] > 0;
        }

        public int GetMomentum(Player player)
        {
            return momentumTokens[player];
        }

        /// <summary>
        /// Evaluate turn bonuses at the end of a player's turn.
        /// </summary>
        public void EvaluateTurnBonuses(Player player, List<Piece> allPieces)
        {
            int harmonyCount = 0;
            foreach (var piece in allPieces)
            {
                if (piece.Owner != player) continue;

                foreach (var other in allPieces)
                {
                    if (other == piece || other.Owner != player) continue;

                    if (HarmonyManager.Instance.IsHarmony(piece, other))
                    {
                        harmonyCount++;
                        break;
                    }
                }

                if (SeasonManager.Instance.IsInSeason(piece.Type))
                    AwardMomentum(player, "Seasonal Bloom Placement");
            }

            if (harmonyCount >= 3)
                AwardMomentum(player, "Harmony Chain (3+ harmonies)");
        }

        /// <summary>
        /// Award a bonus number of momentum tokens directly.
        /// </summary>
        public void AwardBonus(Player player, int count)
        {
            if (count <= 0) return;
            if (!momentumTokens.ContainsKey(player)) momentumTokens[player] = 0;
            momentumTokens[player] += count;
        }

        public int GetTotalEarned(Player player)
        {
            return GetMomentum(player); // Total momentum of that player
        }

    }
}
