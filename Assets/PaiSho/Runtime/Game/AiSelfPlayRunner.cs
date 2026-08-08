using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UnityEngine;
using PaiSho.Pieces;

namespace PaiSho.Game
{
    /// <summary>
    /// Runs headless AI-vs-AI matches to benchmark heuristics.
    /// Yields every turn and budgets CPU per frame so Unity does not freeze.
    /// </summary>
    public class AiSelfPlayRunner : MonoBehaviour
    {
        public static AiSelfPlayRunner Instance { get; private set; }
        public static AiScorerWeights ActiveWeights { get; private set; }

        [SerializeField] private int gamesPerBatch = 3;
        [SerializeField] private int maxTurnsPerGame = 180;
        [SerializeField] private float frameBudgetMs = 8f;
        [SerializeField] private AiScorerWeights weightsOverride;
        [SerializeField] private bool logEachGameSummary = true;

        /// <summary>Effective cap for the active batch (menu can force this past stale scene serialization).</summary>
        private int activeMaxTurnsPerGame = 180;

        private Coroutine batchRoutine;
        private AiSelfPlayReport lastReport;

        public bool IsRunning => batchRoutine != null;
        public AiSelfPlayReport LastReport => lastReport;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            activeMaxTurnsPerGame = maxTurnsPerGame > 0 ? maxTurnsPerGame : 180;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;

            StopBenchmark();
        }

        [ContextMenu("Run AI Self-Play Benchmark")]
        public void RunBenchmarkFromInspector()
        {
            StartBenchmark(gamesPerBatch, weightsOverride);
        }

        [ContextMenu("Stop AI Self-Play")]
        public void StopBenchmarkFromInspector() => StopBenchmark();

        public void StartBenchmark(int gameCount, AiScorerWeights weights = null, int maxTurns = -1)
        {
            if (!Application.isPlaying)
            {
                UnityEngine.Debug.LogError("AI self-play only works in Play Mode.");
                return;
            }

            if (GameManager.Instance == null || TileSelector.Instance == null)
            {
                UnityEngine.Debug.LogError("AI self-play needs an active game scene (GameManager + TileSelector).");
                return;
            }

            if (batchRoutine != null)
                StopCoroutine(batchRoutine);

            // Prefer explicit arg, then inspector, then hard default.
            activeMaxTurnsPerGame = maxTurns > 0
                ? maxTurns
                : (maxTurnsPerGame > 0 ? maxTurnsPerGame : 180);
            activeMaxTurnsPerGame = Mathf.Clamp(activeMaxTurnsPerGame, 40, 500);

            batchRoutine = StartCoroutine(RunBatch(Mathf.Clamp(gameCount, 1, 50), weights));
        }

        public void StopBenchmark()
        {
            if (batchRoutine != null)
            {
                StopCoroutine(batchRoutine);
                batchRoutine = null;
            }

            HeadlessActionExecutor.End();
            ActiveWeights = null;
        }

        private IEnumerator RunBatch(int gameCount, AiScorerWeights weights)
        {
            bool aiWasEnabled = AiController.Instance != null && AiController.Instance.IsAiEnabled;
            if (AiController.Instance != null)
                AiController.Instance.SetAiEnabled(false);

            ActiveWeights = weights;
            HeadlessActionExecutor.Begin();
            lastReport = new AiSelfPlayReport();

            UnityEngine.Debug.Log($"AI self-play starting ({gameCount} games, max {activeMaxTurnsPerGame} turns each)...");

            for (int g = 0; g < gameCount; g++)
            {
                yield return RunSingleGame(lastReport);

                if (logEachGameSummary && lastReport.CompletedGames > 0)
                {
                    AiSelfPlayGameResult latest = lastReport.Results[lastReport.Results.Count - 1];
                    UnityEngine.Debug.Log($"[Self-Play {g + 1}/{gameCount}] {latest}");
                }

                // Always breathe between games so the editor stays responsive.
                yield return null;
            }

            string summaryPath = null;
            if (GameLogManager.Instance != null)
                summaryPath = GameLogManager.Instance.ExportMatchLog("self-play-batch");

            HeadlessActionExecutor.End();
            ActiveWeights = null;

            if (AiController.Instance != null)
                AiController.Instance.SetAiEnabled(aiWasEnabled);

            batchRoutine = null;
            UnityEngine.Debug.Log(lastReport.BuildSummary());
            if (!string.IsNullOrEmpty(summaryPath))
                UnityEngine.Debug.Log($"Self-play board snapshot also saved: {summaryPath}");
            UnityEngine.Debug.Log($"Self-play done: {lastReport.BuildShortSummary()}");
        }

        private IEnumerator RunSingleGame(AiSelfPlayReport report)
        {
            GameResetService.ResetMatch();
            yield return null;

            int turns = 0;
            int consecutiveFailures = 0;
            int lastTurnNumber = GameManager.Instance != null ? GameManager.Instance.GetTurnNumber() : 0;
            var stopwatch = Stopwatch.StartNew();

            while (GameStateManager.Instance != null &&
                   !GameStateManager.Instance.IsEndPhase() &&
                   turns < activeMaxTurnsPerGame)
            {
                if (GameManager.Instance == null || TileSelector.Instance == null)
                    break;

                Player current = GameManager.Instance.GetCurrentPlayer();
                List<GameAction> actions = ActionGenerator.GetAllLegalActions(current);

                bool advanced;
                if (actions.Count == 0)
                {
                    GameManager.Instance.PassTurn();
                    advanced = true;
                }
                else
                {
                    GameAction action = AiScorer.PickAction(actions, current);
                    GameAction preferred = action;
                    int moveFrom = preferred.Kind == GameActionKind.Move && preferred.Piece != null
                        ? preferred.Piece.BoardCoordinate
                        : -1;
                    advanced = AiController.TryExecuteWithFallback(ref action, actions, current);
                    if (advanced && action.Kind == GameActionKind.Move && action.Piece != null)
                    {
                        if (!ReferenceEquals(preferred.Piece, action.Piece))
                            moveFrom = -1;
                        AiPlanMemory.RecordMove(current, action.Piece, moveFrom, action.Coordinate);
                    }

                    // After a move: buy Extra Move + second action if valuable, else end the held turn.
                    if (advanced && action.Kind == GameActionKind.Move &&
                        GameManager.Instance != null)
                    {
                        AiController.TryExecuteBonusMove(current);
                    }

                    if (!advanced)
                    {
                        // Action generation/scoring disagreed with execution — don't spin forever.
                        consecutiveFailures++;
                        if (consecutiveFailures >= 3)
                        {
                            GameManager.Instance.PassTurn();
                            advanced = true;
                            consecutiveFailures = 0;
                        }
                    }
                    else
                    {
                        consecutiveFailures = 0;
                    }
                }

                turns++;

                int turnNumber = GameManager.Instance.GetTurnNumber();
                if (advanced && turnNumber == lastTurnNumber)
                {
                    // Turn counter didn't move — force pass once, then abort the game.
                    consecutiveFailures++;
                    if (consecutiveFailures >= 2)
                    {
                        report.RecordGame(new AiSelfPlayGameResult
                        {
                            Turns = turns,
                            Winner = null,
                            WinReason = "Stuck turn",
                            HostScore = GameManager.Instance.GetLiveScore(Player.Host),
                            OpponentScore = GameManager.Instance.GetLiveScore(Player.Opponent)
                        });
                        yield break;
                    }

                    GameManager.Instance.PassTurn();
                }
                else if (advanced)
                {
                    lastTurnNumber = turnNumber;
                }

                if (stopwatch.ElapsedMilliseconds >= frameBudgetMs)
                {
                    yield return null;
                    stopwatch.Restart();
                }
            }

            // Ensure ring victories always resolve a Winner before digests read results.
            EnsureMatchResolved();

            report.RecordGame(BuildGameResult(turns));
            GameLogManager.Instance?.ExportMatchLog($"self-play-g{report.CompletedGames}");
            yield return null;
        }

        private static void EnsureMatchResolved()
        {
            if (GameEndManager.Instance != null && GameEndManager.Instance.Winner.HasValue)
                return;

            if (GameStateManager.Instance != null && GameStateManager.Instance.IsEndPhase())
            {
                foreach (Player player in new[] { Player.Host, Player.Opponent })
                {
                    if (!HarmonyRingDetector.HasCompleteRing(player))
                        continue;
                    GameManager.Instance?.EndGame(player);
                    return;
                }
            }

            // Turn-limit / incomplete: still write final scores via EndGame reason if phase not End.
            if (GameStateManager.Instance != null &&
                !GameStateManager.Instance.IsEndPhase() &&
                GameManager.Instance != null)
            {
                GameStateManager.Instance.SetPhase(GamePhase.End);
                GameEndManager.Instance?.ResolveFinalScore("Turn limit", ringWinner: null);
            }
        }

        private static bool ExecuteHeadless(GameAction action)
        {
            return AiController.Execute(action);
        }

        private AiSelfPlayGameResult BuildGameResult(int turns)
        {
            var result = new AiSelfPlayGameResult { Turns = turns };

            if (GameEndManager.Instance != null && GameEndManager.Instance.Winner.HasValue)
            {
                result.Winner = GameEndManager.Instance.Winner.Value;
                result.WinReason = GameEndManager.Instance.WinReason;
            }
            else
            {
                result.Winner = null;
                result.WinReason = turns >= activeMaxTurnsPerGame ? "Turn limit" : "Incomplete";
            }

            if (GameManager.Instance != null)
            {
                result.HostScore = GameManager.Instance.GetLiveScore(Player.Host);
                result.OpponentScore = GameManager.Instance.GetLiveScore(Player.Opponent);
            }

            if (GameLogManager.Instance != null)
            {
                MatchMetricPeaks peaks = GameLogManager.Instance.GetMetricPeaks();
                result.MaxHostHarmony = peaks.MaxHostHarmony;
                result.MaxOpponentHarmony = peaks.MaxOpponentHarmony;
                result.MaxHostRing = peaks.MaxHostRing;
                result.MaxOpponentRing = peaks.MaxOpponentRing;
                result.FirstHostRingTurn = peaks.FirstHostRingTurn;
                result.FirstOpponentRingTurn = peaks.FirstOpponentRingTurn;
                result.FinalHostWilt = peaks.FinalHostWilt;
                result.FinalOpponentWilt = peaks.FinalOpponentWilt;
            }

            return result;
        }
    }

    public class AiSelfPlayGameResult
    {
        public Player? Winner;
        public string WinReason;
        public int Turns;
        public int HostScore;
        public int OpponentScore;
        public int MaxHostHarmony;
        public int MaxOpponentHarmony;
        public int MaxHostRing;
        public int MaxOpponentRing;
        public int FirstHostRingTurn = -1;
        public int FirstOpponentRingTurn = -1;
        public int FinalHostWilt;
        public int FinalOpponentWilt;

        public override string ToString()
        {
            string victor = Winner.HasValue ? Winner.Value.ToString() : "Draw";
            return $"{victor} in {Turns} turns ({WinReason}) - {HostScore} vs {OpponentScore} " +
                   $"| maxH {MaxHostHarmony}/{MaxOpponentHarmony} maxR {MaxHostRing}/{MaxOpponentRing} " +
                   $"wilt {FinalHostWilt}/{FinalOpponentWilt}";
        }
    }

    public class AiSelfPlayReport
    {
        public readonly List<AiSelfPlayGameResult> Results = new();
        public int CompletedGames => Results.Count;
        public int HostWins { get; private set; }
        public int OpponentWins { get; private set; }
        public int Draws { get; private set; }

        public void RecordGame(AiSelfPlayGameResult result)
        {
            Results.Add(result);
            if (!result.Winner.HasValue)
                Draws++;
            else if (result.Winner.Value == Player.Host)
                HostWins++;
            else
                OpponentWins++;
        }

        public string BuildShortSummary() =>
            $"{HostWins}-{OpponentWins}-{Draws} (H-O-D) over {CompletedGames}";

        public string BuildSummary()
        {
            if (CompletedGames == 0)
                return "Self-play: no games completed.";

            float avgTurns = 0f;
            int ringTouchGames = 0;
            int maxRingSeen = 0;
            float avgMaxHarmony = 0f;
            foreach (AiSelfPlayGameResult result in Results)
            {
                avgTurns += result.Turns;
                avgMaxHarmony += Mathf.Max(result.MaxHostHarmony, result.MaxOpponentHarmony);
                int gameMaxRing = Mathf.Max(result.MaxHostRing, result.MaxOpponentRing);
                if (gameMaxRing > 0)
                    ringTouchGames++;
                if (gameMaxRing > maxRingSeen)
                    maxRingSeen = gameMaxRing;
            }

            avgTurns /= CompletedGames;
            avgMaxHarmony /= CompletedGames;

            var builder = new StringBuilder();
            builder.AppendLine($"=== AI self-play ({CompletedGames} games) ===");
            builder.AppendLine($"Host wins: {HostWins}  ·  Opponent wins: {OpponentWins}  ·  Draws: {Draws}");
            builder.AppendLine($"Avg turns: {avgTurns:F1}");
            builder.AppendLine($"Games with ring progress > 0: {ringTouchGames}/{CompletedGames} (peak ring {maxRingSeen})");
            builder.AppendLine($"Avg peak harmony (best side): {avgMaxHarmony:F1}");
            builder.AppendLine("See match-*-self-play-g*.tsv for per-action host_ring / host_harmony / wilt / quads.");
            return builder.ToString();
        }
    }
}
