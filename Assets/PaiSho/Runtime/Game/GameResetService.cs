using PaiSho.Board;
using PaiSho.Pieces;

namespace PaiSho.Game
{
    /// <summary>Soft-resets match state without reloading the scene (keeps camera and room).</summary>
    public static class GameResetService
    {
        public static void ResetMatch()
        {
            if (BoardManager.Instance == null || GameManager.Instance == null)
                return;

            PieceFeedbackManager.Instance?.CancelAll();
            BoardManager.Instance.ClearAllPieces();
            BoatManager.Instance?.ClearAll();
            PotManager.Instance?.ClearCaptured();
            PotVisualManager.Instance?.ClearAll();
            EchoTileManager.Instance?.ResetForNewMatch();
            MomentumManager.Instance?.ResetForNewMatch();
            ScoringManager.Instance?.ResetForNewMatch();
            SeasonManager.Instance?.ResetForNewMatch();
            GameEndManager.Instance?.ResetForNewMatch();
            TileLifecycleManager.Instance?.ResetForNewMatch();
            GameManager.Instance.ResetForNewMatch();
            GameStateManager.Instance?.SetPhase(GamePhase.Spring);

            ReserveManager.Instance?.InitializeDefaultReserves();
            ReserveManager.Instance?.PrepareSpringTurn(GameManager.Instance.GetCurrentPlayer());
            MovementManager.Instance?.StartTurn();

            GameLogManager.Instance?.ClearEntries();
            AiPlanMemory.Clear();
            GameplayFeedback.Clear();
            GameInputController.Instance?.ClearSelection();
            LegalMoveHighlighter.Instance?.Clear();
            GameplayVisualizer.Instance?.Refresh();
            GameManager.Instance.RefreshLiveScores();
            HandTrayController.Instance?.Refresh();

            DebugLogger.Log("--- New match ---");
            DebugLogger.Log("Pai Sho ready. Spring: random flower draws - place on your side.");
            if (!HeadlessActionExecutor.IsActive)
                AiController.Instance?.TryPlayTurn();
        }
    }
}
