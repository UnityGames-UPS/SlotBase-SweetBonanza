using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace SweetBonanza.View
{
    public class MainView : MonoBehaviour
    {
        internal event Action OnSpinClicked;
        internal event Action OnStopClicked;
        internal event Action OnAutoplayStopClicked;
        internal event Action OnBetIncreaseClicked;
        internal event Action OnBetDecreaseClicked;
        internal event Action OnBuyBonusClicked;
        internal event Action OnAnteBetToggled;

        [Header("Buttons")]
        [SerializeField] private Button spinButton;
        [SerializeField] private Button stopSpinButton;
        [SerializeField] private Button autoSpinStopButton;
        [SerializeField] private GameObject autoplayButtonObj;
        [SerializeField] private Button betPlusButton;
        [SerializeField] private Button betMinusButton;
        [SerializeField] private Button buyBonusButton;
        [SerializeField] private Button anteBetToggleButton;

        [Header("Texts")]
        [SerializeField] private TMP_Text balanceText;
        [SerializeField] private TMP_Text currentBetText;
        [SerializeField] private TMP_Text totalWinText;
        [SerializeField] private TMP_Text freeSpinsCountText;
        [SerializeField] private TMP_Text statusBannerText;

        [Header("Indicators & Overlays")]
        [SerializeField] private GameObject freeSpinsBadge;

        [Header("Ante Bet Display")]
        [SerializeField] private TMP_Text anteBetAmountText;

        [Header("Theme Visuals (Day / Night Free Spins)")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Sprite backgroundDaySprite;
        [SerializeField] private Sprite backgroundNightSprite;
        [SerializeField] private Image slotFrameImage;
        [SerializeField] private Sprite slotFrameDaySprite;
        [SerializeField] private Sprite slotFrameNightSprite;
        [SerializeField] private Sprite cloudsDaySprite;
        [SerializeField] private Sprite cloudsNightSprite;
        [SerializeField] private Image candyTransitionImage;

        [Header("Top Display Objects")]
        [SerializeField] private GameObject topPromoBannerObj;
        [SerializeField] private GameObject topTumbleWinBanner;
        [SerializeField] private TMP_Text topTumbleWinText;

        [Header("Bottom Bar Display Objects")]
       /* [SerializeField] private GameObject betCounterObj;*/
        [SerializeField] private Button betPlusBtn;
        [SerializeField] private Button betMinusBtn;
        [SerializeField] private TMP_Text betDisplayText;

        [SerializeField] private GameObject spinInstructionObj;
        [SerializeField] private GameObject payoutMultiplayerObj;
        [SerializeField] private TMP_Text payoutMultiplayerText;
        [SerializeField] private Image payoutMultiplierIcon;
        [SerializeField] private TMP_Text payoutMultiplierLabel;

        [SerializeField] private GameObject winCountObj;
        [SerializeField] private TMP_Text winCountText;

        [SerializeField] private GameObject autoSpinCountObj;
        [SerializeField] private TMP_Text autoSpinRemainingText;

        private double displayedWin = 0;
        private bool isCurrentlyNightMode = false;
        private TMP_Text flyerText;

        private bool isSpinning = false;
        private bool isAutoplaying = false;
        private bool isInFreeSpins = false;
        private bool isAnteBetActive = false;
        private int remainingAutoplayCount = 0;

        private CanvasGroup buyBonusCanvasGroup;
        private CanvasGroup anteBetCanvasGroup;

        private Vector2 slotFrameDayPos;
        private Vector2 buyButtonDayPos;
        private Vector2 anteButtonDayPos;
        private bool dayPositionsCaptured = false;

        /// <summary>Captures default Day (base game) anchored positions for slot frame and feature buttons.</summary>
        private void CaptureDayPositions()
        {
            if (dayPositionsCaptured) return;
            if (slotFrameImage != null) slotFrameDayPos = slotFrameImage.rectTransform.anchoredPosition;
            if (buyBonusButton != null) buyButtonDayPos = buyBonusButton.GetComponent<RectTransform>().anchoredPosition;
            if (anteBetToggleButton != null) anteButtonDayPos = anteBetToggleButton.GetComponent<RectTransform>().anchoredPosition;
            dayPositionsCaptured = true;
        }

        /// <summary>Ensures CanvasGroup components exist on Buy Feature and Ante Bet buttons.</summary>
        private void EnsureFeatureCanvasGroups()
        {
            if (buyBonusButton != null && buyBonusCanvasGroup == null)
            {
                buyBonusCanvasGroup = buyBonusButton.GetComponent<CanvasGroup>();
                if (buyBonusCanvasGroup == null) buyBonusCanvasGroup = buyBonusButton.gameObject.AddComponent<CanvasGroup>();
            }
            if (anteBetToggleButton != null && anteBetCanvasGroup == null)
            {
                anteBetCanvasGroup = anteBetToggleButton.GetComponent<CanvasGroup>();
                if (anteBetCanvasGroup == null) anteBetCanvasGroup = anteBetToggleButton.gameObject.AddComponent<CanvasGroup>();
            }
        }

        /// <summary>Updates interactivity and alpha opacity for Buy Feature and Ante Bet buttons.</summary>
        internal void UpdateFeatureButtonsVisualState()
        {
            EnsureFeatureCanvasGroups();
            bool featuresLocked = isSpinning || isAutoplaying || isInFreeSpins;

            // Ante Bet Button (BetFeaturebuttton): interactive only when fully idle
            bool anteInteractive = !featuresLocked;
            if (anteBetToggleButton != null) anteBetToggleButton.interactable = anteInteractive;
            if (anteBetCanvasGroup != null)
            {
                anteBetCanvasGroup.alpha = anteInteractive ? 1.0f : 0.42f;
                anteBetCanvasGroup.interactable = anteInteractive;
                anteBetCanvasGroup.blocksRaycasts = anteInteractive;
            }

            // Buy Feature Button (BuyFeaturebuttton): interactive only when idle AND Ante Bet is OFF
            bool buyInteractive = !featuresLocked && !isAnteBetActive;
            if (buyBonusButton != null) buyBonusButton.interactable = buyInteractive;
            if (buyBonusCanvasGroup != null)
            {
                buyBonusCanvasGroup.alpha = buyInteractive ? 1.0f : 0.42f;
                buyBonusCanvasGroup.interactable = buyInteractive;
                buyBonusCanvasGroup.blocksRaycasts = buyInteractive;
            }
        }

#if UNITY_EDITOR
        /// <summary>Ensures buttons stay fully opaque (alpha = 1) in the Unity Editor when not in Play Mode.</summary>
        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                if (buyBonusButton != null)
                {
                    var cg = buyBonusButton.GetComponent<CanvasGroup>();
                    if (cg != null)
                    {
                        cg.alpha = 1f;
                        cg.interactable = true;
                        cg.blocksRaycasts = true;
                    }
                }
                if (anteBetToggleButton != null)
                {
                    var cg = anteBetToggleButton.GetComponent<CanvasGroup>();
                    if (cg != null)
                    {
                        cg.alpha = 1f;
                        cg.interactable = true;
                        cg.blocksRaycasts = true;
                    }
                }
            }
        }
#endif

        /// <summary>Initializes canvas groups, button click listeners, and default overlay states.</summary>
        private void Awake()
        {
            CaptureDayPositions();
            EnsureFeatureCanvasGroups();
            UpdateFeatureButtonsVisualState();
            if (spinButton != null)
            {
                spinButton.onClick.AddListener(HandleSpinButtonClicked);
            }
            if (stopSpinButton != null)
            {
                stopSpinButton.onClick.AddListener(() =>
                {
                    SetStoppingButtonsState();
                    AudioManager.Instance?.PlaySpinStop();
                    OnStopClicked?.Invoke();
                });
            }
            if (autoSpinStopButton != null)
            {
                autoSpinStopButton.onClick.AddListener(() =>
                {
                    AudioManager.Instance?.PlayAutoplayStop();
                    OnAutoplayStopClicked?.Invoke();
                });
            }
            if (betPlusButton != null) betPlusButton.onClick.AddListener(() => OnBetIncreaseClicked?.Invoke());
            if (betMinusButton != null) betMinusButton.onClick.AddListener(() => OnBetDecreaseClicked?.Invoke());
            if (betPlusBtn != null) betPlusBtn.onClick.AddListener(() => OnBetIncreaseClicked?.Invoke());
            if (betMinusBtn != null) betMinusBtn.onClick.AddListener(() => OnBetDecreaseClicked?.Invoke());
            if (buyBonusButton != null) buyBonusButton.onClick.AddListener(() => OnBuyBonusClicked?.Invoke());
            if (anteBetToggleButton != null) anteBetToggleButton.onClick.AddListener(() => OnAnteBetToggled?.Invoke());

            if (candyTransitionImage != null)
            {
                candyTransitionImage.gameObject.SetActive(false);
            }
            if (freeSpinsBadge != null)
            {
                freeSpinsBadge.SetActive(false);
            }
        }

        /// <summary>Initializes spin button visuals and bottom bar layout.</summary>
        private void Start()
        {
            UpdateSpinButtonVisuals();
            SetIdleBottomBarState();
            if (topTumbleWinBanner != null) topTumbleWinBanner.SetActive(false);
            if (topPromoBannerObj != null) topPromoBannerObj.SetActive(true);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (isAutoplaying)
                {
                    if (autoSpinStopButton != null && autoSpinStopButton.gameObject.activeInHierarchy && autoSpinStopButton.interactable)
                    {
                        autoSpinStopButton.onClick?.Invoke();
                    }
                }
                else if (stopSpinButton != null && stopSpinButton.gameObject.activeInHierarchy && stopSpinButton.interactable)
                {
                    stopSpinButton.onClick?.Invoke();
                }
                else if (spinButton != null && spinButton.gameObject.activeInHierarchy && spinButton.interactable)
                {
                    HandleSpinButtonClicked();
                }
            }
        }

        /// <summary>Handles spin button clicks with press animation, stopping autoplay, or triggering spin start.</summary>
        private void HandleSpinButtonClicked()
        {
            if (spinButton != null)
            {
                spinButton.transform.DOKill();
                spinButton.transform.localScale = Vector3.one;
                spinButton.transform.DOPunchScale(Vector3.one * -0.08f, 0.14f);
            }

            if (isAutoplaying)
            {
                AudioManager.Instance?.PlayAutoplayStop();
                OnAutoplayStopClicked?.Invoke();
            }
            else if (isSpinning)
            {
                AudioManager.Instance?.PlaySpinStop();
                OnStopClicked?.Invoke();
            }
            else
            {
                AudioManager.Instance?.PlaySpinStart();
                OnSpinClicked?.Invoke();
            }
        }




        /// <summary>Updates the player's balance text display on the bottom HUD.</summary>
        internal void SetBalance(double balance)
        {
            if (balanceText != null) balanceText.text = $"${balance:F2}";
        }

        /// <summary>Updates the current bet text and calculates effective bet when Ante Bet is active.</summary>
        internal void SetBet(double baseBet, bool isAnteBet)
        {
            double effectiveBet = isAnteBet ? baseBet * 1.25 : baseBet;

            if (currentBetText != null)
            {
                currentBetText.text = $"${effectiveBet:F2}";
            }

            if (betDisplayText != null)
            {
                betDisplayText.text = $"${effectiveBet:F2}";
            }

            if (anteBetAmountText != null)
            {
                double anteCost = baseBet * 1.25;
                anteBetAmountText.text = SpriteTextUtility.FormatCurrency(anteCost);
            }
        }

        private CanvasGroup GetOrCreateCanvasGroup(GameObject go)
        {
            if (go == null) return null;
            var cg = go.GetComponent<CanvasGroup>();
            if (cg == null) cg = go.AddComponent<CanvasGroup>();
            return cg;
        }

        /// <summary>Called when Buy Feature opens or is active: all bottom bar display objects are hidden.</summary>
        public void SetBuyFeatureState()
        {
            if (spinInstructionObj != null) spinInstructionObj.SetActive(false);
            if (payoutMultiplayerObj != null) payoutMultiplayerObj.SetActive(false);
            if (winCountObj != null) winCountObj.SetActive(false);
            if (autoSpinCountObj != null) autoSpinCountObj.SetActive(false);
        }

        /// <summary>Called when a spin starts (manual or autoplay).</summary>
        public void SetSpinStartedState(bool isAutoplay, int remainingSpins)
        {
            this.isAutoplaying = isAutoplay;
            this.remainingAutoplayCount = remainingSpins;

            if (winCountObj != null) winCountObj.SetActive(false);
            if (payoutMultiplayerObj != null) payoutMultiplayerObj.SetActive(false);

            if (isAutoplay)
            {
                if (autoSpinCountObj != null) autoSpinCountObj.SetActive(true);
                if (autoSpinRemainingText != null) autoSpinRemainingText.text = $"{remainingSpins}";
                if (spinInstructionObj != null) spinInstructionObj.SetActive(false);
            }
            else
            {
                if (autoSpinCountObj != null) autoSpinCountObj.SetActive(false);
                if (spinInstructionObj != null) spinInstructionObj.SetActive(true);
            }
        }

        /// <summary>Updates bottom bar display on tumble step win: displays win count and symbol payout detail with icon.</summary>
        internal void SetTumbleStepWin(double tumbleWin, Sprite symbolSprite, int count, double payout, string detail = null)
        {
            displayedWin = tumbleWin;

            // 1. Win Count (WIN $X.XX)
            if (tumbleWin > 0)
            {
                if (winCountObj != null) winCountObj.SetActive(true);
                if (winCountText != null) winCountText.text = $"WIN ${tumbleWin:F2}";
                if (totalWinText != null) totalWinText.text = $"<color=#F1C40F>WIN</color> ${tumbleWin:F2}";
            }
            else
            {
                if (winCountObj != null) winCountObj.SetActive(false);
            }

            // 2. Win Detail (e.g. 8x BANANA PAYS $60.00)
            if (count > 0 && payout > 0)
            {
                if (payoutMultiplayerObj != null) payoutMultiplayerObj.SetActive(true);
                if (spinInstructionObj != null) spinInstructionObj.SetActive(false);

                if (payoutMultiplayerText != null) payoutMultiplayerText.text = $"{count}x";
                if (payoutMultiplierIcon != null)
                {
                    payoutMultiplierIcon.sprite = symbolSprite;
                    payoutMultiplierIcon.gameObject.SetActive(symbolSprite != null);
                }
                if (payoutMultiplierLabel != null) payoutMultiplierLabel.text = $"PAYS ${payout:F2}";

                if (totalWinText != null && !string.IsNullOrEmpty(detail))
                    totalWinText.text = $"<color=#F1C40F>WIN</color> ${tumbleWin:F2}\n<size=65%>{detail}</size>";
            }
            else
            {
                if (payoutMultiplayerObj != null) payoutMultiplayerObj.SetActive(false);
                if (!isAutoplaying && spinInstructionObj != null) spinInstructionObj.SetActive(true);
            }

            // 3. Autoplay Count
            if (isAutoplaying && remainingAutoplayCount > 0)
            {
                if (autoSpinCountObj != null) autoSpinCountObj.SetActive(true);
            }
            else
            {
                if (autoSpinCountObj != null) autoSpinCountObj.SetActive(false);
            }
        }

        /// <summary>Overload for backwards compatibility.</summary>
        internal void SetTumbleStepWin(double tumbleWin, string detail = null)
        {
            SetTumbleStepWin(tumbleWin, null, 0, 0, detail);
        }

        /// <summary>Displays the spin final win amount directly with snap transition (no animation).</summary>
        internal void SetWin(double targetWin, bool animate = false)
        {
            displayedWin = targetWin;

            if (targetWin > 0)
            {
                if (winCountObj != null) winCountObj.SetActive(true);
                if (winCountText != null) winCountText.text = $"WIN ${targetWin:F2}";
                if (totalWinText != null) totalWinText.text = $"<color=#F1C40F>WIN</color> ${targetWin:F2}";
            }
            else
            {
                if (winCountObj != null) winCountObj.SetActive(false);
                if (totalWinText != null) totalWinText.text = "$0.00";
            }

            if (!isAutoplaying)
            {
                if (payoutMultiplayerObj != null) payoutMultiplayerObj.SetActive(false);
                if (spinInstructionObj != null) spinInstructionObj.SetActive(true);
                if (autoSpinCountObj != null) autoSpinCountObj.SetActive(false);
            }
        }

        /// <summary>Restores the bottom bar back to its default idle state.</summary>
        internal void SetIdleBottomBarState()
        {
            displayedWin = 0;
            if (payoutMultiplayerObj != null) payoutMultiplayerObj.SetActive(false);

            if (isAutoplaying && remainingAutoplayCount > 0)
            {
                if (spinInstructionObj != null) spinInstructionObj.SetActive(false);
                if (autoSpinCountObj != null) autoSpinCountObj.SetActive(true);
                if (winCountObj != null) winCountObj.SetActive(false);
            }
            else
            {
                if (autoSpinCountObj != null) autoSpinCountObj.SetActive(false);
                if (spinInstructionObj != null) spinInstructionObj.SetActive(true);
                if (winCountObj != null) winCountObj.SetActive(false);
            }
        }

        #region Top Tumble Win Banner & Multiplier Bomb Fly-Up

        private TMP_Text GetFlyer()
        {
            if (flyerText == null)
            {
                var go = new GameObject("MultiplierFlyer", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                go.transform.SetParent(transform, false);
                flyerText = go.GetComponent<TextMeshProUGUI>();
                if (topTumbleWinText != null) flyerText.font = topTumbleWinText.font;
                flyerText.fontSize = 44;
                flyerText.fontStyle = FontStyles.Bold;
                flyerText.alignment = TextAlignmentOptions.Center;
                flyerText.color = new Color(1f, 0.95f, 0.15f, 1f);
                go.SetActive(false);
            }
            return flyerText;
        }

        /// <summary>Shows the purple capsule banner at the top of the grid during Free Spins night mode.</summary>
        internal void ShowTopTumbleWinBanner(double initialTumbleWin)
        {
            if (topTumbleWinBanner == null || !isInFreeSpins) return;

            if (topPromoBannerObj != null) topPromoBannerObj.SetActive(false);

            var cg = GetOrCreateCanvasGroup(topTumbleWinBanner);
            topTumbleWinBanner.SetActive(true);
            cg.DOKill();
            cg.alpha = 0f;
            cg.DOFade(1f, 0.25f);

            topTumbleWinBanner.transform.DOKill();
            topTumbleWinBanner.transform.localScale = Vector3.one * 0.85f;
            topTumbleWinBanner.transform.DOScale(1f, 0.25f).SetEase(Ease.OutBack);

            if (topTumbleWinText != null)
            {
                topTumbleWinText.text = $"<color=#FFE600>TUMBLE WIN</color>\n${initialTumbleWin:F2}";
            }
        }

        /// <summary>Hides the top tumble win banner with a smooth fade-out.</summary>
        internal void HideTopTumbleWinBanner(bool immediate = false)
        {
            if (topTumbleWinBanner == null || !topTumbleWinBanner.activeSelf) return;

            if (immediate)
            {
                topTumbleWinBanner.SetActive(false);
                if (topPromoBannerObj != null && !isInFreeSpins) topPromoBannerObj.SetActive(true);
                return;
            }

            var cg = GetOrCreateCanvasGroup(topTumbleWinBanner);
            cg.DOKill();
            cg.DOFade(0f, 0.25f).OnComplete(() =>
            {
                topTumbleWinBanner.SetActive(false);
                if (topPromoBannerObj != null && !isInFreeSpins) topPromoBannerObj.SetActive(true);
            });
        }

        /// <summary>Animates a flying multiplier from a grid bomb cell up into the top tumble banner.</summary>
        internal IEnumerator AnimateBombMultiplierFlyUp(Vector3 fromWorldPos, int multiplier, double baseTumbleWin, int runningMultiplier)
        {
            if (topTumbleWinBanner == null || !topTumbleWinBanner.activeInHierarchy) yield break;

            var flyer = GetFlyer();
            flyer.gameObject.SetActive(true);
            flyer.transform.position = fromWorldPos;
            flyer.transform.localScale = Vector3.one * 0.5f;
            flyer.text = $"{multiplier}x";

            Vector3 targetPos = topTumbleWinBanner.transform.position;

            flyer.transform.DOScale(1.25f, 0.15f);
            yield return flyer.transform.DOMove(targetPos, 0.35f).SetEase(Ease.OutQuad).WaitForCompletion();

            flyer.gameObject.SetActive(false);

            AudioManager.Instance?.PlaySymbolPop();
            topTumbleWinBanner.transform.DOKill();
            topTumbleWinBanner.transform.localScale = Vector3.one;
            topTumbleWinBanner.transform.DOPunchScale(Vector3.one * 0.18f, 0.22f, 10, 1f);

            if (topTumbleWinText != null)
            {
                topTumbleWinText.text = $"<color=#FFE600>TUMBLE WIN</color>\n${baseTumbleWin:F2} x {runningMultiplier}";
            }

            yield return new WaitForSeconds(0.15f);
        }

        /// <summary>Calculates and flashes the final multiplied tumble win on the top banner.</summary>
        internal IEnumerator FinalizeTumbleWinMultipliers(double baseTumbleWin, int totalMultiplier)
        {
            if (topTumbleWinBanner == null || !topTumbleWinBanner.activeInHierarchy) yield break;

            double finalWin = baseTumbleWin * totalMultiplier;
            topTumbleWinBanner.transform.DOKill();
            topTumbleWinBanner.transform.localScale = Vector3.one;
            topTumbleWinBanner.transform.DOPunchScale(Vector3.one * 0.25f, 0.3f, 8, 1f);

            if (topTumbleWinText != null)
            {
                topTumbleWinText.text = $"<color=#FFE600>TUMBLE WIN</color>\n<color=#FFF700>${finalWin:F2}</color>";
            }

            yield return new WaitForSeconds(0.8f);
        }

        #endregion

        /// <summary>Updates reel spinning state, toggles bet adjustments, and refreshes button visuals.</summary>
        public void SetSpinButtonInteractable(bool interactable)
        {
            this.isSpinning = !interactable;

            bool canInteractBet = interactable && !isAutoplaying && !isInFreeSpins;
            if (betPlusButton != null) betPlusButton.interactable = canInteractBet;
            if (betMinusButton != null) betMinusButton.interactable = canInteractBet;
            if (betPlusBtn != null) betPlusBtn.interactable = canInteractBet;
            if (betMinusBtn != null) betMinusBtn.interactable = canInteractBet;
            
            UpdateFeatureButtonsVisualState();
            UpdateSpinButtonVisuals();
        }

        /// <summary>Updates Ante Bet visual indicators, glow button sprite, and animated slider handle.</summary>
        internal void SetAnteBetState(bool isActive)
        {
            this.isAnteBetActive = isActive;
            UpdateFeatureButtonsVisualState();
        }

        /// <summary>Updates the HUD and spin button state for active Autoplay sessions.</summary>
        internal void SetAutoplayState(bool isAutoplay, int remainingSpins)
        {
            this.isAutoplaying = isAutoplay;
            this.remainingAutoplayCount = remainingSpins;

            bool canInteractBet = !isAutoplay && !isSpinning && !isInFreeSpins;
            if (betPlusButton != null) betPlusButton.interactable = canInteractBet;
            if (betMinusButton != null) betMinusButton.interactable = canInteractBet;
            if (betPlusBtn != null) betPlusBtn.interactable = canInteractBet;
            if (betMinusBtn != null) betMinusBtn.interactable = canInteractBet;

            UpdateFeatureButtonsVisualState();

            if (isAutoplay && remainingSpins > 0)
            {
                SetStatusMessage($"AUTO SPINS LEFT {remainingSpins}");
                if (autoSpinRemainingText != null)
                {
                    autoSpinRemainingText.text = $"{remainingSpins}";
                }
                if (autoSpinCountObj != null) autoSpinCountObj.SetActive(true);
                if (spinInstructionObj != null) spinInstructionObj.SetActive(false);
                if (payoutMultiplayerObj != null) payoutMultiplayerObj.SetActive(false);
            }
            else if (!isAutoplay)
            {
                SetStatusMessage(string.Empty);
                if (autoSpinCountObj != null) autoSpinCountObj.SetActive(false);
                if (payoutMultiplayerObj != null) payoutMultiplayerObj.SetActive(false);
                if (spinInstructionObj != null) spinInstructionObj.SetActive(true);
            }

            UpdateSpinButtonVisuals();
        }

        //** ----------------------------------------------------
        //** Controls Spin / Stop / Autoplay Button States (Hot Chilli style)
        //** ----------------------------------------------------
        /// <summary>Swaps visibility between SpinButton, StopSpin button, and AutoSpinStopButton.</summary>
        public void SetSpinningButtonsState(bool isSpinning)
        {
            if (isAutoplaying)
            {
                // AutoSpin: Active -> spinButton: OFF, stopButton: OFF, autoSpinStopButton: ON (interactable)
                if (spinButton != null) spinButton.gameObject.SetActive(false);
                if (stopSpinButton != null) stopSpinButton.gameObject.SetActive(false);
                if (autoSpinStopButton != null)
                {
                    autoSpinStopButton.gameObject.SetActive(true);
                    autoSpinStopButton.interactable = true;
                }
                if (autoplayButtonObj != null) autoplayButtonObj.SetActive(false);
                return;
            }

            // Manual Mode: autoSpinStopButton is ALWAYS OFF
            if (autoSpinStopButton != null) autoSpinStopButton.gameObject.SetActive(false);

            if (isSpinning)
            {
                // Manual: Spinning -> spinButton: OFF, stopButton: ON (interactable)
                if (spinButton != null) spinButton.gameObject.SetActive(false);
                if (stopSpinButton != null)
                {
                    stopSpinButton.gameObject.SetActive(true);
                    stopSpinButton.interactable = true;
                }
            }
            else
            {
                // Manual: Idle -> spinButton: ON (interactable), stopButton: OFF
                if (stopSpinButton != null) stopSpinButton.gameObject.SetActive(false);
                if (spinButton != null)
                {
                    spinButton.gameObject.SetActive(true);
                    spinButton.interactable = true;
                }
            }
            if (autoplayButtonObj != null) autoplayButtonObj.SetActive(!isSpinning);
        }

        /// <summary>Puts the buttons in Stopping/Resolving state: stop button remains visible but non-interactable to prevent spam.</summary>
        public void SetStoppingButtonsState()
        {
            if (isAutoplaying)
            {
                // AutoSpin: Stopping -> spinButton: OFF, stopButton: OFF, autoSpinStopButton: ON (disabled while final landing)
                if (spinButton != null) spinButton.gameObject.SetActive(false);
                if (stopSpinButton != null) stopSpinButton.gameObject.SetActive(false);
                if (autoSpinStopButton != null)
                {
                    autoSpinStopButton.gameObject.SetActive(true);
                    autoSpinStopButton.interactable = false;
                }
                if (autoplayButtonObj != null) autoplayButtonObj.SetActive(false);
                return;
            }

            // Manual: Stopping -> spinButton: OFF, stopButton: ON (disabled), autoSpinStopButton: OFF
            if (autoSpinStopButton != null) autoSpinStopButton.gameObject.SetActive(false);
            if (spinButton != null) spinButton.gameObject.SetActive(false);
            if (stopSpinButton != null)
            {
                stopSpinButton.gameObject.SetActive(true);
                stopSpinButton.interactable = false;
            }
            if (autoplayButtonObj != null) autoplayButtonObj.SetActive(false);
        }

        /// <summary>Updates spin button interactability and autoplay remaining spin count display.</summary>
        internal void UpdateSpinButtonVisuals()
        {
            SetSpinningButtonsState(isSpinning);

            if (isAutoplaying)
            {
                if (autoSpinRemainingText != null)
                {
                    autoSpinRemainingText.gameObject.SetActive(true);
                    autoSpinRemainingText.text = remainingAutoplayCount > 0 ? $"{remainingAutoplayCount}" : string.Empty;
                }
            }
            else
            {
                if (autoSpinRemainingText != null) autoSpinRemainingText.gameObject.SetActive(false);
                if (spinButton != null) spinButton.interactable = !isSpinning && !isInFreeSpins;
            }
        }

        /// <summary>Toggles Free Spins badge animation, remaining count text, and day/night theme mode.</summary>
        internal void SetFreeSpinsDisplay(bool isInFreeSpins, int remainingSpins)
        {
            this.isInFreeSpins = isInFreeSpins;
            UpdateFeatureButtonsVisualState();

            if (freeSpinsBadge != null)
            {
                if (isInFreeSpins)
                {
                    if (!freeSpinsBadge.activeSelf)
                    {
                        freeSpinsBadge.SetActive(true);
                        freeSpinsBadge.transform.DOKill();
                        freeSpinsBadge.transform.localScale = Vector3.zero;
                        freeSpinsBadge.transform.DOScale(1f, 0.35f).SetEase(Ease.OutBack);
                    }
                    else
                    {
                        freeSpinsBadge.transform.DOKill();
                        freeSpinsBadge.transform.DOPunchScale(Vector3.one * 0.15f, 0.2f);
                    }
                }
                else
                {
                    freeSpinsBadge.transform.DOKill();
                    freeSpinsBadge.transform.DOScale(0f, 0.25f).SetEase(Ease.InBack).OnComplete(() =>
                    {
                        freeSpinsBadge.SetActive(false);
                    });
                }
            }

            if (freeSpinsCountText != null) freeSpinsCountText.text = isInFreeSpins ? $"{remainingSpins}" : "";
            if (buyBonusButton != null && isInFreeSpins) buyBonusButton.interactable = false;

            // Pause promo banner cycle during free spins; resume when returning to base game
            if (isInFreeSpins)
            {
                if (topPromoBannerObj != null) topPromoBannerObj.SetActive(false);
            }
            else
            {
                if (topTumbleWinBanner != null) topTumbleWinBanner.SetActive(false);
                if (topPromoBannerObj != null) topPromoBannerObj.SetActive(true);
            }

            ApplyThemeMode(isInFreeSpins);
            UpdateSpinButtonVisuals();
        }

        /// <summary>Fades background, frame, and clouds smoothly between Day and Night (Free Spins) themes, and repositions slot grid and side buttons.</summary>
        private void ApplyThemeMode(bool nightMode)
        {
            if (isCurrentlyNightMode == nightMode) return;
            isCurrentlyNightMode = nightMode;

            CaptureDayPositions();

            // 1. Slide Buy Feature and Ante Bet buttons off-screen (Night) or back in (Day)
            float offscreenOffset = -550f;
            if (buyBonusButton != null)
            {
                RectTransform buyRect = buyBonusButton.GetComponent<RectTransform>();
                buyRect.DOKill();
                float targetX = nightMode ? buyButtonDayPos.x + offscreenOffset : buyButtonDayPos.x;
                buyRect.DOAnchorPosX(targetX, 0.45f).SetEase(nightMode ? Ease.InBack : Ease.OutBack);
            }

            if (anteBetToggleButton != null)
            {
                RectTransform anteRect = anteBetToggleButton.GetComponent<RectTransform>();
                anteRect.DOKill();
                float targetX = nightMode ? anteButtonDayPos.x + offscreenOffset : anteButtonDayPos.x;
                anteRect.DOAnchorPosX(targetX, 0.45f).SetEase(nightMode ? Ease.InBack : Ease.OutBack);
            }

            // 2. Center slot frame (Night) or return to Day offset (Day)
            if (slotFrameImage != null)
            {
                slotFrameImage.rectTransform.DOKill();
                float targetSlotX = nightMode ? 0f : slotFrameDayPos.x;
                slotFrameImage.rectTransform.DOAnchorPosX(targetSlotX, 0.5f).SetEase(Ease.InOutCubic);
            }

            // 3. Trigger candy transition overlay burst
            if (candyTransitionImage != null)
            {
                candyTransitionImage.gameObject.SetActive(true);
                candyTransitionImage.transform.DOKill();
                candyTransitionImage.transform.localScale = Vector3.zero;
                candyTransitionImage.color = new Color(1f, 1f, 1f, 1f);

                Sequence transSeq = DOTween.Sequence();
                transSeq.Append(candyTransitionImage.transform.DOScale(1.8f, 0.35f).SetEase(Ease.OutBack));
                transSeq.AppendInterval(0.1f);
                transSeq.Append(candyTransitionImage.DOFade(0f, 0.25f).SetEase(Ease.InQuad));
                transSeq.OnComplete(() =>
                {
                    candyTransitionImage.gameObject.SetActive(false);
                });
            }

            // 4. Swap Background, Frame & Clouds Sprites with smooth fade
            if (backgroundImage != null)
            {
                Sprite targetBg = nightMode ? (backgroundNightSprite != null ? backgroundNightSprite : backgroundDaySprite) : backgroundDaySprite;
                if (targetBg != null)
                {
                    backgroundImage.DOKill();
                    backgroundImage.DOFade(0.3f, 0.18f).OnComplete(() =>
                    {
                        backgroundImage.sprite = targetBg;
                        backgroundImage.DOFade(1f, 0.22f);
                    });
                }
            }

            if (slotFrameImage != null)
            {
                Sprite targetFrame = nightMode ? (slotFrameNightSprite != null ? slotFrameNightSprite : slotFrameDaySprite) : slotFrameDaySprite;
                if (targetFrame != null)
                {
                    slotFrameImage.DOKill();
                    slotFrameImage.DOFade(0.3f, 0.18f).OnComplete(() =>
                    {
                        slotFrameImage.sprite = targetFrame;
                        slotFrameImage.DOFade(1f, 0.22f);                    });
                }
            }
        }

        /// <summary>Displays a punch-animated notification or status message on the status banner.</summary>
        internal void SetStatusMessage(string message)
        {
            if (statusBannerText != null)
            {
                statusBannerText.text = message;
                statusBannerText.transform.DOKill();
                statusBannerText.transform.localScale = Vector3.one;
                statusBannerText.transform.DOPunchScale(Vector3.one * 0.12f, 0.18f);
            }
        }

        /// <summary>Kills active DOTween tweens when the MainView component is destroyed.</summary>
        private void OnDestroy()
        {
            if (slotFrameImage != null) slotFrameImage.rectTransform.DOKill();
            if (buyBonusButton != null) buyBonusButton.transform.DOKill();
            if (anteBetToggleButton != null) anteBetToggleButton.transform.DOKill();
            if (spinButton != null) spinButton.transform.DOKill();
            if (autoSpinStopButton != null) autoSpinStopButton.transform.DOKill();
            if (autoSpinRemainingText != null) autoSpinRemainingText.transform.DOKill();
        }
    }
}
