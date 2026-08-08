using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using PaiSho.Board;
using PaiSho.Pieces;

namespace PaiSho.Game
{
    public enum ActionType { Placement, Move, Capture, Revival, EchoSummon }

    public struct LogEntry
    {
        public int Turn;
        public Player Player;
        public ActionType Action;
        public PieceType Type;
        public int From;
        public int To;

        // Board metrics immediately after this action (for AI study).
        public int HostHarmony;
        public int OpponentHarmony;
        public int HostRing;
        public int OpponentRing;
        public int HostWilt;
        public int OpponentWilt;
        public int HostPieces;
        public int OpponentPieces;
        public int HostQuads;
        public int OpponentQuads;
        public int HostMomentum;
        public int OpponentMomentum;
        public int HostScore;
        public int OpponentScore;

        public override string ToString()
        {
            string fromTo = Action == ActionType.Placement || From == To
                ? $"at {To}"
                : $"from {From} to {To}";

            return $"[Turn {Turn}] {Player} {Action}: {Type} {fromTo} " +
                   $"| H {HostHarmony}/{OpponentHarmony} R {HostRing}/{OpponentRing} W {HostWilt}/{OpponentWilt}";
        }
    }

    public struct MatchMetricPeaks
    {
        public int MaxHostHarmony;
        public int MaxOpponentHarmony;
        public int MaxHostRing;
        public int MaxOpponentRing;
        public int FirstHostHarmonyTurn;
        public int FirstOpponentHarmonyTurn;
        public int FirstHostRingTurn;
        public int FirstOpponentRingTurn;
        public int TurnsWithHostRing;
        public int TurnsWithOpponentRing;
        public int FinalHostWilt;
        public int FinalOpponentWilt;
    }

    public class GameLogManager : MonoBehaviour
    {
        public const string SchemaVersion = "v2";

        public static GameLogManager Instance;

        private readonly List<LogEntry> entries = new();

        /// <summary>Absolute path of the most recent successful export this session.</summary>
        public string LastExportPath { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(gameObject);
            else
                Instance = this;
        }

        public void Log(ActionType action, Player player, PieceType type, int from, int to)
        {
            BoardMetrics metrics = CaptureBoardMetrics();

            entries.Add(new LogEntry
            {
                Turn = GameManager.Instance != null ? GameManager.Instance.GetTurnNumber() : 0,
                Player = player,
                Action = action,
                Type = type,
                From = from,
                To = to,
                HostHarmony = metrics.HostHarmony,
                OpponentHarmony = metrics.OpponentHarmony,
                HostRing = metrics.HostRing,
                OpponentRing = metrics.OpponentRing,
                HostWilt = metrics.HostWilt,
                OpponentWilt = metrics.OpponentWilt,
                HostPieces = metrics.HostPieces,
                OpponentPieces = metrics.OpponentPieces,
                HostQuads = metrics.HostQuads,
                OpponentQuads = metrics.OpponentQuads,
                HostMomentum = metrics.HostMomentum,
                OpponentMomentum = metrics.OpponentMomentum,
                HostScore = metrics.HostScore,
                OpponentScore = metrics.OpponentScore
            });
        }

        public void ClearEntries() => entries.Clear();

        public void PrintLog()
        {
            if (entries.Count == 0)
            {
                DebugLogger.Log("======= Full Game Log (empty) =======");
                return;
            }

            var builder = new StringBuilder();
            builder.AppendLine("======= Full Game Log =======");
            foreach (LogEntry entry in entries)
                builder.AppendLine(entry.ToString());

            DebugLogger.Log(builder.ToString());
        }

        public List<LogEntry> GetEntries() => new List<LogEntry>(entries);

        public MatchMetricPeaks GetMetricPeaks()
        {
            var peaks = new MatchMetricPeaks
            {
                FirstHostHarmonyTurn = -1,
                FirstOpponentHarmonyTurn = -1,
                FirstHostRingTurn = -1,
                FirstOpponentRingTurn = -1
            };

            foreach (LogEntry entry in entries)
            {
                if (entry.HostHarmony > peaks.MaxHostHarmony)
                    peaks.MaxHostHarmony = entry.HostHarmony;
                if (entry.OpponentHarmony > peaks.MaxOpponentHarmony)
                    peaks.MaxOpponentHarmony = entry.OpponentHarmony;
                if (entry.HostRing > peaks.MaxHostRing)
                    peaks.MaxHostRing = entry.HostRing;
                if (entry.OpponentRing > peaks.MaxOpponentRing)
                    peaks.MaxOpponentRing = entry.OpponentRing;

                if (entry.HostHarmony > 0 && peaks.FirstHostHarmonyTurn < 0)
                    peaks.FirstHostHarmonyTurn = entry.Turn;
                if (entry.OpponentHarmony > 0 && peaks.FirstOpponentHarmonyTurn < 0)
                    peaks.FirstOpponentHarmonyTurn = entry.Turn;
                if (entry.HostRing > 0 && peaks.FirstHostRingTurn < 0)
                    peaks.FirstHostRingTurn = entry.Turn;
                if (entry.OpponentRing > 0 && peaks.FirstOpponentRingTurn < 0)
                    peaks.FirstOpponentRingTurn = entry.Turn;

                if (entry.HostRing > 0)
                    peaks.TurnsWithHostRing++;
                if (entry.OpponentRing > 0)
                    peaks.TurnsWithOpponentRing++;
            }

            if (entries.Count > 0)
            {
                LogEntry last = entries[entries.Count - 1];
                peaks.FinalHostWilt = last.HostWilt;
                peaks.FinalOpponentWilt = last.OpponentWilt;
            }

            return peaks;
        }

        /// <summary>
        /// Writes a parseable TSV match log under Logs/ for AI tuning and review.
        /// Returns the absolute path, or null on failure.
        /// </summary>
        public string ExportMatchLog(string reason = "manual")
        {
            try
            {
                string directory = ResolveLogDirectory();
                Directory.CreateDirectory(directory);

                string stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
                string fileName = $"match-{stamp}-{Sanitize(reason)}.tsv";
                string path = Path.Combine(directory, fileName);

                File.WriteAllText(path, BuildExportText(reason), Encoding.UTF8);
                LastExportPath = path;
                DebugLogger.Log($"Match log saved: {path}");
                return path;
            }
            catch (Exception ex)
            {
                DebugLogger.LogWarning($"Failed to save match log: {ex.Message}");
                return null;
            }
        }

        /// <summary>Exports if no successful end-of-match save yet; always returns a path when possible.</summary>
        public string EnsureMatchEndExport()
        {
            if (!string.IsNullOrEmpty(LastExportPath) &&
                LastExportPath.IndexOf("match-end", StringComparison.OrdinalIgnoreCase) >= 0 &&
                File.Exists(LastExportPath))
                return LastExportPath;

            return ExportMatchLog("match-end");
        }

        public string BuildExportText(string reason = "manual")
        {
            var builder = new StringBuilder();
            builder.AppendLine($"# pai_sho_match_log {SchemaVersion}");
            builder.AppendLine("# schema=tsv");
            builder.AppendLine($"# exported_at={DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture)}");
            builder.AppendLine($"# export_reason={Sanitize(reason)}");

            if (GameManager.Instance != null)
            {
                builder.AppendLine($"# turn={GameManager.Instance.GetTurnNumber()}");
                builder.AppendLine($"# current_player={GameManager.Instance.GetCurrentPlayer()}");
                builder.AppendLine($"# host_score={GameManager.Instance.GetLiveScore(Player.Host)}");
                builder.AppendLine($"# opponent_score={GameManager.Instance.GetLiveScore(Player.Opponent)}");
                builder.AppendLine($"# host_harmony={GameManager.Instance.CountHarmonizedPieces(Player.Host)}");
                builder.AppendLine($"# opponent_harmony={GameManager.Instance.CountHarmonizedPieces(Player.Opponent)}");
                builder.AppendLine($"# spring_remaining={GameManager.Instance.GetSpringPlacementsRemaining()}");
            }

            if (GameStateManager.Instance != null)
                builder.AppendLine($"# phase={GameStateManager.Instance.GetCurrentPhase()}");

            if (SeasonManager.Instance != null)
                builder.AppendLine($"# season={SeasonManager.Instance.GetCurrentSeason()}");

            if (MomentumManager.Instance != null)
            {
                builder.AppendLine($"# host_momentum={MomentumManager.Instance.GetMomentum(Player.Host)}");
                builder.AppendLine($"# opponent_momentum={MomentumManager.Instance.GetMomentum(Player.Opponent)}");
            }

            builder.AppendLine($"# host_ring={HarmonyRingDetector.GetRingProgress(Player.Host)}");
            builder.AppendLine($"# opponent_ring={HarmonyRingDetector.GetRingProgress(Player.Opponent)}");

            if (GameEndManager.Instance != null)
            {
                builder.AppendLine($"# winner={(GameEndManager.Instance.Winner.HasValue ? GameEndManager.Instance.Winner.Value.ToString() : "None")}");
                builder.AppendLine($"# win_reason={Sanitize(GameEndManager.Instance.WinReason)}");
                builder.AppendLine($"# final_host_score={GameEndManager.Instance.HostScore}");
                builder.AppendLine($"# final_opponent_score={GameEndManager.Instance.OpponentScore}");
            }

            if (AiController.Instance != null)
            {
                builder.AppendLine($"# ai_enabled={AiController.Instance.IsAiEnabled}");
                builder.AppendLine($"# ai_player={AiController.Instance.AiPlayer}");
            }

            MatchMetricPeaks peaks = GetMetricPeaks();
            builder.AppendLine($"# max_host_harmony={peaks.MaxHostHarmony}");
            builder.AppendLine($"# max_opponent_harmony={peaks.MaxOpponentHarmony}");
            builder.AppendLine($"# max_host_ring={peaks.MaxHostRing}");
            builder.AppendLine($"# max_opponent_ring={peaks.MaxOpponentRing}");
            builder.AppendLine($"# first_host_harmony_turn={peaks.FirstHostHarmonyTurn}");
            builder.AppendLine($"# first_opponent_harmony_turn={peaks.FirstOpponentHarmonyTurn}");
            builder.AppendLine($"# first_host_ring_turn={peaks.FirstHostRingTurn}");
            builder.AppendLine($"# first_opponent_ring_turn={peaks.FirstOpponentRingTurn}");
            builder.AppendLine($"# actions_with_host_ring={peaks.TurnsWithHostRing}");
            builder.AppendLine($"# actions_with_opponent_ring={peaks.TurnsWithOpponentRing}");

            const string columns =
                "turn\tplayer\taction\tpiece\tfrom\tto\t" +
                "host_harmony\topponent_harmony\thost_ring\topponent_ring\t" +
                "host_wilt\topponent_wilt\thost_pieces\topponent_pieces\t" +
                "host_quads\topponent_quads\thost_momentum\topponent_momentum\t" +
                "host_score\topponent_score";

            builder.AppendLine($"# columns={columns.Replace('\t', ' ')}");
            builder.AppendLine(columns);

            foreach (LogEntry entry in entries)
            {
                builder.Append(entry.Turn).Append('\t')
                    .Append(entry.Player).Append('\t')
                    .Append(entry.Action).Append('\t')
                    .Append(entry.Type).Append('\t')
                    .Append(entry.From).Append('\t')
                    .Append(entry.To).Append('\t')
                    .Append(entry.HostHarmony).Append('\t')
                    .Append(entry.OpponentHarmony).Append('\t')
                    .Append(entry.HostRing).Append('\t')
                    .Append(entry.OpponentRing).Append('\t')
                    .Append(entry.HostWilt).Append('\t')
                    .Append(entry.OpponentWilt).Append('\t')
                    .Append(entry.HostPieces).Append('\t')
                    .Append(entry.OpponentPieces).Append('\t')
                    .Append(entry.HostQuads).Append('\t')
                    .Append(entry.OpponentQuads).Append('\t')
                    .Append(entry.HostMomentum).Append('\t')
                    .Append(entry.OpponentMomentum).Append('\t')
                    .Append(entry.HostScore).Append('\t')
                    .Append(entry.OpponentScore)
                    .AppendLine();
            }

            builder.AppendLine("# board_snapshot");
            builder.AppendLine("coord\towner\tpiece\twilt\tharmony\tghost");
            AppendBoardSnapshot(builder);

            return builder.ToString();
        }

        private static void AppendBoardSnapshot(StringBuilder builder)
        {
            if (BoardManager.Instance == null)
                return;

            List<Piece> pieces = BoardManager.Instance.GetAllPieces();
            pieces.Sort((a, b) => a.BoardCoordinate.CompareTo(b.BoardCoordinate));

            foreach (Piece piece in pieces)
            {
                if (piece == null)
                    continue;

                builder.Append(piece.BoardCoordinate).Append('\t')
                    .Append(piece.Owner).Append('\t')
                    .Append(piece.Type).Append('\t')
                    .Append(piece.WiltLevel).Append('\t')
                    .Append(piece.InHarmony ? 1 : 0).Append('\t')
                    .Append(piece.IsGhost ? 1 : 0)
                    .AppendLine();
            }
        }

        private static BoardMetrics CaptureBoardMetrics()
        {
            var metrics = new BoardMetrics();

            if (BoardManager.Instance != null)
            {
                bool[] hostQuads = new bool[4];
                bool[] oppQuads = new bool[4];
                float centerCol = BoardUtils.GetColumn(BoardUtils.MiddleGate);
                float centerRow = BoardUtils.GetRow(BoardUtils.MiddleGate);

                foreach (Piece piece in BoardManager.Instance.GetAllPieces())
                {
                    if (piece == null || piece.IsGhost)
                        continue;

                    bool host = piece.Owner == Player.Host;
                    if (host)
                        metrics.HostPieces++;
                    else if (piece.Owner == Player.Opponent)
                        metrics.OpponentPieces++;
                    else
                        continue;

                    if (piece.InHarmony)
                    {
                        if (host) metrics.HostHarmony++;
                        else metrics.OpponentHarmony++;
                    }

                    if (piece.WiltLevel > 0)
                    {
                        if (host) metrics.HostWilt++;
                        else metrics.OpponentWilt++;
                    }

                    if (piece.InHarmony || (piece.IsFlower() && piece.WiltLevel == 0))
                    {
                        float dx = BoardUtils.GetColumn(piece.BoardCoordinate) - centerCol;
                        float dy = BoardUtils.GetRow(piece.BoardCoordinate) - centerRow;
                        float deg = ((Mathf.Atan2(dy, dx) * Mathf.Rad2Deg) % 360f + 360f) % 360f;
                        int q = Mathf.Clamp(Mathf.FloorToInt(deg / 90f), 0, 3);
                        if (host) hostQuads[q] = true;
                        else oppQuads[q] = true;
                    }
                }

                for (int i = 0; i < 4; i++)
                {
                    if (hostQuads[i]) metrics.HostQuads++;
                    if (oppQuads[i]) metrics.OpponentQuads++;
                }
            }

            metrics.HostRing = HarmonyRingDetector.GetRingProgress(Player.Host);
            metrics.OpponentRing = HarmonyRingDetector.GetRingProgress(Player.Opponent);

            if (MomentumManager.Instance != null)
            {
                metrics.HostMomentum = MomentumManager.Instance.GetMomentum(Player.Host);
                metrics.OpponentMomentum = MomentumManager.Instance.GetMomentum(Player.Opponent);
            }

            if (ScoringManager.Instance != null && BoardManager.Instance != null)
            {
                List<Piece> pieces = BoardManager.Instance.GetAllPieces();
                metrics.HostScore = ScoringManager.Instance.ComputeLiveScore(Player.Host, pieces);
                metrics.OpponentScore = ScoringManager.Instance.ComputeLiveScore(Player.Opponent, pieces);
            }
            else if (GameManager.Instance != null)
            {
                metrics.HostScore = GameManager.Instance.GetLiveScore(Player.Host);
                metrics.OpponentScore = GameManager.Instance.GetLiveScore(Player.Opponent);
            }

            return metrics;
        }

        public static string ResolveLogDirectory()
        {
#if UNITY_EDITOR
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.Combine(projectRoot, "Logs");
#else
            return Path.Combine(Application.persistentDataPath, "Logs");
#endif
        }

        private static string Sanitize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "none";

            var builder = new StringBuilder(value.Length);
            foreach (char c in value.Trim())
            {
                if (char.IsLetterOrDigit(c) || c is '-' or '_')
                    builder.Append(c);
                else if (char.IsWhiteSpace(c))
                    builder.Append('_');
            }

            return builder.Length > 0 ? builder.ToString() : "none";
        }

        private struct BoardMetrics
        {
            public int HostHarmony;
            public int OpponentHarmony;
            public int HostRing;
            public int OpponentRing;
            public int HostWilt;
            public int OpponentWilt;
            public int HostPieces;
            public int OpponentPieces;
            public int HostQuads;
            public int OpponentQuads;
            public int HostMomentum;
            public int OpponentMomentum;
            public int HostScore;
            public int OpponentScore;
        }
    }
}
