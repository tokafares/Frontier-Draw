using UnityEngine;
using UnityEngine.InputSystem;
using FrontierDraw.Player;

namespace FrontierDraw.Duel
{
    /// <summary>
    /// Owns the duel state machine for the local 2-player prototype:
    /// Positioning -> WaitingForSignal -> DrawWindow -> Resolved.
    /// No UI yet - outcomes are logged to the Console. Press R to reset and try again.
    /// </summary>
    public class DuelController : MonoBehaviour
    {
        private enum DuelState
        {
            Positioning,
            WaitingForSignal,
            DrawWindow,
            Resolved
        }

        [Header("Player References")]
        [SerializeField] private PlayerInputHandler playerAInput;
        [SerializeField] private PlayerInputHandler playerBInput;

        [Header("Signal Timing")]
        [SerializeField] private float minDelaySeconds = 2f;
        [SerializeField] private float maxDelaySeconds = 5f;

        private DuelState state;
        private DrawSignalTimer signalTimer;

        private void Start()
        {
            BeginDuel();
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            {
                BeginDuel();
                return;
            }

            switch (state)
            {
                case DuelState.WaitingForSignal:
                    TickWaitingForSignal();
                    break;

                case DuelState.DrawWindow:
                    TickDrawWindow();
                    break;
            }
        }

        private void BeginDuel()
        {
            state = DuelState.Positioning;
            Debug.Log("[Duel] Positioning... get ready.");

            signalTimer = new DrawSignalTimer(minDelaySeconds, maxDelaySeconds);
            state = DuelState.WaitingForSignal;
            Debug.Log("[Duel] Waiting for signal - do not draw yet!");
        }

        private void TickWaitingForSignal()
        {
            if (playerAInput.DrawPressedThisFrame)
            {
                Resolve("Player B wins - Player A false-started!");
                return;
            }

            if (playerBInput.DrawPressedThisFrame)
            {
                Resolve("Player A wins - Player B false-started!");
                return;
            }

            if (signalTimer.Tick(Time.deltaTime))
            {
                state = DuelState.DrawWindow;
                Debug.Log("[Duel] DRAW!");
            }
        }

        private void TickDrawWindow()
        {
            bool aPressed = playerAInput.DrawPressedThisFrame;
            bool bPressed = playerBInput.DrawPressedThisFrame;

            if (aPressed && bPressed)
            {
                Resolve("Draw (tie) - both players drew on the same frame!");
            }
            else if (aPressed)
            {
                Resolve("Player A wins - fastest draw!");
            }
            else if (bPressed)
            {
                Resolve("Player B wins - fastest draw!");
            }
        }

        private void Resolve(string outcome)
        {
            state = DuelState.Resolved;
            Debug.Log($"[Duel] {outcome} (Press R to reset)");
        }
    }
}
