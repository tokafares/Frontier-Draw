using System;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using FrontierDraw.Core;

namespace FrontierDraw.Networking
{
    /// <summary>
    /// Wraps Relay + Netcode for GameObjects setup for a 1v1 duel.
    /// Host: call StartHostAsync() -> get a join code, share it with the other player.
    /// Client: call StartClientAsync(joinCode) using the code the host shared.
    /// Requires ServicesBootstrapper to have signed in first (checks IsReady).
    /// </summary>
    public class NetClient : MonoBehaviour
    {
        // Max players connecting to the host, NOT counting the host itself.
        // A 1v1 duel = host + 1 other player = 1 extra connection.
        private const int MaxConnections = 1;

        public async Task<string> StartHostAsync()
        {
            if (!ServicesBootstrapper.IsReady)
            {
                Debug.LogError("[NetClient] Services not ready yet - can't start host.");
                return null;
            }

            try
            {
                Allocation allocation = await RelayService.Instance.CreateAllocationAsync(MaxConnections);
                string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

                var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
                var relayServerData = new RelayServerData(allocation, "udp");
                transport.SetRelayServerData(relayServerData);

                // These callbacks fire on the host when a remote client actually
                // finishes connecting/disconnecting - the definitive proof the
                // Relay connection worked, beyond just "StartHost() didn't throw".
                // NOTE: duelist ownership handoff used to happen here via a directly-dragged
                // Inspector reference, but that broke once NetClient moved into MainMenu.unity
                // (a scene reference can't point across scenes) - see
                // DuelController.AssignRemotePlayerOwnership, which now does this instead,
                // from within Duel.unity where the duelists actually live.
                NetworkManager.Singleton.OnClientConnectedCallback += clientId =>
                    Debug.Log($"[NetClient] Client connected! clientId={clientId}");
                NetworkManager.Singleton.OnClientDisconnectCallback += clientId =>
                    Debug.Log($"[NetClient] Client disconnected. clientId={clientId}");

                NetworkManager.Singleton.StartHost();

                Debug.Log($"[NetClient] Hosting. Join code: {joinCode}");
                return joinCode;
            }
            catch (Exception e)
            {
                Debug.LogError($"[NetClient] StartHostAsync failed: {e}");
                return null;
            }
        }

        public async Task<bool> StartClientAsync(string joinCode)
        {
            if (!ServicesBootstrapper.IsReady)
            {
                Debug.LogError("[NetClient] Services not ready yet - can't join.");
                return false;
            }

            try
            {
                JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

                var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
                var relayServerData = new RelayServerData(joinAllocation, "udp");
                transport.SetRelayServerData(relayServerData);

                NetworkManager.Singleton.StartClient();

                Debug.Log("[NetClient] Joined as client.");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[NetClient] StartClientAsync failed: {e}");
                return false;
            }
        }
    }
}
