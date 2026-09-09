using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using FrontierDraw.Networking;

namespace FrontierDraw.UI
{
    /// <summary>
    /// Placeholder MainMenu flow (Phase 6 of the plan doc): MainMenuPanel (Play button,
    /// win/loss placeholder, settings placeholder) and MatchmakingPanel (Host/Join buttons +
    /// join code field). "Matchmaking" here is still the manual host/join-code flow from M2 -
    /// real auto-matchmaking is M3, deliberately skipped (see frontier-draw-m2-scope memory).
    /// Replaces the old NetTestTrigger keyboard-only debug harness with real UI wired to the
    /// same NetClient methods.
    ///
    /// Lives in MainMenu.unity alongside the persistent Services/NetworkManager/NetClient
    /// GameObjects (moved here from Duel.unity - NetworkManager DontDestroyOnLoads itself,
    /// so they survive the load into the Duel scene).
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("Networking")]
        [SerializeField] private NetClient netClient;
        [Tooltip("Scene to load once hosting starts. Only the host actually triggers this load - " +
                 "NGO's scene management (Enable Scene Management on NetworkManager) syncs any " +
                 "connected, or later-connecting, client into it automatically.")]
        [SerializeField] private string duelSceneName = "Duel";

        [Header("MainMenuPanel")]
        [SerializeField] private GameObject mainMenuPanelRoot;
        [SerializeField] private Button playButton;
        [Tooltip("Placeholder - no persisted profile yet (M5: Currency & Basic Menu not built).")]
        [SerializeField] private Text winLossText;
        [SerializeField] private Button settingsButton;

        [Header("MatchmakingPanel")]
        [SerializeField] private GameObject matchmakingPanelRoot;
        [SerializeField] private Button hostButton;
        [SerializeField] private Button joinButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private InputField joinCodeInputField;
        [SerializeField] private Text statusText;
        [SerializeField] private Text hostJoinCodeText;

        private void Awake()
        {
            ShowMainMenu();

            if (playButton != null) playButton.onClick.AddListener(ShowMatchmaking);
            if (cancelButton != null) cancelButton.onClick.AddListener(ShowMainMenu);
            if (hostButton != null) hostButton.onClick.AddListener(OnHostClicked);
            if (joinButton != null) joinButton.onClick.AddListener(OnJoinClicked);

            // Placeholder - no SettingsPanel built yet.
            if (settingsButton != null)
            {
                settingsButton.onClick.AddListener(() =>
                    Debug.Log("[MainMenu] Settings clicked - no SettingsPanel built yet (placeholder)."));
            }

            if (winLossText != null)
            {
                winLossText.text = "Wins: 0  Losses: 0";
            }
        }

        private void ShowMainMenu()
        {
            if (mainMenuPanelRoot != null) mainMenuPanelRoot.SetActive(true);
            if (matchmakingPanelRoot != null) matchmakingPanelRoot.SetActive(false);

            // Cancelling out of matchmaking while hosting/waiting shouldn't leave a stale
            // subscription around for next time Host is clicked.
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnRemoteClientConnected;
            }
        }

        private void OnDestroy()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnRemoteClientConnected;
            }
        }

        private void ShowMatchmaking()
        {
            if (mainMenuPanelRoot != null) mainMenuPanelRoot.SetActive(false);
            if (matchmakingPanelRoot != null) matchmakingPanelRoot.SetActive(true);
            SetStatus("Press Host to start a duel, or enter a join code and press Join.");
            if (hostJoinCodeText != null) hostJoinCodeText.text = "";
        }

        private async void OnHostClicked()
        {
            SetStatus("Starting host...");
            string joinCode = await netClient.StartHostAsync();

            if (joinCode == null)
            {
                SetStatus("Host failed - see console.");
                return;
            }

            if (hostJoinCodeText != null)
            {
                hostJoinCodeText.text = $"Join code: {joinCode}";
            }
            SetStatus("Hosting. Share the join code above - waiting for the other player...");

            // Wait for an actual remote player before jumping to the Duel scene - loading
            // immediately (the old behavior) would swap the screen away before the host ever
            // got a chance to read/copy the join code they need to share.
            NetworkManager.Singleton.OnClientConnectedCallback += OnRemoteClientConnected;
        }

        private void OnRemoteClientConnected(ulong clientId)
        {
            // This callback also fires for the host's own connection - only react to an
            // actual REMOTE client joining (same guard NetClient uses for ownership handoff).
            if (clientId == NetworkManager.Singleton.LocalClientId)
            {
                return;
            }

            NetworkManager.Singleton.OnClientConnectedCallback -= OnRemoteClientConnected;

            SetStatus("Player joined - loading duel scene...");

            // Only the host calls this - NGO's NetworkSceneManager then syncs the connected
            // client into the same scene automatically. Requires "Enable Scene Management"
            // checked on the NetworkManager component.
            NetworkManager.Singleton.SceneManager.LoadScene(duelSceneName, LoadSceneMode.Single);
        }

        private async void OnJoinClicked()
        {
            string cleanCode = (joinCodeInputField != null ? joinCodeInputField.text : "")
                .Trim().ToUpperInvariant();

            if (string.IsNullOrEmpty(cleanCode))
            {
                SetStatus("Type a join code first.");
                return;
            }

            SetStatus("Joining...");

            bool ok = await netClient.StartClientAsync(cleanCode);
            SetStatus(ok ? "Joined! Waiting for the host to start the duel..." : "Join failed - see console.");
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
        }
    }
}
