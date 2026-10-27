using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace SweetBonanza.View
{
    public class UIManager : MonoBehaviour
    {
        internal event Action OnSpinClicked;
        internal event Action OnBetIncreaseClicked;
        internal event Action OnBetDecreaseClicked;
        internal event Action OnBetMaxClicked;
        internal event Action OnAnteBetToggled;
        internal event Action OnBuyFeatureRequested;
        internal event Action OnBuyFeatureConfirmed;
        internal event Action<int> OnAutoplayStarted;
        internal event Action OnAutoplayStopped;
        internal event Action<bool> OnQuickSpinChanged;
        internal event Action<bool> OnBatterySaverChanged;
        internal event Action<bool> OnAmbientMusicChanged;
        internal event Action<bool> OnSoundFxChanged;
        internal event Action<bool> OnIntroScreenChanged;

        [Header("Main HUD View Binding")]
        [SerializeField] private MainView mainView;

        [Header("Modal Backdrop")]
        [SerializeField] private Image modalBackdrop;

        [Header("1. Game Rules Modal")]
        [SerializeField] private GameObject gameRulesPanel;
        [SerializeField] private Button buttonOpenGameRules;
        [SerializeField] private Button buttonCloseGameRules;
        [SerializeField] private Button buttonNextRulesPage;
        [SerializeField] private Button buttonPrevRulesPage;
        [SerializeField] private Image rulesPageDisplayImage;
        [SerializeField] private TMP_Text rulesPageIndicatorText;
        [SerializeField] private List<Sprite> rulesPageSprites = new List<Sprite>();

        [Header("2. System Settings Modal")]
        [SerializeField] private GameObject systemSettingsPanel;
        [SerializeField] private Button buttonOpenSettings;
        [SerializeField] private Button buttonCloseSettings;
        [SerializeField] private Toggle toggleQuickSpin;
        [SerializeField] private Toggle toggleBatterySaver;
        [SerializeField] private Toggle toggleAmbientMusic;
        [SerializeField] private Toggle toggleSoundFx;
        [SerializeField] private Toggle toggleIntroScreen;

        [Header("3. Sound Settings Modal")]
        [SerializeField] private GameObject soundSettingsPanel;
        [SerializeField] private Button buttonOpenSound;
        [SerializeField] private Button buttonCloseSound;
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;

        [Header("4. Buy Feature Modal")]
        [SerializeField] private GameObject buyFeatureConfirmPanel;
        [SerializeField] private Button buttonConfirmBuyFeature;
        [SerializeField] private Button buttonCancelBuyFeature;
        [SerializeField] private TMP_Text buyFeatureCostText;

        [Header("5. Bet Adjustment Modal")]
        [SerializeField] private GameObject betAdjustmentPanel;
        [SerializeField] private Button buttonOpenBetAdjustment;
        [SerializeField] private Button buttonCloseBetAdjustment;
        [SerializeField] private Button buttonIncreaseBetLevel;
        [SerializeField] private Button buttonDecreaseBetLevel;
        [SerializeField] private TMP_Text betCoinsText;
        [SerializeField] private TMP_Text coinValueText;
        [SerializeField] private TMP_Text totalBetAmountText;
        [SerializeField] private TMP_Text settingsBetAmountText;

        [Header("Autoplay Panel")]
        [SerializeField] private GameObject autoplayPanel;
        [SerializeField] private Button buttonOpenAutoplay;
        [SerializeField] private Button buttonCloseAutoplay;
        [SerializeField] private Button buttonStartAutoplay;
        [SerializeField] private Button buttonStopAutoplay;
        [SerializeField] private TMP_Text autoplayRemainingText;

        [Header("Feature Overlays")]
        [SerializeField] private GameObject freeSpinsIntroPanel;
        [SerializeField] private TMP_Text freeSpinsIntroCountText;
        [SerializeField] private GameObject freeSpinsOutroPanel;
        [SerializeField] private TMP_Text freeSpinsOutroWinText;

        [Header("Win Celebration")]
        [SerializeField] private GameObject winCelebrationPanel;
        [SerializeField] private TMP_Text winCelebrationTitleText;
        [SerializeField] private TMP_Text winCelebrationAmountText;
        [SerializeField] private ParticleSystem winCelebrationParticles;

        private int currentRulesPageIndex = 0;
        private readonly Stack<GameObject> openModalStack = new Stack<GameObject>();
        private Tweener winCelebrationTweener;

        private void Awake()
        {
            RegisterButtonCallbacks();
            CloseAllModalsImmediate();
        }

        private void RegisterButtonCallbacks()
        {
            if (buttonOpenGameRules != null) buttonOpenGameRules.onClick.AddListener(OpenGameRules);
            if (buttonCloseGameRules != null) buttonCloseGameRules.onClick.AddListener(() => CloseModal(gameRulesPanel));
            if (buttonNextRulesPage != null) buttonNextRulesPage.onClick.AddListener(NextRulesPage);
            if (buttonPrevRulesPage != null) buttonPrevRulesPage.onClick.AddListener(PrevRulesPage);

            if (buttonOpenSettings != null) buttonOpenSettings.onClick.AddListener(OpenSystemSettings);
            if (buttonCloseSettings != null) buttonCloseSettings.onClick.AddListener(() => CloseModal(systemSettingsPanel));

            // System Settings: 5 Toggles
            if (toggleQuickSpin != null)
            {
                toggleQuickSpin.isOn = PlayerPrefs.GetInt("settings_quick_spin", 0) == 1;
                toggleQuickSpin.onValueChanged.AddListener(isOn =>
                {
                    PlayerPrefs.SetInt("settings_quick_spin", isOn ? 1 : 0);
                    OnQuickSpinChanged?.Invoke(isOn);
                });
            }

            if (toggleBatterySaver != null)
            {
                toggleBatterySaver.isOn = PlayerPrefs.GetInt("settings_battery_saver", 0) == 1;
                toggleBatterySaver.onValueChanged.AddListener(isOn =>
                {
                    PlayerPrefs.SetInt("settings_battery_saver", isOn ? 1 : 0);
                    OnBatterySaverChanged?.Invoke(isOn);
                });
            }

            if (toggleAmbientMusic != null)
            {
                toggleAmbientMusic.isOn = AudioManager.Instance != null ? AudioManager.Instance.MusicEnabled : true;
                toggleAmbientMusic.onValueChanged.AddListener(isOn =>
                {
                    OnAmbientMusicChanged?.Invoke(isOn);
                });
            }

            if (toggleSoundFx != null)
            {
                toggleSoundFx.isOn = AudioManager.Instance != null ? AudioManager.Instance.SfxEnabled : true;
                toggleSoundFx.onValueChanged.AddListener(isOn =>
                {
                    OnSoundFxChanged?.Invoke(isOn);
                });
            }

            if (toggleIntroScreen != null)
            {
                toggleIntroScreen.isOn = PlayerPrefs.GetInt("settings_intro_screen", 1) == 1;
                toggleIntroScreen.onValueChanged.AddListener(isOn =>
                {
                    PlayerPrefs.SetInt("settings_intro_screen", isOn ? 1 : 0);
                    OnIntroScreenChanged?.Invoke(isOn);
                });
            }

            // Sound Settings: Master, Music, SFX Sliders
            if (buttonOpenSound != null) buttonOpenSound.onClick.AddListener(OpenSoundSettings);
            if (buttonCloseSound != null) buttonCloseSound.onClick.AddListener(() => CloseModal(soundSettingsPanel));
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.value = AudioListener.volume;
                masterVolumeSlider.onValueChanged.AddListener(v => AudioListener.volume = v);
            }
            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.value = AudioManager.Instance != null ? AudioManager.Instance.MusicVolume : 0.5f;
                musicVolumeSlider.onValueChanged.AddListener(v => AudioManager.Instance?.SetMusicVolume(v));
            }
            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.value = AudioManager.Instance != null ? AudioManager.Instance.SfxVolume : 1.0f;
                sfxVolumeSlider.onValueChanged.AddListener(v => AudioManager.Instance?.SetSfxVolume(v));
            }

            if (buttonConfirmBuyFeature != null) buttonConfirmBuyFeature.onClick.AddListener(() =>
            {
                CloseModal(buyFeatureConfirmPanel);
                OnBuyFeatureConfirmed?.Invoke();
            });
            if (buttonCancelBuyFeature != null) buttonCancelBuyFeature.onClick.AddListener(() => CloseModal(buyFeatureConfirmPanel));

            if (buttonOpenBetAdjustment != null) buttonOpenBetAdjustment.onClick.AddListener(OpenBetAdjustmentModal);
            if (buttonCloseBetAdjustment != null) buttonCloseBetAdjustment.onClick.AddListener(() => CloseModal(betAdjustmentPanel));

            if (buttonIncreaseBetLevel != null) buttonIncreaseBetLevel.onClick.AddListener(() => OnBetIncreaseClicked?.Invoke());
            if (buttonDecreaseBetLevel != null) buttonDecreaseBetLevel.onClick.AddListener(() => OnBetDecreaseClicked?.Invoke());

            if (buttonOpenAutoplay != null) buttonOpenAutoplay.onClick.AddListener(OpenAutoplayModal);
            if (buttonCloseAutoplay != null) buttonCloseAutoplay.onClick.AddListener(() => CloseModal(autoplayPanel));
            if (buttonStartAutoplay != null) buttonStartAutoplay.onClick.AddListener(() =>
            {
                CloseModal(autoplayPanel);
                OnAutoplayStarted?.Invoke(10);
            });
            if (buttonStopAutoplay != null) buttonStopAutoplay.onClick.AddListener(() => OnAutoplayStopped?.Invoke());

            if (mainView != null)
            {
                mainView.OnSpinClicked += () => OnSpinClicked?.Invoke();
                mainView.OnBetIncreaseClicked += () => OnBetIncreaseClicked?.Invoke();
                mainView.OnBetDecreaseClicked += () => OnBetDecreaseClicked?.Invoke();
                mainView.OnAnteBetToggled += () => OnAnteBetToggled?.Invoke();
                mainView.OnBuyBonusClicked += () => OnBuyFeatureRequested?.Invoke();
            }
        }

        private void OpenModal(GameObject modal, bool useBackdrop = true)
        {
            if (modal == null) return;
            if (useBackdrop && modalBackdrop != null)
            {
                modalBackdrop.gameObject.SetActive(true);
                modalBackdrop.DOKill();
                modalBackdrop.DOFade(0.6f, 0.25f);
            }
            modal.SetActive(true);
            modal.transform.localScale = Vector3.one * 0.9f;
            modal.transform.DOKill();
            modal.transform.DOScale(1f, 0.25f).SetEase(Ease.OutBack);
            openModalStack.Push(modal);
        }

        private void CloseModal(GameObject modal)
        {
            if (modal == null) return;
            modal.transform.DOKill();
            modal.transform.DOScale(0.9f, 0.18f).SetEase(Ease.InBack).OnComplete(() => modal.SetActive(false));

            if (openModalStack.Count > 0 && openModalStack.Peek() == modal) openModalStack.Pop();
            if (openModalStack.Count == 0 && modalBackdrop != null)
            {
                modalBackdrop.DOKill();
                modalBackdrop.DOFade(0f, 0.2f).OnComplete(() => modalBackdrop.gameObject.SetActive(false));
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

        internal void OpenSystemSettings() => OpenModal(systemSettingsPanel);
        internal void OpenSoundSettings() => OpenModal(soundSettingsPanel);

        internal void OpenBuyFeatureModal(double betAmount)
        {
            double buyCost = betAmount * 100.0;
            if (buyFeatureCostText != null) buyFeatureCostText.text = $"${buyCost:F2}";
            OpenModal(buyFeatureConfirmPanel, false);
        }

        internal void OpenBetAdjustmentModal() => OpenModal(betAdjustmentPanel);
        internal void OpenAutoplayModal() => OpenModal(autoplayPanel);

        internal void ShowFreeSpinsIntro(int spinsCount)
        {
            if (freeSpinsIntroCountText != null) freeSpinsIntroCountText.text = $"{spinsCount}";
            OpenModal(freeSpinsIntroPanel, false);
        }

        internal void ShowFreeSpinsOutro(double totalWin)
        {
            if (freeSpinsOutroWinText != null) freeSpinsOutroWinText.text = $"${totalWin:F2}";
            OpenModal(freeSpinsOutroPanel, false);
        }

        internal void ShowWinCelebration(string title, double winAmount, float displayDuration = 3.0f)
        {
            if (winCelebrationTitleText != null) winCelebrationTitleText.text = title;
            if (winCelebrationPanel != null) OpenModal(winCelebrationPanel, true);
            if (winCelebrationParticles != null) winCelebrationParticles.Play();
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
            if (winCelebrationPanel != null && winCelebrationPanel.activeSelf) CloseModal(winCelebrationPanel);
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

        private void OnDestroy()
        {
            if (winCelebrationTweener != null && winCelebrationTweener.IsActive()) winCelebrationTweener.Kill();
        }
    }
}
