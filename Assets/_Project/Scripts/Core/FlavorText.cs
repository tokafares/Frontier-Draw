using UnityEngine;

namespace FrontierDraw.Core
{
    /// <summary>
    /// Small pool of world-flavor lines shown on the pre-duel screen, implying a wider
    /// world/reputation without any actual quest/dialogue system behind it - pure flavor text,
    /// picked at random. See PreDuelScreen in MainMenuController.
    /// </summary>
    public static class FlavorText
    {
        private static readonly string[] Lines =
        {
            "Word's spread through the territory about a new gun in town.",
            "They say the sheriff's stopped counting the graves outside of town.",
            "Another stranger rides in, hand never far from their holster.",
            "The saloon's gone quiet - everyone's heard there's a duel today."
        };

        public static string GetRandomLine()
        {
            return Lines[Random.Range(0, Lines.Length)];
        }
    }
}
