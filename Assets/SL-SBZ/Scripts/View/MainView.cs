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

        [Header("Theme Visuals (Day / Night Free Spins)")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Sprite backgroundDaySprite;
        [SerializeField] private Sprite backgroundNightSprite;
        [SerializeField] private Image slotFrameImage;
        [SerializeField] private Sprite slotFrameDaySprite;
        [SerializeField] private Sprite slotFrameNightSprite;
        [SerializeField] private Image candyTransitionImage;

        [Header("Spin Button Visuals")]
        [SerializeField] private Transform spinButtonArrows;

        private Tweener winCountTweener;
        private Tweener spinRotationTweener;
        private Coroutine bannerCycleCoroutine;
        private double displayedWin = 0;
        private bool isCurrentlyNightMode = false;

        private void Awake()
        {
            if (spinButton != null)
            {
                spinButton.onClick.AddListener(() =>
                {
                    spinButton.transform.DOKill();
                    spinButton.transform.localScale = Vector3.one;
                    spinButton.transform.DOPunchScale(Vector3.one * -0.08f, 0.14f);
                    OnSpinClicked?.Invoke();
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
        }

        private void Start()
        {
            if (topDisplayBannerImage != null && bannerFreeSpinsSprite != null && bannerPayAnywhereSprite != null)
            {
                bannerCycleCoroutine = StartCoroutine(CycleTopDisplayBanners());
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
        }

        internal void SetWin(double targetWin, bool animate = false)
        {
            if (winCountTweener != null && winCountTweener.IsActive()) winCountTweener.Kill();

            if (!animate || totalWinText == null)
            {
                displayedWin = targetWin;
                if (totalWinText != null) totalWinText.text = targetWin > 0 ? $"${targetWin:F2}" : "$0.00";
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
            if (spinButton != null) spinButton.interactable = interactable;
            if (betPlusButton != null) betPlusButton.interactable = interactable;
            if (betMinusButton != null) betMinusButton.interactable = interactable;
            if (buyBonusButton != null) buyBonusButton.interactable = interactable;
            if (anteBetToggleButton != null) anteBetToggleButton.interactable = interactable;

            if (spinButtonArrows != null)
            {
                if (spinRotationTweener != null && spinRotationTweener.IsActive()) spinRotationTweener.Kill();

                if (!interactable)
                {
                    // Start continuous rotation loop while spin/tumble is active
                    spinRotationTweener = spinButtonArrows
                        .DORotate(new Vector3(0, 0, -360f), 0.85f, RotateMode.FastBeyond360)
                        .SetEase(Ease.Linear)
                        .SetLoops(-1);
                }
                else
                {
                    // Spin ended: stop rotation cleanly and return to upright position
                    spinRotationTweener = spinButtonArrows
                        .DORotate(Vector3.zero, 0.22f)
                        .SetEase(Ease.OutQuad);
                }
            }
        }

        internal void SetAnteBetState(bool isActive)
        {
            if (anteBetActiveIndicator != null) anteBetActiveIndicator.SetActive(isActive);
            if (buyBonusButton != null) buyBonusButton.interactable = !isActive;
            if (anteBetButtonImage != null)
            {
                anteBetButtonImage.sprite = isActive && anteBetGlowSprite != null ? anteBetGlowSprite : anteBetNormalSprite;
            }
        }

        internal void SetFreeSpinsDisplay(bool isInFreeSpins, int remainingSpins)
        {
            if (freeSpinsBadge != null) freeSpinsBadge.SetActive(isInFreeSpins);
            if (freeSpinsCountText != null) freeSpinsCountText.text = isInFreeSpins ? $"{remainingSpins}" : "";
            if (buyBonusButton != null && isInFreeSpins) buyBonusButton.interactable = false;

            ApplyThemeMode(isInFreeSpins);
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

            // Swap Background & Frame Sprites with smooth fade
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
