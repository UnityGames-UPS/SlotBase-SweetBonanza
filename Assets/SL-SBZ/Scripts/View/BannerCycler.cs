using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace SweetBonanza.View
{
    public class BannerCycler : MonoBehaviour
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
        [SerializeField] private MainView mainView;

        private int currentIndex = 0;
        private bool isFront = true;
        private Coroutine cycleCoroutine;
        private bool isPaused = false;

        private void OnEnable()
        {
            if (mainView != null) mainView.OnSpinClicked += AdvanceOnSpin;
            InitializeDisplay();
            StartCycle();
        }

        private void OnDisable()
        {
            if (mainView != null) mainView.OnSpinClicked -= AdvanceOnSpin;
            StopCycle();
            KillTweens();
        }

        /// <summary>Advances immediately to the next banner sprite when Spin is clicked.</summary>
        public void AdvanceOnSpin()
        {
            if (imageList == null || imageList.Count <= 1) return;
            currentIndex = (currentIndex + 1) % imageList.Count;
            ApplySpriteInstant(currentIndex);
            StartCycle();
        }

        /// <summary>Shows or hides the banner images.</summary>
        public void SetBannerVisible(bool visible)
        {
            if (bannerImageFront != null) bannerImageFront.gameObject.SetActive(visible);
            if (bannerImageBack != null) bannerImageBack.gameObject.SetActive(visible);
        }

        /// <summary>Pauses or resumes banner cycling (e.g. during tumble sequences).</summary>
        public void PauseCycle(bool pause)
        {
            isPaused = pause;
            if (pause) StopCycle();
            else StartCycle();
        }

        private void InitializeDisplay()
        {
            if (imageList == null || imageList.Count == 0) return;

            currentIndex = 0;
            isFront = true;

            if (bannerImageFront != null)
            {
                bannerImageFront.sprite = imageList[0];
                bannerImageFront.color = new Color(bannerImageFront.color.r, bannerImageFront.color.g, bannerImageFront.color.b, 1f);
            }
            if (bannerImageBack != null)
            {
                bannerImageBack.color = new Color(bannerImageBack.color.r, bannerImageBack.color.g, bannerImageBack.color.b, 0f);
            }
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
            if (isActiveAndEnabled && !isPaused && imageList != null && imageList.Count > 1)
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
            while (!isPaused)
            {
                float waitTime = Random.Range(minInterval, maxInterval);
                yield return new WaitForSeconds(waitTime);

                if (imageList == null || imageList.Count <= 1) yield break;

                currentIndex = (currentIndex + 1) % imageList.Count;

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

