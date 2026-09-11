using UnityEngine;

namespace FrontierDraw.Duel
{
    /// <summary>
    /// Computes whether this duelist is close enough to their opponent to land a hit,
    /// and exposes that as InRange. No reticle UI yet, so as a placeholder it tints a
    /// renderer green (in range) or red (out of range) - matches West Gunfighter's
    /// red-cross/green-target readability idea from the plan.
    ///
    /// Item 2 (character import): the old [RequireComponent(typeof(Renderer))] assumed
    /// the capsule's own MeshRenderer lived on this same GameObject. Now that the
    /// placeholder capsule is being replaced by the Malbers character, point
    /// targetRenderer at whichever renderer should carry the tint (e.g. the character's
    /// SkinnedMeshRenderer) - leave it unset to fall back to the old self-lookup
    /// behavior unchanged.
    /// </summary>
    public class LockOnTarget : MonoBehaviour
    {
        [Tooltip("The opponent duelist to measure distance against.")]
        [SerializeField] private Transform opponent;

        [Tooltip("Maximum distance (world units) at which a hit is considered possible.")]
        [SerializeField] private float inRangeDistance = 5f;

        [Tooltip("Renderer to tint red/green. Leave empty to use this GameObject's own " +
                 "Renderer (the old capsule behavior) - set explicitly (e.g. the character's " +
                 "SkinnedMeshRenderer) once the placeholder capsule is replaced.")]
        [SerializeField] private Renderer targetRenderer;

        private Renderer capsuleRenderer;
        private Material materialInstance;

        /// <summary>True if close enough to the opponent to land a hit right now.</summary>
        public bool InRange { get; private set; }

        private void Awake()
        {
            capsuleRenderer = targetRenderer != null ? targetRenderer : GetComponent<Renderer>();
            if (capsuleRenderer == null)
            {
                Debug.LogWarning($"[LockOnTarget] {name} has no renderer to tint - assign " +
                                  "Target Renderer in the Inspector.");
                return;
            }

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

            if (materialInstance != null)
            {
                materialInstance.color = InRange ? Color.green : Color.red;
            }
        }
    }
}
