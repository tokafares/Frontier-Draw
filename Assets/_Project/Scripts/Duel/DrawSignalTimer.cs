using UnityEngine;

namespace FrontierDraw.Duel
{
    /// <summary>
    /// Plain C# timer (not a MonoBehaviour) that picks a random delay and tracks whether
    /// it has elapsed. Knows nothing about duels or players - DuelController drives it.
    /// Randomized so players can't learn a fixed pattern and jump the draw safely.
    /// </summary>
    public class DrawSignalTimer
    {
        private readonly float delaySeconds;
        private float elapsedSeconds;

        public DrawSignalTimer(float minDelaySeconds, float maxDelaySeconds)
        {
            delaySeconds = Random.Range(minDelaySeconds, maxDelaySeconds);
            elapsedSeconds = 0f;
        }

        /// <summary>Advance the timer by this frame's delta time. Returns true once elapsed.</summary>
        public bool Tick(float deltaTime)
        {
            elapsedSeconds += deltaTime;
            return elapsedSeconds >= delaySeconds;
        }
    }
}
