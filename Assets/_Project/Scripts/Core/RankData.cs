namespace FrontierDraw.Core
{
    /// <summary>
    /// Win-count -> rank title/tier lookup. Presentational world-flavor only - no gameplay
    /// effect (no stat bonuses, no matchmaking weight). Shared by MainMenuController (rank
    /// title text) and the MapScreen (pin position), so thresholds live in one place.
    /// </summary>
    public static class RankData
    {
        /// <summary>Rank tier index (0-2) for a given win count - used to place the map pin.</summary>
        public static int GetTier(int wins)
        {
            if (wins >= 7) return 2;
            if (wins >= 3) return 1;
            return 0;
        }

        /// <summary>Flavor title shown in the MainMenu for a given win count.</summary>
        public static string GetTitle(int wins)
        {
            switch (GetTier(wins))
            {
                case 2: return "Legend of the Frontier";
                case 1: return "Gunslinger";
                default: return "Drifter";
            }
        }
    }
}
