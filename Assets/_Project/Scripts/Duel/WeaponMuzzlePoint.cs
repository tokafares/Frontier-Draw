using UnityEngine;

namespace FrontierDraw.Duel
{
    /// <summary>
    /// Marker for where a duelist's muzzle flash/gunshot effect should spawn - put this
    /// on an empty child object positioned at the Reichsrevolver's barrel tip (once the
    /// revolver prefab is attached to the character's hand bone).
    ///
    /// Purely a position/rotation marker - no logic of its own. DuelController looks this
    /// up via GetComponentInChildren on the shooter and uses its transform instead of the
    /// old "shooter position + Vector3.up" placeholder. If a duelist doesn't have one yet
    /// (e.g. still using the placeholder capsule), DuelController falls back to the old
    /// behavior automatically - so this is safe to wire in one duelist at a time.
    /// </summary>
    public class WeaponMuzzlePoint : MonoBehaviour
    {
    }
}
