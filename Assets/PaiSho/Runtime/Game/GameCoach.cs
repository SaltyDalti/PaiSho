using UnityEngine;
using PaiSho.Board;

namespace PaiSho.Game
{
    /// <summary>
    /// One-shot onboarding tips for new players — each fires at most once per install (PlayerPrefs),
    /// routed through GameplayFeedback so they read like any other toast. Never spams.
    /// </summary>
    public class GameCoach : MonoBehaviour
    {
        public static GameCoach Instance;

        private const string SpringPlaceKey = "pai_sho_coach_spring_place";
        private const string FirstPlayKey = "pai_sho_coach_first_play";
        private const string FirstHarmonyKey = "pai_sho_coach_first_harmony";
        private const string WiltRiskKey = "pai_sho_coach_wilt_risk";
        private const string FirstMomentumKey = "pai_sho_coach_first_momentum";

        private void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(gameObject);
            else
                Instance = this;
        }

        /// <summary>Central check point — call once per HUD refresh with the player currently at the wheel.</summary>
        public void EvaluateHudState(Player current, bool humanTurn)
        {
            if (!humanTurn || HeadlessActionExecutor.IsActive)
                return;

            if (GameStateManager.Instance != null && GameStateManager.Instance.IsSpringPhase())
            {
                NotifySpringPlacement(current);
                return;
            }

            if (GameStateManager.Instance != null && GameStateManager.Instance.IsPlayPhase())
                NotifyPlayPhaseStarted(current);

            if (GameManager.Instance != null && GameManager.Instance.CountHarmonizedPieces(current) > 0)
                NotifyHarmonyFormed(current);

            if (MomentumManager.Instance != null && MomentumManager.Instance.GetMomentum(current) > 0)
                NotifyFirstMomentum(current);

            if (HasWiltedTile(current))
                NotifyWiltRisk(current);
        }

        private void NotifySpringPlacement(Player player)
        {
            if (!IsHuman(player) || !TryConsume(SpringPlaceKey))
                return;

            GameplayFeedback.Show("Tip: tap a glowing spot on your side of the board to place your Spring flower.", 5.5f);
        }

        private void NotifyPlayPhaseStarted(Player player)
        {
            if (!IsHuman(player) || !TryConsume(FirstPlayKey))
                return;

            GameplayFeedback.Show(
                "Tip: drag tiles from your rack onto a lit space, or place basic flowers through their matching port.",
                5.5f);
        }

        private void NotifyHarmonyFormed(Player player)
        {
            if (!IsHuman(player) || !TryConsume(FirstHarmonyKey))
                return;

            GameplayFeedback.Show(
                "Tip: gold lines are Harmony — close a ring of them around Mid to win.",
                5.5f);
        }

        private void NotifyWiltRisk(Player player)
        {
            if (!IsHuman(player) || !TryConsume(WiltRiskKey))
                return;

            GameplayFeedback.Show(
                "Tip: idle tiles wilt over time. Spend a Momentum token to Revive or Freeze one from the dock.",
                5.5f);
        }

        private void NotifyFirstMomentum(Player player)
        {
            if (!IsHuman(player) || !TryConsume(FirstMomentumKey))
                return;

            GameplayFeedback.Show(
                "Tip: Momentum lets you move again this turn, or Revive / Freeze a tile from the dock.",
                5.5f);
        }

        private static bool HasWiltedTile(Player player)
        {
            if (BoardManager.Instance == null)
                return false;

            foreach (var piece in BoardManager.Instance.GetAllPieces())
            {
                if (piece != null && piece.Owner == player && piece.WiltLevel > 0)
                    return true;
            }

            return false;
        }

        private static bool IsHuman(Player player) =>
            AiController.Instance == null || !AiController.Instance.IsAiPlayer(player);

        private static bool TryConsume(string key)
        {
            if (PlayerPrefs.GetInt(key, 0) != 0)
                return false;

            PlayerPrefs.SetInt(key, 1);
            PlayerPrefs.Save();
            return true;
        }
    }
}
