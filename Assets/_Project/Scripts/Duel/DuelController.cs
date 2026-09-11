using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using FrontierDraw.Core;
using FrontierDraw.Player;
using FrontierDraw.UI;

namespace FrontierDraw.Duel
{
    /// <summary>
    /// Owns the duel state machine for the 2-player prototype:
    /// Positioning -> WaitingForSignal -> DrawWindow -> Resolved.
    ///
    /// Host-authoritative: only the HOST actually runs the state machine and decides
    /// the outcome (single source of truth - two independent judges could disagree).
    /// Each client just reports its own player's draw press to the host via ServerRpc,
    /// and the host pushes state changes (the DRAW! signal, and the final result) back
    /// out to both machines via ClientRpc so everyone sees the same thing.
    ///
    /// KNOWN ISSUE: the DRAW! signal is decided on the host's clock and reaches the
    /// non-host client one network round-trip late, giving the host a small inherent
    /// reaction-time edge. Not fixed yet - revisit once basic sync works end-to-end
    /// (e.g. by having the host stamp a server timestamp in the RPC for compensation).
    ///
    /// No UI yet - outcomes are logged to the Console. Host can press R to reset.
    /// </summary>
    public class DuelController : NetworkBehaviour
    {
        private enum DuelState
        {
            Positioning,
            WaitingForSignal,
            DrawWindow,
            Resolved
        }

        // Who a resolved duel was won by - RPCs can't send an outcome string and also
        // "figure out" who to aim the win/hit-react effects at, so this rides alongside
        // the outcome text.
        private enum DuelResult
        {
            PlayerAWins,
            PlayerBWins,
            Tie
        }

        [Header("Player References")]
        [SerializeField] private PlayerInputHandler playerAInput;
        [SerializeField] private PlayerInputHandler playerBInput;

        // Captured once in Awake, before any strafing/animation happens - every machine
        // loads the same Duel.unity scene with the same starting transforms, so this is
        // safe to capture locally on each machine rather than needing to network it.
        // Used to snap both duelists back to their starting spot/facing on Rematch.
        private Vector3 playerAStartPosition;
        private Quaternion playerAStartRotation;
        private Vector3 playerBStartPosition;
        private Quaternion playerBStartRotation;

        [Header("Signal Timing")]
        [SerializeField] private float minDelaySeconds = 2f;
        [SerializeField] private float maxDelaySeconds = 5f;

        [Header("Character Animation (Item 2 - Malbers/Mixamo character, local-only)")]
        [Tooltip("Animator on Duelist_A's character model. Leave empty to skip animation " +
                 "(e.g. still using the placeholder capsule) - camera shake/muzzle flash still play.")]
        [SerializeField] private Animator playerAAnimator;
        [Tooltip("Animator on Duelist_B's character model. Leave empty to skip animation.")]
        [SerializeField] private Animator playerBAnimator;
        [Tooltip("Animator trigger fired on both duelists the instant DRAW! shows (Idle -> Aim).")]
        [SerializeField] private string drawTriggerName = "Draw";
        [Tooltip("Animator trigger fired on the winning shooter when the duel resolves.")]
        [SerializeField] private string shootTriggerName = "Shoot";
        [Tooltip("Animator trigger fired on the losing duelist when the duel resolves.")]
        [SerializeField] private string hitReactTriggerName = "HitReact";

        [Header("Placeholder Polish (Phase 5 assets not built yet)")]
        [Tooltip("Color of the full-screen flash shown the instant DRAW! fires.")]
        [SerializeField] private Color drawSignalFlashColor = Color.white;
        [Tooltip("How long the DRAW! flash takes to fade out, in seconds.")]
        [SerializeField] private float drawSignalFlashDuration = 0.15f;
        [Tooltip("How long the placeholder muzzle-flash light stays on, in seconds.")]
        [SerializeField] private float muzzleFlashDuration = 0.08f;
        [Tooltip("Brightness of the placeholder muzzle-flash light. URP point lights need much " +
                 "higher values than the old built-in pipeline to read against scene lighting.")]
        [SerializeField] private float muzzleFlashIntensity = 200f;
        [Tooltip("How far the placeholder muzzle-flash light reaches, in world units.")]
        [SerializeField] private float muzzleFlashRange = 8f;
        [Tooltip("How far the camera shakes (world units) as a stand-in for a hit-react animation.")]
        [SerializeField] private float hitReactShakeStrength = 0.15f;
        [Tooltip("How long the hit-react camera shake lasts, in seconds.")]
        [SerializeField] private float hitReactShakeDuration = 0.2f;

        [Header("Placeholder SFX (Phase 5 real audio assets not built yet)")]
        [Tooltip("Plays on every machine the instant DRAW! fires. Leave empty to use a generated placeholder beep.")]
        [SerializeField] private AudioClip drawSignalSound;
        [Tooltip("Plays for the winning shooter (both shooters on a tie). Leave empty to use a generated placeholder beep.")]
        [SerializeField] private AudioClip gunshotSound;
        [Tooltip("Pitch (Hz) of the generated placeholder draw-signal beep, used only if drawSignalSound is unset.")]
        [SerializeField] private float placeholderDrawBeepFrequency = 880f;
        [Tooltip("Pitch (Hz) of the generated placeholder gunshot beep, used only if gunshotSound is unset.")]
        [SerializeField] private float placeholderGunshotBeepFrequency = 220f;

        [Header("DuelHUD (Phase 6 - DRAW! text)")]
        [Tooltip("Optional. Drives the DuelHUD's DRAW! text pop/fade - complements the flash below, doesn't replace it.")]
        [SerializeField] private DuelHudView hudView;

        [Header("ResultPanel (Phase 6 - win/lose banner)")]
        [Tooltip("Optional. Shown on Resolved, hidden on reset.")]
        [SerializeField] private ResultPanelView resultPanel;
        [Tooltip("Scene to load when Main Menu is clicked. Doesn't exist yet (M0/M1 only built the Duel scene) - this just warns instead of crashing until it does.")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        private DuelState state;
        private DrawSignalTimer signalTimer;
        private string lastOutcome = "";

        // Host-only bookkeeping: which player(s) have already submitted a draw this
        // duel, so a late/duplicate RPC can't resolve the duel twice.
        private bool playerAHasDrawn;
        private bool playerBHasDrawn;

        // Current opacity of the full-screen DRAW! flash, 0 (invisible) to 1 (fully white).
        // Drawn in OnGUI, driven down to 0 over time by DrawSignalFlashRoutine.
        private float drawFlashAlpha;
        private Texture2D flashTexture;
        private Camera mainCamera;
        private AudioSource audioSource;
        private AudioClip placeholderDrawBeep;
        private AudioClip placeholderGunshotBeep;

        private void Awake()
        {
            // 1x1 white pixel, tinted+faded via GUI.color when drawn - cheapest way to
            // paint a flat full-screen color in legacy OnGUI without an actual UI asset.
            flashTexture = Texture2D.whiteTexture;
            mainCamera = Camera.main;

            // Own AudioSource rather than relying on one already existing on this
            // GameObject in the scene - keeps the duel module dropped-in-portable
            // (plan doc, Section 0) without extra required scene setup.
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // 2D - this is a UI-ish cue, not positional audio.

            placeholderDrawBeep = GenerateBeepClip("PlaceholderDrawBeep", placeholderDrawBeepFrequency);
            placeholderGunshotBeep = GenerateBeepClip("PlaceholderGunshotBeep", placeholderGunshotBeepFrequency);

            if (resultPanel != null)
            {
                resultPanel.RematchClicked += OnRematchClicked;
                resultPanel.MainMenuClicked += OnMainMenuClicked;
            }

            if (playerAInput != null)
            {
                playerAStartPosition = playerAInput.transform.position;
                playerAStartRotation = playerAInput.transform.rotation;
            }

            if (playerBInput != null)
            {
                playerBStartPosition = playerBInput.transform.position;
                playerBStartRotation = playerBInput.transform.rotation;
            }
        }

        private void OnDestroy()
        {
            if (resultPanel != null)
            {
                resultPanel.RematchClicked -= OnRematchClicked;
                resultPanel.MainMenuClicked -= OnMainMenuClicked;
            }
        }

        public override void OnNetworkSpawn()
        {
            // Only the host runs the actual state machine - clients just react to RPCs.
            if (IsServer)
            {
                AssignRemotePlayerOwnership();
                BeginDuel();
            }
        }

        /// <summary>
        /// Hands ownership of playerBInput's NetworkObject to whichever remote client is
        /// connected, so that player's own input actually passes the IsOwner check.
        ///
        /// This used to live in NetClient.StartHostAsync via a directly-dragged-in Inspector
        /// reference to Duelist_B - that broke once NetClient moved to MainMenu.unity for the
        /// MainMenu/matchmaking flow, since a scene object reference can't survive pointing at
        /// an object in a DIFFERENT scene (Unity silently clears it to None). DuelController
        /// lives in the same scene as the duelists (Duel.unity), so it can hold this reference
        /// safely and do the handoff itself once the Duel scene actually spawns in.
        /// </summary>
        private void AssignRemotePlayerOwnership()
        {
            if (playerBInput == null)
            {
                return;
            }

            ulong localClientId = NetworkManager.Singleton.LocalClientId;
            foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
            {
                if (clientId == localClientId)
                {
                    continue;
                }

                playerBInput.NetworkObject.ChangeOwnership(clientId);
                Debug.Log($"[Duel] Gave ownership of '{playerBInput.name}' to clientId={clientId}");
                return;
            }

            Debug.LogWarning("[Duel] No remote client connected yet - Duelist_B stays host-owned. " +
                              "Expected the MainMenu Host flow to wait for a player before loading this scene.");
        }

        // TEMPORARY debug display - Debug.Log messages don't show anywhere in a
        // standalone build (only in the Editor Console), so this is the only way
        // to actually see the duel state/outcome when testing via the .exe.
        // Remove once a real DuelHUD panel exists (Phase 6 of the plan).
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 170, 400, 80), GUI.skin.box);
            GUILayout.Label($"Duel state: {state}");
            if (!string.IsNullOrEmpty(lastOutcome))
            {
                GUILayout.Label(lastOutcome);
            }
            GUILayout.EndArea();

            // Full-screen DRAW! flash - drawn last so it sits on top of everything else.
            if (drawFlashAlpha > 0f)
            {
                Color previousColor = GUI.color;
                GUI.color = new Color(drawSignalFlashColor.r, drawSignalFlashColor.g, drawSignalFlashColor.b, drawFlashAlpha);
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), flashTexture);
                GUI.color = previousColor;
            }
        }

        private void Update()
        {
            // Reset is host-only for now - letting either player reset independently
            // would cause the same "who's in charge" problem the state machine itself
            // has to avoid.
            if (IsServer && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            {
                BeginDuel();
                return;
            }

            // Every machine (host included) reads its own local players' input and
            // reports draw presses to the host - the host is the only one that acts
            // on them (see SubmitDrawServerRpc).
            if (playerAInput != null && playerAInput.IsOwner && playerAInput.DrawPressedThisFrame)
            {
                SubmitDrawServerRpc(true);
            }

            if (playerBInput != null && playerBInput.IsOwner && playerBInput.DrawPressedThisFrame)
            {
                SubmitDrawServerRpc(false);
            }

            // Only the host ticks the signal timer - clients wait for ShowDrawSignalClientRpc.
            if (IsServer && state == DuelState.WaitingForSignal)
            {
                TickWaitingForSignal();
            }
        }

        private void BeginDuel()
        {
            state = DuelState.Positioning;
            lastOutcome = "";
            playerAHasDrawn = false;
            playerBHasDrawn = false;
            Debug.Log("[Duel] Positioning... get ready.");

            HidePresentation();

            // ResultPanel is now an actual blocking overlay with clickable buttons (unlike the
            // old debug-only OnGUI text) - unlike the rest of BeginDuel, which is host-only and
            // whose visible effects were previously cosmetic, a stale ResultPanel left open on
            // a non-host client would sit on screen for the whole next duel. Broadcast the hide
            // explicitly rather than relying on each client running BeginDuel itself.
            if (IsServer)
            {
                HidePresentationClientRpc();
            }

            signalTimer = new DrawSignalTimer(minDelaySeconds, maxDelaySeconds);
            state = DuelState.WaitingForSignal;
            Debug.Log("[Duel] Waiting for signal - do not draw yet!");
        }

        private void HidePresentation()
        {
            if (hudView != null)
            {
                hudView.Hide();
            }

            if (resultPanel != null)
            {
                resultPanel.Hide();
            }

            // Runs on every machine (host directly here, non-host clients via
            // HidePresentationClientRpc below) - resets whatever was left over from the
            // previous duel (stuck Shoot/Death pose, strafed-away position) back to a
            // clean Positioning state for the rematch.
            ResetAnimatorToIdle(playerAAnimator);
            ResetAnimatorToIdle(playerBAnimator);

            // Position/rotation is owner-authoritative (see OwnerNetworkTransform) - only
            // the client that owns a duelist is allowed to move its transform, so gate
            // the reset the same way movement itself is gated, and let the sync out to
            // everyone else happen the normal way.
            //
            // Uses NetworkTransform.Teleport(...) here rather than a raw
            // transform.SetPositionAndRotation(...) - a direct transform write isn't
            // flagged as a teleport, so non-owner observers (interpolating from wherever
            // that duelist last was, mid-strafe) can visibly slide/overshoot toward the
            // reset spot instead of snapping instantly. Teleport() tells every observer
            // to snap immediately, which is what a rematch reset should look like.
            ResetDuelistTransform(playerAInput, playerAStartPosition, playerAStartRotation);
            ResetDuelistTransform(playerBInput, playerBStartPosition, playerBStartRotation);
        }

        /// <summary>Snaps a duelist back to its start position/rotation for a rematch, only if
        /// this machine owns it (see HidePresentation). Uses NetworkTransform.Teleport(...)
        /// instead of a raw transform write so every observer snaps instantly instead of
        /// interpolating/overshooting - see the comment at the HidePresentation call site.</summary>
        private static void ResetDuelistTransform(PlayerInputHandler duelist, Vector3 startPosition, Quaternion startRotation)
        {
            if (duelist == null || !duelist.IsOwner)
            {
                return;
            }

            var networkTransform = duelist.GetComponent<NetworkTransform>();
            if (networkTransform != null)
            {
                networkTransform.Teleport(startPosition, startRotation, duelist.transform.localScale);
            }
            else
            {
                // Shouldn't happen (every duelist has OwnerNetworkTransform) but fall back
                // to a raw write rather than silently doing nothing.
                duelist.transform.SetPositionAndRotation(startPosition, startRotation);
            }
        }

        /// <summary>Forces an Animator back to its default state (Pistol Idle) and clears any
        /// pending triggers - Animator.SetTrigger alone can't get out of a state like Death that
        /// has no outgoing transition, so Rebind is used instead of another trigger.</summary>
        private static void ResetAnimatorToIdle(Animator animator)
        {
            if (animator == null)
            {
                return;
            }

            animator.Rebind();
            animator.Update(0f);
        }

        [ClientRpc]
        private void HidePresentationClientRpc()
        {
            // Host already hid its own presentation locally above - this is purely for the
            // non-host client(s), but is harmless to also run on the host (it's a no-op there).
            if (IsServer)
            {
                return;
            }

            HidePresentation();
        }

        private void TickWaitingForSignal()
        {
            if (signalTimer.Tick(Time.deltaTime))
            {
                state = DuelState.DrawWindow;
                Debug.Log("[Duel] DRAW!");
                ShowDrawSignalClientRpc();
            }
        }

        /// <summary>
        /// Called on the host by any client (via RPC) when their local player presses
        /// the draw key. isPlayerA identifies which duelist drew - RPCs can't send the
        /// PlayerInputHandler reference itself across the network.
        /// </summary>
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void SubmitDrawServerRpc(bool isPlayerA)
        {
            if (state == DuelState.Resolved)
            {
                return;
            }

            if (state == DuelState.WaitingForSignal)
            {
                // Pressed before the signal - instant false-start loss.
                Resolve(
                    isPlayerA ? "Player B wins - Player A false-started!" : "Player A wins - Player B false-started!",
                    isPlayerA ? DuelResult.PlayerBWins : DuelResult.PlayerAWins);
                return;
            }

            if (state == DuelState.DrawWindow)
            {
                if (isPlayerA) playerAHasDrawn = true;
                else playerBHasDrawn = true;

                if (playerAHasDrawn && playerBHasDrawn)
                {
                    Resolve("Draw (tie) - both players drew almost simultaneously!", DuelResult.Tie);
                }
                else if (playerAHasDrawn)
                {
                    Resolve("Player A wins - fastest draw!", DuelResult.PlayerAWins);
                }
                else if (playerBHasDrawn)
                {
                    Resolve("Player B wins - fastest draw!", DuelResult.PlayerBWins);
                }
            }
        }

        private void Resolve(string outcome, DuelResult result)
        {
            state = DuelState.Resolved;
            string finalOutcome = $"{outcome} (Host: press R to reset)";
            lastOutcome = finalOutcome;
            Debug.Log($"[Duel] {finalOutcome}");
            ResolveDuelClientRpc(finalOutcome, result);
        }

        [ClientRpc]
        private void ShowDrawSignalClientRpc()
        {
            // Runs on every machine, host included (ClientRpc fires for the host's own
            // client half too) - the effect should play everywhere, so it's outside the
            // IsServer guard below, which only exists to avoid re-setting state the host
            // already set locally in TickWaitingForSignal.
            StartCoroutine(DrawSignalFlashRoutine());
            PlaySound(drawSignalSound, placeholderDrawBeep);
            TriggerAnimator(playerAAnimator, drawTriggerName);
            TriggerAnimator(playerBAnimator, drawTriggerName);

            if (hudView != null)
            {
                hudView.ShowDrawSignal();
            }

            if (IsServer)
            {
                return;
            }

            state = DuelState.DrawWindow;
            Debug.Log("[Duel] DRAW!");
        }

        [ClientRpc]
        private void ResolveDuelClientRpc(string outcome, DuelResult result)
        {
            PlayResolveEffects(result);
            RecordLocalPlayerStats(result);

            if (resultPanel != null)
            {
                resultPanel.Show(outcome);
            }

            if (IsServer)
            {
                return;
            }

            state = DuelState.Resolved;
            lastOutcome = outcome;
            Debug.Log($"[Duel] {outcome}");
        }

        // --- ResultPanel button actions ---
        // Duel-flow decisions (who's allowed to reset, what "main menu" currently does) stay
        // here rather than in ResultPanelView, same reasoning as the R-key reset above.

        private void OnRematchClicked()
        {
            // Same "one authority" reasoning as the R-key reset (see Update()) - only the host
            // actually restarts the state machine. Routed through a ServerRpc rather than
            // requiring IsServer locally, so BOTH players' Rematch buttons work, not just the
            // host's - unlike R-key reset (a dev/debug shortcut), Rematch is user-facing UI and
            // should work the same for whichever player clicks it.
            RequestRematchServerRpc();
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void RequestRematchServerRpc()
        {
            BeginDuel();
        }

        private void OnMainMenuClicked()
        {
            if (!Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
            {
                // Defensive fallback in case the scene ever gets removed from Build Settings
                // again - warn instead of hard-crashing.
                Debug.LogWarning($"[Duel] Main Menu clicked, but scene \"{mainMenuSceneName}\" " +
                                  "isn't in Build Settings.");
                return;
            }

            // Same "either player can trigger it, routed through the host" shape as Rematch -
            // whoever clicks Main Menu shouldn't leave the OTHER player stranded alone in the
            // Duel scene with no host/session once the clicker leaves.
            RequestReturnToMainMenuServerRpc();
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void RequestReturnToMainMenuServerRpc()
        {
            ReturnToMainMenuClientRpc();
        }

        [ClientRpc]
        private void ReturnToMainMenuClientRpc()
        {
            // Runs on every machine, host included - this is what actually sends BOTH players
            // back, not just the one who clicked. Deferred a frame via the coroutine below so
            // we don't tear down the NetworkManager while NGO is still mid-dispatch of this
            // very RPC.
            StartCoroutine(ReturnToMainMenuRoutine());
        }

        private IEnumerator ReturnToMainMenuRoutine()
        {
            yield return null;

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.Shutdown();
            }

            // Plain scene load, not NetworkManager.SceneManager - we've already shut networking
            // down above, so there's no NGO session left to keep in sync.
            SceneManager.LoadScene(mainMenuSceneName);
        }

        // --- Placeholder polish effects (Phase 5 real assets not built yet) ---
        // These only react to the existing resolution - no new duel logic here.

        private IEnumerator DrawSignalFlashRoutine()
        {
            drawFlashAlpha = 1f;
            float elapsed = 0f;
            while (elapsed < drawSignalFlashDuration)
            {
                elapsed += Time.deltaTime;
                drawFlashAlpha = 1f - (elapsed / drawSignalFlashDuration);
                yield return null;
            }
            drawFlashAlpha = 0f;
        }

        private void PlayResolveEffects(DuelResult result)
        {
            switch (result)
            {
                case DuelResult.PlayerAWins:
                    SpawnMuzzleFlash(playerAInput);
                    PlaySound(gunshotSound, placeholderGunshotBeep);
                    StartCoroutine(CameraShakeRoutine());
                    TriggerAnimator(playerAAnimator, shootTriggerName);
                    TriggerAnimator(playerBAnimator, hitReactTriggerName);
                    break;

                case DuelResult.PlayerBWins:
                    SpawnMuzzleFlash(playerBInput);
                    PlaySound(gunshotSound, placeholderGunshotBeep);
                    StartCoroutine(CameraShakeRoutine());
                    TriggerAnimator(playerBAnimator, shootTriggerName);
                    TriggerAnimator(playerAAnimator, hitReactTriggerName);
                    break;

                case DuelResult.Tie:
                    // Both false-started/drew together - both "fire", nobody gets hit-reacted.
                    SpawnMuzzleFlash(playerAInput);
                    SpawnMuzzleFlash(playerBInput);
                    PlaySound(gunshotSound, placeholderGunshotBeep);
                    TriggerAnimator(playerAAnimator, shootTriggerName);
                    TriggerAnimator(playerBAnimator, shootTriggerName);
                    break;
            }
        }

        /// <summary>
        /// World-flavor pass (presentational only): tallies a local win/loss into PlayerStats
        /// so the MainMenu rank title/map pin have a real number behind them. Purely a side
        /// effect of the ALREADY-DECIDED result - does not participate in, or change, how that
        /// result was decided. Runs on every machine (host included) since each machine only
        /// knows its own local player's win/loss from its own point of view. A tie records
        /// nothing for either side.
        /// </summary>
        private void RecordLocalPlayerStats(DuelResult result)
        {
            bool localIsPlayerA = playerAInput != null && playerAInput.IsOwner;
            bool localIsPlayerB = playerBInput != null && playerBInput.IsOwner;

            bool localWon = (result == DuelResult.PlayerAWins && localIsPlayerA) ||
                             (result == DuelResult.PlayerBWins && localIsPlayerB);
            bool localLost = (result == DuelResult.PlayerAWins && localIsPlayerB) ||
                              (result == DuelResult.PlayerBWins && localIsPlayerA);

            if (localWon) PlayerStats.RecordWin();
            else if (localLost) PlayerStats.RecordLoss();
        }

        /// <summary>Fires an Animator trigger if an Animator is assigned - no-ops otherwise (e.g. a
        /// duelist still using the placeholder capsule with no Animator wired in yet).</summary>
        private static void TriggerAnimator(Animator animator, string triggerName)
        {
            if (animator != null && !string.IsNullOrEmpty(triggerName))
            {
                animator.SetTrigger(triggerName);
            }
        }

        /// <summary>Plays clip if assigned, otherwise falls back to the generated placeholder.</summary>
        private void PlaySound(AudioClip clip, AudioClip placeholder)
        {
            audioSource.PlayOneShot(clip != null ? clip : placeholder);
        }

        /// <summary>
        /// Generates a short sine-wave beep as a stand-in for a real SFX clip (Phase 5
        /// asset checklist - "Gunshot / draw / footstep SFX" not built yet). Wiring first,
        /// swap in real .wav files via the Inspector fields later with no code changes.
        /// </summary>
        private static AudioClip GenerateBeepClip(string clipName, float frequencyHz)
        {
            const int sampleRate = 44100;
            const float durationSeconds = 0.12f;
            int sampleCount = Mathf.CeilToInt(sampleRate * durationSeconds);

            var clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
            var samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                // Simple linear fade-out envelope so the beep doesn't click on cutoff.
                float envelope = 1f - (t / durationSeconds);
                samples[i] = Mathf.Sin(2f * Mathf.PI * frequencyHz * t) * envelope;
            }
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// Placeholder muzzle flash: a short-lived bright Light at the winner's position.
        /// Deliberately doesn't touch the duelist's material (LockOnTarget repaints that
        /// every frame) or its transform (owner-authoritative NetworkTransform owns that) -
        /// this is a separate, purely local, non-networked GameObject.
        /// </summary>
        private void SpawnMuzzleFlash(PlayerInputHandler shooter)
        {
            if (shooter == null)
            {
                return;
            }

            // Item 2: spawn at the revolver's actual muzzle tip once one is wired in
            // (WeaponMuzzlePoint, attached under the character's hand/weapon socket).
            // Falls back to the old "shooter position + up" placeholder for any duelist
            // that doesn't have one yet, so this is safe to wire in one side at a time.
            var muzzlePoint = shooter.GetComponentInChildren<WeaponMuzzlePoint>();
            Vector3 spawnPosition = muzzlePoint != null
                ? muzzlePoint.transform.position
                : shooter.transform.position + Vector3.up;

            var flashObject = new GameObject("PlaceholderMuzzleFlash");
            flashObject.transform.position = spawnPosition;

            var light = flashObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = Color.yellow;
            light.intensity = muzzleFlashIntensity;
            light.range = muzzleFlashRange;

            Destroy(flashObject, muzzleFlashDuration);
        }

        /// <summary>
        /// Placeholder hit-react: shakes the local camera briefly instead of animating the
        /// loser (no rig/animation clips yet - see Phase 5 asset checklist). Purely local,
        /// non-networked, so it's safe to run independently on every machine.
        /// </summary>
        private IEnumerator CameraShakeRoutine()
        {
            if (mainCamera == null)
            {
                yield break;
            }

            Vector3 originalPosition = mainCamera.transform.localPosition;
            float elapsed = 0f;
            while (elapsed < hitReactShakeDuration)
            {
                elapsed += Time.deltaTime;
                Vector2 offset = Random.insideUnitCircle * hitReactShakeStrength;
                mainCamera.transform.localPosition = originalPosition + new Vector3(offset.x, offset.y, 0f);
                yield return null;
            }
            mainCamera.transform.localPosition = originalPosition;
        }
    }
}
