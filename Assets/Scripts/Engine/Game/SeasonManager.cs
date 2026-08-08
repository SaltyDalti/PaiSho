using System.Collections.Generic;
using UnityEngine;
using PaiSho.Pieces;
using PaiSho.Domain;

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
            if (SeasonRules.ShouldRotate(turnCounter))
            {
                RotateSeason();
                turnCounter = 0;
            }
        }

        public void RotateSeason()
        {
            currentSeason = (Season)(int)SeasonRules.Next(SeasonMapping.ToDomain(currentSeason));
            Debug.Log($">>> The season has changed to: {currentSeason}");
        }

        public bool IsInSeason(PieceType type) =>
            SeasonRules.IsInSeason(type, SeasonMapping.ToDomain(currentSeason));

        public void EvaluateSeasonalBonuses(Player player, List<Piece> pieces)
        {
            Seat seat = PlacementValidator.ToSeat(player);
            List<PieceStatus> snapshot = PieceStatusFactory.FromPieces(pieces);
            bool placed = MovementManager.Instance != null
                && MovementManager.Instance.PlacedThisTurn(player);
            int moved = MovementManager.Instance != null
                ? MovementManager.Instance.GetMovedTileCount()
                : 0;

            SeasonalTurnContext ctx = SeasonRules.BuildContext(
                SeasonMapping.ToDomain(currentSeason),
                seat,
                snapshot,
                placed,
                moved);
            SeasonalBonusResult bonuses = SeasonRules.EvaluateBonuses(ctx);

            if (bonuses.ScoreBonus > 0)
                DebugLogger.Log($">>> {player} earned {bonuses.ScoreBonus} bonus points from {currentSeason} rewards!");

            if (bonuses.MomentumBonus > 0)
                DebugLogger.Log($">>> {player} earned {bonuses.MomentumBonus} momentum from {currentSeason} rewards!");

            ScoringManager.Instance?.AwardBonus(player, bonuses.ScoreBonus);
            MomentumManager.Instance?.AwardBonus(player, bonuses.MomentumBonus);
        }
    }
}
