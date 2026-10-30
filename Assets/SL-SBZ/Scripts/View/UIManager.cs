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
        internal event Action OnBetIncreaseClicked;
        internal event Action OnBetDecreaseClicked;
        internal event Action OnBetMaxClicked;
        internal event Action<double> OnBetAmountSelected;
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

        [Header("1. Game Rules Modal")]
        [SerializeField] private GameObject gameRulesPanel;
        [SerializeField] private Button buttonOpenGameRules;
        [SerializeField] private Button buttonCloseGameRules;
        [SerializeField] private Button buttonNextRulesPage;
        [SerializeField] private Button buttonPrevRulesPage;
        [SerializeField] private TMP_Text rulesPageIndicatorText;
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

        [Header("3. Sound Settings Modal")]
        [SerializeField] private GameObject soundSettingsPanel;
        [SerializeField] private Button buttonOpenSound;
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Button buttonSoundMute;
        [SerializeField] private Image soundFillImage;
        [SerializeField] private TMP_Text soundVolumeText;
        [SerializeField] private Sprite speakerAudibleSprite;
        [SerializeField] private Sprite speakerMutedSprite;

        [Header("4. Buy Feature Modal")]
        [SerializeField] private GameObject buyFeatureConfirmPanel;
        [SerializeField] private Button buttonConfirmBuyFeature;
        [SerializeField] private Button buttonCancelBuyFeature;
        [SerializeField] private TMP_Text buyFeatureCostText;
        [SerializeField] private TMP_Text buyFeatureButtonCostText;

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

        [Header("System Settings Bet Controls")]
        [SerializeField] private Button buttonSystemSettingIncreaseBet;
        [SerializeField] private Button buttonSystemSettingDecreaseBet;
        [SerializeField] private TMP_Text settingsBetAmountText;

        [Header("Autoplay Panel")]
        [SerializeField] private GameObject autoplayPanel;
        [SerializeField] private Button buttonOpenAutoplay;
        [SerializeField] private Button buttonCloseAutoplay;
        [SerializeField] private Button buttonStartAutoplay;
        [SerializeField] private TMP_Text autoplayRemainingText;
        [SerializeField] private Slider autoplaySpinsSlider;
        [SerializeField] private TMP_Text autoplaySpinsCountText;
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
        [SerializeField] private TMP_Text winCelebrationTitleText;
        [SerializeField] private TMP_Text winCelebrationAmountText;
        [SerializeField] private ParticleSystem winCelebrationParticles;

        private readonly int[] betLevels = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        private readonly float[] coinValues = new float[]
        {
            0.01f, 0.02f, 0.03f, 0.04f, 0.05f, 0.06f, 0.07f, 0.08f, 0.09f, 0.10f,
            0.12f, 0.15f, 0.20f, 0.25f, 0.50f, 0.75f, 1.00f, 1.25f, 1.50f
        };
        private readonly int[] autoplaySpinOptions = new int[] { 10, 20, 30, 50, 70, 100 };

        private int currentBetLevelIndex = 0;
        private int currentCoinValueIndex = 9;
        private bool isAnteBet = false;
        private bool isAutoplaying = false;
        private int currentRulesPageIndex = 0;
        private GameObject currentOpenPanel;
        private Tweener winCelebrationTweener;
        private double currentBetAmount = 2.0;
        private float lastNonZeroVolume = 0.5f;

        #region Initialization & Button Bindings

        private void Awake()
        {
            RegisterButtonCallbacks();
            CloseAllModalsImmediate();
        }

        private void RegisterButtonCallbacks()
        {
            // Game Rules navigation
            BindClick(buttonOpenGameRules, OpenGameRules);
            BindClick(buttonCloseGameRules, CloseCurrentPanel);
            BindClick(buttonNextRulesPage, NextRulesPage);
            BindClick(buttonPrevRulesPage, PrevRulesPage);

            // System Settings
            BindClick(buttonOpenSettings, OpenSystemSettings);
            BindClick(buttonCloseSettings, CloseCurrentPanel);
            BindToggle(toggleQuickSpin, "settings_quick_spin", 0, v => OnQuickSpinChanged?.Invoke(v));
            BindToggle(toggleBatterySaver, "settings_battery_saver", 0, v => OnBatterySaverChanged?.Invoke(v));
            BindToggle(toggleAmbientMusic, "settings_ambient_music", 1, v => OnAmbientMusicChanged?.Invoke(v));
            BindToggle(toggleSoundFx, "settings_sound_fx", 1, v => OnSoundFxChanged?.Invoke(v));
            BindToggle(toggleIntroScreen, "settings_intro_screen", 1, v => OnIntroScreenChanged?.Invoke(v));

            // Sound Settings
            BindClick(buttonOpenSound, ToggleSoundSettings);
            if (masterVolumeSlider != null)
            {
                float savedVol = PlayerPrefs.GetFloat("settings_master_volume", 0.5f);
                masterVolumeSlider.value = savedVol;
                AudioListener.volume = savedVol;
                UpdateSoundUI(savedVol);
                masterVolumeSlider.onValueChanged.AddListener(v =>
                {
                    AudioListener.volume = v;
                    PlayerPrefs.SetFloat("settings_master_volume", v);
                    if (v > 0) lastNonZeroVolume = v;
                    UpdateSoundUI(v);
                });
            }
            BindClick(buttonSoundMute, () =>
            {
                if (masterVolumeSlider == null) return;
                if (masterVolumeSlider.value > 0)
                {
                    lastNonZeroVolume = masterVolumeSlider.value;
                    masterVolumeSlider.value = 0f;
                }
                else
                {
                    masterVolumeSlider.value = lastNonZeroVolume > 0 ? lastNonZeroVolume : 0.5f;
                }
            });

            // Buy Feature Modal
            BindClick(buttonConfirmBuyFeature, () => { CloseCurrentPanel(); OnBuyFeatureConfirmed?.Invoke(); });
            BindClick(buttonCancelBuyFeature, CloseCurrentPanel);

            // Bet Adjustment Modal & Steppers
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

            // Settings Bet Controls
            BindClick(buttonSystemSettingIncreaseBet, () => OnBetIncreaseClicked?.Invoke());
            BindClick(buttonSystemSettingDecreaseBet, () => OnBetDecreaseClicked?.Invoke());

            // Autoplay Modal
            BindClick(buttonOpenAutoplay, () =>
            {
                if (isAutoplaying) OnAutoplayStopped?.Invoke();
                else OpenAutoplayModal();
            });
            BindClick(buttonCloseAutoplay, CloseCurrentPanel);
            if (autoplaySpinsSlider != null) autoplaySpinsSlider.onValueChanged.AddListener(val => UpdateAutoplaySpinsText((int)val));
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

            // MainView Events
            if (mainView != null)
            {
                mainView.OnSpinClicked += () => OnSpinClicked?.Invoke();
                mainView.OnBetIncreaseClicked += HandleBottomBarBetPlus;
                mainView.OnBetDecreaseClicked += HandleBottomBarBetMinus;
                mainView.OnAnteBetToggled += () => OnAnteBetToggled?.Invoke();
                mainView.OnBuyBonusClicked += () => OnBuyFeatureRequested?.Invoke();
            }

            // Tap anywhere to dismiss overlays
            if (winCelebrationPanel != null) BindClick(winCelebrationPanel.GetComponent<Button>(), CloseCurrentPanel);
            if (freeSpinsIntroPanel != null) BindClick(freeSpinsIntroPanel.GetComponent<Button>(), CloseCurrentPanel);
            if (freeSpinsOutroPanel != null) BindClick(freeSpinsOutroPanel.GetComponent<Button>(), CloseCurrentPanel);
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

        #region Modal Management

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape) && currentOpenPanel != null)
            {
                CloseCurrentPanel();
            }
        }

        internal void CloseCurrentPanel()
        {
            if (currentOpenPanel != null)
            {
                CloseModal(currentOpenPanel);
            }
        }

        private void OpenModal(GameObject panel)
        {
            if (panel == null) return;
            if (currentOpenPanel != null && currentOpenPanel != panel)
            {
                currentOpenPanel.SetActive(false);
            }
            currentOpenPanel = panel;
            currentOpenPanel.SetActive(true);
        }

        private void CloseModal(GameObject panel)
        {
            if (panel != null)
            {
                if (panel == winCelebrationPanel)
                {
                    var sunburst = winCelebrationPanel.transform.Find("EffectSunburst");
                    if (sunburst != null) sunburst.DOKill();
                }
                panel.SetActive(false);
            }
            if (currentOpenPanel == panel) currentOpenPanel = null;
        }

        private void CloseAllModalsImmediate()
        {
            currentOpenPanel = null;
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
            UpdateBetLimits(0.20, 300.00);
            OpenModal(gameRulesPanel);
        }

        internal void UpdateBetLimits(double minBet, double maxBet)
        {
            if (minBetText != null) minBetText.text = string.Format(": ${0:F2}", minBet);
            if (maxBetText != null) maxBetText.text = string.Format(" ${0:F2}", maxBet);
        }

        private void InitializePaytableHolders()
        {
            if (paytableSymbols != null && paytableSymbols.Count >= 10) return;
            if (gameRulesPanel == null) return;

            var page1 = gameRulesPanel.transform.Find("page  (1)");
            if (page1 == null) return;

            var tp1 = page1.Find("Textplace 1");
            var tp2 = page1.Find("Textplace 1 (1)");
            var tp3 = page1.Find("Textplace 1 (2)");
            if (tp1 == null || tp2 == null || tp3 == null) return;

            if (paytableSymbols == null) paytableSymbols = new List<PaytableSymbolEntry>();
            paytableSymbols.Clear();

            // 4 Candies
            AddHolder(tp1.GetChild(0), SymbolType.RedHeart);
            AddHolder(tp1.GetChild(1), SymbolType.PurpleSquare);
            AddHolder(tp1.GetChild(2), SymbolType.GreenPentagon);
            AddHolder(tp1.GetChild(3), SymbolType.BlueOval);

            // 5 Fruits
            AddHolder(tp2.GetChild(0), SymbolType.Apple);
            AddHolder(tp2.GetChild(1), SymbolType.Plum);
            AddHolder(tp2.GetChild(2), SymbolType.Watermelon);
            AddHolder(tp2.GetChild(3), SymbolType.Grapes);
            AddHolder(tp2.GetChild(4), SymbolType.Banana);

            // Scatter
            AddHolder(tp3.GetChild(0), SymbolType.Lollipop);
        }

        private void AddHolder(Transform parent, SymbolType symbol)
        {
            if (parent == null) return;
            var t1 = parent.Find("Text  (1)")?.GetComponent<TMP_Text>();
            var t2 = parent.Find("Text  (2)")?.GetComponent<TMP_Text>();
            var t3 = parent.Find("Text  (3)")?.GetComponent<TMP_Text>();

            paytableSymbols.Add(new PaytableSymbolEntry
            {
                symbol = symbol,
                textTier1 = t1,
                textTier2 = t2,
                textTier3 = t3
            });
        }

        internal void UpdatePaytableDisplay(double bet)
        {
            if (bet <= 0) bet = 0.20;
            currentBetAmount = bet;
            InitializePaytableHolders();

            for (int i = 0; i < paytableSymbols.Count; i++)
            {
                var h = paytableSymbols[i];
                if (h.symbol == SymbolType.Lollipop)
                {
                    double s1 = bet * GameConfig.GetPayoutMultiplier(h.symbol, 6);
                    double s2 = bet * GameConfig.GetPayoutMultiplier(h.symbol, 5);
                    double s3 = bet * GameConfig.GetPayoutMultiplier(h.symbol, 4);

                    if (h.textTier1 != null) h.textTier1.text = string.Format("6+  ${0:F2}", s1);
                    if (h.textTier2 != null) h.textTier2.text = string.Format("5  ${0:F2}", s2);
                    if (h.textTier3 != null) h.textTier3.text = string.Format("4  ${0:F2}", s3);
                }
                else
                {
                    double m1 = bet * GameConfig.GetPayoutMultiplier(h.symbol, 12);
                    double m2 = bet * GameConfig.GetPayoutMultiplier(h.symbol, 10);
                    double m3 = bet * GameConfig.GetPayoutMultiplier(h.symbol, 8);

                    if (h.textTier1 != null) h.textTier1.text = string.Format("12+  ${0:F2}", m1);
                    if (h.textTier2 != null) h.textTier2.text = string.Format("10 -11  ${0:F2}", m2);
                    if (h.textTier3 != null) h.textTier3.text = string.Format("8-9  ${0:F2}", m3);
                }
            }
        }

        internal void NextRulesPage()
        {
            int total = GetTotalRulesPages();
            if (total <= 0) return;
            currentRulesPageIndex = (currentRulesPageIndex + 1) % total;
            UpdateRulesPageDisplay();
        }

        internal void PrevRulesPage()
        {
            int total = GetTotalRulesPages();
            if (total <= 0) return;
            currentRulesPageIndex = (currentRulesPageIndex - 1 + total) % total;
            UpdateRulesPageDisplay();
        }

        private int GetTotalRulesPages()
        {
            return (rulesPageObjects != null && rulesPageObjects.Count > 0) ? rulesPageObjects.Count : 7;
        }

        private void UpdateRulesPageDisplay()
        {
            int total = GetTotalRulesPages();
            if (rulesPageObjects != null)
            {
                for (int i = 0; i < rulesPageObjects.Count; i++)
                {
                    if (rulesPageObjects[i] != null) rulesPageObjects[i].SetActive(i == currentRulesPageIndex);
                }
            }
            if (rulesPageIndicatorText != null) rulesPageIndicatorText.text = $"{currentRulesPageIndex + 1} / {total}";
        }

        #endregion

        #region Sound & Settings Modals

        internal void OpenSystemSettings() => OpenModal(systemSettingsPanel);

        internal void ToggleSoundSettings()
        {
            if (soundSettingsPanel != null && soundSettingsPanel.activeSelf) CloseCurrentPanel();
            else OpenSoundSettings();
        }

        internal void OpenSoundSettings()
        {
            if (masterVolumeSlider != null) UpdateSoundUI(masterVolumeSlider.value);
            OpenModal(soundSettingsPanel);
        }

        private void UpdateSoundUI(float volume)
        {
            int percentage = Mathf.RoundToInt(volume * 100f);
            if (soundVolumeText != null) soundVolumeText.text = percentage.ToString();

            bool isMuted = percentage <= 0;
            if (soundFillImage != null)
            {
                soundFillImage.color = isMuted ? new Color(0.95f, 0.15f, 0.15f, 1f) : new Color(0.1f, 0.85f, 0.3f, 1f);
            }
            if (buttonSoundMute != null)
            {
                var img = buttonSoundMute.GetComponent<Image>();
                if (img != null)
                {
                    if (isMuted && speakerMutedSprite != null) img.sprite = speakerMutedSprite;
                    else if (!isMuted && speakerAudibleSprite != null) img.sprite = speakerAudibleSprite;
                }
            }
        }

        #endregion

        #region Buy Feature & Bet Adjustment

        internal void OpenBuyFeatureModal(double betAmount)
        {
            if (buyFeatureCostText != null) buyFeatureCostText.text = $"${(betAmount * 100.0):F2}";
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
            double targetProduct = baseBet / 20.0;
            double bestDiff = double.MaxValue;
            int bestBetIdx = currentBetLevelIndex;
            int bestCoinIdx = currentCoinValueIndex;

            for (int b = 0; b < betLevels.Length; b++)
            {
                for (int c = 0; c < coinValues.Length; c++)
                {
                    double diff = Math.Abs((betLevels[b] * (double)coinValues[c]) - targetProduct);
                    if (diff < bestDiff)
                    {
                        bestDiff = diff;
                        bestBetIdx = b;
                        bestCoinIdx = c;
                        if (diff < 0.0001) break;
                    }
                }
                if (bestDiff < 0.0001) break;
            }
            currentBetLevelIndex = bestBetIdx;
            currentCoinValueIndex = bestCoinIdx;
        }

        private void RefreshBetAdjustmentTexts(double baseBet)
        {
            int betLevel = betLevels[currentBetLevelIndex];
            float coinVal = coinValues[currentCoinValueIndex];
            double effectiveTotalBet = isAnteBet ? baseBet * 1.25 : baseBet;

            if (betCoinsText != null) betCoinsText.text = betLevel.ToString();
            if (coinValueText != null) coinValueText.text = string.Format("${0:F2}", coinVal);
            if (totalBetAmountText != null) totalBetAmountText.text = string.Format("${0:F2}", effectiveTotalBet);
            if (settingsBetAmountText != null) settingsBetAmountText.text = string.Format("${0:F2}", effectiveTotalBet);
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
            if (autoplaySpinsCountText != null) autoplaySpinsCountText.text = autoplaySpinOptions[idx].ToString();
        }

        internal void SetAutoplayState(bool isAutoplay, int remainingSpins = 0)
        {
            this.isAutoplaying = isAutoplay;
            if (autoplayRemainingText != null)
            {
                autoplayRemainingText.text = isAutoplay ? $"STOP ({remainingSpins})" : "AUTOPLAY";
                autoplayRemainingText.gameObject.SetActive(true);
            }
            if (buttonOpenAutoplay != null)
            {
                var img = buttonOpenAutoplay.GetComponent<Image>();
                if (img != null)
                {
                    img.color = isAutoplay ? new Color(0.85f, 0.22f, 0.22f, 0.95f) : Color.white;
                }
            }
            if (mainView != null)
            {
                mainView.SetAutoplayState(isAutoplay, remainingSpins);
            }
        }

        #endregion

        #region Feature Overlays & Win Celebration

        internal void ShowFreeSpinsIntro(int spinsCount)
        {
            if (freeSpinsIntroCountText != null) freeSpinsIntroCountText.text = $"{spinsCount}";
            OpenModal(freeSpinsIntroPanel);
            StartCoroutine(AutoDismissModal(freeSpinsIntroPanel, 3.0f));
        }

        internal void ShowFreeSpinsOutro(double totalWin)
        {
            if (freeSpinsOutroWinText != null) freeSpinsOutroWinText.text = $"${totalWin:F2}";
            OpenModal(freeSpinsOutroPanel);
            StartCoroutine(AutoDismissModal(freeSpinsOutroPanel, 3.5f));
        }

        internal void ShowWinCelebration(string title, double winAmount, float displayDuration = 3.0f)
        {
            if (winCelebrationTitleText != null) winCelebrationTitleText.text = title;
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
            if (winCelebrationParticles != null) winCelebrationParticles.Play();
            if (winCelebrationTweener != null && winCelebrationTweener.IsActive()) winCelebrationTweener.Kill();

            float startWin = 0;
            winCelebrationTweener = DOTween.To(() => startWin, x =>
            {
                startWin = x;
                if (winCelebrationAmountText != null) winCelebrationAmountText.text = $"${x:F2}";
            }, (float)winAmount, 1.2f).SetEase(Ease.OutCubic);

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
            UpdatePaytableDisplay(bet);
            UpdateBetAdjustmentDisplay(bet, isAnteBet);
            if (buyFeatureButtonCostText != null) buyFeatureButtonCostText.text = $"${(bet * 100.0):F2}";

            if (mainView != null)
            {
                mainView.SetBalance(balance);
                mainView.SetBet(bet, isAnteBet);
                mainView.SetWin(win, false);
                mainView.SetSpinButtonInteractable(!isSpinning);
            }
        }

        internal void SetStatusMessage(string message)
        {
            if (mainView != null) mainView.SetStatusMessage(message);
        }

        #endregion

        private void OnDestroy()
        {
            if (winCelebrationTweener != null && winCelebrationTweener.IsActive()) winCelebrationTweener.Kill();
        }
    }
}
