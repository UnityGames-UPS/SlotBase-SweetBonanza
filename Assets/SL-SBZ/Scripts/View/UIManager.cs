using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using SweetBonanza.Core;

namespace SweetBonanza.View
{
    public class UIManager : MonoBehaviour
    {
        #region Internal Action Events
        internal event Action OnSpinClicked;
        internal event Action OnBetIncreaseClicked;
        internal event Action OnBetDecreaseClicked;
        internal event Action OnBetCoinsIncreaseClicked;
        internal event Action OnBetCoinsDecreaseClicked;
        internal event Action OnCoinValueIncreaseClicked;
        internal event Action OnCoinValueDecreaseClicked;
        internal event Action OnBetMaxClicked;
        internal event Action OnAnteBetToggled;
        internal event Action OnBuyFeatureRequested;
        internal event Action OnBuyFeatureConfirmed;
        internal event Action<int> OnAutoplayStarted;
        internal event Action OnAutoplayStopped;

        // System Settings Events
        internal event Action OnGameHistoryClicked;
        internal event Action<bool> OnBatterySaverToggled;
        internal event Action<bool> OnMusicToggled;
        internal event Action<bool> OnSoundFXToggled;
        internal event Action<bool> OnIntroScreenToggled;
        internal event Action<bool> OnSlotsPromotionsToggled;
        internal event Action<bool> OnQuickSpinToggled;

        // Sound Events
        internal event Action<float> OnMasterVolumeChanged;
        internal event Action<float> OnMusicVolumeChanged;
        internal event Action<float> OnSFXVolumeChanged;

        // Free Spins Events
        internal event Action OnFreeSpinsIntroDismissed;
        internal event Action OnFreeSpinsOutroCollected;
        #endregion

        #region Serialized Fields
        [Header("Main HUD Reference")]
        [SerializeField] private MainView mainView;

        [Header("Main HUD Trigger Buttons")]
        [SerializeField] private Button buttonInfo;
        [SerializeField] private Button buttonSystemSettings;
        [SerializeField] private Button buttonSound;
        [SerializeField] private Button buttonOpenBetAdjustment;
        [SerializeField] private Button buttonBuyFeature;
        [SerializeField] private Button buttonAnteBetToggle;
        [SerializeField] private Button buttonOpenAutoplay;
        [SerializeField] private Button buttonStopAutoplay;

        [Header("Modal Backdrop Dimmer Layer")]
        [SerializeField] private CanvasGroup modalBackdrop;
        [SerializeField] private Button backdropClickCatcher;

        [Header("Game Rules Panel - Root & Display")]
        [SerializeField] private GameObject gameRulesPanel;
        [SerializeField] private Image rulesPageDisplayImage;
        [SerializeField] private List<Sprite> rulesPageSprites = new List<Sprite>();
        [SerializeField] private TMP_Text rulesPageIndicatorText;

        [Header("Game Rules Panel - Navigation Buttons")]
        [SerializeField] private Button rulesNextButton;
        [SerializeField] private Button rulesPrevButton;
        [SerializeField] private Button rulesCloseButton;
        private int currentRulesPageIndex = 0;

        [Header("System Settings Panel - Root")]
        [SerializeField] private GameObject systemSettingsPanel;

        [Header("System Settings Panel - Left Column")]
        [SerializeField] private Button buttonGameHistory;
        [SerializeField] private Button settingsBetMinusButton;
        [SerializeField] private Button settingsBetPlusButton;
        [SerializeField] private TMP_Text settingsBetAmountText;

        [Header("System Settings Panel - Right Column")]
        [SerializeField] private Toggle batterySaverToggle;
        [SerializeField] private Toggle ambientMusicToggle;
        [SerializeField] private Toggle soundFXToggle;
        [SerializeField] private Toggle introScreenToggle;
        [SerializeField] private Toggle slotsPromotionsToggle;

        [Header("System Settings Panel - Close Button")]
        [SerializeField] private Button systemSettingsCloseButton;

        [Header("Sound Settings Panel - Root")]
        [SerializeField] private GameObject soundSettingsPanel;

        [Header("Sound Settings Panel - Volume Sliders")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;

        [Header("Sound Settings Panel - Close Button")]
        [SerializeField] private Button soundSettingsCloseButton;

        [Header("Buy Feature Confirmation Modal")]
        [SerializeField] private GameObject buyFeatureConfirmPanel;
        [SerializeField] private TMP_Text buyFeatureCostText;
        [SerializeField] private Button buyFeatureConfirmButton;
        [SerializeField] private Button buyFeatureCancelButton;

        [Header("Bet Adjustment Panel - Root")]
        [SerializeField] private GameObject betAdjustmentPanel;

        [Header("Bet Adjustment Panel - Bet Coins Controls")]
        [SerializeField] private Button betCoinsMinusButton;
        [SerializeField] private Button betCoinsPlusButton;
        [SerializeField] private TMP_Text betCoinsText;

        [Header("Bet Adjustment Panel - Coin Value Controls")]
        [SerializeField] private Button coinValueMinusButton;
        [SerializeField] private Button coinValuePlusButton;
        [SerializeField] private TMP_Text coinValueText;

        [Header("Bet Adjustment Panel - Total Bet Controls")]
        [SerializeField] private Button totalBetMinusButton;
        [SerializeField] private Button totalBetPlusButton;
        [SerializeField] private TMP_Text totalBetAmountText;
        [SerializeField] private Button betMaxButton;

        [Header("Bet Adjustment Panel - Close Button")]
        [SerializeField] private Button betAdjustmentCloseButton;

        [Header("Autoplay Panel - Root")]
        [SerializeField] private GameObject autoplayPanel;

        [Header("Autoplay Panel - Spin Count Presets")]
        [SerializeField] private Button[] autoplayCountButtons;
        [SerializeField] private int[] autoplayCounts = { 10, 20, 30, 50, 100 };

        [Header("Autoplay Panel - Speed & Mode Toggles")]
        [SerializeField] private Toggle autoplayTurboToggle;
        [SerializeField] private Toggle autoplayQuickToggle;

        [Header("Autoplay Panel - Actions & Status")]
        [SerializeField] private Button autoplayStartButton;
        [SerializeField] private Button autoplayCloseButton;
        [SerializeField] private TMP_Text autoplayRemainingText;
        private int selectedAutoplayRounds = 10;

        [Header("Free Spins Intro Modal")]
        [SerializeField] private GameObject freeSpinsIntroPanel;
        [SerializeField] private TMP_Text freeSpinsIntroCountText;
        [SerializeField] private Button freeSpinsStartButton;

        [Header("Free Spins Outro Modal")]
        [SerializeField] private GameObject freeSpinsOutroPanel;
        [SerializeField] private TMP_Text freeSpinsOutroWinText;
        [SerializeField] private Button freeSpinsCollectButton;

        [Header("Win Celebration Overlay")]
        [SerializeField] private GameObject winCelebrationPanel;
        [SerializeField] private TMP_Text winCelebrationTitleText;
        [SerializeField] private TMP_Text winCelebrationAmountText;
        [SerializeField] private ParticleSystem winCelebrationParticles;
        [SerializeField] private Button winCelebrationDismissButton;
        #endregion

        private readonly Stack<GameObject> openModalStack = new Stack<GameObject>();
        private Tweener winCelebrationTweener;

        private void Awake()
        {
            SetupHUDButtons();
            SetupGameRulesModal();
            SetupSystemSettingsModal();
            SetupSoundModal();
            SetupBuyFeatureModal();
            SetupBetAdjustmentModal();
            SetupAutoplayModal();
            SetupFreeSpinsModals();
            SetupWinCelebrationModal();

            if (backdropClickCatcher != null)
            {
                backdropClickCatcher.onClick.AddListener(OnBackdropClicked);
            }

            CloseAllModalsImmediate();
        }

        #region Event Wiring
        private void SetupHUDButtons()
        {
            if (mainView != null)
            {
                mainView.OnSpinClicked += () =>
                {
                    AudioManager.Instance?.PlayPrimaryActionButton();
                    OnSpinClicked?.Invoke();
                };
                mainView.OnBetIncreaseClicked += () =>
                {
                    AudioManager.Instance?.PlayBetPlusMinus();
                    OnBetIncreaseClicked?.Invoke();
                };
                mainView.OnBetDecreaseClicked += () =>
                {
                    AudioManager.Instance?.PlayBetPlusMinus();
                    OnBetDecreaseClicked?.Invoke();
                };
                mainView.OnAnteBetToggled += () =>
                {
                    AudioManager.Instance?.PlayButton();
                    OnAnteBetToggled?.Invoke();
                };
                mainView.OnBuyBonusClicked += () =>
                {
                    AudioManager.Instance?.PlayButton();
                    OnBuyFeatureRequested?.Invoke();
                };
                mainView.OnSettingClicked += () =>
                {
                    AudioManager.Instance?.PlayPopupOpen();
                    OpenSystemSettings();
                };
                mainView.OnAudioClicked += () =>
                {
                    AudioManager.Instance?.PlayPopupOpen();
                    OpenSoundSettings();
                };
                mainView.OnOptionClicked += () =>
                {
                    AudioManager.Instance?.PlayPopupOpen();
                    OpenGameRules();
                };
            }

            if (buttonInfo != null)
            {
                buttonInfo.onClick.AddListener(() =>
                {
                    AudioManager.Instance?.PlayPopupOpen();
                    OpenGameRules();
                });
            }

            if (buttonSystemSettings != null)
            {
                buttonSystemSettings.onClick.AddListener(() =>
                {
                    AudioManager.Instance?.PlayPopupOpen();
                    OpenSystemSettings();
                });
            }

            if (buttonSound != null)
            {
                buttonSound.onClick.AddListener(() =>
                {
                    AudioManager.Instance?.PlayPopupOpen();
                    OpenSoundSettings();
                });
            }

            if (buttonOpenBetAdjustment != null)
            {
                buttonOpenBetAdjustment.onClick.AddListener(() =>
                {
                    AudioManager.Instance?.PlayPopupOpen();
                    OpenBetAdjustmentModal();
                });
            }

            if (buttonBuyFeature != null)
            {
                buttonBuyFeature.onClick.AddListener(() =>
                {
                    AudioManager.Instance?.PlayButton();
                    OnBuyFeatureRequested?.Invoke();
                });
            }

            if (buttonAnteBetToggle != null)
            {
                buttonAnteBetToggle.onClick.AddListener(() =>
                {
                    AudioManager.Instance?.PlayButton();
                    OnAnteBetToggled?.Invoke();
                });
            }

            if (buttonOpenAutoplay != null)
            {
                buttonOpenAutoplay.onClick.AddListener(() =>
                {
                    AudioManager.Instance?.PlayAutoplayPanelOpen();
                    OpenAutoplayModal();
                });
            }

            if (buttonStopAutoplay != null)
            {
                buttonStopAutoplay.onClick.AddListener(() =>
                {
                    AudioManager.Instance?.PlayAutoplayStop();
                    OnAutoplayStopped?.Invoke();
                });
            }
        }

        private void SetupGameRulesModal()
        {
            if (rulesNextButton != null)
            {
                rulesNextButton.onClick.AddListener(() =>
                {
                    AudioManager.Instance?.PlayButton();
                    NextRulesPage();
                });
            }

            if (rulesPrevButton != null)
            {
                rulesPrevButton.onClick.AddListener(() =>
                {
                    AudioManager.Instance?.PlayButton();
                    PrevRulesPage();
                });
            }

            if (rulesCloseButton != null)
            {
                rulesCloseButton.onClick.AddListener(() =>
                {
                    AudioManager.Instance?.PlayPopupClose();
                    CloseModal(gameRulesPanel);
                });
            }
        }

        private void SetupSystemSettingsModal()
        {
            if (buttonGameHistory != null)
            {
                buttonGameHistory.onClick.AddListener(() =>
                {
                    AudioManager.Instance?.PlayButton();
                    OnGameHistoryClicked?.Invoke();
                });
            }

            if (settingsBetMinusButton != null)
            {
                settingsBetMinusButton.onClick.AddListener(() =>
                {
                    AudioManager.Instance?.PlayBetPlusMinus();
                    OnBetDecreaseClicked?.Invoke();
                });
            }

            if (settingsBetPlusButton != null)
            {
                settingsBetPlusButton.onClick.AddListener(() =>
                {
                    AudioManager.Instance?.PlayBetPlusMinus();
                    OnBetIncreaseClicked?.Invoke();
                });
            }

            if (batterySaverToggle != null)
            {
                batterySaverToggle.onValueChanged.AddListener(val =>
                {
                    AudioManager.Instance?.PlayButton();
                    Application.targetFrameRate = val ? 30 : 60;
                    OnBatterySaverToggled?.Invoke(val);
                });
            }

            if (ambientMusicToggle != null)
            {
                ambientMusicToggle.onValueChanged.AddListener(val =>
                {
                    AudioManager.Instance?.PlayButton();
                    AudioManager.Instance?.SetMusicEnabled(val);
                    OnMusicToggled?.Invoke(val);
                });
            }

            if (soundFXToggle != null)
            {
                soundFXToggle.onValueChanged.AddListener(val =>
                {
                    AudioManager.Instance?.PlayButton();
                    AudioManager.Instance?.SetSfxEnabled(val);
                    OnSoundFXToggled?.Invoke(val);
                });
            }

            if (introScreenToggle != null)
            {
                introScreenToggle.onValueChanged.AddListener(val =>
                {
                    AudioManager.Instance?.PlayButton();
                    OnIntroScreenToggled?.Invoke(val);
                });
            }

            if (slotsPromotionsToggle != null)
            {
                slotsPromotionsToggle.onValueChanged.AddListener(val =>
                {
                    AudioManager.Instance?.PlayButton();
                    OnSlotsPromotionsToggled?.Invoke(val);
                });
            }

            if (systemSettingsCloseButton != null)
            {
                systemSettingsCloseButton.onClick.AddListener(() =>
                {
                    AudioManager.Instance?.PlayPopupClose();
                    CloseModal(systemSettingsPanel);
                });
            }
        }

        private void SetupSoundModal()
        {
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.onValueChanged.AddListener(val =>
                {
                    AudioListener.volume = Mathf.Clamp01(val);
                    OnMasterVolumeChanged?.Invoke(val);
                });
            }

            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.onValueChanged.AddListener(val =>
                {
                    AudioManager.Instance?.SetMusicVolume(val);
                    OnMusicVolumeChanged?.Invoke(val);
                });
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.onValueChanged.AddListener(val =>
                {
                    AudioManager.Instance?.SetSfxVolume(val);
                    OnSFXVolumeChanged?.Invoke(val);
                });
            }

            if (soundSettingsCloseButton != null)
            {
                soundSettingsCloseButton.onClick.AddListener(() =>
                {
                    AudioManager.Instance?.PlayPopupClose();
                    CloseModal(soundSettingsPanel);
                });
            }
        }

        private void SetupBuyFeatureModal()
        {
            if (buyFeatureConfirmButton != null)
            {
                buyFeatureConfirmButton.onClick.AddListener(() =>
                {
                    AudioManager.Instance?.PlayPrimaryActionButton();
                    CloseModal(buyFeatureConfirmPanel);
                    OnBuyFeatureConfirmed?.Invoke();
                });
            }

            if (buyFeatureCancelButton != null)
            {
                buyFeatureCancelButton.onClick.AddListener(() =>
                {
                    AudioManager.Instance?.PlayPopupClose();
                    CloseModal(buyFeatureConfirmPanel);
                });
            }
        }

        private void SetupBetAdjustmentModal()
        {
            if (betCoinsPlusButton != null)
            {
                betCoinsPlusButton.onClick.AddListener(() =>
                {
                    AudioManager.Instance?.PlayBetPlusMinus();
                    OnBetCoinsIncreaseClicked?.Invoke();
                });
            }

            if (betCoinsMinusButton != null)
            {
                betCoinsMinusButton.onClick.AddListener(() =>
                {
                    AudioManager.Instance?.PlayBetPlusMinus();
                    OnBetCoinsDecreaseClicked?.Invoke();
                });
            }

            if (coinValuePlusButton != null)
            {
                coinValuePlusButton.onClick.AddListener(() =>
                {
                    AudioManager.Instance?.PlayBetPlusMinus();
                    OnCoinValueIncreaseClicked?.Invoke();
                });
            }

            if (coinValueMinusButton != null)
            {
                coinValueMinusButton.onClick.AddListener(() =>
                {
                    AudioManager.Instance?.PlayBetPlusMinus();
                    OnCoinValueDecreaseClicked?.Invoke();
                });
            }

            if (totalBetPlusButton != null)
            {
                totalBetPlusButton.onClick.AddListener(() =>
                {
                    AudioManager.Instance?.PlayBetPlusMinus();
                    OnBetIncreaseClicked?.Invoke();
                });
            }

            if (totalBetMinusButton != null)
            {
                totalBetMinusButton.onClick.AddListener(() =>
                {
                    AudioManager.Instance?.PlayBetPlusMinus();
                    OnBetDecreaseClicked?.Invoke();
                });
            }

            if (betMaxButton != null)
            {
                betMaxButton.onClick.AddListener(() =>
                {
                    AudioManager.Instance?.PlayMaxBetReached();
                    OnBetMaxClicked?.Invoke();
                });
            }

            if (betAdjustmentCloseButton != null)
            {
                betAdjustmentCloseButton.onClick.AddListener(() =>
                {
                    AudioManager.Instance?.PlayPopupClose();
                    CloseModal(betAdjustmentPanel);
                });
            }
        }

        private void SetupAutoplayModal()
        {
            if (autoplayCountButtons != null)
            {
                for (int i = 0; i < autoplayCountButtons.Length; i++)
                {
                    int index = i;
                    if (index < autoplayCounts.Length && autoplayCountButtons[index] != null)
                    {
                        autoplayCountButtons[index].onClick.AddListener(() =>
                        {
                            AudioManager.Instance?.PlayButton();
                            selectedAutoplayRounds = autoplayCounts[index];
                        });
                    }
                }
            }

            if (autoplayQuickToggle != null)
            {
                autoplayQuickToggle.onValueChanged.AddListener(val =>
                {
                    AudioManager.Instance?.PlayButton();
                    OnQuickSpinToggled?.Invoke(val);
                });
            }

            if (autoplayStartButton != null)
            {
                autoplayStartButton.onClick.AddListener(() =>
                {
                    AudioManager.Instance?.PlayPrimaryActionButton();
                    CloseModal(autoplayPanel);
                    OnAutoplayStarted?.Invoke(selectedAutoplayRounds);
                });
            }

            if (autoplayCloseButton != null)
            {
                autoplayCloseButton.onClick.AddListener(() =>
                {
                    AudioManager.Instance?.PlayPopupClose();
                    CloseModal(autoplayPanel);
                });
            }
        }

        private void SetupFreeSpinsModals()
        {
            if (freeSpinsStartButton != null)
            {
                freeSpinsStartButton.onClick.AddListener(() =>
                {
                    AudioManager.Instance?.PlayPrimaryActionButton();
                    CloseModal(freeSpinsIntroPanel);
                    OnFreeSpinsIntroDismissed?.Invoke();
                });
            }

            if (freeSpinsCollectButton != null)
            {
                freeSpinsCollectButton.onClick.AddListener(() =>
                {
                    AudioManager.Instance?.PlayPrimaryActionButton();
                    CloseModal(freeSpinsOutroPanel);
                    OnFreeSpinsOutroCollected?.Invoke();
                });
            }
        }

        private void SetupWinCelebrationModal()
        {
            if (winCelebrationDismissButton != null)
            {
                winCelebrationDismissButton.onClick.AddListener(() =>
                {
                    AudioManager.Instance?.PlayPopupClose();
                    CloseModal(winCelebrationPanel);
                });
            }
        }
        #endregion

        #region Modal Transitions
        internal void OpenModal(GameObject modal, bool dismissible = true)
        {
            if (modal == null) return;

            if (openModalStack.Count == 0 && modalBackdrop != null)
            {
                modalBackdrop.gameObject.SetActive(true);
                modalBackdrop.DOKill();
                modalBackdrop.alpha = 0f;
                modalBackdrop.DOFade(1f, 0.25f);
            }

            openModalStack.Push(modal);
            modal.SetActive(true);

            var rect = modal.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.DOKill();
                rect.localScale = Vector3.one * 0.85f;
                rect.DOScale(1f, 0.28f).SetEase(Ease.OutBack);
            }
        }

        internal void CloseModal(GameObject modal)
        {
            if (modal == null || !modal.activeSelf) return;

            var rect = modal.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.DOKill();
                rect.DOScale(0.85f, 0.18f).SetEase(Ease.InBack).OnComplete(() =>
                {
                    modal.SetActive(false);
                });
            }
            else
            {
                modal.SetActive(false);
            }

            if (openModalStack.Count > 0 && openModalStack.Peek() == modal)
            {
                openModalStack.Pop();
            }

            if (openModalStack.Count == 0 && modalBackdrop != null)
            {
                modalBackdrop.DOKill();
                modalBackdrop.DOFade(0f, 0.2f).OnComplete(() =>
                {
                    modalBackdrop.gameObject.SetActive(false);
                });
            }
        }

        private void OnBackdropClicked()
        {
            if (openModalStack.Count > 0)
            {
                var topModal = openModalStack.Peek();
                if (topModal != buyFeatureConfirmPanel && topModal != freeSpinsIntroPanel && topModal != freeSpinsOutroPanel)
                {
                    AudioManager.Instance?.PlayPopupClose();
                    CloseModal(topModal);
                }
            }
        }

        private void CloseAllModalsImmediate()
        {
            openModalStack.Clear();
            if (modalBackdrop != null) modalBackdrop.gameObject.SetActive(false);
            if (gameRulesPanel != null) gameRulesPanel.SetActive(false);
            if (systemSettingsPanel != null) systemSettingsPanel.SetActive(false);
            if (soundSettingsPanel != null) soundSettingsPanel.SetActive(false);
            if (buyFeatureConfirmPanel != null) buyFeatureConfirmPanel.SetActive(false);
            if (betAdjustmentPanel != null) betAdjustmentPanel.SetActive(false);
            if (autoplayPanel != null) autoplayPanel.SetActive(false);
            if (freeSpinsIntroPanel != null) freeSpinsIntroPanel.SetActive(false);
            if (freeSpinsOutroPanel != null) freeSpinsOutroPanel.SetActive(false);
            if (winCelebrationPanel != null) winCelebrationPanel.SetActive(false);
        }
        #endregion

        #region Public Methods (Strictly Internal)
        internal void OpenGameRules()
        {
            currentRulesPageIndex = 0;
            UpdateRulesPageDisplay();
            OpenModal(gameRulesPanel);
        }

        internal void NextRulesPage()
        {
            if (rulesPageSprites == null || rulesPageSprites.Count == 0) return;
            currentRulesPageIndex = (currentRulesPageIndex + 1) % rulesPageSprites.Count;
            UpdateRulesPageDisplay();
        }

        internal void PrevRulesPage()
        {
            if (rulesPageSprites == null || rulesPageSprites.Count == 0) return;
            currentRulesPageIndex--;
            if (currentRulesPageIndex < 0) currentRulesPageIndex = rulesPageSprites.Count - 1;
            UpdateRulesPageDisplay();
        }

        private void UpdateRulesPageDisplay()
        {
            if (rulesPageDisplayImage != null && rulesPageSprites != null && rulesPageSprites.Count > 0 && currentRulesPageIndex < rulesPageSprites.Count)
            {
                rulesPageDisplayImage.sprite = rulesPageSprites[currentRulesPageIndex];
            }

            if (rulesPageIndicatorText != null && rulesPageSprites != null && rulesPageSprites.Count > 0)
            {
                rulesPageIndicatorText.text = $"{currentRulesPageIndex + 1} / {rulesPageSprites.Count}";
            }
        }

        internal void OpenSystemSettings()
        {
            OpenModal(systemSettingsPanel);
        }

        internal void OpenSoundSettings()
        {
            OpenModal(soundSettingsPanel);
        }

        internal void OpenBuyFeatureModal(double betAmount)
        {
            double buyCost = betAmount * 100.0;
            if (buyFeatureCostText != null)
            {
                buyFeatureCostText.text = $"${buyCost:F2}";
            }
            OpenModal(buyFeatureConfirmPanel, false);
        }

        internal void OpenBetAdjustmentModal()
        {
            OpenModal(betAdjustmentPanel);
        }

        internal void OpenAutoplayModal()
        {
            OpenModal(autoplayPanel);
        }

        internal void ShowFreeSpinsIntro(int spinsCount)
        {
            if (freeSpinsIntroCountText != null)
            {
                freeSpinsIntroCountText.text = $"{spinsCount}";
            }
            OpenModal(freeSpinsIntroPanel, false);
        }

        internal void ShowFreeSpinsOutro(double totalWin)
        {
            if (freeSpinsOutroWinText != null)
            {
                freeSpinsOutroWinText.text = $"${totalWin:F2}";
            }
            OpenModal(freeSpinsOutroPanel, false);
        }

        internal void ShowWinCelebration(string title, double winAmount, float displayDuration = 3.0f)
        {
            if (winCelebrationTitleText != null) winCelebrationTitleText.text = title;
            if (winCelebrationPanel != null) OpenModal(winCelebrationPanel, true);

            if (winCelebrationParticles != null)
            {
                winCelebrationParticles.Play();
            }

            if (winCelebrationTweener != null && winCelebrationTweener.IsActive()) winCelebrationTweener.Kill();

            float startWin = 0;
            winCelebrationTweener = DOTween.To(() => startWin, x =>
            {
                startWin = x;
                if (winCelebrationAmountText != null) winCelebrationAmountText.text = $"${x:F2}";
            }, (float)winAmount, 1.2f).SetEase(Ease.OutCubic);

            StartCoroutine(AutoDismissCelebration(displayDuration));
        }

        private IEnumerator AutoDismissCelebration(float duration)
        {
            yield return new WaitForSeconds(duration);
            if (winCelebrationPanel != null && winCelebrationPanel.activeSelf)
            {
                CloseModal(winCelebrationPanel);
            }
        }

        internal void UpdateHUD(double balance, double bet, double win, bool isAnteBet, bool isSpinning)
        {
            if (mainView != null)
            {
                mainView.SetBalance(balance);
                mainView.SetBet(bet, isAnteBet);
                mainView.SetWin(win, false);
                mainView.SetSpinButtonInteractable(!isSpinning);
            }

            if (settingsBetAmountText != null)
            {
                double effectiveBet = isAnteBet ? bet * 1.25 : bet;
                settingsBetAmountText.text = $"{effectiveBet}";
            }

            if (totalBetAmountText != null)
            {
                double effectiveBet = isAnteBet ? bet * 1.25 : bet;
                totalBetAmountText.text = $"{effectiveBet:F2}";
            }
        }

        internal void UpdateBetAdjustmentDetails(int betCoins, double coinValue, double totalBet, bool isAnteBet)
        {
            if (betCoinsText != null) betCoinsText.text = $"{betCoins}";
            if (coinValueText != null) coinValueText.text = $"{coinValue:F2}";
            if (totalBetAmountText != null)
            {
                double effectiveBet = isAnteBet ? totalBet * 1.25 : totalBet;
                totalBetAmountText.text = $"{effectiveBet:F2}";
            }
        }

        internal void SetAutoplayState(bool isAutoplaying, int remainingSpins = 0)
        {
            if (buttonStopAutoplay != null) buttonStopAutoplay.gameObject.SetActive(isAutoplaying);
            if (buttonOpenAutoplay != null) buttonOpenAutoplay.interactable = !isAutoplaying;
            if (autoplayRemainingText != null)
            {
                autoplayRemainingText.text = isAutoplaying ? $"{remainingSpins}" : "";
                autoplayRemainingText.gameObject.SetActive(isAutoplaying);
            }
        }

        internal void SetStatusMessage(string message)
        {
            if (mainView != null) mainView.SetStatusMessage(message);
        }
        #endregion

        private void OnDestroy()
        {
            if (winCelebrationTweener != null && winCelebrationTweener.IsActive())
            {
                winCelebrationTweener.Kill();
            }
        }
    }
}
