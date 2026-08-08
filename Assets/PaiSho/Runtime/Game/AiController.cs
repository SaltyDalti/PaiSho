using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PaiSho.Pieces;

namespace PaiSho.Game
{
    public class AiController : MonoBehaviour
    {
        public static AiController Instance;

        [SerializeField] private bool enableAi = true;
        [SerializeField] private Player aiPlayer = Player.Opponent;
        [SerializeField] private float thinkDelaySeconds = 0.65f;
        [SerializeField] private float idleRetrySeconds = 1.25f;

        private Coroutine playCoroutine;
        private float nextIdleKickUnscaled;
        private int consecutiveFailures;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Update()
        {
            if (!IsAiTurn() || HeadlessActionExecutor.IsActive)
            {
                consecutiveFailures = 0;
                return;
            }

            // Recover if a prior think died, failed to end the turn, or never started.
            if (playCoroutine == null && Time.unscaledTime >= nextIdleKickUnscaled)
            {
                nextIdleKickUnscaled = Time.unscaledTime + idleRetrySeconds;
                TryPlayTurn();
            }
        }

        public bool IsAiEnabled => enableAi;
        public Player AiPlayer => aiPlayer;

        public void SetAiEnabled(bool enabled)
        {
            enableAi = enabled;
            DebugLogger.Log(enabled ? "AI opponent enabled." : "AI opponent disabled.");
            if (enabled && IsAiTurn())
                TryPlayTurn();
        }

        public bool IsAiPlayer(Player player)
        {
            return enableAi && player == aiPlayer;
        }

        public bool IsAiTurn()
        {
            if (!enableAi || GameManager.Instance == null || GameStateManager.Instance == null)
                return false;

            if (GameStateManager.Instance.IsEndPhase())
                return false;

            if (VictoryManager.Instance != null && VictoryManager.Instance.IsCelebrating)
                return false;

            return GameManager.Instance.GetCurrentPlayer() == aiPlayer;
        }

        public void StopThinking()
        {
            if (playCoroutine != null)
            {
                StopCoroutine(playCoroutine);
                playCoroutine = null;
            }

            consecutiveFailures = 0;
            nextIdleKickUnscaled = Time.unscaledTime + idleRetrySeconds;
        }

        public void TryPlayTurn()
        {
            if (!IsAiTurn())
                return;

            if (playCoroutine != null)
                StopCoroutine(playCoroutine);

            playCoroutine = StartCoroutine(PlayTurnCoroutine());
        }

        private IEnumerator PlayTurnCoroutine()
        {
            try
            {
                GameplayFeedback.Show("Computer is thinking...", 2.5f);

                // Realtime so F9 / other timeScale=0 pauses can't softlock the AI forever.
                float delay = thinkDelaySeconds * UnityEngine.Random.Range(0.85f, 1.2f);
                yield return new WaitForSecondsRealtime(delay);

                if (!IsAiTurn())
                    yield break;

                Player player = GameManager.Instance.GetCurrentPlayer();
                List<GameAction> actions;
                GameAction action;

                try
                {
                    actions = ActionGenerator.GetAllLegalActions(player);
                    if (actions == null || actions.Count == 0)
                    {
                        DebugLogger.LogWarning($"AI ({player}) has no legal actions - passing turn.");
                        GameplayFeedback.Show("Computer passes.", 3f);
                        GameManager.Instance.PassTurn();
                        consecutiveFailures = 0;
                        yield break;
                    }

                    action = AiScorer.PickAction(actions, player);
                }
                catch (Exception ex)
                {
                    consecutiveFailures++;
                    DebugLogger.LogError($"AI think failed ({ex.GetType().Name}): {ex.Message}");
                    if (consecutiveFailures >= 2)
                    {
                        GameplayFeedback.Show("Computer passes.", 3f);
                        GameManager.Instance.PassTurn();
                        consecutiveFailures = 0;
                    }
                    yield break;
                }

                if (NeedsPiece(action) && action.Piece == null)
                {
                    DebugLogger.LogWarning("AI picked an empty action - passing turn.");
                    GameManager.Instance.PassTurn();
                    consecutiveFailures = 0;
                    yield break;
                }

                DebugLogger.Log($"AI plays: {action}");
                GameplayFeedback.Show(DescribeAction(action), 4.5f);
                UiAudio.Instance?.PlayNotify();

                int moveFrom = action.Kind == GameActionKind.Move && action.Piece != null
                    ? action.Piece.BoardCoordinate
                    : -1;

                bool executed = Execute(action);
                if (action.Kind == GameActionKind.Move && action.Piece != null && executed)
                    AiPlanMemory.RecordMove(player, action.Piece, moveFrom, action.Coordinate);

                if (PieceFeedbackManager.Instance != null)
                    yield return PieceFeedbackManager.Instance.WaitForAnimations();

                // After a move: buy Extra Move + second action if valuable, else end the held turn.
                if (executed && action.Kind == GameActionKind.Move &&
                    GameManager.Instance != null && IsAiTurn())
                {
                    TryExecuteBonusMove(player);
                    if (PieceFeedbackManager.Instance != null)
                        yield return PieceFeedbackManager.Instance.WaitForAnimations();
                }

                // Allow deferred EndTurn callbacks a short beat after travel settles.
                float grace = 0f;
                while (IsAiTurn() && grace < 1.5f)
                {
                    grace += Time.unscaledDeltaTime;
                    yield return null;
                }

                if (IsAiTurn() && GameManager.Instance != null &&
                    (GameManager.Instance.PendingBonusMove ||
                     (MovementManager.Instance != null &&
                      MovementManager.Instance.GetMovedTileCount(player) > 0)))
                    GameManager.Instance.TryEndTurnEarly();

                if (IsAiTurn())
                {
                    consecutiveFailures++;
                    DebugLogger.LogWarning(
                        executed
                            ? "AI action did not end the turn - passing."
                            : "AI action failed - passing.");
                    GameplayFeedback.Show("Computer passes.", 3f);
                    GameManager.Instance.PassTurn();
                    consecutiveFailures = 0;
                }
                else
                {
                    consecutiveFailures = 0;
                }
            }
            finally
            {
                playCoroutine = null;
                nextIdleKickUnscaled = Time.unscaledTime + idleRetrySeconds;
            }
        }

        /// <summary>Cap 1 Extra Move when the first action left the turn open for a buy decision.</summary>
        public static void TryExecuteBonusMove(Player player)
        {
            if (GameManager.Instance == null)
                return;

            if (MomentumManager.Instance == null || MovementManager.Instance == null)
            {
                GameManager.Instance.TryEndTurnEarly();
                return;
            }

            // Already spent Extra Move — take the second move if valuable.
            // Otherwise evaluate, buy with Momentum if worth it, then move (or end).
            List<GameAction> actions = ActionGenerator.GetAllLegalActions(player);
            var moves = new List<GameAction>();
            foreach (GameAction candidate in actions)
            {
                if (candidate.Kind == GameActionKind.Move && candidate.Piece != null)
                    moves.Add(candidate);
            }

            if (moves.Count == 0)
            {
                GameManager.Instance.TryEndTurnEarly();
                return;
            }

            GameAction bonus = AiScorer.PickAction(moves, player);
            if (bonus.Kind != GameActionKind.Move || bonus.Piece == null)
            {
                GameManager.Instance.TryEndTurnEarly();
                return;
            }

            int bonusScore = AiScorer.ScoreAction(bonus, player);
            if (bonusScore <= 40)
            {
                GameManager.Instance.TryEndTurnEarly();
                return;
            }

            if (!GameManager.Instance.PendingBonusMove)
            {
                if (!GameManager.Instance.TryGrantExtraMove(player))
                {
                    GameManager.Instance.TryEndTurnEarly();
                    return;
                }
            }

            int from = bonus.Piece.BoardCoordinate;
            if (!Execute(bonus))
            {
                GameManager.Instance.TryEndTurnEarly();
                return;
            }

            AiPlanMemory.RecordMove(player, bonus.Piece, from, bonus.Coordinate);

            if (HeadlessActionExecutor.IsActive &&
                GameManager.Instance != null &&
                GameManager.Instance.PendingBonusMove)
            {
                GameManager.Instance.TryEndTurnEarly();
            }
        }

        public static string DescribeAction(GameAction action)
        {
            return action.Kind switch
            {
                GameActionKind.Move => action.Piece != null
                    ? $"Computer moved {FriendlyName(action.Piece.Type)}."
                    : "Computer moved a tile.",
                GameActionKind.Place => $"Computer placed {FriendlyName(action.PlaceType)}.",
                GameActionKind.Revive => action.Piece != null
                    ? $"Computer revived {FriendlyName(action.Piece.Type)}."
                    : "Computer revived a tile.",
                GameActionKind.Freeze => action.Piece != null
                    ? $"Computer protected {FriendlyName(action.Piece.Type)} from wilting."
                    : "Computer protected a tile from wilting.",
                GameActionKind.BoatLoad => "Computer loaded a flower onto a Boat.",
                GameActionKind.BoatUnload => "Computer unloaded a flower from a Boat.",
                GameActionKind.WheelRotate => "Computer rotated a Wheel.",
                _ => "Computer played."
            };
        }

        private static string FriendlyName(PieceType type) =>
            type switch
            {
                PieceType.Chrysanthemum => "Chrysanthemum",
                PieceType.Orchid => "Dragon Orchid",
                PieceType.Knotweed => "Knotweed",
                _ => type.ToString()
            };

        private static bool NeedsPiece(GameAction action)
        {
            return action.Kind is GameActionKind.Move or GameActionKind.Revive or GameActionKind.Freeze
                or GameActionKind.BoatLoad or GameActionKind.BoatUnload or GameActionKind.WheelRotate;
        }

        public static bool Execute(GameAction action)
        {
            if (TileSelector.Instance == null)
                return false;

            switch (action.Kind)
            {
                case GameActionKind.Move:
                    return TileSelector.Instance.TryMoveTile(action.Piece, action.Coordinate);
                case GameActionKind.Place:
                    return TileSelector.Instance.TryPlaceTile(action.Player, action.PlaceType, action.Coordinate);
                case GameActionKind.Revive:
                    return TileSelector.Instance.TryMomentumRevive(action.Player, action.Piece);
                case GameActionKind.Freeze:
                    return TileSelector.Instance.TryMomentumFreeze(action.Player, action.Piece);
                case GameActionKind.BoatLoad:
                    return TileSelector.Instance.TryBoatLoad(action.Player, action.Piece, action.Coordinate);
                case GameActionKind.BoatUnload:
                    return TileSelector.Instance.TryBoatUnload(action.Player, action.Piece, action.Coordinate);
                case GameActionKind.WheelRotate:
                    return TileSelector.Instance.TryRotateWheel(action.Player, action.Piece);
                default:
                    return false;
            }
        }
    }
}
