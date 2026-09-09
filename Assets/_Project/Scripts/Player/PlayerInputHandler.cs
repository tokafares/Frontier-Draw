using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FrontierDraw.Player
{
    /// <summary>
    /// Reads raw keyboard input for one player and exposes it as simple values.
    /// Does not move anything or know about duel rules - just "what is this player pressing".
    ///
    /// Now a NetworkBehaviour: each duelist exists on both host and client, but only
    /// the client that OWNS this object should read the local keyboard for it -
    /// otherwise every machine would end up reading input for both duelists.
    /// IsOwner is a NetworkBehaviour property: true only on the instance belonging
    /// to whichever client (or the host) currently controls this object.
    /// </summary>
    public class PlayerInputHandler : NetworkBehaviour
    {
        [Header("Movement Keys")]
        [SerializeField] private Key moveLeftKey = Key.A;
        [SerializeField] private Key moveRightKey = Key.D;

        [Header("Draw Key")]
        [SerializeField] private Key drawKey = Key.Space;

        /// <summary>-1 = moving left, 1 = moving right, 0 = not moving.</summary>
        public float MoveDirection { get; private set; }

        /// <summary>True only on the exact frame the draw key was pressed.</summary>
        public bool DrawPressedThisFrame { get; private set; }

        // Properties above aren't visible in the Inspector (Unity only shows serialized
        // fields, not C# properties). These mirror the same values purely so we can watch
        // them in the Inspector while testing - not used by any other script.
        [Header("Debug (read only, do not edit)")]
        [SerializeField] private float debugMoveDirection;
        [SerializeField] private bool debugDrawPressedThisFrame;

        private void Update()
        {
            // Not our object to control - stay neutral. (Also true before spawn/ownership
            // is assigned, so this guard has to come before touching the keyboard.)
            if (!IsOwner)
            {
                MoveDirection = 0f;
                DrawPressedThisFrame = false;
                return;
            }

            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                // No keyboard connected (e.g. running on a device) - stay neutral.
                MoveDirection = 0f;
                DrawPressedThisFrame = false;
                return;
            }

            float direction = 0f;
            if (keyboard[moveLeftKey].isPressed) direction -= 1f;
            if (keyboard[moveRightKey].isPressed) direction += 1f;
            MoveDirection = direction;

            DrawPressedThisFrame = keyboard[drawKey].wasPressedThisFrame;

            debugMoveDirection = MoveDirection;
            debugDrawPressedThisFrame = DrawPressedThisFrame;
        }
    }
}
