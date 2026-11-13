using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace SweetBonanza.View
{
    internal class BannerCycler : MonoBehaviour
    {
        [Header("Banner Sprites")]
        [SerializeField] private List<Sprite> imageList;

        [Header("Image Display")]
        [SerializeField] private Image bannerImageFront;
        [SerializeField] private Image bannerImageBack; // Optional: if assigned, uses cross-fade; if null, uses snap (1 image)

        [Header("Timing")]
        [SerializeField] private float minInterval = 15f;
        [SerializeField] private float maxInterval = 20f;
        [SerializeField] private float fadeDuration = 1f;

        [Header("Spin Trigger (Optional)")]
        [SerializeField] private bool syncWithSpin = true;
        [SerializeField] private MainView mainView;

        private int currentIndex = 0;
        private bool isFront = true;
        private Coroutine cycleCoroutine;
        private bool hasInitialized = false;
        private bool isSpinCycleLocked = false;

        private void Awake()
        {
            if (!syncWithSpin) return;
            if (bannerImageFront != null) bannerImageFront.raycastTarget = false;
            if (mainView == null)
            {
                mainView = GetComponentInParent<MainView>();
                if (mainView == null) mainView = UnityEngine.Object.FindFirstObjectByType<MainView>();
            }
            if (mainView != null)
            {
                mainView.OnSpinStarted += AdvanceOnSpin;
                mainView.OnSpinStopped += HandleSpinStopped;
            }
        }

        private void OnEnable()
        {
            InitializeDisplay();
            StartCycle();
        }

        private void OnDisable()
        {
            StopCycle();
            KillTweens();
        }

        private void OnDestroy()
        {
            if (syncWithSpin && mainView != null)
            {
                mainView.OnSpinStarted -= AdvanceOnSpin;
                mainView.OnSpinStopped -= HandleSpinStopped;
            }
            StopCycle();
            KillTweens();
        }

        /// <summary>Locks the banner to index 0 (GOOD LUCK) when a spin starts.</summary>
        internal void AdvanceOnSpin()
        {
            if (!syncWithSpin) return;
            if (imageList == null || imageList.Count == 0) return;
            isSpinCycleLocked = true;
            currentIndex = 0;
            ApplySpriteInstant(currentIndex);
            StopCycle();
        }

        /// <summary>Switches to a random idle banner (excluding index 0) and resumes cycling when spin stops.</summary>
        internal void HandleSpinStopped()
        {
            if (!syncWithSpin) return;
            if (imageList == null || imageList.Count == 0) return;
            isSpinCycleLocked = false;
            currentIndex = GetRandomIdleIndex();
            ApplySpriteInstant(currentIndex);
            StartCycle();
        }

        private int GetRandomIdleIndex()
        {
            if (imageList == null || imageList.Count <= 1) return 0;
            if (imageList.Count == 2) return 1;

            int next = Random.Range(1, imageList.Count);
            if (next == currentIndex)
            {
                next = next + 1 < imageList.Count ? next + 1 : 1;
            }
            return next;
        }

        private void InitializeDisplay()
        {
            if (imageList == null || imageList.Count == 0) return;

            if (!hasInitialized)
            {
                currentIndex = syncWithSpin ? GetRandomIdleIndex() : 0;
                hasInitialized = true;
            }
            ApplySpriteInstant(currentIndex);
        }

        private void ApplySpriteInstant(int index)
        {
            KillTweens();
            if (bannerImageBack == null)
            {
                // Pure Snap Mode (1 Image)
                if (bannerImageFront != null)
                {
                    bannerImageFront.sprite = imageList[index];
                    bannerImageFront.color = new Color(bannerImageFront.color.r, bannerImageFront.color.g, bannerImageFront.color.b, 1f);
                }
            }
            else
            {
                // Fade Mode reset to front
                if (bannerImageFront != null)
                {
                    bannerImageFront.sprite = imageList[index];
                    bannerImageFront.color = new Color(bannerImageFront.color.r, bannerImageFront.color.g, bannerImageFront.color.b, 1f);
                }
                if (bannerImageBack != null)
                {
                    bannerImageBack.color = new Color(bannerImageBack.color.r, bannerImageBack.color.g, bannerImageBack.color.b, 0f);
                }
                isFront = true;
            }
        }

        private void StartCycle()
        {
            StopCycle();
            if (syncWithSpin && isSpinCycleLocked) return;
            int minCount = syncWithSpin ? 2 : 1;
            if (isActiveAndEnabled && imageList != null && imageList.Count > minCount)
            {
                cycleCoroutine = StartCoroutine(CycleRoutine());
            }
        }

        private void StopCycle()
        {
            if (cycleCoroutine != null)
            {
                StopCoroutine(cycleCoroutine);
                cycleCoroutine = null;
            }
        }

        private IEnumerator CycleRoutine()
        {
            while (!(syncWithSpin && isSpinCycleLocked))
            {
                float waitTime = Random.Range(minInterval, maxInterval);
                float elapsed = 0f;
                while (elapsed < waitTime)
                {
                    if (syncWithSpin && isSpinCycleLocked) yield break;
                    yield return null;
                    elapsed += Time.deltaTime;
                }

                if (imageList == null || imageList.Count <= 1) yield break;

                currentIndex = syncWithSpin
                    ? GetRandomIdleIndex()
                    : (currentIndex + 1) % imageList.Count;

                if (bannerImageBack == null)
                {
                    // Snap Mode (1 Image)
                    if (bannerImageFront != null) bannerImageFront.sprite = imageList[currentIndex];
                }
                else
                {
                    // Cross-Fade Mode (2 Images)
                    KillTweens();
                    if (isFront)
                    {
                        bannerImageBack.sprite = imageList[currentIndex];
                        bannerImageFront.DOFade(0f, fadeDuration);
                        bannerImageBack.DOFade(1f, fadeDuration);
                    }
                    else
                    {
                        bannerImageFront.sprite = imageList[currentIndex];
                        bannerImageBack.DOFade(0f, fadeDuration);
                        bannerImageFront.DOFade(1f, fadeDuration);
                    }
                    isFront = !isFront;
                }
            }
        }

        private void KillTweens()
        {
            if (bannerImageFront != null) bannerImageFront.DOKill();
            if (bannerImageBack != null) bannerImageBack.DOKill();
        }
    }
}

