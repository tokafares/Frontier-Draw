using Unity.Netcode;
using UnityEngine;

namespace FrontierDraw.Player
{
    /// <summary>
    /// Moves this duelist left/right along the world X-axis based on input from
    /// PlayerInputHandler. Straight-line strafing for now (not a circular arc around
    /// the opponent) - simplest way to test whether the core timing/signal loop feels
    /// good before investing in real circle-strafe math.
    ///
    /// Now a NetworkBehaviour: only the owning client actually applies movement to
    /// the transform - OwnerNetworkTransform then replicates that position to
    /// everyone else watching (see OwnerNetworkTransform.cs for why "owner", not
    /// "server", is authoritative here).
    /// </summary>
    [RequireComponent(typeof(PlayerInputHandler))]
    public class PlayerAimController : NetworkBehaviour
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

        // NetworkBehaviour lifecycle method - fires once, right after this object
        // finishes spawning/syncing on THIS machine (host or client). At this exact
        // moment OwnerClientId is guaranteed correct, unlike in Awake/Start which can
        // run before networking info is ready.
        public override void OnNetworkSpawn()
        {
            Debug.Log($"[PlayerAimController] {name} spawned. IsOwner={IsOwner}, OwnerClientId={OwnerClientId}, IsHost={IsHost}, IsClient={IsClient}");
        }

        public override void OnGainedOwnership()
        {
            Debug.Log($"[PlayerAimController] {name} - ownership GAINED by this instance. OwnerClientId={OwnerClientId}");
        }

        private void Update()
        {
            // Only the client that owns this duelist is allowed to move it.
            if (!IsOwner) return;

            float newX = transform.position.x + input.MoveDirection * moveSpeed * Time.deltaTime;
            newX = Mathf.Clamp(newX, startX - strafeRange, startX + strafeRange);

            Vector3 position = transform.position;
            position.x = newX;
            transform.position = position;
        }
    }
}
