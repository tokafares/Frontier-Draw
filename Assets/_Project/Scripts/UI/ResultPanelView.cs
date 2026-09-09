using UnityEngine;
using UnityEngine.UI;

namespace FrontierDraw.UI
{
    /// <summary>
    /// Placeholder ResultPanel (Phase 6 of the plan doc): win/lose text + Rematch/Main Menu
    /// buttons. Deliberately dumb, same split as DuelHudView - this only shows/hides itself
    /// and exposes its buttons; DuelController decides WHEN to show it and WHAT the buttons do
    /// (duel-flow decisions stay in one place).
    /// </summary>
    public class ResultPanelView : MonoBehaviour
    {
        [Header("Placeholder ResultPanel (Phase 5 real UI art not built yet)")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text outcomeText;
        [SerializeField] private Button rematchButton;
        [SerializeField] private Button mainMenuButton;

        /// <summary>Fired when the Rematch button is clicked - DuelController subscribes.</summary>
        public event System.Action RematchClicked;

        /// <summary>Fired when the Main Menu button is clicked - DuelController subscribes.</summary>
        public event System.Action MainMenuClicked;

        private void Awake()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }

            if (rematchButton != null)
            {
                rematchButton.onClick.AddListener(() => RematchClicked?.Invoke());
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.AddListener(() => MainMenuClicked?.Invoke());
            }
        }

        /// <summary>Shows the panel with the given outcome text.</summary>
        public void Show(string outcome)
        {
            if (panelRoot == null)
            {
                return;
            }

            if (outcomeText != null)
            {
                outcomeText.text = outcome;
            }

            panelRoot.SetActive(true);
        }

        /// <summary>Hides the panel - called on duel reset.</summary>
        public void Hide()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }
    }
}
