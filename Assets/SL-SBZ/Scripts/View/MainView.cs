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
        internal event Action OnSpinStarted;
        internal event Action OnSpinStopped;

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
        [SerializeField] private Image freeSpinsBadgeImage;
        [SerializeField] private Sprite freeSpinsBadgeSprite;
        [SerializeField] private Sprite autoSpinBadgeSprite;

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
        [SerializeField] private CanvasGroup promoCanvasGroup;
        [SerializeField] private CanvasGroup tumbleCanvasGroup;

        [Header("Bottom Bar Display Objects")]
       
       /* [SerializeField] private Button betPlusBtn;
        [SerializeField] private Button betMinusBtn;
        [SerializeField] private TMP_Text betDisplayText;*/

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
        private bool isStopping = false;
        private bool isAutoplaying = false;
        private bool isInFreeSpins = false;
        private bool isAnteBetActive = false;
        private bool isBuyFeatureActive = false;
        private int remainingAutoplayCount = 0;

        private static readonly WaitForSeconds Wait015 = new WaitForSeconds(0.15f);
        private static readonly WaitForSeconds Wait035 = new WaitForSeconds(0.35f);
        private static readonly WaitForSeconds Wait08 = new WaitForSeconds(0.8f);
        private static readonly WaitForSeconds Wait11 = new WaitForSeconds(1.1f);

        private CanvasGroup buyBonusCanvasGroup;
        private CanvasGroup anteBetCanvasGroup;
        private RectTransform buyBonusRect;
        private RectTransform anteBetRect;
        private RectTransform slotFrameContainerRect;

        private Vector2 slotFrameDayPos;
        private Vector2 buyButtonDayPos;
        private Vector2 anteButtonDayPos;
        private bool dayPositionsCaptured = false;

        /// <summary>Captures default Day (base game) anchored positions for slot frame and feature buttons.</summary>
        private void CaptureDayPositions()
        {
            if (dayPositionsCaptured) return;
            if (slotFrameImage != null)
            {
                slotFrameContainerRect = slotFrameImage.transform.parent as RectTransform ?? slotFrameImage.rectTransform;
                slotFrameDayPos = slotFrameContainerRect != null ? slotFrameContainerRect.anchoredPosition : new Vector2(143.60f, 57f);
            }
            if (buyBonusButton != null)
            {
                buyBonusRect = buyBonusButton.GetComponent<RectTransform>();
                buyButtonDayPos = buyBonusRect.anchoredPosition;
            }
            if (anteBetToggleButton != null)
            {
                anteBetRect = anteBetToggleButton.GetComponent<RectTransform>();
                anteButtonDayPos = anteBetRect.anchoredPosition;
            }
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

        /// <summary>Ensures CanvasGroup components exist on top promo banner and top tumble win banner.</summary>
        private void EnsureTopBannerCanvasGroups()
        {
            if (topPromoBannerObj != null && promoCanvasGroup == null)
            {
                promoCanvasGroup = topPromoBannerObj.GetComponent<CanvasGroup>();
                if (promoCanvasGroup == null) promoCanvasGroup = topPromoBannerObj.AddComponent<CanvasGroup>();
            }
            if (topTumbleWinBanner != null && tumbleCanvasGroup == null)
            {
                tumbleCanvasGroup = topTumbleWinBanner.GetComponent<CanvasGroup>();
                if (tumbleCanvasGroup == null) tumbleCanvasGroup = topTumbleWinBanner.AddComponent<CanvasGroup>();
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
            EnsureTopBannerCanvasGroups();
            UpdateFeatureButtonsVisualState();
            if (spinButton != null)
            {
                spinButton.onClick.AddListener(HandleSpinButtonClicked);
            }
            if (stopSpinButton != null)
            {
                stopSpinButton.onClick.AddListener(() =>
                {
                    if (isStopping) return;
                    SetStoppingButtonsState();
                    AudioManager.Instance?.PlaySpinStop();
                    OnStopClicked?.Invoke();
                });
            }
            if (autoSpinStopButton != null)
            {
                autoSpinStopButton.onClick.AddListener(() =>
                {
                    if (isStopping) return;
                    SetStoppingButtonsState();
                    AudioManager.Instance?.PlayAutoplayStop();
                    OnAutoplayStopClicked?.Invoke();
                });
            }
            if (betPlusButton != null) betPlusButton.onClick.AddListener(() => OnBetIncreaseClicked?.Invoke());
            //if (betPlusBtn != null && betPlusBtn != betPlusButton) betPlusBtn.onClick.AddListener(() => OnBetIncreaseClicked?.Invoke());
            if (betMinusButton != null) betMinusButton.onClick.AddListener(() => OnBetDecreaseClicked?.Invoke());
            //if (betMinusBtn != null && betMinusBtn != betMinusButton) betMinusBtn.onClick.AddListener(() => OnBetDecreaseClicked?.Invoke());
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
            EnsureTopBannerCanvasGroups();
            if (topTumbleWinBanner != null) topTumbleWinBanner.SetActive(false);
            if (tumbleCanvasGroup != null)
            {
                tumbleCanvasGroup.alpha = 0f;
                tumbleCanvasGroup.blocksRaycasts = false;
            }
            if (topPromoBannerObj != null) topPromoBannerObj.SetActive(true);
            if (promoCanvasGroup != null)
            {
                promoCanvasGroup.alpha = 1f;
                promoCanvasGroup.blocksRaycasts = true;
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (isAutoplaying || isInFreeSpins)
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
                spinButton.interactable = false;
            }

            if (isAutoplaying)
            {
                SetStoppingButtonsState();
                AudioManager.Instance?.PlayAutoplayStop();
                OnAutoplayStopClicked?.Invoke();
            }
            else if (isSpinning)
            {
                SetStoppingButtonsState();
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
            if (balanceText != null)
            {
                balanceText.textWrappingMode = TextWrappingModes.NoWrap;
                balanceText.text = SpriteTextUtility.FormatWithSpriteAsset(balance, balanceText);
            }
        }

        /// <summary>Updates the current bet text and calculates effective bet when Ante Bet is active.</summary>
        internal void SetBet(double baseBet, bool isAnteBet)
        {
            double effectiveBet = isAnteBet ? baseBet * 1.25 : baseBet;

            if (currentBetText != null)
            {
                currentBetText.textWrappingMode = TextWrappingModes.NoWrap;
                currentBetText.text = SpriteTextUtility.FormatWithSpriteAsset(effectiveBet, currentBetText);
            }

           /* if (betDisplayText != null)
            {
                betDisplayText.text = $"{effectiveBet:F2}";
            }*/

            if (anteBetAmountText != null)
            {
                anteBetAmountText.textWrappingMode = TextWrappingModes.NoWrap;
                double anteCost = baseBet * 1.25;
                anteBetAmountText.text = SpriteTextUtility.FormatWithSpriteAsset(anteCost, anteBetAmountText);
            }
        }

        /// <summary>Called when Buy Feature opens or is active: all bottom bar display objects are hidden.</summary>
        internal void SetBuyFeatureState()
        {
            isBuyFeatureActive = true;
            if (spinInstructionObj != null) spinInstructionObj.SetActive(false);
            if (payoutMultiplayerObj != null) payoutMultiplayerObj.SetActive(false);
            if (winCountObj != null) winCountObj.SetActive(false);
            if (autoSpinCountObj != null) autoSpinCountObj.SetActive(false);
            if (betPlusButton != null) betPlusButton.interactable = false;
            if (betMinusButton != null) betMinusButton.interactable = false;
        }

        /// <summary>Updates the spins badge with sprite swap and priority (Free Spins > AutoSpin > Spin Instruction / Idle).</summary>
        private void UpdateSpinsBadge(int remainingCount = -1)
        {
            int autoCount = remainingCount >= 0 ? remainingCount : remainingAutoplayCount;
            bool show = isInFreeSpins || (isAutoplaying && autoCount > 0);
            bool hasWinDisplay = (winCountObj != null && winCountObj.activeSelf)
                              || (payoutMultiplayerObj != null && payoutMultiplayerObj.activeSelf);

            if (freeSpinsBadge != null) freeSpinsBadge.SetActive(show);
            if (spinInstructionObj != null) spinInstructionObj.SetActive(!show && !isBuyFeatureActive && !hasWinDisplay);
            if (!show) return;

            if (freeSpinsBadgeImage != null)
                freeSpinsBadgeImage.sprite = isInFreeSpins ? freeSpinsBadgeSprite : autoSpinBadgeSprite;

            if (freeSpinsCountText != null)
            {
                if (isInFreeSpins)
                {
                    if (remainingCount >= 0) freeSpinsCountText.text = $"{remainingCount}";
                }
                else
                {
                    freeSpinsCountText.text = $"{autoCount}";
                }
            }
        }

        /// <summary>Called when a spin starts (manual or autoplay).</summary>
        internal void SetSpinStartedState(bool isAutoplay, int remainingSpins)
        {
            this.isSpinning = true;
            this.isStopping = false;
            this.isAutoplaying = isAutoplay;
            this.remainingAutoplayCount = remainingSpins;
            this.displayedWin = 0;

            if (winCountObj != null) winCountObj.SetActive(false);
            if (payoutMultiplayerObj != null) payoutMultiplayerObj.SetActive(false);

            UpdateSpinsBadge(remainingSpins);

            SetSpinningButtonsState(true);
            OnSpinStarted?.Invoke();
        }

        /// <summary>Updates bottom bar display on tumble step win: displays win count and symbol payout detail with icon.</summary>
        internal void SetTumbleStepWin(double tumbleWin, Sprite symbolSprite, int count, double payout, string detail = null)
        {
            displayedWin = tumbleWin;

            // Keep winCountObj active continuously from the first win through the entire spin
            if (tumbleWin > 0)
            {
                if (winCountObj != null) winCountObj.SetActive(true);
                if (winCountText != null) winCountText.text = $"{tumbleWin:F2}";
                if (freeSpinsBadge != null) freeSpinsBadge.SetActive(false);
            }

            if (count > 0 && payout > 0)
            {
                if (payoutMultiplayerObj != null) payoutMultiplayerObj.SetActive(true);
                if (spinInstructionObj != null) spinInstructionObj.SetActive(false);
                if (freeSpinsBadge != null) freeSpinsBadge.SetActive(false);
                if (autoSpinCountObj != null) autoSpinCountObj.SetActive(false);

                if (payoutMultiplayerText != null) payoutMultiplayerText.text = SpriteTextUtility.FormatCountWithSpriteAsset(count, payoutMultiplayerText);
                if (payoutMultiplierIcon != null)
                {
                    payoutMultiplierIcon.sprite = symbolSprite;
                    payoutMultiplierIcon.gameObject.SetActive(symbolSprite != null);
                }
                if (payoutMultiplierLabel != null) payoutMultiplierLabel.text = SpriteTextUtility.FormatWithSpriteAsset(payout, payoutMultiplierLabel);

                if (totalWinText != null && !string.IsNullOrEmpty(detail))
                    totalWinText.text = $"<color=#F1C40F>WIN</color> {tumbleWin:F2}\n<size=65%>{detail}</size>";
            }
            else if (tumbleWin > 0)
            {
                // Cascade step with no new cluster match: hide cluster payout detail, keep running tumble win count
                if (payoutMultiplayerObj != null) payoutMultiplayerObj.SetActive(false);
                if (spinInstructionObj != null) spinInstructionObj.SetActive(false);
                if (freeSpinsBadge != null) freeSpinsBadge.SetActive(false);
                if (autoSpinCountObj != null) autoSpinCountObj.SetActive(false);

                if (totalWinText != null) totalWinText.text = $"<color=#F1C40F>WIN</color> {tumbleWin:F2}";
            }
            else
            {
                if (payoutMultiplayerObj != null) payoutMultiplayerObj.SetActive(false);
                if (winCountObj != null) winCountObj.SetActive(false);
                UpdateSpinsBadge();
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
            if (targetWin > 0)
            {
                displayedWin = targetWin;
                // Final round win: show winCountObj cleanly in the center
                if (winCountObj != null) winCountObj.SetActive(true);
                if (winCountText != null) winCountText.text = $"{targetWin:F2}";
                if (totalWinText != null) totalWinText.text = $"<color=#F1C40F>WIN</color> {targetWin:F2}";

                if (payoutMultiplayerObj != null) payoutMultiplayerObj.SetActive(false);
                if (spinInstructionObj != null) spinInstructionObj.SetActive(false);
                if (freeSpinsBadge != null) freeSpinsBadge.SetActive(false);
                if (autoSpinCountObj != null) autoSpinCountObj.SetActive(false);
            }
            else
            {
                // Only clear win display if displayedWin is also 0
                if (displayedWin <= 0)
                {
                    if (winCountObj != null) winCountObj.SetActive(false);
                    if (totalWinText != null) totalWinText.text = "0.00";

                    if (payoutMultiplayerObj != null) payoutMultiplayerObj.SetActive(false);
                    UpdateSpinsBadge();
                }
            }
        }

        /// <summary>Restores the bottom bar back to its default idle state.</summary>
        internal void SetIdleBottomBarState()
        {
            this.isBuyFeatureActive = false;
            this.isSpinning = false;
            this.isStopping = false;
            if (payoutMultiplayerObj != null) payoutMultiplayerObj.SetActive(false);
            if (winCountObj != null) winCountObj.SetActive(false);

            UpdateSpinsBadge();

            SetSpinningButtonsState(false);
            SetSpinButtonInteractable(true);
            OnSpinStopped?.Invoke();
        }

        #region Top Tumble Win Banner & Multiplier Bomb Fly-Up

        private TMP_Text GetFlyer()
        {
            if (flyerText == null)
            {
                var go = new GameObject("MultiplierFlyer", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                Transform parentCanvas = topTumbleWinBanner != null ? (topTumbleWinBanner.transform.parent?.parent ?? topTumbleWinBanner.transform.parent) : transform;
                go.transform.SetParent(parentCanvas, false);

                var rt = go.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.sizeDelta = new Vector2(300f, 100f);
                }

                flyerText = go.GetComponent<TextMeshProUGUI>();
                if (topTumbleWinText != null)
                {
                    flyerText.font = topTumbleWinText.font;
                    flyerText.fontSharedMaterial = topTumbleWinText.fontSharedMaterial;
                    flyerText.spriteAsset = topTumbleWinText.spriteAsset;
                }
                flyerText.fontSize = 54;
                flyerText.fontStyle = FontStyles.Bold;
                flyerText.alignment = TextAlignmentOptions.Center;
                flyerText.color = new Color(1f, 0.95f, 0.15f, 1f);
                flyerText.raycastTarget = false;
                flyerText.textWrappingMode = TextWrappingModes.NoWrap;
                flyerText.overflowMode = TextOverflowModes.Overflow;
                go.SetActive(false);
            }
            return flyerText;
        }

        /// <summary>Shows the purple capsule banner at the top of the grid with smooth cross-fade.</summary>
        internal void ShowTopTumbleWinBanner(double initialTumbleWin)
        {
            if (topTumbleWinBanner == null) return;
            EnsureTopBannerCanvasGroups();

            // Fade out promo banner (topImageReplacePose) if currently visible
            if (promoCanvasGroup != null && topPromoBannerObj != null && topPromoBannerObj.activeSelf)
            {
                promoCanvasGroup.DOKill();
                promoCanvasGroup.blocksRaycasts = false;
                promoCanvasGroup.DOFade(0f, 0.25f).OnComplete(() =>
                {
                    if (topPromoBannerObj != null) topPromoBannerObj.SetActive(false);
                });
            }

            // Fade in tumble win banner if not already active
            if (!topTumbleWinBanner.activeSelf)
            {
                topTumbleWinBanner.SetActive(true);
                if (tumbleCanvasGroup != null)
                {
                    tumbleCanvasGroup.DOKill();
                    tumbleCanvasGroup.alpha = 0f;
                    tumbleCanvasGroup.DOFade(1f, 0.25f);
                    tumbleCanvasGroup.blocksRaycasts = true;
                }
                topTumbleWinBanner.transform.DOKill();
                topTumbleWinBanner.transform.localScale = Vector3.one * 0.85f;
                topTumbleWinBanner.transform.DOScale(1f, 0.25f).SetEase(Ease.OutBack);
            }

            if (topTumbleWinText != null)
            {
                topTumbleWinText.textWrappingMode = TextWrappingModes.NoWrap;
                topTumbleWinText.text = SpriteTextUtility.FormatTopTumbleWin(initialTumbleWin, topTumbleWinText);
            }
        }

        /// <summary>Hides the top tumble win banner with a smooth cross-fade back to promo banner.</summary>
        internal void HideTopTumbleWinBanner(bool immediate = false)
        {
            EnsureTopBannerCanvasGroups();

            if (immediate)
            {
                if (tumbleCanvasGroup != null)
                {
                    tumbleCanvasGroup.DOKill();
                    tumbleCanvasGroup.alpha = 0f;
                    tumbleCanvasGroup.blocksRaycasts = false;
                }
                if (topTumbleWinBanner != null) topTumbleWinBanner.SetActive(false);

                if (topPromoBannerObj != null)
                {
                    topPromoBannerObj.SetActive(true);
                    if (promoCanvasGroup != null)
                    {
                        promoCanvasGroup.DOKill();
                        promoCanvasGroup.alpha = 1f;
                        promoCanvasGroup.blocksRaycasts = true;
                    }
                }
                return;
            }

            if (topTumbleWinBanner == null || !topTumbleWinBanner.activeSelf) return;

            if (tumbleCanvasGroup != null)
            {
                tumbleCanvasGroup.DOKill();
                tumbleCanvasGroup.blocksRaycasts = false;
                tumbleCanvasGroup.DOFade(0f, 0.25f).OnComplete(() =>
                {
                    if (topTumbleWinBanner != null) topTumbleWinBanner.SetActive(false);
                });
            }
            else if (topTumbleWinBanner != null)
            {
                topTumbleWinBanner.SetActive(false);
            }

            if (topPromoBannerObj != null)
            {
                topPromoBannerObj.SetActive(true);
                if (promoCanvasGroup != null)
                {
                    promoCanvasGroup.DOKill();
                    promoCanvasGroup.alpha = 0f;
                    promoCanvasGroup.DOFade(1f, 0.25f);
                    promoCanvasGroup.blocksRaycasts = true;
                }
            }
        }

        /// <summary>Animates a flying multiplier from a grid bomb cell up into the top tumble banner.</summary>
        internal IEnumerator AnimateBombMultiplierFlyUp(Vector3 fromWorldPos, int multiplier, double baseTumbleWin, int runningMultiplier)
        {
            if (topTumbleWinBanner == null) yield break;
            if (!topTumbleWinBanner.activeSelf) topTumbleWinBanner.SetActive(true);

            var flyer = GetFlyer();
            if (flyer == null) yield break;

            flyer.transform.SetAsLastSibling();
            flyer.gameObject.SetActive(true);
            flyer.transform.position = fromWorldPos;
            flyer.transform.localScale = Vector3.one;
            flyer.text = SpriteTextUtility.FormatBombMultiplier(multiplier);

            Vector3 targetPos = topTumbleWinBanner.transform.position;

            flyer.transform.DOScale(1.5f, 0.21f).SetEase(Ease.OutQuad)
                .OnComplete(() => flyer.transform.DOScale(1.0f, 0.21f).SetEase(Ease.InQuad));
            yield return flyer.transform.DOMove(targetPos, 0.42f).SetEase(Ease.OutQuad).WaitForCompletion();

            flyer.gameObject.SetActive(false);

            AudioManager.Instance?.PlaySymbolPop();
            topTumbleWinBanner.transform.DOKill();
            topTumbleWinBanner.transform.localScale = Vector3.one;
            topTumbleWinBanner.transform.DOPunchScale(Vector3.one * 0.18f, 0.22f, 10, 1f);

            if (topTumbleWinText != null)
            {
                topTumbleWinText.text = SpriteTextUtility.FormatTopTumbleWinFormula(baseTumbleWin, runningMultiplier, topTumbleWinText);
            }

            yield return Wait015;
        }

        /// <summary>Calculates and flashes the final multiplied tumble win on the top banner.</summary>
        internal IEnumerator FinalizeTumbleWinMultipliers(double baseTumbleWin, int totalMultiplier)
        {
            if (topTumbleWinBanner == null) yield break;
            if (!topTumbleWinBanner.activeSelf) topTumbleWinBanner.SetActive(true);

            yield return Wait035;

            double finalWin = baseTumbleWin * totalMultiplier;
            topTumbleWinBanner.transform.DOKill();
            topTumbleWinBanner.transform.localScale = Vector3.one;
            topTumbleWinBanner.transform.DOPunchScale(Vector3.one * 0.25f, 0.3f, 8, 1f);

            if (topTumbleWinText != null)
            {
                topTumbleWinText.text = SpriteTextUtility.FormatTopTumbleWin(finalWin, topTumbleWinText);
            }

            yield return Wait08;
        }

        #endregion

        /// <summary>Updates reel spinning state, toggles bet adjustments, and refreshes button visuals.</summary>
        internal void SetSpinButtonInteractable(bool interactable)
        {
            if (isInFreeSpins)
            {
                SetSpinningButtonsState(isSpinning);
                return;
            }

            // If already spinning, keep spinning state locked until SetIdleBottomBarState resets it at round end
            if (!this.isSpinning)
            {
                this.isSpinning = !interactable;
            }

            bool canInteractBet = interactable && !this.isSpinning && !isAutoplaying && !isInFreeSpins && !isBuyFeatureActive;
            if (betPlusButton != null) betPlusButton.interactable = canInteractBet;
            if (betMinusButton != null) betMinusButton.interactable = canInteractBet;
            //if (betPlusBtn != null) betPlusBtn.interactable = canInteractBet;
            //if (betMinusBtn != null) betMinusBtn.interactable = canInteractBet;

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
            //if (betPlusBtn != null) betPlusBtn.interactable = canInteractBet;
            //if (betMinusBtn != null) betMinusBtn.interactable = canInteractBet;

            UpdateFeatureButtonsVisualState();

            if (isAutoplay && remainingSpins > 0)
            {
                SetStatusMessage($"AUTO SPINS LEFT {remainingSpins}");
                if (payoutMultiplayerObj != null) payoutMultiplayerObj.SetActive(false);
                if (winCountObj != null) winCountObj.SetActive(false);
                UpdateSpinsBadge(remainingSpins);
            }
            else if (!isAutoplay && !isInFreeSpins)
            {
                SetStatusMessage(string.Empty);
                if (payoutMultiplayerObj != null) payoutMultiplayerObj.SetActive(false);
                if (winCountObj != null) winCountObj.SetActive(false);
                UpdateSpinsBadge();
            }

            UpdateSpinButtonVisuals();
        }

        //** ----------------------------------------------------
        //** Controls Spin / Stop / Autoplay Button States (Hot Chilli style)
        //** ----------------------------------------------------
        /// <summary>Swaps visibility between SpinButton, StopSpin button, and AutoSpinStopButton.</summary>
        internal void SetSpinningButtonsState(bool isSpinning)
        {
            this.isSpinning = isSpinning;

            if (isAutoplaying || isInFreeSpins)
            {
                // AutoSpin / Free Spins: spinButton: OFF, stopButton: OFF, autoSpinStopButton: ON
                if (spinButton != null) spinButton.gameObject.SetActive(false);
                if (stopSpinButton != null) stopSpinButton.gameObject.SetActive(false);
                if (autoSpinStopButton != null)
                {
                    autoSpinStopButton.gameObject.SetActive(true);
                    autoSpinStopButton.interactable = isSpinning && !isStopping;
                }
                if (autoplayButtonObj != null) autoplayButtonObj.SetActive(false);
                return;
            }

            if (isSpinning)
            {
                // If autoplay was stopping or autoSpinStopButton is active, keep it visible (disabled) until idle
                if (autoSpinStopButton != null && autoSpinStopButton.gameObject.activeInHierarchy)
                {
                    if (spinButton != null) spinButton.gameObject.SetActive(false);
                    if (stopSpinButton != null) stopSpinButton.gameObject.SetActive(false);
                    autoSpinStopButton.interactable = false;
                    if (autoplayButtonObj != null) autoplayButtonObj.SetActive(false);
                    return;
                }

                if (autoSpinStopButton != null) autoSpinStopButton.gameObject.SetActive(false);

                // Manual: Spinning -> spinButton: OFF, stopButton: ON
                if (spinButton != null) spinButton.gameObject.SetActive(false);
                if (stopSpinButton != null)
                {
                    stopSpinButton.gameObject.SetActive(true);
                    stopSpinButton.interactable = !isStopping;
                }
            }
            else
            {
                // Manual: Idle -> spinButton: ON (interactable), stop buttons: OFF
                if (stopSpinButton != null) stopSpinButton.gameObject.SetActive(false);
                if (autoSpinStopButton != null) autoSpinStopButton.gameObject.SetActive(false);
                if (spinButton != null)
                {
                    spinButton.gameObject.SetActive(true);
                    spinButton.interactable = true;
                }
            }
            if (autoplayButtonObj != null) autoplayButtonObj.SetActive(!isSpinning);
        }

        /// <summary>Puts the buttons in Stopping/Resolving state: stop button remains visible but non-interactable to prevent spam.</summary>
        internal void SetStoppingButtonsState()
        {
            this.isSpinning = true;
            this.isStopping = true;

            if (isAutoplaying || isInFreeSpins || (autoSpinStopButton != null && autoSpinStopButton.gameObject.activeInHierarchy))
            {
                // AutoSpin / Free Spins: Stopping -> spinButton: OFF, stopButton: OFF, autoSpinStopButton: ON (disabled while final landing)
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

            if (isInFreeSpins)
            {
                if (spinButton != null)
                {
                    spinButton.gameObject.SetActive(false);
                    spinButton.interactable = false;
                }
                return;
            }

            if (isAutoplaying)
            {
                UpdateSpinsBadge();
            }
            else
            {
                if (spinButton != null) spinButton.interactable = !isSpinning;
            }
        }

        /// <summary>Updates Free Spins display, remaining count, and day/night theme mode.</summary>
        internal void SetFreeSpinsDisplay(bool isInFreeSpins, int remainingSpins)
        {
            this.isInFreeSpins = isInFreeSpins;
            UpdateFeatureButtonsVisualState();

            UpdateSpinsBadge(remainingSpins);

            if (buyBonusButton != null && isInFreeSpins) buyBonusButton.interactable = false;

            // Top promo banner vs tumble win banner visibility is handled via CanvasGroup cross-fade
            if (!isInFreeSpins && topTumbleWinBanner != null && topTumbleWinBanner.activeSelf)
            {
                HideTopTumbleWinBanner(immediate: true);
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
            if (buyBonusRect != null)
            {
                buyBonusRect.DOKill();
                float targetX = nightMode ? buyButtonDayPos.x + offscreenOffset : buyButtonDayPos.x;
                buyBonusRect.DOAnchorPosX(targetX, 0.45f).SetEase(nightMode ? Ease.InBack : Ease.OutBack);
            }

            if (anteBetRect != null)
            {
                anteBetRect.DOKill();
                float targetX = nightMode ? anteButtonDayPos.x + offscreenOffset : anteButtonDayPos.x;
                anteBetRect.DOAnchorPosX(targetX, 0.45f).SetEase(nightMode ? Ease.InBack : Ease.OutBack);
            }

            // 2. Center slot frame to 6 (Night / Free Spins) or 143.60 (Day / Base Game)
            if (slotFrameContainerRect != null)
            {
                slotFrameContainerRect.DOKill();
                float targetSlotX = nightMode ? 6f : (slotFrameDayPos.x != 0 ? slotFrameDayPos.x : 143.60f);
                slotFrameContainerRect.DOAnchorPosX(targetSlotX, 0.5f).SetEase(Ease.InOutCubic);
            }

            // 3. Trigger candy transition overlay
            if (candyTransitionImage != null)
            {
                StartCoroutine(PlayCandyTransitionRoutine());
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
                        slotFrameImage.DOFade(1f, 0.22f);
                    });
                }
            }
        }

        private IEnumerator PlayCandyTransitionRoutine()
        {
            if (candyTransitionImage == null) yield break;
            candyTransitionImage.gameObject.SetActive(true);
            yield return Wait11;
            if (candyTransitionImage != null) candyTransitionImage.gameObject.SetActive(false);
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

        private void KillAllTweens()
        {
            if (slotFrameImage != null)
            {
                slotFrameImage.DOKill();
                slotFrameImage.rectTransform.DOKill();
                if (slotFrameImage.transform.parent != null) slotFrameImage.transform.parent.DOKill();
            }
            if (backgroundImage != null) backgroundImage.DOKill();
            if (statusBannerText != null) statusBannerText.transform.DOKill();
            if (buyBonusButton != null) buyBonusButton.transform.DOKill();
            if (anteBetToggleButton != null) anteBetToggleButton.transform.DOKill();
            if (spinButton != null) spinButton.transform.DOKill();
            if (autoSpinStopButton != null) autoSpinStopButton.transform.DOKill();
            if (autoSpinRemainingText != null) autoSpinRemainingText.transform.DOKill();
            if (flyerText != null) flyerText.transform.DOKill();
            if (topTumbleWinBanner != null) topTumbleWinBanner.transform.DOKill();
            if (promoCanvasGroup != null) promoCanvasGroup.DOKill();
            if (tumbleCanvasGroup != null) tumbleCanvasGroup.DOKill();
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            KillAllTweens();
        }

        /// <summary>Kills active DOTween tweens when the MainView component is destroyed.</summary>
        private void OnDestroy()
        {
            StopAllCoroutines();
            KillAllTweens();
        }
    }
}
