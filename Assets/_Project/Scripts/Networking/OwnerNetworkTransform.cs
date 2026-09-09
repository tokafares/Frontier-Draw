using Unity.Netcode.Components;

namespace FrontierDraw.Networking
{
    /// <summary>
    /// NGO's built-in NetworkTransform defaults to "server authoritative" - only the
    /// host is allowed to move the object, and clients just watch it happen.
    /// For a duel, we want each PLAYER to move their own duelist directly (feels
    /// instant, no round-trip to the host first) - that's "owner authoritative".
    /// This is the standard, documented way to get that: override one method.
    /// </summary>
    public class OwnerNetworkTransform : NetworkTransform
    {
        protected override bool OnIsServerAuthoritative()
        {
            return false;
        }
    }
}
