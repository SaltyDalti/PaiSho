using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using PaiSho.Pieces;

namespace PaiSho.Game
{
    /// <summary>
    /// 54-tile reserve per player, 7-tile hand after spring, on-hold pile during spring draws.
    /// </summary>
    public class ReserveManager : MonoBehaviour
    {
        public static ReserveManager Instance;

        private readonly Dictionary<Player, List<PieceType>> reserve = new();
        private readonly Dictionary<Player, List<PieceType>> hand = new();
        private readonly Dictionary<Player, List<PieceType>> onHold = new();
        private readonly Dictionary<Player, PieceType?> springDraw = new();
        private readonly Dictionary<Player, int> playTurnsByPlayer = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(gameObject);
            else
            {
                Instance = this;
                EnsurePlayerEntries();
            }
        }

        private void EnsurePlayerEntries()
        {
            foreach (Player player in new[] { Player.Host, Player.Opponent })
            {
                if (!reserve.ContainsKey(player))
                    reserve[player] = new List<PieceType>();
                if (!hand.ContainsKey(player))
                    hand[player] = new List<PieceType>();
                if (!onHold.ContainsKey(player))
                    onHold[player] = new List<PieceType>();
                if (!springDraw.ContainsKey(player))
                    springDraw[player] = null;
                if (!playTurnsByPlayer.ContainsKey(player))
                    playTurnsByPlayer[player] = 0;
            }
        }

        public void InitializeDefaultReserves()
        {
            EnsurePlayerEntries();
            foreach (Player player in new[] { Player.Host, Player.Opponent })
            {
                reserve[player] = CreateBasicReserve();
                hand[player] = new List<PieceType>();
                onHold[player] = new List<PieceType>();
                springDraw[player] = null;
                playTurnsByPlayer[player] = 0;
            }
        }

        private static List<PieceType> CreateBasicReserve()
        {
            var tiles = new List<PieceType>();

            PieceType[] flowers =
            {
                PieceType.Jasmine, PieceType.Rose, PieceType.Lily,
                PieceType.Jade, PieceType.Chrysanthemum, PieceType.Rhododendron
            };

            foreach (PieceType flower in flowers)
            {
                for (int i = 0; i < 6; i++)
                    tiles.Add(flower);
            }

            for (int i = 0; i < 3; i++) tiles.Add(PieceType.Knotweed);
            for (int i = 0; i < 3; i++) tiles.Add(PieceType.Wheel);
            for (int i = 0; i < 3; i++) tiles.Add(PieceType.Rock);
            for (int i = 0; i < 3; i++) tiles.Add(PieceType.Boat);
            for (int i = 0; i < 3; i++) tiles.Add(PieceType.Lotus);
            for (int i = 0; i < 3; i++) tiles.Add(PieceType.Orchid);

            return tiles;
        }

        public void PrepareSpringTurn(Player player)
        {
            springDraw[player] = DrawUntilFlower(player);
            if (springDraw[player].HasValue)
                DebugLogger.Log($"{player} drew {springDraw[player].Value} for spring (flowers only).");
        }

        private PieceType? DrawUntilFlower(Player player)
        {
            while (reserve[player].Count > 0)
            {
                PieceType drawn = DrawRandomFromReserve(player);
                if (PieceRules.IsBasicFlower(drawn))
                    return drawn;

                onHold[player].Add(drawn);
                DebugLogger.Log($"{drawn} held for {player} until after their 3rd play turn.");
            }

            DebugLogger.LogWarning($"{player} reserve empty while drawing spring flower.");
            return null;
        }

        private PieceType DrawRandomFromReserve(Player player)
        {
            int index = Random.Range(0, reserve[player].Count);
            PieceType drawn = reserve[player][index];
            reserve[player].RemoveAt(index);
            return drawn;
        }

        public PieceType? GetSpringDrawnFlower(Player player)
        {
            return springDraw.TryGetValue(player, out PieceType? drawn) ? drawn : null;
        }

        public void ClearSpringDraw(Player player)
        {
            springDraw[player] = null;
        }

        public void DealOpeningHands(int handSize)
        {
            foreach (Player player in new[] { Player.Host, Player.Opponent })
                DealHand(player, handSize);
        }

        private void DealHand(Player player, int count)
        {
            hand[player].Clear();
            for (int i = 0; i < count && reserve[player].Count > 0; i++)
                hand[player].Add(DrawRandomFromReserve(player));

            DebugLogger.Log($"{player} dealt a hand of {hand[player].Count} tiles.");
        }

        public void OnPlayerFinishedPlayTurn(Player player)
        {
            playTurnsByPlayer[player]++;
            if (playTurnsByPlayer[player] == PieceRules.TurnsBeforeHoldRelease)
                ReleaseHold(player);
        }

        public void OnPlayTurnStart(Player player)
        {
            DrawUpToHandSize(player, PieceRules.HandSize);
        }

        private void ReleaseHold(Player player)
        {
            if (!onHold.TryGetValue(player, out List<PieceType> held) || held.Count == 0)
                return;

            int count = held.Count;
            reserve[player].AddRange(held);
            held.Clear();
            DebugLogger.Log($"{player}'s {count} held tile(s) returned to reserve.");

            GameplayFeedback.Show(player == Player.Host
                ? $"Specials unlocked! {count} held tile(s) — Lotus and Dragon Orchid — are back in your reserve."
                : $"Opponent's specials unlocked — {count} held tile(s) returned to their reserve.",
                5f);
        }

        private void DrawUpToHandSize(Player player, int handSize)
        {
            while (hand[player].Count < handSize && reserve[player].Count > 0)
                hand[player].Add(DrawRandomFromReserve(player));
        }

        public bool HasInHand(Player player, PieceType type)
        {
            return hand.TryGetValue(player, out var list) && list.Contains(type);
        }

        public bool HasAvailableToPlace(Player player, PieceType type)
        {
            if (GameStateManager.Instance.IsSpringPhase())
                return GetSpringDrawnFlower(player) == type;

            return HasInHand(player, type);
        }

        public void RemovePlacedTile(Player player, PieceType type)
        {
            if (GameStateManager.Instance.IsSpringPhase())
            {
                ClearSpringDraw(player);
                return;
            }

            hand[player].Remove(type);
        }

        public int GetReserveCount(Player player) => reserve[player].Count;
        public int GetHandCount(Player player) => hand[player].Count;
        public int GetOnHoldCount(Player player) =>
            onHold.TryGetValue(player, out List<PieceType> list) ? list.Count : 0;

        public int GetPlayTurns(Player player) =>
            playTurnsByPlayer.TryGetValue(player, out int turns) ? turns : 0;

        public bool HasHoldReleased(Player player)
        {
            return playTurnsByPlayer.TryGetValue(player, out int turns)
                && turns >= PieceRules.TurnsBeforeHoldRelease;
        }

        public IReadOnlyList<PieceType> GetHand(Player player)
        {
            return hand.TryGetValue(player, out List<PieceType> list)
                ? list
                : (IReadOnlyList<PieceType>)System.Array.Empty<PieceType>();
        }

        public IReadOnlyList<PieceType> GetOnHold(Player player)
        {
            return onHold.TryGetValue(player, out List<PieceType> list)
                ? list
                : (IReadOnlyList<PieceType>)System.Array.Empty<PieceType>();
        }

        public IEnumerable<KeyValuePair<PieceType, int>> GetHandCounts(Player player)
        {
            return GetHand(player)
                .GroupBy(type => type)
                .Select(group => new KeyValuePair<PieceType, int>(group.Key, group.Count()))
                .OrderBy(entry => GetSortOrder(entry.Key));
        }

        public void AddToReserve(Player player, PieceType type)
        {
            reserve[player].Add(type);
        }

        private static int GetSortOrder(PieceType type)
        {
            return type switch
            {
                PieceType.Jasmine => 0,
                PieceType.Lily => 1,
                PieceType.Jade => 2,
                PieceType.Rose => 3,
                PieceType.Chrysanthemum => 4,
                PieceType.Rhododendron => 5,
                PieceType.Knotweed => 6,
                PieceType.Wheel => 7,
                PieceType.Rock => 8,
                PieceType.Boat => 9,
                PieceType.Lotus => 10,
                PieceType.Orchid => 11,
                _ => 99
            };
        }
    }
}
