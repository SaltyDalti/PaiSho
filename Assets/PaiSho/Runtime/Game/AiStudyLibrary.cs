using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using PaiSho.Pieces;

namespace PaiSho.Game
{
    /// <summary>
    /// Lightweight study of recent match logs: prefer piece/action patterns that
    /// historically raised ring or harmony for the human (Host) side.
    /// </summary>
    public static class AiStudyLibrary
    {
        private const int MaxLogsToStudy = 12;
        private const int ProgressBonus = 95;
        private const int CaptureProgressBonus = 70;

        private static bool loaded;
        private static readonly Dictionary<string, int> PatternScores = new();

        public static void EnsureLoaded()
        {
            if (loaded)
                return;

            loaded = true;
            PatternScores.Clear();

            try
            {
                string directory = GameLogManager.ResolveLogDirectory();
                if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
                    return;

                var files = Directory.GetFiles(directory, "match-*-*.tsv");
                Array.Sort(files, (a, b) => File.GetLastWriteTimeUtc(b).CompareTo(File.GetLastWriteTimeUtc(a)));

                int studied = 0;
                foreach (string path in files)
                {
                    if (studied >= MaxLogsToStudy)
                        break;
                    if (StudyFile(path))
                        studied++;
                }

                if (studied > 0)
                    DebugLogger.Log($"AI study library loaded from {studied} recent match log(s).");
            }
            catch (Exception ex)
            {
                DebugLogger.LogWarning($"AI study library failed: {ex.Message}");
            }
        }

        public static void Invalidate() => loaded = false;

        public static int ScoreActionBias(GameAction action)
        {
            EnsureLoaded();
            if (PatternScores.Count == 0 || action.Kind == GameActionKind.Freeze)
                return 0;

            string key = PatternKey(action);
            return PatternScores.TryGetValue(key, out int score) ? Mathf.Min(score, 220) : 0;
        }

        private static bool StudyFile(string path)
        {
            string[] lines = File.ReadAllLines(path);
            bool sawRows = false;
            int prevHostHarmony = 0;
            int prevHostRing = 0;
            bool havePrev = false;

            foreach (string raw in lines)
            {
                if (string.IsNullOrWhiteSpace(raw) || raw.StartsWith("#"))
                    continue;
                if (raw.StartsWith("turn\t", StringComparison.Ordinal))
                    continue;

                string[] cols = raw.Split('\t');
                if (cols.Length < 10)
                    continue;

                if (!Enum.TryParse(cols[1], out Player player) ||
                    !Enum.TryParse(cols[2], out ActionType action) ||
                    !Enum.TryParse(cols[3], out PieceType piece))
                    continue;

                if (!int.TryParse(cols[6], NumberStyles.Integer, CultureInfo.InvariantCulture, out int hostHarmony) ||
                    !int.TryParse(cols[8], NumberStyles.Integer, CultureInfo.InvariantCulture, out int hostRing))
                    continue;

                sawRows = true;

                if (player == Player.Host && havePrev)
                {
                    int harmonyDelta = hostHarmony - prevHostHarmony;
                    int ringDelta = hostRing - prevHostRing;
                    if (harmonyDelta > 0 || ringDelta > 0)
                    {
                        string key = PatternKey(action, piece);
                        int gain = ringDelta * ProgressBonus + Mathf.Max(0, harmonyDelta) * (ProgressBonus / 2);
                        if (action == ActionType.Capture)
                            gain += CaptureProgressBonus;
                        PatternScores[key] = PatternScores.TryGetValue(key, out int existing)
                            ? existing + gain
                            : gain;
                    }
                }

                prevHostHarmony = hostHarmony;
                prevHostRing = hostRing;
                havePrev = true;
            }

            return sawRows;
        }

        private static string PatternKey(GameAction action)
        {
            PieceType type = action.Kind switch
            {
                GameActionKind.Place => action.PlaceType,
                GameActionKind.Move or GameActionKind.Revive or GameActionKind.Freeze
                    when action.Piece != null => action.Piece.Type,
                _ => default
            };

            ActionType mapped = action.Kind switch
            {
                GameActionKind.Place => ActionType.Placement,
                GameActionKind.Move => ActionType.Move,
                GameActionKind.Revive => ActionType.Revival,
                _ => ActionType.Move
            };

            return PatternKey(mapped, type);
        }

        private static string PatternKey(ActionType action, PieceType piece) =>
            $"{action}:{piece}";
    }
}
