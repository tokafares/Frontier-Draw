using UnityEngine;

namespace FrontierDraw.Player
{
    /// <summary>
    /// Moves this duelist left/right along the world X-axis based on input from
    /// PlayerInputHandler. Straight-line strafing for now (not a circular arc around
    /// the opponent) - simplest way to test whether the core timing/signal loop feels
    /// good before investing in real circle-strafe math.
    /// </summary>
    [RequireComponent(typeof(PlayerInputHandler))]
    public class PlayerAimController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 3f;

        [Tooltip("How far this duelist can strafe from its starting X position, in either direction.")]
        [SerializeField] private float strafeRange = 2f;

        private PlayerInputHandler input;
        private float startX;

        private void Awake()
        {
            input = GetComponent<PlayerInputHandler>();
            startX = transform.position.x;
        }

        private void Update()
        {
            float newX = transform.position.x + input.MoveDirection * moveSpeed * Time.deltaTime;
            newX = Mathf.Clamp(newX, startX - strafeRange, startX + strafeRange);

            Vector3 position = transform.position;
            position.x = newX;
            transform.position = position;
        }
    }
}
