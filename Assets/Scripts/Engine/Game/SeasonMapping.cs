using PaiSho.Domain;

namespace PaiSho.Game
{
    /// <summary>Maps Unity/game <see cref="Season"/> to Domain <see cref="GardenSeason"/>.</summary>
    public static class SeasonMapping
    {
        public static GardenSeason ToDomain(Season season) => (GardenSeason)(int)season;

        public static GardenSeason Current()
        {
            if (SeasonManager.Instance == null)
                return GardenSeason.Spring;
            return ToDomain(SeasonManager.Instance.GetCurrentSeason());
        }
    }
}
