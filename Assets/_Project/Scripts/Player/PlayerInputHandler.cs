using UnityEngine;
using UnityEngine.InputSystem;

namespace FrontierDraw.Player
{
    /// <summary>
    /// Reads raw keyboard input for one local player and exposes it as simple values.
    /// Does not move anything or know about duel rules - just "what is this player pressing".
    /// Key bindings are set per-instance in the Inspector, so the same script works for
    /// both duelists in local 2-player testing.
    /// </summary>
    public class PlayerInputHandler : MonoBehaviour
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
