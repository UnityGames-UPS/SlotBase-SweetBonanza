using System;
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
        [SerializeField] private GameObject anteBetActiveIndicator;
        [SerializeField] private GameObject freeSpinsBadge;

        [Header("Top Display & Ante Bet Visuals")]
        [SerializeField] private Image topDisplayBannerImage;
        [SerializeField] private Sprite bannerFreeSpinsSprite;
        [SerializeField] private Sprite bannerPayAnywhereSprite;
        [SerializeField] private Image anteBetButtonImage;
        [SerializeField] private Sprite anteBetNormalSprite;
        [SerializeField] private Sprite anteBetGlowSprite;

        [Header("Ante Bet Slider & Cost Display")]
        [SerializeField] private RectTransform anteBetHandleRect;
        [SerializeField] private Image anteBetHandleImage;
        [SerializeField] private Sprite anteBetHandleArrowSprite;
        [SerializeField] private Sprite anteBetHandleCheckSprite;
        [SerializeField] private TMP_Text anteBetAmountText;
        [SerializeField] private float anteBetHandleLeftX = -19f;
        [SerializeField] private float anteBetHandleRightX = 19f;

        [Header("Theme Visuals (Day / Night Free Spins)")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Sprite backgroundDaySprite;
        [SerializeField] private Sprite backgroundNightSprite;
        [SerializeField] private Image slotFrameImage;
        [SerializeField] private Sprite slotFrameDaySprite;
        [SerializeField] private Sprite slotFrameNightSprite;
        [SerializeField] private Image cloudsImage;
        [SerializeField] private Sprite cloudsDaySprite;
        [SerializeField] private Sprite cloudsNightSprite;
        [SerializeField] private Image candyTransitionImage;

        [Header("Spin Button Visuals")]
        [SerializeField] private Image spinButtonBaseImage;
        [SerializeField] private GameObject spinArrowsObj;
        [SerializeField] private Transform spinButtonArrows;
        [SerializeField] private GameObject stopIconObj;
        [SerializeField] private TMP_Text spinAutoplayCountText;
        [SerializeField] private Sprite spinBaseIdleSprite;
        [SerializeField] private Sprite spinBaseDarkSprite;

        private Tweener winCountTweener;
        private Tweener spinRotationTweener;
        private Coroutine bannerCycleCoroutine;
        private double displayedWin = 0;
        private bool isCurrentlyNightMode = false;

        private bool isSpinning = false;
        private bool isAutoplaying = false;
        private bool isInFreeSpins = false;
        private bool isAnteBetActive = false;
        private int remainingAutoplayCount = 0;

        private CanvasGroup buyBonusCanvasGroup;
        private CanvasGroup anteBetCanvasGroup;

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

        private void Awake()
        {
            EnsureFeatureCanvasGroups();
            UpdateFeatureButtonsVisualState();
            if (spinButton != null)
            {
                spinButton.onClick.AddListener(HandleSpinButtonClicked);
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

        private void Start()
        {
            InitializeAnteBetSlider();
            UpdateSpinButtonVisuals();
            if (topDisplayBannerImage != null && bannerFreeSpinsSprite != null && bannerPayAnywhereSprite != null)
            {
                bannerCycleCoroutine = StartCoroutine(CycleTopDisplayBanners());
            }
        }

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

        private System.Collections.IEnumerator CycleTopDisplayBanners()
        {
            bool showFreeSpins = true;
            while (enabled)
            {
                topDisplayBannerImage.sprite = showFreeSpins ? bannerFreeSpinsSprite : bannerPayAnywhereSprite;
                showFreeSpins = !showFreeSpins;
                yield return new WaitForSeconds(4.5f);
            }
        }

        internal void SetBalance(double balance)
        {
            if (balanceText != null) balanceText.text = $"${balance:F2}";
        }

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
                anteBetAmountText.text = $"${anteCost:F2}";
            }
        }

        internal void SetWin(double targetWin, bool animate = false)
        {
            if (winCountTweener != null && winCountTweener.IsActive()) winCountTweener.Kill();

            if (!animate || totalWinText == null)
            {
                displayedWin = targetWin;
                if (totalWinText != null)
                {
                    if (targetWin > 0) totalWinText.text = $"${targetWin:F2}";
                    else if (isAutoplaying && remainingAutoplayCount > 0) totalWinText.text = $"AUTO SPINS LEFT: {remainingAutoplayCount}";
                    else totalWinText.text = "$0.00";
                }
                return;
            }

            double startWin = displayedWin;
            winCountTweener = DOTween.To(() => (float)startWin, x =>
            {
                displayedWin = x;
                totalWinText.text = $"${x:F2}";
            }, (float)targetWin, 0.6f).SetEase(Ease.OutCubic);
        }

        internal void SetSpinButtonInteractable(bool interactable)
        {
            this.isSpinning = !interactable;

            bool canInteractBet = interactable && !isAutoplaying && !isInFreeSpins;
            if (betPlusButton != null) betPlusButton.interactable = canInteractBet;
            if (betMinusButton != null) betMinusButton.interactable = canInteractBet;

            UpdateFeatureButtonsVisualState();
            UpdateSpinButtonVisuals();
        }

        internal void SetAnteBetState(bool isActive)
        {
            this.isAnteBetActive = isActive;
            if (anteBetActiveIndicator != null) anteBetActiveIndicator.SetActive(isActive);
            if (anteBetButtonImage != null)
            {
                anteBetButtonImage.sprite = isActive && anteBetGlowSprite != null ? anteBetGlowSprite : anteBetNormalSprite;
            }

            if (anteBetHandleRect != null)
            {
                float targetX = isActive ? anteBetHandleRightX : anteBetHandleLeftX;
                anteBetHandleRect.DOKill();
                anteBetHandleRect.DOLocalMoveX(targetX, 0.2f).SetEase(Ease.OutQuad);
            }

            if (anteBetHandleImage != null)
            {
                Sprite targetSprite = isActive ? anteBetHandleCheckSprite : anteBetHandleArrowSprite;
                if (targetSprite != null) anteBetHandleImage.sprite = targetSprite;
            }

            UpdateFeatureButtonsVisualState();
        }

        private void InitializeAnteBetSlider()
        {
            if (anteBetHandleRect != null)
            {
                anteBetHandleRect.DOKill();
                Vector3 pos = anteBetHandleRect.localPosition;
                pos.x = isAnteBetActive ? anteBetHandleRightX : anteBetHandleLeftX;
                anteBetHandleRect.localPosition = pos;
            }

            if (anteBetHandleImage != null)
            {
                Sprite targetSprite = isAnteBetActive ? anteBetHandleCheckSprite : anteBetHandleArrowSprite;
                if (targetSprite != null) anteBetHandleImage.sprite = targetSprite;
            }

            if (anteBetButtonImage != null)
            {
                anteBetButtonImage.sprite = isAnteBetActive && anteBetGlowSprite != null ? anteBetGlowSprite : anteBetNormalSprite;
            }
        }

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
                SetStatusMessage($"AUTO SPINS LEFT: {remainingSpins}");
            }
            else if (!isAutoplay)
            {
                SetStatusMessage(string.Empty);
            }

            UpdateSpinButtonVisuals();
        }

        internal void UpdateSpinButtonVisuals()
        {
            if (isAutoplaying)
            {
                if (spinRotationTweener != null && spinRotationTweener.IsActive()) spinRotationTweener.Kill();

                if (spinButtonBaseImage != null && spinBaseDarkSprite != null)
                {
                    spinButtonBaseImage.sprite = spinBaseDarkSprite;
                }

                if (spinArrowsObj != null) spinArrowsObj.SetActive(false);
                if (stopIconObj != null) stopIconObj.SetActive(false);

                if (spinAutoplayCountText != null)
                {
                    spinAutoplayCountText.gameObject.SetActive(true);
                    spinAutoplayCountText.text = remainingAutoplayCount > 0 ? $"{remainingAutoplayCount}" : "";
                }

                if (spinButton != null) spinButton.interactable = true;
            }
            else if (isSpinning)
            {
                if (spinButtonBaseImage != null && spinBaseDarkSprite != null)
                {
                    spinButtonBaseImage.sprite = spinBaseDarkSprite;
                }

                if (spinArrowsObj != null) spinArrowsObj.SetActive(false);
                if (spinAutoplayCountText != null) spinAutoplayCountText.gameObject.SetActive(false);

                if (stopIconObj != null)
                {
                    if (!stopIconObj.activeSelf)
                    {
                        stopIconObj.SetActive(true);
                        stopIconObj.transform.DOKill();
                        stopIconObj.transform.localScale = Vector3.one;
                        stopIconObj.transform.DOPunchScale(Vector3.one * 0.18f, 0.2f);
                    }
                }

                if (spinButton != null) spinButton.interactable = true;
            }
            else
            {
                if (spinRotationTweener != null && spinRotationTweener.IsActive()) spinRotationTweener.Kill();

                if (spinButtonBaseImage != null && spinBaseIdleSprite != null)
                {
                    spinButtonBaseImage.sprite = spinBaseIdleSprite;
                }

                if (stopIconObj != null) stopIconObj.SetActive(false);
                if (spinAutoplayCountText != null) spinAutoplayCountText.gameObject.SetActive(false);

                if (spinArrowsObj != null)
                {
                    if (!spinArrowsObj.activeSelf)
                    {
                        spinArrowsObj.SetActive(true);
                        spinArrowsObj.transform.DOKill();
                        spinArrowsObj.transform.localScale = Vector3.one;
                        spinArrowsObj.transform.localRotation = Quaternion.identity;
                        spinArrowsObj.transform.DOPunchScale(Vector3.one * 0.12f, 0.2f);
                    }
                }

                if (spinButton != null) spinButton.interactable = !isInFreeSpins;
            }
        }

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
            if (isInFreeSpins)
            {
                if (bannerCycleCoroutine != null)
                {
                    StopCoroutine(bannerCycleCoroutine);
                    bannerCycleCoroutine = null;
                }
                if (topDisplayBannerImage != null && bannerFreeSpinsSprite != null)
                {
                    topDisplayBannerImage.sprite = bannerFreeSpinsSprite;
                }
            }
            else
            {
                if (bannerCycleCoroutine == null && topDisplayBannerImage != null && bannerFreeSpinsSprite != null)
                {
                    bannerCycleCoroutine = StartCoroutine(CycleTopDisplayBanners());
                }
            }

            ApplyThemeMode(isInFreeSpins);
            UpdateSpinButtonVisuals();
        }

        private void ApplyThemeMode(bool nightMode)
        {
            if (isCurrentlyNightMode == nightMode) return;
            isCurrentlyNightMode = nightMode;

            // Trigger candy transition overlay burst
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

            // Swap Background & Frame & Clouds Sprites with smooth fade
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

            if (cloudsImage != null)
            {
                Sprite targetClouds = nightMode ? (cloudsNightSprite != null ? cloudsNightSprite : cloudsDaySprite) : cloudsDaySprite;
                if (targetClouds != null)
                {
                    cloudsImage.DOKill();
                    cloudsImage.DOFade(0.2f, 0.18f).OnComplete(() =>
                    {
                        cloudsImage.sprite = targetClouds;
                        cloudsImage.DOFade(1f, 0.22f);
                    });
                }
            }
        }

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

        private void OnDestroy()
        {
            if (winCountTweener != null && winCountTweener.IsActive()) winCountTweener.Kill();
            if (spinRotationTweener != null && spinRotationTweener.IsActive()) spinRotationTweener.Kill();
        }
    }
}
