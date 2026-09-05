using UnityEngine;

namespace FrontierDraw.Duel
{
    /// <summary>
    /// Computes whether this duelist is close enough to their opponent to land a hit,
    /// and exposes that as InRange. No reticle UI yet, so as a placeholder it tints this
    /// duelist's own capsule green (in range) or red (out of range) - matches West
    /// Gunfighter's red-cross/green-target readability idea from the plan.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class LockOnTarget : MonoBehaviour
    {
        [Tooltip("The opponent duelist to measure distance against.")]
        [SerializeField] private Transform opponent;

        [Tooltip("Maximum distance (world units) at which a hit is considered possible.")]
        [SerializeField] private float inRangeDistance = 5f;

        private Renderer capsuleRenderer;
        private Material materialInstance;

        /// <summary>True if close enough to the opponent to land a hit right now.</summary>
        public bool InRange { get; private set; }

        private void Awake()
        {
            capsuleRenderer = GetComponent<Renderer>();
            // .material (not .sharedMaterial) creates a per-instance copy, so tinting
            // this duelist doesn't also tint the other one if they share a material asset.
            materialInstance = capsuleRenderer.material;
        }

        private void Update()
        {
            if (opponent == null)
            {
                return;
            }

            float distance = Vector3.Distance(transform.position, opponent.position);
            InRange = distance <= inRangeDistance;

            materialInstance.color = InRange ? Color.green : Color.red;
        }
    }
}
