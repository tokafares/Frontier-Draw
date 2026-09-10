using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using FrontierDraw.Core;
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
        [Tooltip("Now shows a rank title derived from PlayerStats.Wins (see RankData), not a " +
                 "raw win/loss count. Field name kept as-is so the existing Inspector wiring " +
                 "to the WinLossText object survives - only what it displays changed.")]
        [SerializeField] private Text winLossText;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button mapButton;

        [Header("PreDuelPanel (world-flavor screen shown before matchmaking)")]
        [SerializeField] private GameObject preDuelPanelRoot;
        [Tooltip("Placeholder - just a plain Image standing in for a real NPC portrait.")]
        [SerializeField] private Text preDuelFlavorText;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button preDuelBackButton;

        [Header("MapPanel (static rank-tier map)")]
        [SerializeField] private GameObject mapPanelRoot;
        [Tooltip("The pin Image's RectTransform - moved between 3 preset spots based on rank tier.")]
        [SerializeField] private RectTransform mapPinRectTransform;
        [Tooltip("Anchored positions for tiers 0 (Drifter), 1 (Gunslinger), 2 (Legend), in order.")]
        [SerializeField] private Vector2[] mapPinPositionsByTier = new Vector2[]
        {
            new Vector2(-150f, -80f),
            new Vector2(0f, 40f),
            new Vector2(160f, 120f),
        };
        [SerializeField] private Button mapBackButton;

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

            if (playButton != null) playButton.onClick.AddListener(ShowPreDuel);
            if (continueButton != null) continueButton.onClick.AddListener(ShowMatchmaking);
            if (preDuelBackButton != null) preDuelBackButton.onClick.AddListener(ShowMainMenu);
            if (cancelButton != null) cancelButton.onClick.AddListener(ShowMainMenu);
            if (hostButton != null) hostButton.onClick.AddListener(OnHostClicked);
            if (joinButton != null) joinButton.onClick.AddListener(OnJoinClicked);
            if (mapButton != null) mapButton.onClick.AddListener(ShowMap);
            if (mapBackButton != null) mapBackButton.onClick.AddListener(ShowMainMenu);

            // Placeholder - no SettingsPanel built yet.
            if (settingsButton != null)
            {
                settingsButton.onClick.AddListener(() =>
                    Debug.Log("[MainMenu] Settings clicked - no SettingsPanel built yet (placeholder)."));
            }

            RefreshRankTitle();
        }

        private void RefreshRankTitle()
        {
            // Re-read every time this panel shows (Awake fires fresh each time MainMenu.unity
            // loads) so a win/loss recorded during the just-finished duel is reflected
            // immediately, not just after a manual refresh.
            if (winLossText != null)
            {
                winLossText.text = RankData.GetTitle(PlayerStats.Wins);
            }
        }

        private void ShowMainMenu()
        {
            if (mainMenuPanelRoot != null) mainMenuPanelRoot.SetActive(true);
            if (preDuelPanelRoot != null) preDuelPanelRoot.SetActive(false);
            if (mapPanelRoot != null) mapPanelRoot.SetActive(false);
            if (matchmakingPanelRoot != null) matchmakingPanelRoot.SetActive(false);

            RefreshRankTitle();

            // Cancelling out of matchmaking while hosting/waiting shouldn't leave a stale
            // subscription around for next time Host is clicked.
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnRemoteClientConnected;
            }
        }

        private void ShowPreDuel()
        {
            if (mainMenuPanelRoot != null) mainMenuPanelRoot.SetActive(false);
            if (preDuelPanelRoot != null) preDuelPanelRoot.SetActive(true);

            if (preDuelFlavorText != null)
            {
                preDuelFlavorText.text = FlavorText.GetRandomLine();
            }
        }

        private void ShowMap()
        {
            if (mainMenuPanelRoot != null) mainMenuPanelRoot.SetActive(false);
            if (mapPanelRoot != null) mapPanelRoot.SetActive(true);

            if (mapPinRectTransform != null && mapPinPositionsByTier.Length > 0)
            {
                int tier = Mathf.Clamp(RankData.GetTier(PlayerStats.Wins), 0, mapPinPositionsByTier.Length - 1);
                mapPinRectTransform.anchoredPosition = mapPinPositionsByTier[tier];
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
            if (preDuelPanelRoot != null) preDuelPanelRoot.SetActive(false);
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
