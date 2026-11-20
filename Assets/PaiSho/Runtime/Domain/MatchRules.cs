namespace PaiSho.Domain
{
    /// <summary>
    /// Match rule profile for the portfolio slice vs full house garden-life systems.
    /// </summary>
    /// <remarks>
    /// Default is <see cref="HouseExtrasEnabled"/> = false: core Pai Sho only
    /// (placement, movement, harmony, capture, ring victory, forfeit).
    /// Seasons calendar, momentum, wilt aging, echo ghosts, and related bonuses
    /// from <c>docs/RULES.md</c> §9 stay implemented but inactive until this
    /// flag is set true (or <see cref="EnableHouseExtras"/> is called).
    /// Design text and manager code are intentionally kept for easy restore.
    /// </remarks>
    public static class MatchRules
    {
        /// <summary>
        /// When false (portfolio default), seasons / momentum / wilt / echo extras are parked.
        /// Set true to restore house systems without deleting design or manager code.
        /// </summary>
        public static bool HouseExtrasEnabled { get; set; }

        /// <summary>Portfolio vertical slice: core rules only.</summary>
        public static void UsePortfolioCore() => HouseExtrasEnabled = false;

        /// <summary>Restore seasons, momentum, wilt, echo, and related bonuses.</summary>
        public static void EnableHouseExtras() => HouseExtrasEnabled = true;
    }
}
