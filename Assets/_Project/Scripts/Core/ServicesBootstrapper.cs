using System;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;

namespace FrontierDraw.Core
{
    /// <summary>
    /// Signs the game into Unity Gaming Services on startup (anonymous auth).
    /// Must finish successfully before anything touches Relay/Networking.
    /// Attach this to one persistent GameObject that exists before any
    /// networking code runs (e.g. a "Services" object in the Bootstrap scene).
    /// </summary>
    public class ServicesBootstrapper : MonoBehaviour
    {
        public static bool IsReady { get; private set; }

        // async void is normally avoided (exceptions can't be awaited/caught by a caller),
        // but it's the accepted pattern for a Unity lifecycle method like Start(),
        // since Unity itself can't "await" Start(). We wrap the body in try/catch instead.
        private async void Start()
        {
            await InitializeServices();
        }

        private async Task InitializeServices()
        {
            try
            {
                await UnityServices.InitializeAsync();

                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                }

                IsReady = true;
                Debug.Log($"[ServicesBootstrapper] Signed in. Player ID: {AuthenticationService.Instance.PlayerId}");
            }
            catch (Exception e)
            {
                IsReady = false;
                Debug.LogError($"[ServicesBootstrapper] Failed to initialize Unity Services: {e}");
            }
        }
    }
}
