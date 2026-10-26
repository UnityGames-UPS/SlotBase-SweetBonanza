using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using SweetBonanza.Core;

namespace SweetBonanza.View
{
    [RequireComponent(typeof(RectTransform))]
    internal class CellView : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private Image symbolImage;
        [SerializeField] private TMP_Text multiplierText;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform rectTransform;

        private SymbolType currentSymbol;
        private int currentMultiplier;
        private Sequence activeSequence;
        private Tweener activeTweener;

        private void Awake()
        {
            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        }

        internal SymbolType CurrentSymbol => currentSymbol;
        internal RectTransform RectTransform => rectTransform;

        internal void Setup(SymbolType symbol, Sprite sprite, int multiplier = 0)
        {
            KillActiveTweens();

            currentSymbol = symbol;
            currentMultiplier = multiplier;

            if (symbolImage != null)
            {
                symbolImage.sprite = sprite;
                symbolImage.color = Color.white;
                symbolImage.enabled = sprite != null;
            }

            if (multiplierText != null)
            {
                if (symbol == SymbolType.MultiplierBomb && multiplier > 0)
                {
                    multiplierText.text = $"{multiplier}X";
                    multiplierText.gameObject.SetActive(true);
                }
                else
                {
                    multiplierText.gameObject.SetActive(false);
                }
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }

            rectTransform.localScale = Vector3.one;
        }

        internal void PlayWinPulseAnimation(Action onComplete)
        {
            KillActiveTweens();

            activeSequence = DOTween.Sequence();
            activeSequence.Append(rectTransform.DOScale(1.25f, 0.18f).SetEase(Ease.OutQuad));
            activeSequence.Append(rectTransform.DOScale(0.95f, 0.12f).SetEase(Ease.InQuad));
            activeSequence.Append(rectTransform.DOScale(1.15f, 0.12f).SetEase(Ease.OutQuad));
            activeSequence.OnComplete(() => onComplete?.Invoke());
        }

        internal void PlayExplodeAnimation(Action onComplete)
        {
            KillActiveTweens();

            activeSequence = DOTween.Sequence();
            activeSequence.Append(rectTransform.DOScale(1.3f, 0.1f).SetEase(Ease.OutQuad));
            activeSequence.Join(canvasGroup != null ? canvasGroup.DOFade(0.3f, 0.15f) : null);
            activeSequence.Append(rectTransform.DOScale(0.0f, 0.15f).SetEase(Ease.InBack));
            activeSequence.OnComplete(() =>
            {
                if (symbolImage != null) symbolImage.enabled = false;
                onComplete?.Invoke();
            });
        }

        internal void PlayFallAnimation(Vector2 targetAnchoredPos, float duration, Action onComplete)
        {
            KillActiveTweens();

            activeTweener = rectTransform.DOAnchorPos(targetAnchoredPos, duration)
                .SetEase(Ease.OutBounce, 1.2f)
                .OnComplete(() => onComplete?.Invoke());
        }

        internal void PlayDropInAnimation(Vector2 targetAnchoredPos, float delay, float duration, Action onComplete)
        {
            KillActiveTweens();

            rectTransform.localScale = Vector3.one;
            if (canvasGroup != null) canvasGroup.alpha = 1f;
            if (symbolImage != null) symbolImage.enabled = true;

            activeSequence = DOTween.Sequence();
            if (delay > 0)
            {
                activeSequence.AppendInterval(delay);
            }
            activeSequence.Append(rectTransform.DOAnchorPos(targetAnchoredPos, duration).SetEase(Ease.OutBack, 1.1f));
            activeSequence.OnComplete(() => onComplete?.Invoke());
        }

        internal void PlayMultiplierBombRevealAnimation(int multiplier, Action onComplete)
        {
            KillActiveTweens();

            currentMultiplier = multiplier;
            if (multiplierText != null)
            {
                multiplierText.text = $"{multiplier}X";
                multiplierText.gameObject.SetActive(true);
            }

            activeSequence = DOTween.Sequence();
            activeSequence.Append(rectTransform.DOScale(1.4f, 0.25f).SetEase(Ease.OutElastic));
            activeSequence.Append(rectTransform.DOScale(1.0f, 0.15f).SetEase(Ease.InOutQuad));
            activeSequence.OnComplete(() => onComplete?.Invoke());
        }

        internal void ResetCell()
        {
            KillActiveTweens();
            rectTransform.localScale = Vector3.one;
            if (canvasGroup != null) canvasGroup.alpha = 1f;
            if (symbolImage != null)
            {
                symbolImage.sprite = null;
                symbolImage.enabled = false;
            }
            if (multiplierText != null) multiplierText.gameObject.SetActive(false);
        }

        private void KillActiveTweens()
        {
            if (activeSequence != null && activeSequence.IsActive())
            {
                activeSequence.Kill();
            }
            activeSequence = null;

            if (activeTweener != null && activeTweener.IsActive())
            {
                activeTweener.Kill();
            }
            activeTweener = null;
        }

        private void OnDestroy()
        {
            KillActiveTweens();
        }
    }
}
