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
    /// "server", is authoritative here). The strafe animation's MoveSpeed is synced
    /// separately via a NetworkVariable, since OwnerNetworkTransform only replicates
    /// the transform, not Animator parameters - without it, only the owner's own
    /// screen would ever see this duelist's legs animate.
    /// </summary>
    [RequireComponent(typeof(PlayerInputHandler))]
    public class PlayerAimController : NetworkBehaviour
    {
        [SerializeField] private float moveSpeed = 3f;

        [Tooltip("How far this duelist can strafe from its starting X position, in either direction.")]
        [SerializeField] private float strafeRange = 2f;

        [Header("Movement Animation")]
        [Tooltip("Animator that plays this duelist's Idle/Left-Strafe/Right-Strafe blend tree. " +
                 "Leave empty to skip (e.g. still using the placeholder capsule with no Animator).")]
        [SerializeField] private Animator animator;
        [Tooltip("Float parameter on the Animator - the DuelistAnimator controller blends " +
                 "Left Strafe (-1) / Pistol Idle (0) / Right Strafe (+1) off of this.")]
        [SerializeField] private string moveSpeedParameterName = "MoveSpeed";

        // Owner writes every frame it moves; everyone (including the owner itself) reads -
        // this is what lets the OTHER player's machine see this duelist's strafe animation
        // play, not just the owner's own screen. Position already syncs fine on its own via
        // OwnerNetworkTransform, but that only moves the transform - it doesn't know or care
        // about Animator parameters, so this needed its own explicit sync.
        private readonly NetworkVariable<float> networkMoveSpeed =
            new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        private PlayerInputHandler input;
        private float startX;
        private int moveSpeedParameterHash;

        private void Awake()
        {
            input = GetComponent<PlayerInputHandler>();
            startX = transform.position.x;
            moveSpeedParameterHash = Animator.StringToHash(moveSpeedParameterName);
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
            // Only the client that owns this duelist is allowed to move it - but EVERY
            // machine (owner and observers alike) needs to keep its local Animator in sync,
            // so the animator update below happens regardless of ownership.
            if (IsOwner)
            {
                float newX = transform.position.x + input.MoveDirection * moveSpeed * Time.deltaTime;
                newX = Mathf.Clamp(newX, startX - strafeRange, startX + strafeRange);

                Vector3 position = transform.position;
                position.x = newX;
                transform.position = position;

                networkMoveSpeed.Value = input.MoveDirection;
            }

            if (animator != null)
            {
                animator.SetFloat(moveSpeedParameterHash, networkMoveSpeed.Value);
            }
        }
    }
}
