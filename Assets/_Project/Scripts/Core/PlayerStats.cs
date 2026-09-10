using UnityEngine;

namespace FrontierDraw.Core
{
    /// <summary>
    /// Minimal local win/loss tally, persisted via PlayerPrefs. Presentational-only stat
    /// tracking to back the MainMenu rank title/map pin (world flavor pass) - NOT a real
    /// profile/backend system (that's M5: Currency &amp; Basic Menu, not built yet). Purely
    /// additive: nothing in the duel state machine's win/lose DECISION reads or depends on
    /// this - it only gets told the result afterward, for display purposes.
    /// </summary>
    public static class PlayerStats
    {
        private const string WinsKey = "FrontierDraw.Wins";
        private const string LossesKey = "FrontierDraw.Losses";

        public static int Wins => PlayerPrefs.GetInt(WinsKey, 0);
        public static int Losses => PlayerPrefs.GetInt(LossesKey, 0);

        public static void RecordWin()
        {
            PlayerPrefs.SetInt(WinsKey, Wins + 1);
            PlayerPrefs.Save();
        }

        public static void RecordLoss()
        {
            PlayerPrefs.SetInt(LossesKey, Losses + 1);
            PlayerPrefs.Save();
        }
    }
}
