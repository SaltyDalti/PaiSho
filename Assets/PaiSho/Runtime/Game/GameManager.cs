using System.Collections.Generic;
using UnityEngine;
using PaiSho;
using PaiSho.Board;
using PaiSho.Pieces;

namespace PaiSho.Game
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance;

        private bool turnComplete = false;
        private int currentPlayerIndex = 0;
        private int turnNumber = 1;
        private int playTurnsCompleted;
        private readonly Player[] players = { Player.Host, Player.Opponent };

        /// <summary>True after Extra Move spent — a second move is allowed this turn.</summary>
        public bool PendingBonusMove { get; private set; }

        public int SpringPlacements { get; private set; }

        private int cachedHostScore;
        private int cachedOpponentScore;
        private int cachedHostHarmony;
        private int cachedOpponentHarmony;

        private void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(gameObject);
            else
                Instance = this;
        }

        public Player GetCurrentPlayer() => players[currentPlayerIndex];

        public void PassTurn()
        {
            if (GameStateManager.Instance.IsEndPhase())
                return;

            DebugLogger.Log($"{GetCurrentPlayer()} passes.");
            MarkTurnComplete();
            EndTurn();
        }

        public void EndTurn()
        {
            if (!turnComplete)
            {
                if (!HeadlessActionExecutor.IsActive)
                    GameplayFeedback.Show("You must place or move a tile before ending your turn.");
                return;
            }

            Player finishingPlayer = GetCurrentPlayer();
            PendingBonusMove = false;
            SeasonManager.Instance.AdvanceTurn();
            bool seasonRotated = SeasonManager.Instance.SeasonJustRotated;

            if (seasonRotated && !HeadlessActionExecutor.IsActive)
            {
                Season newSeason = SeasonManager.Instance.GetCurrentSeason();
                GameplayFeedback.Show(
                    $"The season turns to {newSeason} — {SeasonManager.Instance.DescribeSeasonHint(newSeason)}",
                    5f);
            }

            bool springJustEnded = false;
            if (GameStateManager.Instance.IsSpringPhase() &&
                SpringPlacements >= PieceRules.SpringFlowerCount)
            {
                GameStateManager.Instance.AdvancePhase();
                ReserveManager.Instance.DealOpeningHands(PieceRules.HandSize);
                springJustEnded = true;
                DebugLogger.Log("Spring complete. Hands dealt — normal play begins.");
            }
            else if (GameStateManager.Instance.IsPlayPhase())
            {
                ReserveManager.Instance.OnPlayerFinishedPlayTurn(finishingPlayer);
                playTurnsCompleted++;
            }

            // Ring check before handoff — a completed ring ends the match immediately.
            List<Piece> allPieces = BoardManager.Instance.GetAllPieces();
            BoardManager.Instance.RefreshAllHarmony();
            if (VictoryManager.Instance != null &&
                VictoryManager.Instance.CheckForHarmonyRingEnd(allPieces))
            {
                turnComplete = false;
                if (seasonRotated)
                    SeasonManager.Instance.ClearSeasonJustRotated();
                RefreshLiveScores();
                if (!HeadlessActionExecutor.IsActive)
                {
                    GameInputController.Instance?.ClearSelection();
                    LegalMoveHighlighter.Instance?.Clear();
                    HandTrayController.Instance?.Refresh();
                }

                return;
            }

            currentPlayerIndex = (currentPlayerIndex + 1) % 2;
            turnComplete = false;
            turnNumber++;
            MovementManager.Instance.StartTurn();

            if (!springJustEnded && GameStateManager.Instance.IsSpringPhase())
                ReserveManager.Instance.PrepareSpringTurn(GetCurrentPlayer());
            else if (GameStateManager.Instance.IsPlayPhase())
                ReserveManager.Instance.OnPlayTurnStart(GetCurrentPlayer());

            allPieces = BoardManager.Instance.GetAllPieces();
            TileLifecycleManager.Instance.OnTurnStart(allPieces);
            BoardManager.Instance.RefreshAllHarmony();
            KnotweedManager.Instance?.ProcessDrainEffects();

            if (seasonRotated && GameStateManager.Instance.IsPlayPhase())
            {
                SeasonManager.Instance.EvaluateSeasonalBonuses(finishingPlayer, allPieces);
                if (!HeadlessActionExecutor.IsActive)
                {
                    // Visual: scene pulse + in-season flower boost (no explanatory toast).
                    if (BoardManager.Instance != null)
                    {
                        foreach (Piece piece in allPieces)
                        {
                            if (piece == null)
                                continue;
                            if (SeasonManager.Instance.IsInSeason(piece.Type))
                                PieceStateAnimator.Ensure(piece)?.NotifySeasonalBoost(2f);
                        }
                    }

                    SceneLifeAnimator.PulseSeasonChange();
                }

                SeasonManager.Instance.ClearSeasonJustRotated();
            }

            if (!HeadlessActionExecutor.IsActive)
                GameplayVisualizer.Instance?.Refresh();

            Player current = GetCurrentPlayer();
            if (!GameStateManager.Instance.IsEndPhase())
                MomentumManager.Instance.EvaluateTurnBonuses(current, allPieces);

            if (!HeadlessActionExecutor.IsActive)
                DebugLogger.Log($"Turn ended. {current}'s turn.");

            if (!HeadlessActionExecutor.IsActive &&
                AiController.Instance != null &&
                AiController.Instance.IsAiPlayer(finishingPlayer) &&
                !AiController.Instance.IsAiPlayer(current))
            {
                GameplayFeedback.Show("Your turn", 3f);
                UiAudio.Instance?.PlayNotify();
            }

            if (!HeadlessActionExecutor.IsActive)
                AiController.Instance?.TryPlayTurn();

            RefreshLiveScores();
            if (!HeadlessActionExecutor.IsActive)
            {
                GameInputController.Instance?.SyncSelectionToPhase();
                HandTrayController.Instance?.Refresh();
            }
        }

        public void RefreshLiveScores()
        {
            if (BoardManager.Instance == null || ScoringManager.Instance == null)
                return;

            List<Piece> pieces = BoardManager.Instance.GetAllPieces();
            cachedHostScore = ScoringManager.Instance.ComputeLiveScore(Player.Host, pieces);
            cachedOpponentScore = ScoringManager.Instance.ComputeLiveScore(Player.Opponent, pieces);
            cachedHostHarmony = CountHarmonizedPiecesUncached(Player.Host, pieces);
            cachedOpponentHarmony = CountHarmonizedPiecesUncached(Player.Opponent, pieces);
        }

        private static int CountHarmonizedPiecesUncached(Player player, List<Piece> pieces)
        {
            int count = 0;
            foreach (Piece piece in pieces)
            {
                if (piece.Owner == player && piece.InHarmony && !piece.IsGhost)
                    count++;
            }

            return count;
        }

        public void MarkTurnComplete() => turnComplete = true;

        public int GetTurnNumber() => turnNumber;

        public bool CanGrantExtraMove(Player player)
        {
            if (GameStateManager.Instance == null || MomentumManager.Instance == null ||
                MovementManager.Instance == null)
                return false;

            if (!GameStateManager.Instance.IsPlayPhase() || GameStateManager.Instance.IsEndPhase())
                return false;

            if (PendingBonusMove)
                return false;

            if (!MomentumManager.Instance.HasMomentum(player))
                return false;

            if (MovementManager.Instance.PlacedThisTurn(player))
                return false;

            // Extra Move can be bought before the first move, or while still on the first-move slot.
            return MovementManager.Instance.GetMovedTileCount(player) <= 1;
        }

        /// <summary>Spend 1 Momentum now to allow a second Move this turn. Opt-in — no End Turn required.</summary>
        public bool TryGrantExtraMove(Player player)
        {
            if (!CanGrantExtraMove(player))
                return false;

            if (!MomentumManager.Instance.TrySpendMomentum(player, "Extra Move"))
                return false;

            PendingBonusMove = true;
            if (!HeadlessActionExecutor.IsActive)
                GameplayFeedback.Show("Extra Move ready — you may move a second tile this turn.", 4f);
            return true;
        }

        public void ClearBonusMoveOffer()
        {
            PendingBonusMove = false;
        }

        /// <summary>Finish the turn after a completed action (or AI declining a held bonus).</summary>
        public bool TryEndTurnEarly()
        {
            if (GameStateManager.Instance != null && GameStateManager.Instance.IsEndPhase())
                return false;

            bool progressed = PendingBonusMove ||
                              turnComplete ||
                              (MovementManager.Instance != null &&
                               (MovementManager.Instance.GetMovedTileCount(GetCurrentPlayer()) > 0 ||
                                MovementManager.Instance.PlacedThisTurn(GetCurrentPlayer())));
            if (!progressed)
                return false;

            PendingBonusMove = false;
            MarkTurnComplete();
            EndTurn();
            return true;
        }

        public void EndGame(Player ringCreator)
        {
            DebugLogger.Log($"Game has ended — Harmony Ring formed by {ringCreator}.");
            GameStateManager.Instance.SetPhase(GamePhase.End);
            RefreshLiveScores();
            GameEndManager.Instance.ResolveFinalScore($"Harmony Ring ({ringCreator})", ringCreator);
            GameInputController.Instance?.ClearSelection();
            LegalMoveHighlighter.Instance?.Clear();
        }

        /// <summary>Concedes the match — the other side is credited with the win.</summary>
        public void ForfeitMatch(Player forfeitingPlayer)
        {
            if (GameStateManager.Instance != null && GameStateManager.Instance.IsEndPhase())
                return;

            DebugLogger.Log($"{forfeitingPlayer} forfeited the match.");
            GameStateManager.Instance.SetPhase(GamePhase.End);
            RefreshLiveScores();
            GameEndManager.Instance?.ResolveForfeit(forfeitingPlayer);
            GameInputController.Instance?.ClearSelection();
            LegalMoveHighlighter.Instance?.Clear();
        }

        public int GetLiveScore(Player player)
        {
            return player == Player.Host ? cachedHostScore : cachedOpponentScore;
        }

        public int CountHarmonizedPieces(Player player)
        {
            return player == Player.Host ? cachedHostHarmony : cachedOpponentHarmony;
        }

        public bool HasLegalActions(Player player)
        {
            return ActionGenerator.GetAllLegalActions(player).Count > 0;
        }

        public void RecordSpringPlacement()
        {
            SpringPlacements++;
        }

        public int GetSpringPlacementsRemaining()
        {
            return Mathf.Max(0, PieceRules.SpringFlowerCount - SpringPlacements);
        }

        public bool IsGlobalHarmonyUnlocked()
        {
            return playTurnsCompleted >= PieceRules.TurnsBeforeGlobalHarmony;
        }

        /// <summary>
        /// Spring bud glow ends after 3 completed play-phase turns (or when a flower is first moved).
        /// Placement-phase turns never count.
        /// </summary>
        public bool IsSpringGlowEnded()
        {
            if (GameStateManager.Instance != null && !GameStateManager.Instance.IsPlayPhase())
                return false;

            return playTurnsCompleted >= PieceRules.TurnsBeforeHoldRelease;
        }

        public bool SpecialTilesUnlocked(Player player)
        {
            return ReserveManager.Instance.HasHoldReleased(player);
        }

        public void ResetForNewMatch()
        {
            turnComplete = false;
            PendingBonusMove = false;
            currentPlayerIndex = 0;
            turnNumber = 1;
            playTurnsCompleted = 0;
            SpringPlacements = 0;
            cachedHostScore = 0;
            cachedOpponentScore = 0;
            cachedHostHarmony = 0;
            cachedOpponentHarmony = 0;
        }

        public bool IsSpringPhase() => GameStateManager.Instance.IsSpringPhase();
    }
}
