using System.Collections.Generic;
using UnityEngine;
using PaiSho.Board;
using PaiSho.Pieces;

namespace PaiSho.Game
{
    public class EchoTileManager : MonoBehaviour
    {
        public static EchoTileManager Instance;

        private readonly Dictionary<Player, int> revivalPoints = new();
        private readonly Dictionary<Player, int> echoCount = new();

        public Player? PendingEchoPlayer { get; private set; }
        public readonly List<PieceType> PendingEchoTypes = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(gameObject);
            else
                Instance = this;

            revivalPoints[Player.Host] = 0;
            revivalPoints[Player.Opponent] = 0;
            echoCount[Player.Host] = 0;
            echoCount[Player.Opponent] = 0;
        }

        public int GetEchoCount(Player player)
        {
            return echoCount.TryGetValue(player, out int count) ? count : 0;
        }

        public int GetRevivalPoints(Player player)
        {
            return revivalPoints.TryGetValue(player, out int points) ? points : 0;
        }

        public bool HasPendingEchoChoice => PendingEchoPlayer.HasValue && PendingEchoTypes.Count > 0;

        public void AddRevivalPoints(Player player, int amount)
        {
            if (amount <= 0) return;
            revivalPoints[player] += amount;

            DebugLogger.Log($">>> {player} earned {amount} revival point(s). Total: {revivalPoints[player]}");

            TryResolveEchoThresholds(player);
        }

        public void ResetForNewMatch()
        {
            revivalPoints[Player.Host] = 0;
            revivalPoints[Player.Opponent] = 0;
            echoCount[Player.Host] = 0;
            echoCount[Player.Opponent] = 0;
            ClearPendingEcho();
        }

        public void ClearPendingEcho()
        {
            PendingEchoPlayer = null;
            PendingEchoTypes.Clear();
        }

        /// <summary>Human UI confirms an Echo type while a choice is pending.</summary>
        public bool ConfirmPendingEcho(PieceType type)
        {
            if (!PendingEchoPlayer.HasValue)
                return false;

            Player player = PendingEchoPlayer.Value;
            if (!PendingEchoTypes.Contains(type))
                return false;

            if (revivalPoints[player] < 10)
            {
                ClearPendingEcho();
                return false;
            }

            if (!SummonEcho(player, type))
                return false;

            revivalPoints[player] -= 10;
            ClearPendingEcho();
            TryResolveEchoThresholds(player);
            return true;
        }

        public bool SummonEcho(Player player, PieceType type)
        {
            if (!Piece.IsFlowerType(type) || type == PieceType.Lotus || type == PieceType.Orchid)
                return false;

            List<PotManager.CapturedPieceInfo> pot = PotManager.Instance.GetAllCapturedPieces();
            PotManager.CapturedPieceInfo match = null;
            foreach (var info in pot)
            {
                if (info.Owner == player && info.Type == type)
                {
                    match = info;
                    break;
                }
            }

            if (match == null)
            {
                DebugLogger.LogWarning($">>> {player} could not summon echo — {type} not in pot.");
                return false;
            }

            return SpawnEcho(player, match);
        }

        private void TryResolveEchoThresholds(Player player)
        {
            while (revivalPoints[player] >= 10)
            {
                List<PieceType> eligible = GetEligibleEchoTypes(player);
                if (eligible.Count == 0)
                    return;

                if (ShouldAutoSummon(player))
                {
                    PieceType pick = PickAutoEchoType(player, eligible);
                    if (!SummonEcho(player, pick))
                        return;

                    revivalPoints[player] -= 10;
                    continue;
                }

                // Human chooser — leave points banked until they pick.
                PendingEchoPlayer = player;
                PendingEchoTypes.Clear();
                PendingEchoTypes.AddRange(eligible);
                GameplayFeedback.Show("Choose an Echo flower to summon.", 5f);
                return;
            }
        }

        private static bool ShouldAutoSummon(Player player)
        {
            if (HeadlessActionExecutor.IsActive)
                return true;

            if (AiController.Instance != null && AiController.Instance.IsAiPlayer(player))
                return true;

            return false;
        }

        private static List<PieceType> GetEligibleEchoTypes(Player player)
        {
            var types = new List<PieceType>();
            var seen = new HashSet<PieceType>();
            List<PotManager.CapturedPieceInfo> pot = PotManager.Instance.GetAllCapturedPieces();
            foreach (var info in pot)
            {
                if (info.Owner != player)
                    continue;
                if (info.Type == PieceType.Lotus || info.Type == PieceType.Orchid)
                    continue;
                if (!Piece.IsFlowerType(info.Type))
                    continue;
                if (!seen.Add(info.Type))
                    continue;
                types.Add(info.Type);
            }

            return types;
        }

        private static PieceType PickAutoEchoType(Player player, List<PieceType> eligible)
        {
            // Prefer a recently useful flower that is already on-board for the player.
            if (BoardManager.Instance != null)
            {
                var onBoard = new HashSet<PieceType>();
                foreach (Piece piece in BoardManager.Instance.GetAllPieces())
                {
                    if (piece != null && piece.Owner == player && piece.InHarmony)
                        onBoard.Add(piece.Type);
                }

                foreach (PieceType type in eligible)
                {
                    if (onBoard.Contains(type))
                        return type;
                }
            }

            return eligible[0];
        }

        private bool SpawnEcho(Player player, PotManager.CapturedPieceInfo candidate)
        {
            int targetPos = FindEchoSpawnCoordinate(candidate.Coordinate);
            if (targetPos < 0)
            {
                DebugLogger.LogWarning($">>> {player} could not summon echo — no empty board space.");
                return false;
            }

            Vector3 start = PieceMotion.GetAutoPlaceOrigin(player);
            Piece echo = BoardManager.Instance.PlacePiece(
                player, candidate.Type, targetPos, existingVisual: null, preserveWorldPose: true);
            if (echo == null)
                return false;

            echo.transform.position = start;
            echo.IsNewThisTurn = true;
            echo.PointValue *= 2;
            echo.IsGhost = true;
            echo.SetVisualState("ghost");
            echoCount[player] = GetEchoCount(player) + 1;

            GameLogManager.Instance?.Log(ActionType.EchoSummon, player, candidate.Type, targetPos, targetPos);
            DebugLogger.Log($">>> {player} summoned a Ghost Echo: {candidate.Type} at {targetPos}. Move it to awaken.");
            GameplayFeedback.Show($"{player} summoned a Ghost {candidate.Type}!");

            if (PieceFeedbackManager.Instance != null && !HeadlessActionExecutor.IsActive)
            {
                PieceFeedbackManager.Instance.ExecutePlace(echo, () =>
                {
                    PieceStateAnimator.Ensure(echo)?.SyncFromPiece(immediate: true);
                    GameplayVisualizer.Instance?.Refresh();
                }, PiecePlacementMotion.TrayToBoard);
            }
            else
            {
                BoardManager.Instance.ApplyBoardSeatedVisual(echo.gameObject, targetPos, echo.BoardYawDegrees);
                GameplayVisualizer.Instance?.Refresh();
            }

            return true;
        }

        private static int FindEchoSpawnCoordinate(int preferredCoordinate)
        {
            if (IsOpenEchoSpace(preferredCoordinate))
                return preferredCoordinate;

            var visited = new HashSet<int> { preferredCoordinate };
            var queue = new Queue<int>();
            queue.Enqueue(preferredCoordinate);

            while (queue.Count > 0)
            {
                int current = queue.Dequeue();

                foreach (int neighbor in BoardUtils.GetAdjacentCoordinates(current))
                {
                    if (!visited.Add(neighbor))
                        continue;

                    if (IsOpenEchoSpace(neighbor))
                        return neighbor;

                    queue.Enqueue(neighbor);
                }
            }

            return -1;
        }

        private static bool IsOpenEchoSpace(int coordinate)
        {
            return BoardUtils.IsValidPointCoordinate(coordinate) &&
                   !BoardUtils.IsGate(coordinate) &&
                   BoardManager.Instance.GetPieceAt(coordinate) == null;
        }

        public void OnEchoMoved(Piece piece)
        {
            if (!piece.IsGhost) return;

            piece.IsGhost = false;
            piece.SetVisualState("vibrant");
            PieceStateAnimator.Ensure(piece)?.NotifyRevive();
            DebugLogger.Log($">>> Echo Tile {piece.Type} has entered play.");
        }
    }
}
