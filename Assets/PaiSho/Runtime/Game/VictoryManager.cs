using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PaiSho;
using PaiSho.Pieces;

namespace PaiSho.Game
{
    public class VictoryManager : MonoBehaviour
    {
        public static VictoryManager Instance;

        private const float LiveCelebrationSeconds = 2.8f;

        private bool celebrationRunning;

        private void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(gameObject);
            else
                Instance = this;
        }

        public bool IsCelebrating => celebrationRunning;

        public bool CheckForHarmonyRingEnd(List<Piece> pieces)
        {
            if (celebrationRunning)
                return true;

            foreach (Player player in new[] { Player.Host, Player.Opponent })
            {
                if (!HarmonyRingDetector.HasCompleteRing(player))
                    continue;

                DebugLogger.Log($">>> {player} formed a harmonic ring around the middle gate! Game ends.");
                BeginRingVictory(player);
                return true;
            }

            return false;
        }

        public bool CheckForHarmonyRingEnd(Player player, List<Piece> pieces) =>
            CheckForHarmonyRingEnd(pieces);

        private void BeginRingVictory(Player ringCreator)
        {
            celebrationRunning = true;

            if (GameStateManager.Instance != null && !GameStateManager.Instance.IsEndPhase())
                GameStateManager.Instance.SetPhase(GamePhase.End);

            GameInputController.Instance?.ClearSelection();
            LegalMoveHighlighter.Instance?.Clear();
            AiController.Instance?.StopThinking();

            // Headless / self-play: resolve winner immediately so digests never see Incomplete.
            if (HeadlessActionExecutor.IsActive || HeadlessActionExecutor.SkipPresentation)
            {
                FinishRingVictory(ringCreator);
                return;
            }

            StartCoroutine(CelebrateAndEnd(ringCreator));
        }

        private IEnumerator CelebrateAndEnd(Player ringCreator)
        {
            if (HarmonyRingDetector.TryGetCompleteRing(ringCreator, out List<Piece> ringPieces))
            {
                foreach (Piece piece in ringPieces)
                    PieceStateAnimator.Ensure(piece)?.NotifyVictory();
            }

            SceneLifeAnimator.PulseSeasonChange();
            GameplayFeedback.Show($"{ringCreator} completed a Harmony Ring!");
            GameplayVisualizer.Instance?.Refresh();

            yield return new WaitForSeconds(LiveCelebrationSeconds);

            FinishRingVictory(ringCreator);
        }

        private void FinishRingVictory(Player ringCreator)
        {
            GameManager.Instance?.EndGame(ringCreator);
            celebrationRunning = false;
        }
    }
}
