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
        public Image symbolImage;
        public TMP_Text multiplierText;
        public TMP_Text winAmountText;
        public CanvasGroup canvasGroup;
        public RectTransform rectTransform;

        [Header("Visual Effects")]
        public Image burstEffectImage;
        public Sprite candyBurstSprite;
        public Sprite bombBurstSprite;

        private SymbolType currentSymbol;
        private int currentMultiplier;
        private Sprite baseSymbolSprite;
        private Sequence activeSequence;
        private Tweener activeTweener;
        private Coroutine activeAnimRoutine;
        private Coroutine activeBurstRoutine;

        private void Awake()
        {
            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        }

        public SymbolType CurrentSymbol => currentSymbol;
        public RectTransform RectTransform => rectTransform;
        public Sprite BaseSymbolSprite => baseSymbolSprite;
        public Sprite CurrentSprite => symbolImage != null ? symbolImage.sprite : null;

        public void Setup(SymbolType symbol, Sprite sprite, int multiplier = 0)
        {
            KillActiveTweens();

            currentSymbol = symbol;
            currentMultiplier = multiplier;
            baseSymbolSprite = sprite;

            if (symbolImage != null)
            {
                symbolImage.sprite = sprite;
                symbolImage.color = Color.white;
                symbolImage.enabled = sprite != null;
                if (symbol == SymbolType.Lollipop)
                {
                    symbolImage.transform.localScale = new Vector3(1.75f, 1.75f, 1f);
                    transform.SetAsLastSibling();
                }
                else if (symbol == SymbolType.MultiplierBomb)
                {
                    symbolImage.transform.localScale = new Vector3(1.15f, 1.15f, 1f);
                    transform.SetAsLastSibling();
                }
                else
                {
                    symbolImage.transform.localScale = Vector3.one;
                }
            }

            if (multiplierText != null)
            {
                if (symbol == SymbolType.MultiplierBomb && multiplier > 0)
                {
                    multiplierText.text = SpriteTextUtility.FormatBombMultiplier(multiplier);
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
            transform.SetAsLastSibling();

            activeSequence = DOTween.Sequence();
            activeSequence.Append(rectTransform.DOScale(1.24f, 0.16f).SetEase(Ease.OutBack));
            activeSequence.Append(rectTransform.DOScale(1.08f, 0.14f).SetEase(Ease.InOutQuad));
            activeSequence.Append(rectTransform.DOScale(1.18f, 0.14f).SetEase(Ease.OutQuad));

            if (winAmount > 0 && winAmountText != null)
            {
                winAmountText.text = SpriteTextUtility.FormatWithSpriteAsset(winAmount, winAmountText);
                winAmountText.gameObject.SetActive(true);
                winAmountText.transform.DOKill();
                winAmountText.transform.localScale = Vector3.zero;
                winAmountText.rectTransform.anchoredPosition = Vector2.zero;

                activeSequence.Join(winAmountText.transform.DOScale(1.25f, 0.18f).SetEase(Ease.OutBack));
                activeSequence.Join(winAmountText.rectTransform.DOAnchorPosY(45f, duration).SetEase(Ease.OutQuad));
            }

            if (frames != null && frames.Length > 0 && gameObject.activeInHierarchy)
            {
                activeAnimRoutine = StartCoroutine(CycleFrames(frames, duration, baseSymbolSprite));
            }

            activeSequence.OnComplete(() => onComplete?.Invoke());
        }

        internal void PlayWinPulseAnimation(Action onComplete)
        {
            PlayWinAnimation(null, 0.38f, 0, onComplete);
        }

        internal void PlayExplodeAnimation(Sprite[] burstFrames, Action onComplete, Sprite[] symbolFrames = null)
        {
            KillActiveTweens();

            if (winAmountText != null)
            {
                winAmountText.gameObject.SetActive(false);
            }

            activeSequence = DOTween.Sequence();

            // 1. Play the 3D symbol animation at accelerated speed during the burst pop
            if (symbolFrames != null && symbolFrames.Length > 0 && gameObject.activeInHierarchy)
            {
                activeAnimRoutine = StartCoroutine(CycleFrames(symbolFrames, 0.22f));
            }

            // 2. Play the GEffect candy glass shatter explosion directly over the bursting candy
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

            // 3. Punch scale up, flash, and shatter into pieces
            activeSequence.Append(rectTransform.DOScale(1.35f, 0.08f).SetEase(Ease.OutQuad));
            if (canvasGroup != null) activeSequence.Join(canvasGroup.DOFade(0.1f, 0.14f));
            activeSequence.Append(rectTransform.DOScale(0.0f, 0.14f).SetEase(Ease.InBack));
            activeSequence.OnComplete(() =>
            {
                if (symbolImage != null) symbolImage.enabled = false;
                if (burstEffectImage != null) burstEffectImage.enabled = false;
                onComplete?.Invoke();
            });
        }

        internal void PlayFallAnimation(Vector2 targetAnchoredPos, float duration, Action onComplete, float delay = 0f, Sprite[] animFrames = null)
        {
            KillActiveTweens();
            rectTransform.localScale = Vector3.one;
            rectTransform.localRotation = Quaternion.identity;
            if (canvasGroup != null) canvasGroup.alpha = 1f;
            if (symbolImage != null)
            {
                symbolImage.enabled = true;
                if (baseSymbolSprite != null) symbolImage.sprite = baseSymbolSprite;
            }
            if (burstEffectImage != null) burstEffectImage.enabled = false;
            if (winAmountText != null) winAmountText.gameObject.SetActive(false);

            activeSequence = DOTween.Sequence();
            if (delay > 0) activeSequence.AppendInterval(delay);

            float fallDuration = duration * 0.74f;
            // High velocity downward fall with subtle vertical stretch (strictly upright, no tilt)
            activeSequence.Append(rectTransform.DOAnchorPos(targetAnchoredPos, fallDuration).SetEase(Ease.InQuad));
            activeSequence.Join(rectTransform.DOScale(new Vector3(0.95f, 1.06f, 1f), fallDuration * 0.65f).SetEase(Ease.InQuad));

            // Crisp vertical gummy squash on landing
            activeSequence.Append(rectTransform.DOScale(new Vector3(1.14f, 0.86f, 1f), 0.07f).SetEase(Ease.OutQuad));
            // Elastic rebound and settle
            activeSequence.Append(rectTransform.DOScale(new Vector3(0.98f, 1.02f, 1f), 0.06f).SetEase(Ease.InOutQuad));
            activeSequence.Append(rectTransform.DOScale(Vector3.one, 0.05f).SetEase(Ease.OutQuad));

            // Play the 3D frame animation during tumble fall
            if (animFrames != null && animFrames.Length > 0 && gameObject.activeInHierarchy)
            {
                activeAnimRoutine = StartCoroutine(CycleFrames(animFrames, fallDuration, baseSymbolSprite, delay));
            }

            activeSequence.OnComplete(() => onComplete?.Invoke());
        }

        internal void PlayDropInAnimation(Vector2 targetAnchoredPos, float delay, float duration, Action onComplete, Sprite[] animFrames = null, Action onImpact = null)
        {
            KillActiveTweens();
            rectTransform.localScale = Vector3.one;
            rectTransform.localRotation = Quaternion.identity;
            if (canvasGroup != null) canvasGroup.alpha = 1f;
            if (symbolImage != null)
            {
                symbolImage.enabled = true;
                if (baseSymbolSprite != null) symbolImage.sprite = baseSymbolSprite;
            }
            if (burstEffectImage != null) burstEffectImage.enabled = false;
            if (winAmountText != null) winAmountText.gameObject.SetActive(false);

            activeSequence = DOTween.Sequence();
            if (delay > 0) activeSequence.AppendInterval(delay);

            // 1. Initial fall with vertical acceleration and subtle vertical jelly stretch (strictly upright track alignment)
            float fallDuration = duration * 0.74f;
            activeSequence.Append(rectTransform.DOAnchorPos(targetAnchoredPos, fallDuration).SetEase(Ease.InQuad));
            activeSequence.Join(rectTransform.DOScale(new Vector3(0.92f, 1.10f, 1f), fallDuration * 0.65f).SetEase(Ease.InQuad));

            if (onImpact != null)
            {
                activeSequence.AppendCallback(() => onImpact.Invoke());
            }

            // 2. Pure vertical gummy squash on impact (NO rotation/tilting - 100% upright like Pragmatic Play Sweet Bonanza)
            activeSequence.Append(rectTransform.DOScale(new Vector3(1.16f, 0.84f, 1f), 0.07f).SetEase(Ease.OutQuad));

            // 3. Elastic rebound bounce
            activeSequence.Append(rectTransform.DOScale(new Vector3(0.96f, 1.04f, 1f), 0.07f).SetEase(Ease.InOutQuad));

            // 4. Settle back to upright rest state
            activeSequence.Append(rectTransform.DOScale(Vector3.one, 0.06f).SetEase(Ease.OutQuad));

            // 5. Special playful Scatter Lollipop landing bounce if Scatter
            if (currentSymbol == SymbolType.Lollipop)
            {
                transform.SetAsLastSibling();
                activeSequence.Append(rectTransform.DOPunchScale(Vector3.one * 0.14f, 0.24f, 3, 0.3f));
            }

            // 6. Play the 3D frame animation while dropping
            if (animFrames != null && animFrames.Length > 0 && gameObject.activeInHierarchy)
            {
                activeAnimRoutine = StartCoroutine(CycleFrames(animFrames, fallDuration, baseSymbolSprite, delay));
            }

            activeSequence.OnComplete(() => onComplete?.Invoke());
        }

        internal void PlayClearDropAnimation(float delay, Action onComplete)
        {
            PlayClearDropAnimation(delay, 0.30f, onComplete);
        }

        internal void PlayClearDropAnimation(float delay, float duration, Action onComplete)
        {
            KillActiveTweens();
            rectTransform.localScale = Vector3.one;
            rectTransform.localRotation = Quaternion.identity;
            activeSequence = DOTween.Sequence();
            if (delay > 0) activeSequence.AppendInterval(delay);

            // Plunge cleanly down past the bottom mask boundary with acceleration
            float targetY = rectTransform.anchoredPosition.y - 1100f;
            activeSequence.Append(rectTransform.DOAnchorPosY(targetY, duration).SetEase(Ease.InQuad));
            activeSequence.Join(rectTransform.DOScale(new Vector3(0.90f, 1.12f, 1f), duration * 0.7f).SetEase(Ease.InQuad));
            activeSequence.OnComplete(() => onComplete?.Invoke());
        }

        internal void PlayMultiplierBombRevealAnimation(int multiplier, Action onComplete, Sprite[] threadFrames = null, Sprite[] blastFrames = null, Action onDetonated = null)
        {
            KillActiveTweens();
            currentMultiplier = multiplier;
            transform.SetAsLastSibling();

            if (multiplierText != null)
            {
                multiplierText.text = SpriteTextUtility.FormatBombMultiplier(multiplier);
                multiplierText.gameObject.SetActive(true);
                multiplierText.transform.localScale = Vector3.one;
            }

            if (burstEffectImage != null)
            {
                burstEffectImage.enabled = false;
            }

            if (gameObject.activeInHierarchy)
            {
                activeBurstRoutine = StartCoroutine(PlayBombBlastSequence(blastFrames,threadFrames, onComplete, onDetonated));
            }
            else
            {
                onDetonated?.Invoke();
                onComplete?.Invoke();
            }
        }

        private IEnumerator PlayBombBlastSequence(Sprite[] threadFrames, Sprite[] blastFrames, Action onComplete, Action onDetonated)
        {
            // 1. Light the thread (fuse spark/burning on burst effect overlay)
            //AudioManager.Instance?.PlayBombFuseSpark();

            if (threadFrames != null && threadFrames.Length > 0 && symbolImage != null)
            {
                Debug.Log("Bomb Fuse Spark");
                float threadRate = 1f / 30f;
                for (int i = 0; i < threadFrames.Length; i++)
                {
                    symbolImage.sprite = threadFrames[i];
                    yield return new WaitForSeconds(threadRate);
                }
            }
            // 2. The explosion blast (plays blastFrames 0-59 second on symbolImage)
            AudioManager.Instance?.PlayBombExplode();
            if (blastFrames != null && blastFrames.Length > 0 && symbolImage != null)
            {
                Debug.Log("Bomb Explode");
                float blastRate = 1f / 40f;
                for (int i = 0; i < blastFrames.Length; i++)
                {
                    symbolImage.sprite = blastFrames[i];
                    yield return new WaitForSeconds(blastRate);
                }
            }
            else
            {
                yield return new WaitForSeconds(0.35f);
            }

            if (burstEffectImage != null)
            {
                burstEffectImage.enabled = false;
            }

            // 3. Blast frames ended -> Multiplier text pop
            if (multiplierText != null)
            {
                multiplierText.gameObject.SetActive(true);
                multiplierText.transform.SetAsLastSibling();
                multiplierText.transform.localScale = Vector3.zero;

                activeSequence = DOTween.Sequence();
                activeSequence.Append(multiplierText.transform.DOScale(1.4f, 0.20f).SetEase(Ease.OutBack));
                activeSequence.Append(multiplierText.transform.DOScale(1.0f, 0.12f).SetEase(Ease.InOutQuad));
                activeSequence.AppendCallback(() =>
                {
                    if (multiplierText != null) multiplierText.gameObject.SetActive(false);
                    onDetonated?.Invoke();
                });
                activeSequence.OnComplete(() => onComplete?.Invoke());
            }
            else
            {
                onDetonated?.Invoke();
                onComplete?.Invoke();
            }

            activeBurstRoutine = null;
        }

        internal void PlayScatterTriggerAnimation(Sprite[] frames, float duration)
        {
            KillActiveTweens();

            if (burstEffectImage != null && candyBurstSprite != null)
            {
                burstEffectImage.sprite = candyBurstSprite;
                burstEffectImage.enabled = true;
                burstEffectImage.color = new Color(1f, 0.9f, 0.2f, 1f);
                burstEffectImage.transform.localScale = Vector3.zero;
                activeSequence = DOTween.Sequence();
                activeSequence.Join(burstEffectImage.transform.DOScale(1.7f, 0.3f).SetEase(Ease.OutBack));
                activeSequence.Join(burstEffectImage.DOFade(0f, 0.35f).SetEase(Ease.InQuad));
            }

            if (frames != null && frames.Length > 0 && gameObject.activeInHierarchy)
            {
                activeAnimRoutine = StartCoroutine(CycleFrames(frames, duration, baseSymbolSprite));
            }
        }

        private IEnumerator CycleFrames(Sprite[] frames, float duration, Sprite defaultSprite = null, float startDelay = 0f)
        {
            if (frames == null || frames.Length == 0) yield break;

            if (startDelay > 0f)
            {
                yield return new WaitForSeconds(startDelay);
            }

            float elapsed = 0f;
            // Constant, smooth 24 FPS symbol playback rate.
            // Never hyper-accelerate symbol frames when reel drop / tumble is fast.
            const float SYMBOL_ANIM_FPS = 24f;
            float frameRate = 1f / SYMBOL_ANIM_FPS;
            float frameTimer = 0f;
            int index = 0;

            while (elapsed < duration)
            {
                if (symbolImage != null && frames != null && frames.Length > 0)
                {
                    symbolImage.sprite = frames[index % frames.Length];
                }

                yield return null;
                elapsed += Time.deltaTime;
                frameTimer += Time.deltaTime;

                if (frameTimer >= frameRate)
                {
                    int advance = Mathf.Max(1, Mathf.FloorToInt(frameTimer / frameRate));
                    index += advance;
                    frameTimer -= advance * frameRate;
                }
            }

            if (symbolImage != null)
            {
                symbolImage.sprite = defaultSprite != null ? defaultSprite : baseSymbolSprite;
            }
            activeAnimRoutine = null;
        }

        private IEnumerator CycleBurstFrames(Sprite[] frames, float duration)
        {
            if (frames == null || frames.Length == 0) yield break;

            float elapsed = 0f;
            // Constant smooth 30 FPS burst shatter rate
            const float BURST_FPS = 30f;
            float frameRate = 1f / BURST_FPS;
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
            baseSymbolSprite = null;
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
                symbolImage.transform.localScale = Vector3.one;
            }
            if (multiplierText != null) multiplierText.gameObject.SetActive(false);
            if (winAmountText != null) winAmountText.gameObject.SetActive(false);
            if (burstEffectImage != null) burstEffectImage.enabled = false;
        }

        internal void CompleteAnimation()
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
            if (burstEffectImage != null)
            {
                burstEffectImage.enabled = false;
            }
            if (multiplierText != null && currentMultiplier > 0)
            {
                multiplierText.gameObject.SetActive(true);
                multiplierText.transform.localScale = Vector3.one;
            }
            if (symbolImage != null && baseSymbolSprite != null)
            {
                symbolImage.sprite = baseSymbolSprite;
            }
            if (activeSequence != null && activeSequence.IsActive())
            {
                activeSequence.Complete(true);
            }
            if (activeTweener != null && activeTweener.IsActive())
            {
                activeTweener.Complete(true);
            }
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

            if (multiplierText != null) multiplierText.transform.DOKill();
        }

        private void OnDestroy()
        {
            KillActiveTweens();
        }
    }
}
