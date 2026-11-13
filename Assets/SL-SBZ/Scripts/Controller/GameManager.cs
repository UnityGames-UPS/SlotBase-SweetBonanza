using System.Collections;
using UnityEngine;
using DG.Tweening;
using SweetBonanza.Model;
using SweetBonanza.View;
using SweetBonanza.Service;

namespace SweetBonanza.Controller
{
    internal class GameManager : MonoBehaviour
    {
        [Header("Views Bindings")]
        [SerializeField] private MainView mainView;
        [SerializeField] private UIManager uiManager;
        [SerializeField] private GridView gridView;

        [Header("Service Binding")]
        [SerializeField] private BackendService backendService;

        [Header("Editor / Demo Defaults (replaced by init data)")]
        [SerializeField] private double currentBalance = 11645.27;
        [SerializeField] private float currentBetAmount = 2.0f;
        [SerializeField] private int currentBetIndex = 9;
        [SerializeField] private float anteBetMultiplier = 1.25f;
        [SerializeField] private float buyFeatureCostMultiplier = 100f;
        [SerializeField] private float maxWinMultiplier = 21100f;
        [SerializeField] private float[] availableBets = new float[]
        {
            0.2f, 0.4f, 0.6f, 0.8f, 1.0f, 1.2f, 1.4f, 1.6f, 1.8f, 2.0f,
            2.4f, 3.0f, 4.0f, 5.0f, 10.0f, 15.0f, 20.0f, 25.0f, 50.0f,
            75.0f, 100.0f, 125.0f, 150.0f, 200.0f, 250.0f, 300.0f
        };

        [Header("Debug")]
        [SerializeField] private bool enableSpinDebugLogs = true;

        private static readonly WaitForSeconds Wait04 = new WaitForSeconds(0.4f);
        private static readonly WaitForSeconds Wait05 = new WaitForSeconds(0.5f);
        private static readonly WaitForSeconds Wait06 = new WaitForSeconds(0.6f);
        private static readonly WaitForSeconds Wait10 = new WaitForSeconds(1.0f);
        private static readonly WaitForSeconds Wait20 = new WaitForSeconds(2.0f);

        private readonly GameSession session = new GameSession();

        #region Startup

        private void Start()
        {
            DOTween.SetTweensCapacity(500, 125);
            SeedSessionFromDefaults();
            InitializeServices();
            HookViewEvents();
            ApplyInitialSettings();
            gridView.InitializeInitialGrid();
            UpdateHUD(false);
            PushBetConfigToUI();
            session.State = GameState.Idle;
        }

        private void SeedSessionFromDefaults()
        {
            session.Balance = currentBalance;
            session.BetAmount = currentBetAmount;
            session.BetIndex = currentBetIndex;
            session.AvailableBets = availableBets;
            session.AnteBetMultiplier = anteBetMultiplier;
            session.BuyFeatureCostMultiplier = buyFeatureCostMultiplier;
            session.MaxWinMultiplier = maxWinMultiplier;
            uiManager.SetFeatureMultipliers(session.AnteBetMultiplier, session.BuyFeatureCostMultiplier);
        }

        private void ApplyInitialSettings()
        {
            bool quickSpin = PlayerPrefs.GetInt("settings_quick_spin", 0) == 1;
            gridView.SetQuickSpinMode(quickSpin);

            bool batterySaver = PlayerPrefs.GetInt("settings_battery_saver", 0) == 1;
            ApplyFrameRateSettings(batterySaver);

            bool music = PlayerPrefs.GetInt("settings_ambient_music", 1) == 1;
            AudioManager.Instance?.SetMusicEnabled(music);

            bool sfx = PlayerPrefs.GetInt("settings_sound_fx", 1) == 1;
            AudioManager.Instance?.SetSfxEnabled(sfx);
        }

        private void ApplyFrameRateSettings(bool batterySaver)
        {
            QualitySettings.vSyncCount = 0;
#if UNITY_WEBGL && !UNITY_EDITOR
            Application.targetFrameRate = batterySaver ? 30 : -1;
#else
            Application.targetFrameRate = batterySaver ? 30 : 60;
#endif
        }

        private void InitializeServices()
        {
            if (backendService is LocalDemoService demo)
            {
                demo.SetInitialBalance(session.Balance);
            }

            backendService.OnInitDataReceived += HandleInitDataReceived;
            backendService.OnRoundResultReceived += HandleRoundResultReceived;
            backendService.OnErrorOccurred += HandleErrorOccurred;

            if (SocketIOManager.Instance != null)
            {
                SocketIOManager.Instance.OnBalanceSyncReceived += HandleBalanceSync;
            }

            backendService.Initialize();
        }

        private void HookViewEvents()
        {
            uiManager.OnSpinClicked += HandleSpinClicked;
            uiManager.OnStopClicked += HandleStopClicked;
            uiManager.OnBetIncreaseClicked += HandleBetIncreaseClicked;
            uiManager.OnBetDecreaseClicked += HandleBetDecreaseClicked;
            uiManager.OnBetMaxClicked += HandleBetMaxClicked;
            uiManager.OnBetAmountSelected += HandleBetAmountSelected;
            uiManager.OnAnteBetToggled += HandleAnteBetToggled;
            uiManager.OnBuyFeatureRequested += HandleBuyBonusRequested;
            uiManager.OnBuyFeatureConfirmed += HandleBuyBonusConfirmed;
            uiManager.OnAutoplayStarted += HandleAutoplayStarted;
            uiManager.OnAutoplayStopped += HandleAutoplayStopped;
            uiManager.OnQuickSpinChanged += HandleQuickSpinChanged;
            uiManager.OnBatterySaverChanged += HandleBatterySaverChanged;
            uiManager.OnAmbientMusicChanged += HandleAmbientMusicChanged;
            uiManager.OnSoundFxChanged += HandleSoundFxChanged;
            uiManager.OnIntroScreenChanged += HandleIntroScreenChanged;

            gridView.OnScatterAnticipationTriggered += HandleScatterAnticipation;
        }

        private void OnDestroy()
        {
            if (SocketIOManager.Instance != null)
            {
                SocketIOManager.Instance.OnBalanceSyncReceived -= HandleBalanceSync;
            }

            backendService.OnInitDataReceived -= HandleInitDataReceived;
            backendService.OnRoundResultReceived -= HandleRoundResultReceived;
            backendService.OnErrorOccurred -= HandleErrorOccurred;

            uiManager.OnSpinClicked -= HandleSpinClicked;
            uiManager.OnStopClicked -= HandleStopClicked;
            uiManager.OnBetIncreaseClicked -= HandleBetIncreaseClicked;
            uiManager.OnBetDecreaseClicked -= HandleBetDecreaseClicked;
            uiManager.OnBetMaxClicked -= HandleBetMaxClicked;
            uiManager.OnBetAmountSelected -= HandleBetAmountSelected;
            uiManager.OnAnteBetToggled -= HandleAnteBetToggled;
            uiManager.OnBuyFeatureRequested -= HandleBuyBonusRequested;
            uiManager.OnBuyFeatureConfirmed -= HandleBuyBonusConfirmed;
            uiManager.OnAutoplayStarted -= HandleAutoplayStarted;
            uiManager.OnAutoplayStopped -= HandleAutoplayStopped;
            uiManager.OnQuickSpinChanged -= HandleQuickSpinChanged;
            uiManager.OnBatterySaverChanged -= HandleBatterySaverChanged;
            uiManager.OnAmbientMusicChanged -= HandleAmbientMusicChanged;
            uiManager.OnSoundFxChanged -= HandleSoundFxChanged;
            uiManager.OnIntroScreenChanged -= HandleIntroScreenChanged;

            gridView.OnScatterAnticipationTriggered -= HandleScatterAnticipation;
        }

        #endregion

        #region HUD

        private void UpdateHUD(bool isSpinning)
        {
            uiManager.UpdateHUD(session.Balance, session.BetAmount, session.IsAnteBetActive, isSpinning);
        }

        private void PushBetConfigToUI()
        {
            uiManager.SetFeatureMultipliers(session.AnteBetMultiplier, session.BuyFeatureCostMultiplier);
            if (session.HasBets)
            {
                uiManager.UpdateBetLimits(session.MinBet, session.MaxBet);
            }
        }

        private void SetStatusMessage(string message)
        {
            uiManager.SetStatusMessage(message);
        }

        #endregion

        #region Backend Events

        private void HandleInitDataReceived(InitPayload payload)
        {
            session.Balance = payload.Balance;
            if (payload.AvailableBets != null && payload.AvailableBets.Length > 0)
            {
                session.AvailableBets = payload.AvailableBets;
            }
            session.SelectBetIndex(payload.DefaultBetIndex);
            session.AnteBetMultiplier = payload.AnteBetMultiplier;
            session.BuyFeatureCostMultiplier = payload.BuyFeatureCostMultiplier;
            session.MaxWinMultiplier = payload.MaxWinMultiplier;

            PushBetConfigToUI();
            UpdateHUD(false);
        }

        private void HandleRoundResultReceived(RoundResult result)
        {
            if (result.PlayerBalance > 0)
            {
                session.Balance = result.PlayerBalance;
            }
            else
            {
                session.Balance += result.FinalWinAmount;
            }

            if (enableSpinDebugLogs)
            {
                LogSpinResultDebug(result);
            }

            StartCoroutine(PlayRoundSequence(result));
        }

        private void HandleErrorOccurred(string error)
        {
            if (session.IsAutoplaying)
            {
                HandleAutoplayStopped();
            }
            session.State = GameState.Idle;
            mainView.HideTopTumbleWinBanner();
            mainView.SetIdleBottomBarState();
            UpdateHUD(false);
            SetStatusMessage(error);
        }

        private void HandleBalanceSync(double newBalance)
        {
            session.Balance = newBalance;
            UpdateHUD(!session.IsIdle);
        }

        #endregion

        #region Spin & Stop

        private void HandleSpinClicked()
        {
            if (!session.IsIdle) return;

            if (session.Balance < session.EffectiveBet && !session.IsInFreeSpins)
            {
                SetStatusMessage("INSUFFICIENT BALANCE");
                if (session.IsAutoplaying)
                {
                    HandleAutoplayStopped();
                }
                // The spin button disables itself on click; give it back because no spin started.
                mainView.UpdateSpinButtonVisuals();
                return;
            }

            session.State = GameState.Spinning;
            SendSpinRequest(session.BetAmount, session.IsAnteBetActive, false);
        }

        private void HandleStopClicked()
        {
            if (session.IsRoundStoppable)
            {
                RequestStop();
            }
        }

        private void RequestStop()
        {
            AudioManager.Instance?.StopReelSpinLoop();
            AudioManager.Instance?.StopTensionBuilder();
            uiManager.SetStoppingState();
            gridView.RequestSlamStop();
        }

        private void SendSpinRequest(double betAmount, bool isAnteBet, bool isBonusBuy)
        {
            gridView.ResetSlamStop();
            double cost = session.GetSpinCost(betAmount, isAnteBet, isBonusBuy);
            if (!session.IsInFreeSpins)
            {
                session.Balance -= cost;
            }
            mainView.SetSpinStartedState(session.IsAutoplaying, session.IsInFreeSpins ? session.RemainingFreeSpins : session.RemainingAutoplaySpins);
            UpdateHUD(true);

            if (enableSpinDebugLogs)
            {
                LogSpinRequestDebug(betAmount, cost, isAnteBet, isBonusBuy);
            }

            AudioManager.Instance?.PlayReelSpinLoop();
            backendService.RequestSpin(betAmount, isAnteBet, isBonusBuy);
        }

        #endregion

        #region Bet, Ante & Buy Feature

        private void HandleBetIncreaseClicked()
        {
            if (!session.IsIdle || !session.HasBets) return;
            if (session.StepBet(1))
            {
                AudioManager.Instance?.PlayBetPlusMinus();
                UpdateHUD(false);
            }
            else
            {
                AudioManager.Instance?.PlayMaxBetReached();
            }
        }

        private void HandleBetDecreaseClicked()
        {
            if (!session.IsIdle || !session.HasBets) return;
            if (session.StepBet(-1))
            {
                AudioManager.Instance?.PlayBetPlusMinus();
                UpdateHUD(false);
            }
        }

        private void HandleBetMaxClicked()
        {
            if (!session.IsIdle || !session.HasBets) return;
            session.SelectMaxBet();
            AudioManager.Instance?.PlayMaxBetReached();
            UpdateHUD(false);
        }

        private void HandleBetAmountSelected(double newBet)
        {
            if (!session.IsIdle) return;
            session.SelectCustomBet(newBet);
            UpdateHUD(false);
        }

        private void HandleAnteBetToggled()
        {
            if (!session.IsIdle) return;
            session.IsAnteBetActive = !session.IsAnteBetActive;
            AudioManager.Instance?.PlayButton();
            mainView.SetAnteBetState(session.IsAnteBetActive);
            UpdateHUD(false);
        }

        private void HandleBuyBonusRequested()
        {
            if (!session.IsIdle || session.IsAnteBetActive) return;
            uiManager.OpenBuyFeatureModal(session.BetAmount);
        }

        private void HandleBuyBonusConfirmed()
        {
            if (!session.IsIdle) return;
            if (session.Balance < session.BuyFeatureCost)
            {
                SetStatusMessage("INSUFFICIENT BALANCE FOR BUY FEATURE");
                return;
            }

            session.State = GameState.Spinning;
            SendSpinRequest(session.BetAmount, false, true);
        }

        #endregion

        #region Autoplay

        private void HandleAutoplayStarted(int rounds)
        {
            if (!session.IsIdle) return;
            session.IsAutoplaying = true;
            session.RemainingAutoplaySpins = rounds;
            uiManager.SetAutoplayState(true, session.RemainingAutoplaySpins);
            HandleSpinClicked();
        }

        private void HandleAutoplayStopped()
        {
            session.IsAutoplaying = false;
            session.RemainingAutoplaySpins = 0;

            if (session.IsRoundStoppable)
            {
                RequestStop();
            }

            uiManager.SetAutoplayState(false, 0);
        }

        #endregion

        #region Round Sequence

        private IEnumerator PlayRoundSequence(RoundResult result)
        {
            session.State = GameState.DroppingInitial;
            AudioManager.Instance?.StopTensionBuilder();
            mainView.HideTopTumbleWinBanner(immediate: true);
            yield return gridView.PlayInitialDropSequence(result.InitialMatrix, result.MultiplierBombs);
            AudioManager.Instance?.StopReelSpinLoop();
            AudioManager.Instance?.StopTensionBuilder();

            if (result.ScatterCount >= 4)
            {
                SetStatusMessage($"⭐ {result.ScatterCount} SCATTERS HIT! ⭐");
                yield return Wait04;
            }

            session.State = GameState.TumblingDown;
            double rollingTumbleWin = 0;

            for (int i = 0; i < result.TumbleSteps.Count; i++)
            {
                var step = result.TumbleSteps[i];
                rollingTumbleWin += step.StepWinAmount;

                string payDetail = "";
                SymbolType winSymbol = SymbolType.Banana;
                int winCount = 0;
                double winPayout = 0;

                if (step.Wins != null && step.Wins.Count > 0)
                {
                    var w = step.Wins[0];
                    winSymbol = w.Symbol;
                    winCount = w.Count;
                    winPayout = w.WinAmount;
                    payDetail = $"{w.Count}X {w.Symbol.ToString().ToUpperInvariant()} PAYS {w.WinAmount:F2}";
                }

                SetStatusMessage($"TUMBLE #{i + 1} WIN: {step.StepWinAmount:F2}");

                if (session.IsInFreeSpins && rollingTumbleWin > 0)
                {
                    mainView.ShowTopTumbleWinBanner(rollingTumbleWin);
                }

                // Show cluster payout in bottom bar immediately as symbols celebrate and explode
                Sprite winSprite = winCount > 0 ? gridView.GetSymbolSprite(winSymbol) : null;
                mainView.SetTumbleStepWin(rollingTumbleWin, winSprite, winCount, winPayout, payDetail);

                yield return gridView.PlayTumbleStepSequence(step, result.MultiplierBombs);
            }

            if (result.MultiplierBombs != null && result.MultiplierBombs.Count > 0 && rollingTumbleWin > 0)
            {
                session.State = GameState.EvaluatingMultipliers;
                SetStatusMessage($"💣 MULTIPLIERS ACTIVATED: {result.TotalMultiplier}X!");

                mainView.ShowTopTumbleWinBanner(rollingTumbleWin);

                yield return gridView.PlayMultiplierBombRevealSequence(result.MultiplierBombs, (bombVal, worldPos, runningMult) =>
                {
                    StartCoroutine(mainView.AnimateBombMultiplierFlyUp(worldPos, bombVal, rollingTumbleWin, runningMult));
                });

                yield return mainView.FinalizeTumbleWinMultipliers(rollingTumbleWin, result.TotalMultiplier);

                mainView.SetWin(result.FinalWinAmount);
                yield return Wait05;
            }

            if (result.FinalWinAmount >= session.EffectiveBet * 20)
            {
                uiManager.ShowWinCelebration("MEGA WIN!", result.FinalWinAmount, 3.5f);
                yield return WaitForWinCelebration(3.5f);
            }
            else if (result.FinalWinAmount >= session.EffectiveBet * 2)
            {
                uiManager.ShowWinCelebration("NICE WIN!", result.FinalWinAmount, 3.0f);
                yield return WaitForWinCelebration(3.0f);
            }

            if (result.TriggeredFreeSpins && !session.IsInFreeSpins)
            {
                BeginFreeSpins(result);
                yield return StartCoroutine(gridView.PlayScatterCelebrationSequence());

                mainView.SetFreeSpinsDisplay(true, session.RemainingFreeSpins);
                bool showIntro = PlayerPrefs.GetInt("settings_intro_screen", 1) == 1;
                if (showIntro)
                {
                    uiManager.ShowFreeSpinsIntro(session.TotalFreeSpinsAwarded);
                    yield return Wait20;
                }
            }
            else if (session.IsInFreeSpins)
            {
                if (AdvanceFreeSpins(result)) yield return Wait20;
            }

            if (result.FinalWinAmount > 0)
            {
                mainView.SetWin(result.FinalWinAmount);
                yield return session.IsAutoplaying ? Wait04 : Wait10;
            }

            // Between free spins the round is not really idle: the next spin is already queued.
            // Staying in Spinning blocks any other spin/buy request during the short pause.
            bool nextFreeSpinQueued = session.IsInFreeSpins && session.RemainingFreeSpins > 0;
            session.State = nextFreeSpinQueued ? GameState.Spinning : GameState.Idle;
            mainView.HideTopTumbleWinBanner();
            mainView.SetIdleBottomBarState();
            UpdateHUD(false);

            if (nextFreeSpinQueued)
            {
                yield return Wait06;
                uiManager.CloseCurrentPanel();
                SendSpinRequest(session.BetAmount, false, false);
            }
            else if (session.IsAutoplaying)
            {
                session.RemainingAutoplaySpins--;
                if (session.RemainingAutoplaySpins > 0)
                {
                    uiManager.SetAutoplayState(true, session.RemainingAutoplaySpins);
                    yield return Wait05;
                    // Autoplay may have been stopped during the pause; then no further spin.
                    if (session.IsAutoplaying) HandleSpinClicked();
                }
                else
                {
                    HandleAutoplayStopped();
                }
            }
        }

        /// <summary>Waits for the win celebration, but continues as soon as the player taps it away.</summary>
        private IEnumerator WaitForWinCelebration(float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration && uiManager.IsWinCelebrationOpen)
            {
                yield return null;
                elapsed += Time.deltaTime;
            }
        }

        /// <summary>Base game hit the free spins trigger: switch the session into free spins.</summary>
        private void BeginFreeSpins(RoundResult result)
        {
            if (session.IsAutoplaying)
            {
                HandleAutoplayStopped();
            }

            session.IsInFreeSpins = true;
            session.RemainingFreeSpins = result.RemainingFreeSpins;
            session.TotalFreeSpinsAwarded = result.FreeSpinsAwarded > 0 ? result.FreeSpinsAwarded : 10;
            session.AccumulatedFreeSpinsWin = 0;

            AudioManager.Instance?.PlayScatterCelebration();
            AudioManager.Instance?.PlayFeatureBg();
        }

        /// <summary>Applies one free spin result. Returns true when the free spins round just ended (outro shown).</summary>
        private bool AdvanceFreeSpins(RoundResult result)
        {
            if (result.TriggeredFreeSpins && result.FreeSpinsAwarded > 0)
            {
                session.TotalFreeSpinsAwarded += result.FreeSpinsAwarded;
            }
            session.AccumulatedFreeSpinsWin += result.FinalWinAmount;
            session.RemainingFreeSpins = result.RemainingFreeSpins;
            mainView.SetFreeSpinsDisplay(session.RemainingFreeSpins > 0, session.RemainingFreeSpins);

            if (session.RemainingFreeSpins > 0) return false;

            session.IsInFreeSpins = false;
            AudioManager.Instance?.StopFeatureBg();
            double finalFsWin = session.AccumulatedFreeSpinsWin > 0 ? session.AccumulatedFreeSpinsWin : result.FinalWinAmount;
            uiManager.ShowFreeSpinsOutro(finalFsWin);
            session.AccumulatedFreeSpinsWin = 0;
            return true;
        }

        private void HandleScatterAnticipation(int targetColumn)
        {
            SetStatusMessage("🍭 3 SCATTERS! LOOKING FOR 4TH... 🍭");
            AudioManager.Instance?.PlayTensionBuilder();
        }

        #endregion

        #region System Settings Handlers

        private void HandleQuickSpinChanged(bool isOn)
        {
            gridView.SetQuickSpinMode(isOn);
        }

        private void HandleBatterySaverChanged(bool isOn)
        {
            ApplyFrameRateSettings(isOn);
        }

        private void HandleAmbientMusicChanged(bool isOn)
        {
            AudioManager.Instance?.SetMusicEnabled(isOn);
        }

        private void HandleSoundFxChanged(bool isOn)
        {
            AudioManager.Instance?.SetSfxEnabled(isOn);
        }

        private void HandleIntroScreenChanged(bool isOn)
        {
            PlayerPrefs.SetInt("settings_intro_screen", isOn ? 1 : 0);
        }

        #endregion

        #region Debug Logs

        private void LogSpinRequestDebug(double betAmount, double cost, bool isAnteBet, bool isBonusBuy)
        {
            string anteStr = isAnteBet ? $"<color=#2ecc71>ACTIVE ({session.AnteBetMultiplier}x)</color>" : "OFF";
            string bonusStr = isBonusBuy ? $"<color=#f1c40f>YES ({session.BuyFeatureCostMultiplier}x)</color>" : "NO";
            string fsStr = session.IsInFreeSpins ? $"<color=#e056fd>ACTIVE ({session.RemainingFreeSpins} remaining)</color>" : "NO";
            double effectiveCost = session.IsInFreeSpins ? 0.0 : cost;

            Debug.Log($"<color=#3498db><b>[SPIN REQUEST 📤]</b></color> Base Bet: <b>${betAmount:F2}</b> | Cost: <b>${effectiveCost:F2}</b> | Ante Bet: {anteStr} | Bonus Buy: {bonusStr} | Free Spins: {fsStr} | Balance: <b>${session.Balance:F2}</b>");
        }

        private void LogSpinResultDebug(RoundResult result)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"<color=#2ecc71><b>[SPIN RESULT 📥]</b></color> Final Win: <color=#f1c40f><b>${result.FinalWinAmount:F2}</b></color> | New Balance: <b>${session.Balance:F2}</b>");
            sb.AppendLine($"  • Mode: {(result.IsFreeSpin ? $"<color=#e056fd>FREE SPIN ({result.RemainingFreeSpins} left)</color>" : "BASE GAME")} | Scatters: <b>{result.ScatterCount}</b> {(result.TriggeredFreeSpins ? $"<color=#e056fd><b>-> TRIGGERED {result.FreeSpinsAwarded} FREE SPINS!</b></color>" : "")}");
            sb.AppendLine($"  • Tumbles: <b>{result.TumbleSteps.Count}</b> | Total Multipliers: <b>{result.TotalMultiplier}X</b> (Bombs: {result.MultiplierBombs.Count})");

            for (int i = 0; i < result.TumbleSteps.Count; i++)
            {
                var step = result.TumbleSteps[i];
                sb.Append($"    ┌─ Tumble #{i + 1}: Win = ${step.StepWinAmount:F2} (");
                for (int w = 0; w < step.Wins.Count; w++)
                {
                    var win = step.Wins[w];
                    sb.Append($"{win.Count}x {win.Symbol} [${win.WinAmount:F2}]");
                    if (w < step.Wins.Count - 1) sb.Append(", ");
                }
                sb.AppendLine(")");
            }

            if (result.MultiplierBombs != null && result.MultiplierBombs.Count > 0)
            {
                sb.Append("    └─ Multiplier Bombs: ");
                for (int b = 0; b < result.MultiplierBombs.Count; b++)
                {
                    sb.Append($"{result.MultiplierBombs[b].MultiplierValue}X at Col:{result.MultiplierBombs[b].Position.Column},Row:{result.MultiplierBombs[b].Position.Row}");
                    if (b < result.MultiplierBombs.Count - 1) sb.Append(", ");
                }
                sb.AppendLine();
            }

            Debug.Log(sb.ToString());
        }

#if UNITY_EDITOR
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.B) && session.IsIdle)
            {
                Debug.Log("[DEV TEST] Key B pressed -> Triggering Free Spins Bonus Buy...");
                HandleBuyBonusConfirmed();
            }
        }
#endif

        #endregion
    }
}
