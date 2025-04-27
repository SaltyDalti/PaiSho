using System.Collections.Generic;
using UnityEngine;
using PaiSho.Pieces;

namespace PaiSho.Game
{
    public enum Season
    {
        Spring,
        Summer,
        Autumn,
        Winter
    }

    public class SeasonManager : MonoBehaviour
    {
        public static SeasonManager Instance;

        private Season currentSeason = Season.Spring;
        private int turnsPerSeason = 6;
        private int turnCounter = 0;

        private void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(gameObject);
            else
                Instance = this;
        }

        public Season GetCurrentSeason()
        {
            return currentSeason;
        }

        public void AdvanceTurn()
        {
            turnCounter++;
            if (turnCounter >= turnsPerSeason)
            {
                RotateSeason();
                turnCounter = 0;
            }
        }

        public void RotateSeason()
        {
            currentSeason = (Season)(((int)currentSeason + 1) % 4);
            Debug.Log($">>> The season has changed to: {currentSeason}");
        }

        public bool IsInSeason(PieceType type)
        {
            switch (currentSeason)
            {
                case Season.Spring:
                    return type == PieceType.Jasmine || type == PieceType.Lily || type == PieceType.Jade;
                case Season.Summer:
                    return type == PieceType.Boat || type == PieceType.Knotweed;
                case Season.Autumn:
                    return type == PieceType.Rose || type == PieceType.Chrysanthemum || type == PieceType.Rhododendron;
                case Season.Winter:
                    return type == PieceType.Rock || type == PieceType.Wheel || type == PieceType.Lotus;
                default:
                    return false;
            }
        }

        public void EvaluateSeasonalBonuses(Player player, List<Piece> pieces)
    {
        int harmoniesThisTurn = 0;
        int wiltedRevived = 0;
        int movedTiles = MovementManager.Instance.GetMovedTileCount();
        int tileCount = pieces.FindAll(p => p.Owner == player).Count;
        int scoreBonus = 0;
        int momentumBonus = 0;

        Season season = currentSeason;

        foreach (var piece in pieces)
        {
            if (piece.Owner != player) continue;

            if (piece.WiltLevel < piece.PreviousWiltLevel)
                wiltedRevived++;

            if (piece.InHarmony)
                harmoniesThisTurn++;
        }

        switch (season)
        {
            case Season.Spring:
                if (MovementManager.Instance.PlacedThisTurn(player))
                    scoreBonus++;
                if (harmoniesThisTurn >= 1)
                    momentumBonus++;
                if (harmoniesThisTurn >= 3)
                    scoreBonus += 2;
                break;

            case Season.Summer:
                if (pieces.Exists(p => p.Owner == player && p.FreezeWiltNextTurn))
                    scoreBonus++;
                if (wiltedRevived >= 1)
                    momentumBonus++;
                if (wiltedRevived >= 2)
                    scoreBonus += 2;
                break;

            case Season.Autumn:
                if (wiltedRevived >= 1)
                    scoreBonus++;
                if (pieces.Exists(p => p.Owner == player && p.InHarmony && p.WiltLevel == 0 && p.TurnsSinceMoved < 2))
                    momentumBonus++;
                if (wiltedRevived >= 2 && harmoniesThisTurn >= 2)
                    scoreBonus += 2;
                break;

            case Season.Winter:
                if (movedTiles == 1)
                    scoreBonus++;
                if (movedTiles == 1 && harmoniesThisTurn >= 1)
                    momentumBonus++;
                if (harmoniesThisTurn > 0 && tileCount > 0 && harmoniesThisTurn == tileCount)
                    scoreBonus += 2;
                break;
        }

        if (scoreBonus > 0)
            DebugLogger.Log($">>> {player} earned {scoreBonus} bonus points from {season} rewards!");

        if (momentumBonus > 0)
            DebugLogger.Log($">>> {player} earned {momentumBonus} momentum from {season} rewards!");

        ScoringManager.Instance.AwardBonus(player, scoreBonus);
        MomentumManager.Instance.AwardBonus(player, momentumBonus);
    }
}
}
