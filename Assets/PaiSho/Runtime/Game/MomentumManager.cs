using System.Collections.Generic;
using UnityEngine;
using PaiSho.Board;
using PaiSho.Pieces;

namespace PaiSho.Game
{
    public class MomentumManager : MonoBehaviour
    {
        public static MomentumManager Instance;

        private readonly Dictionary<Player, int> momentumTokens = new Dictionary<Player, int>();
        private readonly Dictionary<Player, int> totalEarned = new Dictionary<Player, int>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(gameObject);
            else
                Instance = this;

            momentumTokens[Player.Host] = 0;
            momentumTokens[Player.Opponent] = 0;
            totalEarned[Player.Host] = 0;
            totalEarned[Player.Opponent] = 0;
        }

        public int GetTotalEarned(Player player)
        {
            return totalEarned.TryGetValue(player, out int count) ? count : 0;
        }

        public void AwardMomentum(Player player, string reason)
        {
            if (!momentumTokens.ContainsKey(player))
                momentumTokens[player] = 0;

            momentumTokens[player]++;
            if (!totalEarned.ContainsKey(player))
                totalEarned[player] = 0;
            totalEarned[player]++;
            DebugLogger.Log($"{player} gained a Momentum token for: {reason}");

            if (!HeadlessActionExecutor.IsActive && IsHuman(player))
                GameplayFeedback.Show($"+1 Momentum — {reason}", 3.5f);

            // Visual cue on the player's board flowers instead of a sentence toast.
            if (BoardManager.Instance != null)
            {
                foreach (Piece piece in BoardManager.Instance.GetAllPieces())
                {
                    if (piece == null || piece.Owner != player)
                        continue;
                    if (reason.Contains("Seasonal") && SeasonManager.Instance != null &&
                        !SeasonManager.Instance.IsInSeason(piece.Type))
                        continue;
                    if (reason.Contains("Harmony") && !piece.InHarmony)
                        continue;

                    PieceStateAnimator.Ensure(piece)?.NotifyMomentumSpark();
                }
            }
        }

        private static bool IsHuman(Player player) =>
            AiController.Instance == null || !AiController.Instance.IsAiPlayer(player);

        public bool TrySpendMomentum(Player player, string reason)
        {
            if (momentumTokens[player] > 0)
            {
                momentumTokens[player]--;
                DebugLogger.Log($">>> {player} spent a Momentum Token for: {reason}");
                return true;
            }

            GameplayFeedback.Show("No momentum tokens available.");
            return false;
        }

        public bool SpendReviveTile(Player player, Piece piece)
        {
            if (!TrySpendMomentum(player, "Revive Wilted Tile"))
                return false;

            piece.WiltLevel = 0;
            piece.PointValue = 1;
            piece.TurnsSinceMoved = 0;
            piece.TurnsSinceHarmonized = 0;
            piece.SetVisualState("vibrant");
            PieceStateAnimator.Ensure(piece)?.NotifyRevive();
            GameplayVisualizer.Instance?.Refresh();

            DebugLogger.Log($">>> {player} revived {piece.Type} using momentum!");
            return true;
        }

        public bool SpendFreezeWilt(Player player, Piece piece)
        {
            if (!TrySpendMomentum(player, "Freeze Wilt Decay"))
                return false;

            piece.FreezeWiltNextTurn = true;
            PieceStateAnimator.Ensure(piece)?.NotifyFreeze();
            GameplayVisualizer.Instance?.Refresh();
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

        public bool SpendMomentum(Player player)
        {
            if (momentumTokens[player] > 0)
            {
                momentumTokens[player]--;
                return true;
            }

            return false;
        }

        public void AwardBonus(Player player, int count)
        {
            if (count <= 0) return;
            if (!momentumTokens.ContainsKey(player))
                momentumTokens[player] = 0;

            momentumTokens[player] += count;
            totalEarned.TryGetValue(player, out int earned);
            totalEarned[player] = earned + count;
        }

        public void ResetForNewMatch()
        {
            momentumTokens[Player.Host] = 0;
            momentumTokens[Player.Opponent] = 0;
            totalEarned[Player.Host] = 0;
            totalEarned[Player.Opponent] = 0;
        }

        public void EvaluateTurnBonuses(Player player, List<Piece> allPieces)
        {
            int harmonyCount = 0;
            bool awardedSeasonal = false;

            foreach (var piece in allPieces)
            {
                if (piece.Owner != player)
                    continue;

                if (piece.InHarmony)
                    harmonyCount++;

                if (!awardedSeasonal && SeasonManager.Instance.IsInSeason(piece.Type))
                {
                    AwardMomentum(player, "Seasonal Bloom");
                    awardedSeasonal = true;
                }
            }

            if (harmonyCount >= 3)
                AwardMomentum(player, "Harmony Chain (3+ harmonies)");
        }
    }
}
