using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace SweetBonanza.View
{
    internal class MainView : MonoBehaviour
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

        private Tweener winCountTweener;
        private double displayedWin = 0;

        private void Awake()
        {
            if (spinButton != null) spinButton.onClick.AddListener(() => OnSpinClicked?.Invoke());
            if (betPlusButton != null) betPlusButton.onClick.AddListener(() => OnBetIncreaseClicked?.Invoke());
            if (betMinusButton != null) betMinusButton.onClick.AddListener(() => OnBetDecreaseClicked?.Invoke());
            if (buyBonusButton != null) buyBonusButton.onClick.AddListener(() => OnBuyBonusClicked?.Invoke());
            if (anteBetToggleButton != null) anteBetToggleButton.onClick.AddListener(() => OnAnteBetToggled?.Invoke());
        }

        internal void SetBalance(double balance)
        {
            if (balanceText != null)
            {
                balanceText.text = $"${balance:F2}";
            }
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
            if (winCountTweener != null && winCountTweener.IsActive())
            {
                winCountTweener.Kill();
            }

            if (!animate || totalWinText == null)
            {
                displayedWin = targetWin;
                if (totalWinText != null)
                {
                    totalWinText.text = targetWin > 0 ? $"${targetWin:F2}" : "$0.00";
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
            if (spinButton != null) spinButton.interactable = interactable;
            if (betPlusButton != null) betPlusButton.interactable = interactable;
            if (betMinusButton != null) betMinusButton.interactable = interactable;
            if (buyBonusButton != null) buyBonusButton.interactable = interactable;
            if (anteBetToggleButton != null) anteBetToggleButton.interactable = interactable;
        }

        internal void SetAnteBetState(bool isActive)
        {
            if (anteBetActiveIndicator != null)
            {
                anteBetActiveIndicator.SetActive(isActive);
            }
            if (buyBonusButton != null)
            {
                buyBonusButton.interactable = !isActive;
            }
        }

        internal void SetFreeSpinsDisplay(bool isInFreeSpins, int remainingSpins)
        {
            if (freeSpinsBadge != null)
            {
                freeSpinsBadge.SetActive(isInFreeSpins);
            }

            if (freeSpinsCountText != null)
            {
                freeSpinsCountText.text = isInFreeSpins ? $"{remainingSpins}" : "";
            }

            if (buyBonusButton != null && isInFreeSpins)
            {
                buyBonusButton.interactable = false;
            }
        }

        internal void SetStatusMessage(string message)
        {
            if (statusBannerText != null)
            {
                statusBannerText.text = message;
            }
        }

        private void OnDestroy()
        {
            if (winCountTweener != null && winCountTweener.IsActive())
            {
                winCountTweener.Kill();
            }
        }
    }
}
