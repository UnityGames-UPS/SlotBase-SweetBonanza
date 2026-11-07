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

        [Header("Spin Button Visuals")]
        [SerializeField] private TMP_Text spinAutoplayCountText;

        [Header("Top Tumble Win (Free Spins Night Mode)")]
        [SerializeField] private GameObject topTumbleWinBanner;
        [SerializeField] private TMP_Text topTumbleWinText;

        [Header("Bottom Bar Controls")]
        [SerializeField] private GameObject totalBetContainer;

        private Tweener winCountTweener;
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
            if (betPlusButton != null) betPlusButton.onClick.AddListener(() => OnBetIncreaseClicked?.Invoke());
            if (betMinusButton != null) betMinusButton.onClick.AddListener(() => OnBetDecreaseClicked?.Invoke());
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
            EnsureTotalBetContainer();
            UpdateBottomBarCenterState();
            if (topTumbleWinBanner != null) topTumbleWinBanner.SetActive(false);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (stopSpinButton != null && stopSpinButton.gameObject.activeInHierarchy && stopSpinButton.interactable)
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
            if (currentBetText != null)
            {
                double effectiveBet = isAnteBet ? baseBet * 1.25 : baseBet;
                currentBetText.text = $"${effectiveBet:F2}";
            }

            if (anteBetAmountText != null)
            {
                double anteCost = baseBet * 1.25;
                anteBetAmountText.text = SpriteTextUtility.FormatCurrency(anteCost);
            }
        }

        private void EnsureTotalBetContainer()
        {
            if (totalBetContainer == null)
            {
                var tbp = GameObject.Find("MainCanvas/BG/BottomBar/totalbet Parent");
                if (tbp != null) totalBetContainer = tbp;
            }
        }

        private CanvasGroup GetOrCreateCanvasGroup(GameObject go)
        {
            if (go == null) return null;
            var cg = go.GetComponent<CanvasGroup>();
            if (cg == null) cg = go.AddComponent<CanvasGroup>();
            return cg;
        }

        /// <summary>Smoothly toggles center bottom bar between Total Bet controls and Win / Autoplay status.</summary>
        private void UpdateBottomBarCenterState()
        {
            EnsureTotalBetContainer();

            bool showBet = !isSpinning && !isAutoplaying && displayedWin <= 0;

            if (totalBetContainer != null)
            {
                if (totalBetContainer.activeSelf != showBet)
                {
                    var cg = GetOrCreateCanvasGroup(totalBetContainer);
                    cg.DOKill();
                    if (showBet)
                    {
                        totalBetContainer.SetActive(true);
                        cg.alpha = 0f;
                        cg.DOFade(1f, 0.2f);
                    }
                    else
                    {
                        cg.DOFade(0f, 0.15f).OnComplete(() => totalBetContainer.SetActive(false));
                    }
                }
            }

            if (totalWinText != null)
            {
                if (isAutoplaying && remainingAutoplayCount > 0 && displayedWin <= 0)
                {
                    totalWinText.text = $"AUTO SPINS LEFT {remainingAutoplayCount}";
                    totalWinText.gameObject.SetActive(true);
                }
                else if (displayedWin > 0)
                {
                    totalWinText.gameObject.SetActive(true);
                }
                else if (showBet)
                {
                    totalWinText.text = string.Empty;
                }
            }
        }

        /// <summary>Displays the spin win amount with smooth yellow/white styling and optional rolling count up.</summary>
        internal void SetWin(double targetWin, bool animate = false)
        {
            if (winCountTweener != null && winCountTweener.IsActive()) winCountTweener.Kill();

            if (!animate || totalWinText == null)
            {
                displayedWin = targetWin;
                if (totalWinText != null)
                {
                    if (targetWin > 0) totalWinText.text = $"<color=#F1C40F>WIN</color> ${targetWin:F2}";
                    else if (isAutoplaying && remainingAutoplayCount > 0) totalWinText.text = $"AUTO SPINS LEFT {remainingAutoplayCount}";
                    else totalWinText.text = "$0.00";
                }
                UpdateBottomBarCenterState();
                return;
            }

            double startWin = displayedWin;
            winCountTweener = DOTween.To(() => (float)startWin, x =>
            {
                displayedWin = x;
                if (totalWinText != null) totalWinText.text = $"<color=#F1C40F>WIN</color> ${x:F2}";
            }, (float)targetWin, 0.6f).SetEase(Ease.OutCubic).OnComplete(() =>
            {
                UpdateBottomBarCenterState();
            });
            UpdateBottomBarCenterState();
        }

        /// <summary>Updates the bottom bar with rolling tumble win and winning symbol pay details (e.g. 8X BANANA PAYS $60.00).</summary>
        internal void SetTumbleStepWin(double tumbleWin, string detail = null)
        {
            displayedWin = tumbleWin;
            UpdateBottomBarCenterState();

            if (totalWinText != null)
            {
                if (!string.IsNullOrEmpty(detail))
                    totalWinText.text = $"<color=#F1C40F>WIN</color> ${tumbleWin:F2}\n<size=65%>{detail}</size>";
                else
                    totalWinText.text = $"<color=#F1C40F>WIN</color> ${tumbleWin:F2}";
            }
        }

        /// <summary>Restores the bottom bar back to its default idle Total Bet state.</summary>
        internal void SetIdleBottomBarState()
        {
            displayedWin = 0;
            UpdateBottomBarCenterState();
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
                return;
            }

            var cg = GetOrCreateCanvasGroup(topTumbleWinBanner);
            cg.DOKill();
            cg.DOFade(0f, 0.25f).OnComplete(() =>
            {
                topTumbleWinBanner.SetActive(false);
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
            
            UpdateFeatureButtonsVisualState();
            UpdateSpinButtonVisuals();
            UpdateBottomBarCenterState();
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

            UpdateFeatureButtonsVisualState();

            if (isAutoplay && remainingSpins > 0)
            {
                SetStatusMessage($"AUTO SPINS LEFT {remainingSpins}");
            }
            else if (!isAutoplay)
            {
                SetStatusMessage(string.Empty);
            }

            UpdateSpinButtonVisuals();
            UpdateBottomBarCenterState();
        }

        //** ----------------------------------------------------
        //** Controls Spin / Stop / Autoplay Button States (Hot Chilli style)
        //** ----------------------------------------------------
        /// <summary>Swaps visibility between SpinButton and StopSpin button during active reel spins.</summary>
        public void SetSpinningButtonsState(bool isSpinning)
        {
            if (spinButton != null) spinButton.gameObject.SetActive(!isSpinning);
            if (stopSpinButton != null)
            {
                stopSpinButton.gameObject.SetActive(isSpinning);
                if (isSpinning)
                {
                    stopSpinButton.interactable = true;
                }
            }
            if (autoplayButtonObj != null) autoplayButtonObj.SetActive(!isSpinning);
        }

        /// <summary>Puts the buttons in Stopping/Resolving state: stop button remains visible but non-interactable to prevent spam.</summary>
        public void SetStoppingButtonsState()
        {
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
                if (spinAutoplayCountText != null)
                {
                    spinAutoplayCountText.gameObject.SetActive(true);
                    spinAutoplayCountText.text = remainingAutoplayCount > 0 ? $"{remainingAutoplayCount}" : string.Empty;
                }

                // In autoplay, clicking the spin button cancels autoplay
                if (spinButton != null) spinButton.interactable = true;
            }
            else
            {
                if (spinAutoplayCountText != null) spinAutoplayCountText.gameObject.SetActive(false);
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

            // Pause banner cycle during free spins to lock on free spins banner
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
            if (winCountTweener != null && winCountTweener.IsActive()) winCountTweener.Kill();
            if (slotFrameImage != null) slotFrameImage.rectTransform.DOKill();
            if (buyBonusButton != null) buyBonusButton.GetComponent<RectTransform>()?.DOKill();
            if (anteBetToggleButton != null) anteBetToggleButton.GetComponent<RectTransform>()?.DOKill();
        }
    }
}
