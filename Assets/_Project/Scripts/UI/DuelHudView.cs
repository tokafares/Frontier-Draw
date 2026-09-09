using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace FrontierDraw.UI
{
    /// <summary>
    /// Placeholder DuelHUD "DRAW!" text element (Phase 6 of the plan doc).
    /// Deliberately dumb: DuelController tells it when to show/hide, it just handles
    /// the pop/fade animation. Kept separate from DuelController so duel *logic* and
    /// duel *presentation* stay in different files - matches the plan's UIManager
    /// pattern of listening independently rather than being wired directly into logic.
    ///
    /// Complements (not replaces) DuelController's existing full-screen flash - the
    /// flash sells the instant, this sells the word, per the plan's "unmistakable"
    /// requirement for the DRAW! signal.
    /// </summary>
    public class DuelHudView : MonoBehaviour
    {
        [Header("Placeholder DRAW! text (Phase 5 real UI art not built yet)")]
        [SerializeField] private Text drawText;
        [Tooltip("How long the DRAW! text takes to pop up to full size, in seconds.")]
        [SerializeField] private float popDuration = 0.1f;
        [Tooltip("How long the DRAW! text stays fully visible before it starts fading, in seconds.")]
        [SerializeField] private float holdDuration = 0.4f;
        [Tooltip("How long the DRAW! text takes to fade out, in seconds.")]
        [SerializeField] private float fadeDuration = 0.3f;
        [Tooltip("Scale the text pops up from (1 = no pop, just fades in).")]
        [SerializeField] private float startScale = 0.5f;

        private Coroutine activeRoutine;

        private void Awake()
        {
            if (drawText != null)
            {
                drawText.gameObject.SetActive(false);
            }
        }

        /// <summary>Plays the pop-in/hold/fade-out "DRAW!" animation. Safe to call repeatedly.</summary>
        public void ShowDrawSignal()
        {
            if (drawText == null)
            {
                return;
            }

            if (activeRoutine != null)
            {
                StopCoroutine(activeRoutine);
            }
            activeRoutine = StartCoroutine(ShowDrawSignalRoutine());
        }

        /// <summary>Hides the text immediately - called on duel reset.</summary>
        public void Hide()
        {
            if (activeRoutine != null)
            {
                StopCoroutine(activeRoutine);
                activeRoutine = null;
            }

            if (drawText != null)
            {
                drawText.gameObject.SetActive(false);
            }
        }

        private IEnumerator ShowDrawSignalRoutine()
        {
            drawText.gameObject.SetActive(true);

            Color color = drawText.color;

            // Pop in: scale up from startScale to 1, fade alpha in alongside it.
            float elapsed = 0f;
            while (elapsed < popDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / popDuration;
                drawText.transform.localScale = Vector3.one * Mathf.Lerp(startScale, 1f, t);
                drawText.color = new Color(color.r, color.g, color.b, Mathf.Lerp(0f, 1f, t));
                yield return null;
            }
            drawText.transform.localScale = Vector3.one;
            drawText.color = new Color(color.r, color.g, color.b, 1f);

            // Hold fully visible.
            yield return new WaitForSeconds(holdDuration);

            // Fade out.
            elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeDuration;
                drawText.color = new Color(color.r, color.g, color.b, Mathf.Lerp(1f, 0f, t));
                yield return null;
            }

            drawText.gameObject.SetActive(false);
            activeRoutine = null;
        }
    }
}
