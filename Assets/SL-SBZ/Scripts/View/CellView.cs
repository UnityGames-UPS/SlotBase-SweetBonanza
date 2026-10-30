using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using SweetBonanza.Model;

namespace SweetBonanza.View
{
    [SelectionBase]
    [RequireComponent(typeof(RectTransform))]
    public class CellView : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private Image symbolImage;
        [SerializeField] private TMP_Text multiplierText;
        [SerializeField] private TMP_Text winAmountText;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform rectTransform;

        [Header("Visual Effects")]
        [SerializeField] private Image burstEffectImage;
        [SerializeField] private Sprite candyBurstSprite;
        [SerializeField] private Sprite bombBurstSprite;

        private SymbolType currentSymbol;
        private int currentMultiplier;
        private Sequence activeSequence;
        private Tweener activeTweener;
        private Coroutine activeAnimRoutine;
        private Coroutine activeBurstRoutine;

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

            if (winAmountText != null)
            {
                winAmountText.gameObject.SetActive(false);
            }

            if (burstEffectImage != null)
            {
                burstEffectImage.enabled = false;
                burstEffectImage.transform.localScale = Vector3.one;
            }

            if (canvasGroup != null) canvasGroup.alpha = 1f;
            if (rectTransform != null)
            {
                rectTransform.localScale = Vector3.one;
                rectTransform.localRotation = Quaternion.identity;
            }
        }

        internal void PlayWinAnimation(Sprite[] frames, float duration, double winAmount, Action onComplete)
        {
            KillActiveTweens();

            activeSequence = DOTween.Sequence();
            activeSequence.Append(rectTransform.DOScale(1.24f, 0.16f).SetEase(Ease.OutBack));
            activeSequence.Append(rectTransform.DOScale(1.08f, 0.14f).SetEase(Ease.InOutQuad));
            activeSequence.Append(rectTransform.DOScale(1.18f, 0.14f).SetEase(Ease.OutQuad));

            if (winAmount > 0 && winAmountText != null)
            {
                winAmountText.text = $"+${winAmount:F2}";
                winAmountText.gameObject.SetActive(true);
                winAmountText.transform.DOKill();
                winAmountText.transform.localScale = Vector3.zero;
                winAmountText.rectTransform.anchoredPosition = Vector2.zero;

                activeSequence.Join(winAmountText.transform.DOScale(1.25f, 0.18f).SetEase(Ease.OutBack));
                activeSequence.Join(winAmountText.rectTransform.DOAnchorPosY(45f, duration).SetEase(Ease.OutQuad));
            }

            if (frames != null && frames.Length > 0 && gameObject.activeInHierarchy)
            {
                activeAnimRoutine = StartCoroutine(CycleFrames(frames, duration));
            }

            activeSequence.OnComplete(() => onComplete?.Invoke());
        }

        internal void PlayWinPulseAnimation(Action onComplete)
        {
            PlayWinAnimation(null, 0.38f, 0, onComplete);
        }

        internal void PlayExplodeAnimation(Sprite[] burstFrames, Action onComplete)
        {
            KillActiveTweens();

            if (winAmountText != null)
            {
                winAmountText.gameObject.SetActive(false);
            }

            activeSequence = DOTween.Sequence();

            if (burstEffectImage != null)
            {
                burstEffectImage.enabled = true;
                burstEffectImage.color = new Color(1f, 1f, 1f, 0.95f);
                burstEffectImage.transform.localScale = Vector3.zero;

                if (burstFrames != null && burstFrames.Length > 0 && gameObject.activeInHierarchy)
                {
                    burstEffectImage.sprite = burstFrames[0];
                    activeBurstRoutine = StartCoroutine(CycleBurstFrames(burstFrames, 0.22f));
                }
                else if (candyBurstSprite != null)
                {
                    burstEffectImage.sprite = candyBurstSprite;
                }

                activeSequence.Join(burstEffectImage.transform.DOScale(1.5f, 0.2f).SetEase(Ease.OutBack));
                activeSequence.Join(burstEffectImage.DOFade(0f, 0.22f).SetEase(Ease.InQuad));
            }

            activeSequence.Append(rectTransform.DOScale(1.3f, 0.08f).SetEase(Ease.OutQuad));
            if (canvasGroup != null) activeSequence.Join(canvasGroup.DOFade(0.2f, 0.14f));
            activeSequence.Append(rectTransform.DOScale(0.0f, 0.14f).SetEase(Ease.InBack));
            activeSequence.OnComplete(() =>
            {
                if (symbolImage != null) symbolImage.enabled = false;
                if (burstEffectImage != null) burstEffectImage.enabled = false;
                onComplete?.Invoke();
            });
        }

        internal void PlayFallAnimation(Vector2 targetAnchoredPos, float duration, Action onComplete)
        {
            KillActiveTweens();

            activeSequence = DOTween.Sequence();
            // Fall with subtle vertical stretch
            activeSequence.Append(rectTransform.DOAnchorPos(targetAnchoredPos, duration).SetEase(Ease.InQuad));
            activeSequence.Join(rectTransform.DOScale(new Vector3(0.93f, 1.08f, 1f), duration * 0.7f).SetEase(Ease.InQuad));

            // Bubbly gummy squash on landing
            activeSequence.Append(rectTransform.DOScale(new Vector3(1.18f, 0.84f, 1f), 0.08f).SetEase(Ease.OutQuad));
            // Rebound and settle
            activeSequence.Append(rectTransform.DOScale(new Vector3(0.97f, 1.03f, 1f), 0.08f).SetEase(Ease.InOutQuad));
            activeSequence.Append(rectTransform.DOScale(Vector3.one, 0.07f).SetEase(Ease.OutQuad));

            activeSequence.OnComplete(() => onComplete?.Invoke());
        }

        internal void PlayDropInAnimation(Vector2 targetAnchoredPos, float delay, float duration, Action onComplete)
        {
            KillActiveTweens();
            rectTransform.localScale = Vector3.one;
            rectTransform.localRotation = Quaternion.identity;
            if (canvasGroup != null) canvasGroup.alpha = 1f;
            if (symbolImage != null) symbolImage.enabled = true;
            if (burstEffectImage != null) burstEffectImage.enabled = false;
            if (winAmountText != null) winAmountText.gameObject.SetActive(false);

            activeSequence = DOTween.Sequence();
            if (delay > 0) activeSequence.AppendInterval(delay);

            // 1. Initial fall with vertical jelly stretch
            float fallDuration = duration * 0.72f;
            activeSequence.Append(rectTransform.DOAnchorPos(targetAnchoredPos, fallDuration).SetEase(Ease.InQuad));
            activeSequence.Join(rectTransform.DOScale(new Vector3(0.90f, 1.12f, 1f), fallDuration * 0.6f).SetEase(Ease.InQuad));

            // 2. Gummy squash on impact (flat horizontal spread)
            float randomTilt = UnityEngine.Random.Range(-4.5f, 4.5f);
            activeSequence.Append(rectTransform.DOScale(new Vector3(1.24f, 0.80f, 1f), 0.08f).SetEase(Ease.OutQuad));
            activeSequence.Join(rectTransform.DORotate(new Vector3(0, 0, randomTilt), 0.08f).SetEase(Ease.OutQuad));

            // 3. Elastic rebound bounce
            activeSequence.Append(rectTransform.DOScale(new Vector3(0.95f, 1.06f, 1f), 0.09f).SetEase(Ease.InOutQuad));
            activeSequence.Join(rectTransform.DORotate(new Vector3(0, 0, -randomTilt * 0.5f), 0.09f).SetEase(Ease.InOutQuad));

            // 4. Settle back to upright rest state
            activeSequence.Append(rectTransform.DOScale(Vector3.one, 0.08f).SetEase(Ease.OutQuad));
            activeSequence.Join(rectTransform.DORotate(Vector3.zero, 0.08f).SetEase(Ease.OutQuad));

            // 5. Special playful Scatter Lollipop landing wobble if Scatter
            if (currentSymbol == SymbolType.Lollipop)
            {
                activeSequence.Append(rectTransform.DORotate(new Vector3(0, 0, 14f), 0.09f).SetEase(Ease.OutQuad));
                activeSequence.Append(rectTransform.DORotate(new Vector3(0, 0, -14f), 0.09f).SetEase(Ease.InOutQuad));
                activeSequence.Append(rectTransform.DORotate(Vector3.zero, 0.08f).SetEase(Ease.OutQuad));
                activeSequence.Join(rectTransform.DOPunchScale(Vector3.one * 0.18f, 0.24f, 4, 0.4f));
            }

            activeSequence.OnComplete(() => onComplete?.Invoke());
        }

        internal void PlayClearDropAnimation(float delay, Action onComplete)
        {
            PlayClearDropAnimation(delay, 0.38f, onComplete);
        }

        internal void PlayClearDropAnimation(float delay, float duration, Action onComplete)
        {
            KillActiveTweens();
            rectTransform.localScale = Vector3.one;
            rectTransform.localRotation = Quaternion.identity;
            activeSequence = DOTween.Sequence();
            if (delay > 0) activeSequence.AppendInterval(delay);

            float targetY = rectTransform.anchoredPosition.y - 480f;
            activeSequence.Append(rectTransform.DOAnchorPosY(targetY, duration).SetEase(Ease.InQuad));
            activeSequence.Join(rectTransform.DOScale(new Vector3(0.92f, 1.12f, 1f), duration * 0.7f).SetEase(Ease.InQuad));
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

            if (burstEffectImage != null && bombBurstSprite != null)
            {
                burstEffectImage.sprite = bombBurstSprite;
                burstEffectImage.enabled = true;
                burstEffectImage.color = new Color(1f, 1f, 1f, 0.95f);
                burstEffectImage.transform.localScale = Vector3.zero;
                activeSequence.Join(burstEffectImage.transform.DOScale(2.0f, 0.35f).SetEase(Ease.OutBack));
                activeSequence.Join(burstEffectImage.DOFade(0f, 0.35f).SetEase(Ease.InQuad));
            }

            // Punch bounce, wiggle, and pop up
            activeSequence.Append(rectTransform.DOScale(1.4f, 0.18f).SetEase(Ease.OutElastic));
            activeSequence.Append(rectTransform.DORotate(new Vector3(0, 0, 15f), 0.08f).SetEase(Ease.OutQuad));
            activeSequence.Append(rectTransform.DORotate(new Vector3(0, 0, -15f), 0.08f).SetEase(Ease.InOutQuad));
            activeSequence.Append(rectTransform.DORotate(Vector3.zero, 0.08f).SetEase(Ease.OutQuad));
            activeSequence.Append(rectTransform.DOScale(1.0f, 0.12f).SetEase(Ease.InOutQuad));
            activeSequence.OnComplete(() =>
            {
                if (burstEffectImage != null) burstEffectImage.enabled = false;
                onComplete?.Invoke();
            });
        }

        internal void PlayScatterTriggerAnimation(Sprite[] frames, float duration)
        {
            KillActiveTweens();

            activeSequence = DOTween.Sequence();
            activeSequence.Append(rectTransform.DOScale(1.35f, 0.22f).SetEase(Ease.OutBack));
            activeSequence.Append(rectTransform.DORotate(new Vector3(0, 0, -360f), 0.75f, RotateMode.FastBeyond360).SetEase(Ease.Linear).SetLoops(2));
            activeSequence.Append(rectTransform.DOScale(1.0f, 0.2f).SetEase(Ease.OutQuad));

            if (burstEffectImage != null && candyBurstSprite != null)
            {
                burstEffectImage.sprite = candyBurstSprite;
                burstEffectImage.enabled = true;
                burstEffectImage.color = new Color(1f, 0.9f, 0.2f, 1f);
                burstEffectImage.transform.localScale = Vector3.zero;
                activeSequence.Join(burstEffectImage.transform.DOScale(1.7f, 0.3f).SetEase(Ease.OutBack));
                activeSequence.Join(burstEffectImage.DOFade(0f, 0.35f).SetEase(Ease.InQuad));
            }

            if (frames != null && frames.Length > 0 && gameObject.activeInHierarchy)
            {
                activeAnimRoutine = StartCoroutine(CycleFrames(frames, duration));
            }
        }

        private IEnumerator CycleFrames(Sprite[] frames, float duration)
        {
            float elapsed = 0f;
            float frameRate = 1f / 24f;
            int index = 0;
            while (elapsed < duration)
            {
                if (symbolImage != null && frames != null && frames.Length > 0)
                {
                    symbolImage.sprite = frames[index % frames.Length];
                }
                index++;
                elapsed += frameRate;
                yield return new WaitForSeconds(frameRate);
            }
            activeAnimRoutine = null;
        }

        private IEnumerator CycleBurstFrames(Sprite[] frames, float duration)
        {
            float elapsed = 0f;
            float frameRate = duration / frames.Length;
            int index = 0;
            while (elapsed < duration && index < frames.Length)
            {
                if (burstEffectImage != null && frames != null && index < frames.Length)
                {
                    burstEffectImage.sprite = frames[index];
                }
                index++;
                elapsed += frameRate;
                yield return new WaitForSeconds(frameRate);
            }
            activeBurstRoutine = null;
        }

        internal void ResetCell()
        {
            KillActiveTweens();
            if (rectTransform != null)
            {
                rectTransform.localScale = Vector3.one;
                rectTransform.localRotation = Quaternion.identity;
            }
            if (canvasGroup != null) canvasGroup.alpha = 1f;
            if (symbolImage != null)
            {
                symbolImage.sprite = null;
                symbolImage.enabled = false;
            }
            if (multiplierText != null) multiplierText.gameObject.SetActive(false);
            if (winAmountText != null) winAmountText.gameObject.SetActive(false);
            if (burstEffectImage != null) burstEffectImage.enabled = false;
        }

        private void KillActiveTweens()
        {
            if (activeAnimRoutine != null)
            {
                StopCoroutine(activeAnimRoutine);
                activeAnimRoutine = null;
            }

            if (activeBurstRoutine != null)
            {
                StopCoroutine(activeBurstRoutine);
                activeBurstRoutine = null;
            }

            if (activeSequence != null && activeSequence.IsActive()) activeSequence.Kill();
            activeSequence = null;

            if (activeTweener != null && activeTweener.IsActive()) activeTweener.Kill();
            activeTweener = null;
        }

        private void OnDestroy()
        {
            KillActiveTweens();
        }
    }
}
