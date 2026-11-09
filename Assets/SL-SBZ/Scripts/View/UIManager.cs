using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using DG.Tweening;
using SweetBonanza.Model;

namespace SweetBonanza.View
{
    [Serializable]
    public class PaytableSymbolEntry
    {
        public SymbolType symbol;
        public TMP_Text textTier1; // 12+ (or 6+ for Scatter)
        public TMP_Text textTier2; // 10-11 (or 5 for Scatter)
        public TMP_Text textTier3; // 8-9 (or 4 for Scatter)
    }

    public class UIManager : MonoBehaviour
    {
        internal event Action OnSpinClicked;
        internal event Action OnStopClicked;
        internal event Action OnBetIncreaseClicked;
        internal event Action OnBetDecreaseClicked;
        internal event Action OnBetMaxClicked;
        internal event Action<double> OnBetAmountSelected;
        internal event Action OnAnteBetToggled;
        internal event Action OnBuyFeatureRequested;
        internal event Action OnBuyFeatureConfirmed;
        internal event Action<int> OnAutoplayStarted;
        internal event Action OnAutoplayStopped;
        internal event Action OnSpinStopped;
        internal event Action<bool> OnQuickSpinChanged;
        internal event Action<bool> OnBatterySaverChanged;
        internal event Action<bool> OnAmbientMusicChanged;
        internal event Action<bool> OnSoundFxChanged;
        internal event Action<bool> OnIntroScreenChanged;

        [Header("Main HUD View Binding")]
        [SerializeField] private MainView mainView;

        [Header("1. Game Rules Modal")]
        [SerializeField] private GameObject gameRulesPanel;
        [SerializeField] private Button buttonOpenGameRules;
        [SerializeField] private Button buttonCloseGameRules;
        [SerializeField] private Button buttonGreenCloseGameRules;
        [SerializeField] private Button buttonNextRulesPage;
        [SerializeField] private Button buttonPrevRulesPage;
        [SerializeField] private TMP_Text minBetText;
        [SerializeField] private TMP_Text maxBetText;
        [SerializeField] private List<GameObject> rulesPageObjects = new List<GameObject>();
        [Header("Rule Book / Paytable Symbols (Page 1)")]
        [SerializeField] private List<PaytableSymbolEntry> paytableSymbols = new List<PaytableSymbolEntry>();

        [Header("2. System Settings Modal")]
        [SerializeField] private GameObject systemSettingsPanel;
        [SerializeField] private Button buttonOpenSettings;
        [SerializeField] private Button buttonCloseSettings;
        [SerializeField] private Toggle toggleQuickSpin;
        [SerializeField] private Toggle toggleBatterySaver;
        [SerializeField] private Toggle toggleAmbientMusic;
        [SerializeField] private Toggle toggleSoundFx;
        [SerializeField] private Toggle toggleIntroScreen;
        [Header("System Settings - Total Bet")]
        [SerializeField] private TMP_Text settingsBetAmountText;
        [SerializeField] private Button buttonSystemSettingIncreaseBet;
        [SerializeField] private Button buttonSystemSettingDecreaseBet;

        [Header("3. Sound Settings Modal")]
        [SerializeField] private GameObject soundSettingsPanel;
        [SerializeField] private Button buttonOpenSound;
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Button buttonSoundMute;
        [SerializeField] private Image soundFillImage;
        [SerializeField] private TMP_Text soundVolumeText;
        [SerializeField] private Sprite speakerAudibleSprite;
        [SerializeField] private Sprite speakerMutedSprite;

        [Header("Sound Slider Sprites (Green Active / Red Muted)")]
        [SerializeField] private Image soundTrackLineImage;
        [SerializeField] private Image soundHandleImage;
        [SerializeField] private Sprite volumeLineGreenSprite;
        [SerializeField] private Sprite volumeLineRedSprite;
        [SerializeField] private Sprite volumeButtonGreenNormal;
        [SerializeField] private Sprite volumeButtonGreenPressed;
        [SerializeField] private Sprite volumeButtonRedNormal;
        [SerializeField] private Sprite volumeButtonRedPressed;

        [Header("4. Buy Feature Modal")]
        [SerializeField] private GameObject buyFeatureConfirmPanel;
        [SerializeField] private Button buttonConfirmBuyFeature;
        [SerializeField] private Button buttonCancelBuyFeature;
        [SerializeField] private TMP_Text buyFeatureCostText;
        [SerializeField] private TMP_Text buyFeatureButtonCostText;
        [SerializeField] private TMP_Text buyFeatureSpinsCountText;

        [Header("Ante Bet Slider & Sprites")]
        [SerializeField] private Button anteBetButton;
        [SerializeField] private Slider anteBetSlider;
        [SerializeField] private Image anteBetHandleImage;
        [SerializeField] private Sprite anteBetOffSprite;
        [SerializeField] private Sprite anteBetOnSprite;

        [Header("5. Bet Adjustment Modal")]
        [SerializeField] private GameObject betAdjustmentPanel;
        [SerializeField] private Button buttonOpenBetAdjustment;
        [SerializeField] private Button buttonCloseBetAdjustment;
        [SerializeField] private TMP_Text betMultiplierHeadingText;

        [Header("Bet Adjustment - Bet Level")]
        [SerializeField] private Button buttonIncreaseBetLevel;
        [SerializeField] private Button buttonDecreaseBetLevel;
        [SerializeField] private TMP_Text betCoinsText;

        [Header("Bet Adjustment - Coin Value")]
        [SerializeField] private Button buttonIncreaseCoinValue;
        [SerializeField] private Button buttonDecreaseCoinValue;
        [SerializeField] private TMP_Text coinValueText;

        [Header("Bet Adjustment - Total Bet")]
        [SerializeField] private Button buttonIncreaseTotalBet;
        [SerializeField] private Button buttonDecreaseTotalBet;
        [SerializeField] private TMP_Text totalBetAmountText;

        [Header("Bet Adjustment - Bet Max")]
        [SerializeField] private Button buttonBetMax;


        [Header("Autoplay Panel")]
        [SerializeField] private GameObject autoplayPanel;
        [SerializeField] private Button buttonOpenAutoplay;
        [SerializeField] private Button buttonCloseAutoplay;
        [SerializeField] private Button buttonStartAutoplay;
        //[SerializeField] private TMP_Text autoplayRemainingText;
        [SerializeField] private Slider autoplaySpinsSlider;
        [SerializeField] private TMP_Text autoplaySpinsCountText;
        [SerializeField] private TMP_Text autoplaySpinsCountSliderText;
        [SerializeField] private Toggle toggleTurboSpin;
        [SerializeField] private Toggle toggleQuickSpinAutoplay;
        [SerializeField] private Toggle toggleSkipScreens;

        [Header("Feature Overlays")]
        [SerializeField] private GameObject freeSpinsIntroPanel;
        [SerializeField] private TMP_Text freeSpinsIntroCountText;
        [SerializeField] private GameObject freeSpinsOutroPanel;
        [SerializeField] private TMP_Text freeSpinsOutroWinText;

        [Header("Win Celebration")]
        [SerializeField] private GameObject winCelebrationPanel;
        [SerializeField] private TMP_Text winCelebrationAmountText;

        [Header("7. Navigation & Fullscreen Controls")]
        [SerializeField] private Button homeButton;
        [SerializeField] private Button fullScreenButton;
        [SerializeField] private Button shrinkButton;

        [Header("External Managers")]
        [SerializeField] private PopupManager popupManager;
        [SerializeField] private JSFunctCalls jsFunctCalls;

        private readonly int[] betLevels = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        private readonly float[] coinValues = new float[]
        {
            0.01f, 0.02f, 0.03f, 0.04f, 0.05f, 0.06f, 0.07f, 0.08f, 0.09f, 0.10f,
            0.12f, 0.15f, 0.20f, 0.25f, 0.50f, 0.75f, 1.00f, 1.25f, 1.50f
        };
        private readonly int[] autoplaySpinOptions = new int[] { 10, 20, 30, 50, 70, 100, 500, 1000 };

        private int currentBetLevelIndex = 0;
        private int currentCoinValueIndex = 9;
        private bool isAnteBet = false;
        private bool isAutoplaying = false;
        private bool isExpanded = false;
        private int currentRulesPageIndex = 0;
        private GameObject currentOpenPanel;
        private Tweener winCelebrationTweener;
        private Tweener freeSpinsOutroTweener;
        private double currentBetAmount = 2.0;
        private float lastNonZeroVolume = 0.5f;
        private bool isSoundMuted = false;

        #region Lifecycle & Event Registrations

        private void Awake()
        {
            if (popupManager == null) popupManager = UnityEngine.Object.FindFirstObjectByType<PopupManager>();
            if (jsFunctCalls == null) jsFunctCalls = UnityEngine.Object.FindFirstObjectByType<JSFunctCalls>();
            if (jsFunctCalls != null) jsFunctCalls.RegisterFullscreenListener(gameObject.name);

            RegisterButtonCallbacks();
            CloseAllModalsImmediate();
            SetExpandShrinkButtons(false);
        }

        private void RegisterButtonCallbacks()
        {
            // Rules
            BindClick(buttonOpenGameRules, OpenGameRules);
            BindClick(buttonCloseGameRules, CloseCurrentPanel);
            BindClick(buttonGreenCloseGameRules, CloseCurrentPanel);
            BindClick(buttonNextRulesPage, NextRulesPage);
            BindClick(buttonPrevRulesPage, PrevRulesPage);

            // System Settings Toggles
            BindClick(buttonOpenSettings, OpenSystemSettings);
            BindClick(buttonCloseSettings, CloseCurrentPanel);
            BindToggle(toggleQuickSpin, "settings_quick_spin", 0, v => OnQuickSpinChanged?.Invoke(v));
            BindToggle(toggleBatterySaver, "settings_battery_saver", 0, v => OnBatterySaverChanged?.Invoke(v));
            BindToggle(toggleAmbientMusic, "settings_ambient_music", 1, v => OnAmbientMusicChanged?.Invoke(v));
            BindToggle(toggleSoundFx, "settings_sound_fx", 1, v => OnSoundFxChanged?.Invoke(v));
            BindToggle(toggleIntroScreen, "settings_intro_screen", 1, v => OnIntroScreenChanged?.Invoke(v));

            // Sound
            BindClick(buttonOpenSound, ToggleSoundSettings);
            if (masterVolumeSlider != null)
            {
                float savedVol = PlayerPrefs.GetFloat("settings_master_volume", 0.5f);
                if (savedVol <= 0.001f) savedVol = 0.5f;
                lastNonZeroVolume = savedVol;
                masterVolumeSlider.value = savedVol;
                SetAudioMute(PlayerPrefs.GetInt("settings_sound_muted", 0) == 1);

                masterVolumeSlider.onValueChanged.AddListener(v =>
                {
                    if (v > 0.001f) lastNonZeroVolume = v;
                    PlayerPrefs.SetFloat("settings_master_volume", v);
                    SetAudioMute(v <= 0.001f);
                });
            }

            BindClick(buttonSoundMute, () =>
            {
                if (masterVolumeSlider == null) return;
                if (isSoundMuted && masterVolumeSlider.value <= 0.001f)
                {
                    masterVolumeSlider.value = lastNonZeroVolume > 0.001f ? lastNonZeroVolume : 0.5f;
                }
                SetAudioMute(!isSoundMuted);
            });

            // Buy Feature Modal
            BindClick(buttonConfirmBuyFeature, () => { CloseCurrentPanel(); OnBuyFeatureConfirmed?.Invoke(); });
            BindClick(buttonCancelBuyFeature, CloseCurrentPanel);

            // Bet Adjustment Modal Steppers
            BindClick(buttonOpenBetAdjustment, OpenBetAdjustmentModal);
            BindClick(buttonCloseBetAdjustment, CloseCurrentPanel);
            BindClick(buttonIncreaseBetLevel, () => StepIndex(ref currentBetLevelIndex, betLevels.Length - 1, 1));
            BindClick(buttonDecreaseBetLevel, () => StepIndex(ref currentBetLevelIndex, betLevels.Length - 1, -1));
            BindClick(buttonIncreaseCoinValue, () => StepIndex(ref currentCoinValueIndex, coinValues.Length - 1, 1));
            BindClick(buttonDecreaseCoinValue, () => StepIndex(ref currentCoinValueIndex, coinValues.Length - 1, -1));
            BindClick(buttonIncreaseTotalBet, () => OnBetIncreaseClicked?.Invoke());
            BindClick(buttonDecreaseTotalBet, () => OnBetDecreaseClicked?.Invoke());
            BindClick(buttonBetMax, () =>
            {
                currentBetLevelIndex = betLevels.Length - 1;
                currentCoinValueIndex = coinValues.Length - 1;
                ApplyBetAdjustment();
                OnBetMaxClicked?.Invoke();
            });

            // Settings Modal Bet Buttons
            BindClick(buttonSystemSettingIncreaseBet, () => OnBetIncreaseClicked?.Invoke());
            BindClick(buttonSystemSettingDecreaseBet, () => OnBetDecreaseClicked?.Invoke());

            // Autoplay Modal
            BindClick(buttonOpenAutoplay, () =>
            {
                if (isAutoplaying) OnAutoplayStopped?.Invoke();
                else OpenAutoplayModal();
            });
            BindClick(buttonCloseAutoplay, CloseCurrentPanel);
            if (autoplaySpinsSlider != null)
            {
                autoplaySpinsSlider.wholeNumbers = true;
                autoplaySpinsSlider.minValue = 0;
                autoplaySpinsSlider.maxValue = autoplaySpinOptions.Length - 1;
                autoplaySpinsSlider.onValueChanged.AddListener(val => UpdateAutoplaySpinsText((int)val));
            }
            BindClick(buttonStartAutoplay, () =>
            {
                CloseCurrentPanel();
                int idx = autoplaySpinsSlider != null ? Mathf.Clamp((int)autoplaySpinsSlider.value, 0, autoplaySpinOptions.Length - 1) : 0;
                OnAutoplayStarted?.Invoke(autoplaySpinOptions[idx]);
            });
            BindToggle(toggleTurboSpin, "settings_turbo_spin", 0);
            BindToggle(toggleQuickSpinAutoplay, "settings_quick_spin", 0, v =>
            {
                if (toggleQuickSpin != null && toggleQuickSpin.isOn != v) toggleQuickSpin.isOn = v;
                OnQuickSpinChanged?.Invoke(v);
            });
            BindToggle(toggleSkipScreens, "settings_skip_screens", 0);

            // MainView Forwarding
            if (mainView != null)
            {
                mainView.OnSpinClicked += () => OnSpinClicked?.Invoke();
                mainView.OnStopClicked += () => OnStopClicked?.Invoke();
                mainView.OnAutoplayStopClicked += () => OnAutoplayStopped?.Invoke();
                mainView.OnSpinStopped += () => OnSpinStopped?.Invoke();
                mainView.OnBetIncreaseClicked += HandleBottomBarBetPlus;
                mainView.OnBetDecreaseClicked += HandleBottomBarBetMinus;
                mainView.OnAnteBetToggled += () => OnAnteBetToggled?.Invoke();
                mainView.OnBuyBonusClicked += () => OnBuyFeatureRequested?.Invoke();
            }

            // Tap anywhere to dismiss modal overlays
            if (winCelebrationPanel != null) BindClick(winCelebrationPanel.GetComponent<Button>(), CloseCurrentPanel);
            if (freeSpinsIntroPanel != null) BindClick(freeSpinsIntroPanel.GetComponent<Button>(), CloseCurrentPanel);
            if (freeSpinsOutroPanel != null) BindClick(freeSpinsOutroPanel.GetComponent<Button>(), CloseCurrentPanel);

            // Home & Fullscreen Controls
            BindClick(homeButton, OnHomeButtonPressed);
            if (fullScreenButton != null && shrinkButton != null)
            {
                BindClick(fullScreenButton, () => SetFullscreen(true));
                BindClick(shrinkButton, () => SetFullscreen(false));
            }
            else if (fullScreenButton != null)
            {
                BindClick(fullScreenButton, () => SetFullscreen(!isExpanded));
            }

            // Ante Bet Slider (0 = OFF, 1 = ON)
            if (anteBetSlider != null)
            {
                anteBetSlider.wholeNumbers = false;
                anteBetSlider.minValue = 0f;
                anteBetSlider.maxValue = 1f;
            }
            BindClick(anteBetButton, () => OnAnteBetToggled?.Invoke());
            UpdateAnteBetSliderVisuals(isAnteBet, false);
            UpdateSettingsBetText(currentBetAmount);
        }

        private void BindClick(Button btn, UnityAction action)
        {
            if (btn != null && action != null) btn.onClick.AddListener(action);
        }

        private void BindToggle(Toggle toggle, string prefKey, int defaultVal, Action<bool> onChanged = null)
        {
            if (toggle == null) return;
            toggle.isOn = PlayerPrefs.GetInt(prefKey, defaultVal) == 1;
            toggle.onValueChanged.AddListener(v =>
            {
                PlayerPrefs.SetInt(prefKey, v ? 1 : 0);
                onChanged?.Invoke(v);
            });
        }

        private void StepIndex(ref int index, int maxIndex, int delta)
        {
            int next = Mathf.Clamp(index + delta, 0, maxIndex);
            if (next != index)
            {
                index = next;
                ApplyBetAdjustment();
            }
        }

        #endregion

        #region Home & Fullscreen Navigation

        private void OnHomeButtonPressed()
        {
            AudioManager.Instance?.PlayButton();
            if (popupManager != null) popupManager.ShowExitGamePopup();
            else Application.Quit();
        }

        private void SetFullscreen(bool expand)
        {
            AudioManager.Instance?.PlayButton();
            isExpanded = expand;
            if (jsFunctCalls != null)
            {
                if (expand) jsFunctCalls.RequestExpandGame();
                else jsFunctCalls.RequestShrinkGame();
            }
            SetExpandShrinkButtons(expand);
        }

        private void SetExpandShrinkButtons(bool expanded)
        {
            if (fullScreenButton != null) fullScreenButton.gameObject.SetActive(!expanded);
            if (shrinkButton != null) shrinkButton.gameObject.SetActive(expanded);
        }

        internal void OnFullscreenChanged(string isFullscreen)
        {
            isExpanded = isFullscreen == "1";
            SetExpandShrinkButtons(isExpanded);
        }

        #endregion

        #region Modal Dialog Management

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape) && currentOpenPanel != null)
            {
                CloseCurrentPanel();
            }
        }

        internal void CloseCurrentPanel()
        {
            if (currentOpenPanel != null) CloseModal(currentOpenPanel);
        }

        private void OpenModal(GameObject panel)
        {
            if (panel == null) return;
            if (currentOpenPanel != null && currentOpenPanel != panel) currentOpenPanel.SetActive(false);
            currentOpenPanel = panel;
            currentOpenPanel.SetActive(true);
        }

        private void CloseModal(GameObject panel)
        {
            if (panel == null) return;
            if (panel == winCelebrationPanel)
            {
                var sunburst = winCelebrationPanel.transform.Find("EffectSunburst");
                if (sunburst != null) sunburst.DOKill();
                AudioManager.Instance?.StopWinTypePopupOpen();
            }
            if (panel == buyFeatureConfirmPanel && mainView != null)
            {
                mainView.SetIdleBottomBarState();
            }
            panel.SetActive(false);
            if (currentOpenPanel == panel) currentOpenPanel = null;
        }

        private void CloseAllModalsImmediate()
        {
            currentOpenPanel = null;
            AudioManager.Instance?.StopWinTypePopupOpen();
            GameObject[] modals = { gameRulesPanel, systemSettingsPanel, soundSettingsPanel, buyFeatureConfirmPanel, betAdjustmentPanel, autoplayPanel, freeSpinsIntroPanel, freeSpinsOutroPanel, winCelebrationPanel };
            for (int i = 0; i < modals.Length; i++)
            {
                if (modals[i] != null) modals[i].SetActive(false);
            }
        }

        #endregion

        #region Game Rules & Paytable

        internal void OpenGameRules()
        {
            currentRulesPageIndex = 0;
            UpdateRulesPageDisplay();
            UpdatePaytableDisplay(currentBetAmount);
            OpenModal(gameRulesPanel);
        }

        internal void UpdateBetLimits(double minBet, double maxBet)
        {
            if (minBetText != null) minBetText.text = $"{minBet:F2}";
            if (maxBetText != null) maxBetText.text = $"{maxBet:F2}";
        }

        internal void UpdatePaytableDisplay(double bet)
        {
            if (bet <= 0) bet = 0.20;
            currentBetAmount = bet;
            if (paytableSymbols == null) return;

            for (int i = 0; i < paytableSymbols.Count; i++)
            {
                var h = paytableSymbols[i];
                if (h == null) continue;
                bool isScatter = h.symbol == SymbolType.Lollipop;

                int t1 = isScatter ? 6 : 12;
                int t2 = isScatter ? 5 : 10;
                int t3 = isScatter ? 4 : 8;

                if (h.textTier1 != null) h.textTier1.text = $"{(isScatter ? "6+" : "12+")}  {bet * GameConfig.GetPayoutMultiplier(h.symbol, t1):F2}";
                if (h.textTier2 != null) h.textTier2.text = $"{(isScatter ? "5" : "10 -11")}  {bet * GameConfig.GetPayoutMultiplier(h.symbol, t2):F2}";
                if (h.textTier3 != null) h.textTier3.text = $"{(isScatter ? "4" : "8-9")}  {bet * GameConfig.GetPayoutMultiplier(h.symbol, t3):F2}";
            }
        }

        internal void NextRulesPage() => ChangeRulesPage(1);
        internal void PrevRulesPage() => ChangeRulesPage(-1);

        private void ChangeRulesPage(int delta)
        {
            int total = (rulesPageObjects != null && rulesPageObjects.Count > 0) ? rulesPageObjects.Count : 7;
            currentRulesPageIndex = (currentRulesPageIndex + delta + total) % total;
            UpdateRulesPageDisplay();
        }

        private void UpdateRulesPageDisplay()
        {
            int total = (rulesPageObjects != null && rulesPageObjects.Count > 0) ? rulesPageObjects.Count : 7;
            if (rulesPageObjects != null)
            {
                for (int i = 0; i < rulesPageObjects.Count; i++)
                {
                    if (rulesPageObjects[i] != null) rulesPageObjects[i].SetActive(i == currentRulesPageIndex);
                }
            }
        }

        #endregion

        #region Sound & Settings Modals

        internal void OpenSystemSettings()
        {
            UpdateSettingsBetText(currentBetAmount);
            OpenModal(systemSettingsPanel);
        }

        internal void ToggleSoundSettings()
        {
            if (soundSettingsPanel != null && soundSettingsPanel.activeSelf) CloseCurrentPanel();
            else OpenSoundSettings();
        }

        internal void OpenSoundSettings()
        {
            if (masterVolumeSlider != null) UpdateSoundUI(masterVolumeSlider.value, isSoundMuted);
            OpenModal(soundSettingsPanel);
        }

        private void SetAudioMute(bool mute)
        {
            isSoundMuted = mute;
            PlayerPrefs.SetInt("settings_sound_muted", mute ? 1 : 0);
            float vol = masterVolumeSlider != null ? masterVolumeSlider.value : 0.5f;
            AudioListener.volume = mute ? 0f : vol;
            AudioManager.Instance?.SetMuteAll(mute);
            if (masterVolumeSlider != null) UpdateSoundUI(vol, mute);
        }

        private void UpdateSoundUI(float volume) => UpdateSoundUI(volume, isSoundMuted);

        private void UpdateSoundUI(float volume, bool isMuted)
        {
            int percentage = Mathf.RoundToInt(volume * 100f);
            if (soundVolumeText != null) soundVolumeText.text = percentage.ToString();

            // Speaker Icon
            if (buttonSoundMute != null)
            {
                var img = buttonSoundMute.GetComponent<Image>();
                if (img != null) img.sprite = isMuted ? speakerMutedSprite : speakerAudibleSprite;
            }

            // Fill Area (Turn Red when muted, Green when unmuted)
            Sprite targetLine = isMuted ? volumeLineRedSprite : volumeLineGreenSprite;
            if (soundFillImage != null)
            {
                if (targetLine != null)
                {
                    soundFillImage.sprite = targetLine;
                    soundFillImage.color = Color.white;
                }
                else
                {
                    soundFillImage.color = isMuted ? new Color(0.95f, 0.15f, 0.15f, 1f) : new Color(0.1f, 0.85f, 0.3f, 1f);
                }
            }

            // Handle Sprites (Turn Red when muted, Green when unmuted)
            Sprite targetHandleNormal = isMuted ? volumeButtonRedNormal : volumeButtonGreenNormal;
            Sprite targetHandlePressed = isMuted ? volumeButtonRedPressed : volumeButtonGreenPressed;

            if (soundHandleImage != null && targetHandleNormal != null)
            {
                soundHandleImage.sprite = targetHandleNormal;
                soundHandleImage.color = Color.white;
            }

            if (masterVolumeSlider != null && targetHandleNormal != null)
            {
                masterVolumeSlider.transition = Selectable.Transition.SpriteSwap;
                var ss = masterVolumeSlider.spriteState;
                ss.highlightedSprite = targetHandleNormal;
                ss.pressedSprite = targetHandlePressed != null ? targetHandlePressed : targetHandleNormal;
                ss.selectedSprite = targetHandleNormal;
                masterVolumeSlider.spriteState = ss;
            }
        }

        #endregion

        #region Buy Feature & Bet Adjustment

        internal void OpenBuyFeatureModal(double betAmount, int freeSpinsCount = 10)
        {
            if (buyFeatureCostText != null) 
                buyFeatureCostText.text = SpriteTextUtility.FormatCurrency(betAmount * 100.0);
            if (buyFeatureSpinsCountText != null) 
                buyFeatureSpinsCountText.text = SpriteTextUtility.FormatCount(freeSpinsCount);
            if (mainView != null)
                mainView.SetBuyFeatureState();
            OpenModal(buyFeatureConfirmPanel);
        }

        internal void OpenBetAdjustmentModal()
        {
            UpdateBetAdjustmentDisplay(currentBetAmount, isAnteBet);
            OpenModal(betAdjustmentPanel);
        }

        private void HandleBottomBarBetPlus()
        {
            if (betAdjustmentPanel != null && !betAdjustmentPanel.activeSelf) OpenBetAdjustmentModal();
            else OnBetIncreaseClicked?.Invoke();
        }

        private void HandleBottomBarBetMinus()
        {
            if (betAdjustmentPanel != null && !betAdjustmentPanel.activeSelf) OpenBetAdjustmentModal();
            else OnBetDecreaseClicked?.Invoke();
        }

        internal void UpdateBetAdjustmentDisplay(double baseBet, bool anteBet)
        {
            isAnteBet = anteBet;
            if (betMultiplierHeadingText != null)
            {
                betMultiplierHeadingText.text = isAnteBet ? "BET MULTIPLIER 25x" : "BET MULTIPLIER 20x";
            }
            SyncIndicesFromBet(baseBet);
            RefreshBetAdjustmentTexts(baseBet);
        }

        private void SyncIndicesFromBet(double baseBet)
        {
            double target = baseBet / 20.0;
            for (int b = 0; b < betLevels.Length; b++)
            {
                for (int c = 0; c < coinValues.Length; c++)
                {
                    if (Math.Abs((betLevels[b] * (double)coinValues[c]) - target) < 0.001)
                    {
                        currentBetLevelIndex = b;
                        currentCoinValueIndex = c;
                        return;
                    }
                }
            }
        }

        private void RefreshBetAdjustmentTexts(double baseBet)
        {
            int betLevel = betLevels[currentBetLevelIndex];
            float coinVal = coinValues[currentCoinValueIndex];
            double effectiveTotalBet = isAnteBet ? baseBet * 1.25 : baseBet;

            if (betCoinsText != null) betCoinsText.text = betLevel.ToString();
            if (coinValueText != null) coinValueText.text = $"{coinVal:F2}";
            if (totalBetAmountText != null) totalBetAmountText.text = $"{effectiveTotalBet:F2}";
            UpdateSettingsBetText(baseBet);
        }

        private void ApplyBetAdjustment()
        {
            int betLevel = betLevels[currentBetLevelIndex];
            float coinVal = coinValues[currentCoinValueIndex];
            double baseBet = Math.Round(betLevel * (double)coinVal * 20.0, 2);
            RefreshBetAdjustmentTexts(baseBet);
            OnBetAmountSelected?.Invoke(baseBet);
        }

        #endregion

        #region Autoplay Modal

        internal void OpenAutoplayModal()
        {
            if (autoplaySpinsSlider != null) UpdateAutoplaySpinsText((int)autoplaySpinsSlider.value);
            OpenModal(autoplayPanel);
        }

        private void UpdateAutoplaySpinsText(int index)
        {
            int idx = Mathf.Clamp(index, 0, autoplaySpinOptions.Length - 1);
            int count = autoplaySpinOptions[idx];
            if (autoplaySpinsCountText != null) autoplaySpinsCountText.text = $"START AUTOPLAY ({count})"; //count.ToString();
            if(autoplaySpinsCountSliderText != null) autoplaySpinsCountSliderText.text = count.ToString();


           /* if (buttonStartAutoplay != null)
            {
                var txt = buttonStartAutoplay.GetComponentInChildren<UnityEngine.UI.Text>();
                if (txt != null) txt.text = $"START AUTOPLAY ({count})";
                else
                {
                    var tmp = buttonStartAutoplay.GetComponentInChildren<TMP_Text>();
                    if (tmp != null) tmp.text = $"START AUTOPLAY ({count})";
                }
            }*///000
        }

        internal void SetAutoplayState(bool isAutoplay, int remainingSpins = 0)
        {
            this.isAutoplaying = isAutoplay;
            /*if (autoplayRemainingText != null)
            {
                autoplayRemainingText.text = isAutoplay ? $"STOP ({remainingSpins})" : "AUTOPLAY";
                autoplayRemainingText.gameObject.SetActive(true);
            }*/ //000
            if (buttonOpenAutoplay != null)
            {
                var img = buttonOpenAutoplay.GetComponent<Image>();
                if (img != null) img.color = isAutoplay ? new Color(0.85f, 0.22f, 0.22f, 0.95f) : Color.white;
            }
            if (mainView != null) mainView.SetAutoplayState(isAutoplay, remainingSpins);
        }

        #endregion

        #region Feature Overlays & Win Celebration

        internal void ShowFreeSpinsIntro(int spinsCount)
        {
            if (freeSpinsIntroCountText != null)
            {
                freeSpinsIntroCountText.text = SpriteTextUtility.FormatCount(spinsCount);
            }
            OpenModal(freeSpinsIntroPanel);
            StartCoroutine(AutoDismissModal(freeSpinsIntroPanel, 3.0f));
        }

        internal void ShowFreeSpinsOutro(double totalWin)
        {
            if (freeSpinsOutroTweener != null && freeSpinsOutroTweener.IsActive()) freeSpinsOutroTweener.Kill();
            if (freeSpinsOutroWinText != null)
            {
                freeSpinsOutroWinText.text = SpriteTextUtility.FormatCurrency(0, includeDollar: false);
                float startWin = 0;
                freeSpinsOutroTweener = DOTween.To(() => startWin, x =>
                {
                    startWin = x;
                    if (freeSpinsOutroWinText != null)
                    {
                        freeSpinsOutroWinText.text = SpriteTextUtility.FormatCurrency(x, includeDollar: false);
                    }
                }, (float)totalWin, 1.2f).SetEase(Ease.OutCubic).OnComplete(() =>
                {
                    if (freeSpinsOutroWinText != null)
                    {
                        freeSpinsOutroWinText.text = SpriteTextUtility.FormatCurrency(totalWin, includeDollar: false);
                    }
                });
            }
            OpenModal(freeSpinsOutroPanel);
            StartCoroutine(AutoDismissModal(freeSpinsOutroPanel, 3.5f));
        }

        internal void ShowWinCelebration(string title, double winAmount, float displayDuration = 3.0f)
        {
            if (winCelebrationPanel != null)
            {
                OpenModal(winCelebrationPanel);
                var sunburst = winCelebrationPanel.transform.Find("EffectSunburst");
                if (sunburst != null)
                {
                    sunburst.DOKill();
                    sunburst.rotation = Quaternion.identity;
                    sunburst.DORotate(new Vector3(0, 0, -360), 8f, RotateMode.FastBeyond360).SetEase(Ease.Linear).SetLoops(-1);
                }
            }
            AudioManager.Instance?.PlayWinTypePopupOpen();
            if (winCelebrationTweener != null && winCelebrationTweener.IsActive()) winCelebrationTweener.Kill();

            float startWin = 0;
            if (winCelebrationAmountText != null)
                winCelebrationAmountText.text = SpriteTextUtility.FormatCurrency(0);

            winCelebrationTweener = DOTween.To(() => startWin, x =>
            {
                startWin = x;
                if (winCelebrationAmountText != null)
                    winCelebrationAmountText.text = SpriteTextUtility.FormatCurrency(x);
            }, (float)winAmount, 1.2f).SetEase(Ease.OutCubic).OnComplete(() =>
            {
                if (winCelebrationAmountText != null)
                    winCelebrationAmountText.text = SpriteTextUtility.FormatCurrency(winAmount);
            });

            StartCoroutine(AutoDismissModal(winCelebrationPanel, displayDuration));
        }

        private IEnumerator AutoDismissModal(GameObject modal, float duration)
        {
            yield return new WaitForSeconds(duration);
            if (modal != null && modal.activeSelf) CloseModal(modal);
        }

        #endregion

        #region HUD & Status Updates

        internal void UpdateHUD(double balance, double bet, double win, bool isAnteBet, bool isSpinning)
        {
            currentBetAmount = bet;
            if (buyFeatureButtonCostText != null) 
                buyFeatureButtonCostText.text = SpriteTextUtility.FormatCurrency(bet * 100.0);

            UpdateAnteBetSliderVisuals(isAnteBet);
            UpdateSettingsBetText(currentBetAmount);

            if (mainView != null)
            {
                mainView.SetBalance(balance);
                mainView.SetBet(bet, isAnteBet);
                mainView.SetWin(win, false);
                mainView.SetSpinButtonInteractable(!isSpinning);
            }
        }

        private void UpdateAnteBetSliderVisuals(bool active, bool animate = true)
        {
            this.isAnteBet = active;
            float targetValue = active ? 1f : 0f;
            if (anteBetSlider != null)
            {
                anteBetSlider.DOKill();
                if (animate && Mathf.Abs(anteBetSlider.value - targetValue) > 0.01f)
                {
                    anteBetSlider.DOValue(targetValue, 0.2f).SetEase(Ease.OutQuad);
                }
                else
                {
                    anteBetSlider.SetValueWithoutNotify(targetValue);
                }
            }
            if (anteBetHandleImage != null)
            {
                anteBetHandleImage.sprite = active && anteBetOnSprite != null ? anteBetOnSprite : anteBetOffSprite;
            }
        }

        private void UpdateSettingsBetText(double bet)
        {
            if (settingsBetAmountText != null)
            {
                double effectiveBet = isAnteBet ? bet * 1.25 : bet;
                settingsBetAmountText.text = (effectiveBet % 1 == 0) 
                    ? effectiveBet.ToString("0") 
                    : effectiveBet.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        internal void SetStatusMessage(string message)
        {
            if (mainView != null) mainView.SetStatusMessage(message);
        }

        internal void SetStoppingState()
        {
            if (mainView != null) mainView.SetStoppingButtonsState();
        }

        //** ----------------------------------------------------
        //** Bottom Banner On/Off & Cross-Fade Cycle
        #endregion

        private void OnDisable()
        {
            StopAllCoroutines();
            if (winCelebrationTweener != null && winCelebrationTweener.IsActive()) winCelebrationTweener.Kill();
            if (freeSpinsOutroTweener != null && freeSpinsOutroTweener.IsActive()) freeSpinsOutroTweener.Kill();
            if (anteBetSlider != null) anteBetSlider.DOKill();
            if (winCelebrationPanel != null)
            {
                var sunburst = winCelebrationPanel.transform.Find("EffectSunburst");
                if (sunburst != null) sunburst.DOKill();
            }
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
            if (winCelebrationTweener != null && winCelebrationTweener.IsActive()) winCelebrationTweener.Kill();
            if (freeSpinsOutroTweener != null && freeSpinsOutroTweener.IsActive()) freeSpinsOutroTweener.Kill();
            if (anteBetSlider != null) anteBetSlider.DOKill();
            if (winCelebrationPanel != null)
            {
                var sunburst = winCelebrationPanel.transform.Find("EffectSunburst");
                if (sunburst != null) sunburst.DOKill();
            }
        }
    }
}
