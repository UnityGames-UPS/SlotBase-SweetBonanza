using System;
using System.Collections;
using UnityEngine;
using SweetBonanza.Model;
using SweetBonanza.View;
using SweetBonanza.Service;

namespace SweetBonanza.Controller
{
    public class GameManager : MonoBehaviour
    {
        [Header("Views Bindings")]
        [SerializeField] private MainView mainView;
        [SerializeField] private UIManager uiManager;
        [SerializeField] private GridView gridView;

        [Header("Service Binding")]
        [SerializeField] private BackendService backendService;

        [Header("Server Data & Balance")]
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

        private GameState currentState = GameState.Initializing;
        private bool isAnteBetActive = false;
        private bool isInFreeSpins = false;
        private int remainingFreeSpins = 0;
        private bool isAutoplaying = false;
        private int remainingAutoplaySpins = 0;

        [Header("Debug")]
        [SerializeField] private bool enableSpinDebugLogs = true;

        internal double CurrentBetAmount => isAnteBetActive ? currentBetAmount * anteBetMultiplier : currentBetAmount;

        private void Start()
        {
            InitializeServices();
            HookViewEvents();
            ApplyInitialSettings();
            UpdateHUD(false);
            if (uiManager != null && availableBets != null && availableBets.Length > 0)
            {
                uiManager.UpdateBetLimits(availableBets[0], availableBets[availableBets.Length - 1]);
            }
            currentState = GameState.Idle;
        }

        private void ApplyInitialSettings()
        {
            bool quickSpin = PlayerPrefs.GetInt("settings_quick_spin", 0) == 1;
            if (gridView != null) gridView.SetQuickSpinMode(quickSpin);

            bool batterySaver = PlayerPrefs.GetInt("settings_battery_saver", 0) == 1;
            Application.targetFrameRate = batterySaver ? 30 : 60;
            QualitySettings.vSyncCount = batterySaver ? 0 : 1;
        }

        private void InitializeServices()
        {
            if (backendService == null)
            {
                backendService = GetComponent<BackendService>();
                if (backendService == null)
                {
                    backendService = gameObject.AddComponent<LocalDemoService>();
                }
            }

            backendService.OnInitDataReceived += HandleInitDataReceived;
            backendService.OnRoundResultReceived += HandleRoundResultReceived;
            backendService.OnErrorOccurred += HandleErrorOccurred;

            backendService.Initialize();
        }

        private void HookViewEvents()
        {
            if (uiManager != null)
            {
                uiManager.OnSpinClicked += HandleSpinClicked;
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
            }
            else if (mainView != null)
            {
                mainView.OnSpinClicked += HandleSpinClicked;
                mainView.OnBetIncreaseClicked += HandleBetIncreaseClicked;
                mainView.OnBetDecreaseClicked += HandleBetDecreaseClicked;
                mainView.OnAnteBetToggled += HandleAnteBetToggled;
                mainView.OnBuyBonusClicked += HandleBuyBonusConfirmed;
            }

            if (gridView != null)
            {
                gridView.OnScatterAnticipationTriggered += HandleScatterAnticipation;
            }
        }

        private void HandleInitDataReceived(InitPayload payload)
        {
            currentBalance = payload.Balance;
            currentBetIndex = Mathf.Clamp(payload.DefaultBetIndex, 0, availableBets.Length - 1);
            currentBetAmount = availableBets[currentBetIndex];
            anteBetMultiplier = payload.AnteBetMultiplier;
            buyFeatureCostMultiplier = payload.BuyFeatureCostMultiplier;
            maxWinMultiplier = payload.MaxWinMultiplier;

            if (uiManager != null && availableBets != null && availableBets.Length > 0)
            {
                uiManager.UpdateBetLimits(availableBets[0], availableBets[availableBets.Length - 1]);
            }
            UpdateHUD(false);
        }

        private void UpdateHUD(bool isSpinning)
        {
            if (uiManager != null)
            {
                uiManager.UpdateHUD(currentBalance, currentBetAmount, 0, isAnteBetActive, isSpinning);
            }
            else if (mainView != null)
            {
                mainView.SetBalance(currentBalance);
                mainView.SetBet(currentBetAmount, isAnteBetActive);
                mainView.SetSpinButtonInteractable(!isSpinning);
            }
        }

        private void HandleSpinClicked()
        {
            if (currentState != GameState.Idle) return;

            if (currentBalance < CurrentBetAmount && !isInFreeSpins)
            {
                SetStatusMessage("INSUFFICIENT BALANCE");
                return;
            }

            currentState = GameState.Spinning;
            SendSpinRequest(currentBetAmount, isAnteBetActive, false);
        }

        private void HandleBuyBonusRequested()
        {
            if (currentState != GameState.Idle || isAnteBetActive) return;
            if (uiManager != null) uiManager.OpenBuyFeatureModal(currentBetAmount);
        }

        private void HandleBuyBonusConfirmed()
        {
            if (currentState != GameState.Idle) return;
            double cost = currentBetAmount * buyFeatureCostMultiplier;
            if (currentBalance < cost)
            {
                SetStatusMessage("INSUFFICIENT BALANCE FOR BUY FEATURE");
                return;
            }

            currentState = GameState.Spinning;
            SendSpinRequest(currentBetAmount, false, true);
        }

        private void SendSpinRequest(double betAmount, bool isAnteBet, bool isBonusBuy)
        {
            double cost = isBonusBuy ? betAmount * buyFeatureCostMultiplier : (isAnteBet ? betAmount * anteBetMultiplier : betAmount);
            if (!isInFreeSpins)
            {
                currentBalance -= cost;
            }
            UpdateHUD(true);

            if (enableSpinDebugLogs)
            {
                string anteStr = isAnteBet ? "<color=#2ecc71>ACTIVE (1.25x)</color>" : "OFF";
                string bonusStr = isBonusBuy ? $"<color=#f1c40f>YES ({buyFeatureCostMultiplier}x)</color>" : "NO";
                string fsStr = isInFreeSpins ? $"<color=#e056fd>ACTIVE ({remainingFreeSpins} remaining)</color>" : "NO";
                double effectiveCost = isInFreeSpins ? 0.0 : cost;

                Debug.Log($"<color=#3498db><b>[SPIN REQUEST 📤]</b></color> Base Bet: <b>${betAmount:F2}</b> | Cost: <b>${effectiveCost:F2}</b> | Ante Bet: {anteStr} | Bonus Buy: {bonusStr} | Free Spins: {fsStr} | Balance: <b>${currentBalance:F2}</b>");
            }

            if (backendService != null)
            {
                backendService.RequestSpin(betAmount, isAnteBet, isBonusBuy);
            }
        }

        private void HandleBetIncreaseClicked()
        {
            if (currentState != GameState.Idle || availableBets == null || availableBets.Length == 0) return;
            currentBetIndex = (currentBetIndex + 1) % availableBets.Length;
            currentBetAmount = availableBets[currentBetIndex];
            UpdateHUD(false);
        }

        private void HandleBetDecreaseClicked()
        {
            if (currentState != GameState.Idle || availableBets == null || availableBets.Length == 0) return;
            currentBetIndex--;
            if (currentBetIndex < 0) currentBetIndex = availableBets.Length - 1;
            currentBetAmount = availableBets[currentBetIndex];
            UpdateHUD(false);
        }

        private void HandleBetMaxClicked()
        {
            if (currentState != GameState.Idle || availableBets == null || availableBets.Length == 0) return;
            currentBetIndex = availableBets.Length - 1;
            currentBetAmount = availableBets[currentBetIndex];
            UpdateHUD(false);
        }

        private void HandleBetAmountSelected(double newBet)
        {
            if (currentState != GameState.Idle) return;
            currentBetAmount = (float)newBet;
            if (availableBets != null && availableBets.Length > 0)
            {
                int closestIdx = 0;
                float closestDiff = float.MaxValue;
                for (int i = 0; i < availableBets.Length; i++)
                {
                    float diff = Mathf.Abs(availableBets[i] - currentBetAmount);
                    if (diff < closestDiff)
                    {
                        closestDiff = diff;
                        closestIdx = i;
                    }
                }
                currentBetIndex = closestIdx;
            }
            UpdateHUD(false);
        }

        private void HandleAnteBetToggled()
        {
            if (currentState != GameState.Idle) return;
            isAnteBetActive = !isAnteBetActive;
            if (mainView != null) mainView.SetAnteBetState(isAnteBetActive);
            UpdateHUD(false);
        }

        private void HandleAutoplayStarted(int rounds)
        {
            if (currentState != GameState.Idle) return;
            isAutoplaying = true;
            remainingAutoplaySpins = rounds;
            if (uiManager != null) uiManager.SetAutoplayState(true, remainingAutoplaySpins);
            HandleSpinClicked();
        }

        private void HandleAutoplayStopped()
        {
            isAutoplaying = false;
            remainingAutoplaySpins = 0;
            if (uiManager != null) uiManager.SetAutoplayState(false, 0);
        }

        private void HandleRoundResultReceived(RoundResult result)
        {
            if (result.PlayerBalance > 0)
            {
                currentBalance = result.PlayerBalance;
            }
            else
            {
                currentBalance += result.FinalWinAmount;
            }

            if (enableSpinDebugLogs && result != null)
            {
                LogSpinResultDebug(result);
            }

            StartCoroutine(PlayRoundSequence(result));
        }

        private void LogSpinResultDebug(RoundResult result)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"<color=#2ecc71><b>[SPIN RESULT 📥]</b></color> Final Win: <color=#f1c40f><b>${result.FinalWinAmount:F2}</b></color> | New Balance: <b>${currentBalance:F2}</b>");
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

        private IEnumerator PlayRoundSequence(RoundResult result)
        {
            currentState = GameState.DroppingInitial;
            if (gridView != null) yield return gridView.PlayInitialDropSequence(result.InitialMatrix);

            if (result.ScatterCount >= 4)
            {
                SetStatusMessage($"⭐ {result.ScatterCount} SCATTERS HIT! ⭐");
                yield return new WaitForSeconds(0.4f);
            }

            currentState = GameState.TumblingDown;
            double rollingTumbleWin = 0;

            for (int i = 0; i < result.TumbleSteps.Count; i++)
            {
                var step = result.TumbleSteps[i];
                rollingTumbleWin += step.StepWinAmount;
                SetStatusMessage($"TUMBLE #{i + 1} WIN: ${step.StepWinAmount:F2}");
                if (gridView != null) yield return gridView.PlayTumbleStepSequence(step);
                if (mainView != null) mainView.SetWin(rollingTumbleWin, true);
            }

            if (result.MultiplierBombs != null && result.MultiplierBombs.Count > 0 && rollingTumbleWin > 0)
            {
                currentState = GameState.EvaluatingMultipliers;
                SetStatusMessage($"💣 MULTIPLIERS ACTIVATED: {result.TotalMultiplier}X!");
                if (gridView != null) yield return gridView.PlayMultiplierBombRevealSequence(result.MultiplierBombs);
                if (mainView != null) mainView.SetWin(result.FinalWinAmount, true);
                yield return new WaitForSeconds(0.5f);
            }

            if (result.FinalWinAmount >= CurrentBetAmount * 10)
            {
                string title = "BIG WIN!";
                if (result.FinalWinAmount >= CurrentBetAmount * 50) title = "SENSATIONAL WIN!";
                else if (result.FinalWinAmount >= CurrentBetAmount * 25) title = "MEGA WIN!";

                if (uiManager != null)
                {
                    uiManager.ShowWinCelebration(title, result.FinalWinAmount, 2.5f);
                    yield return new WaitForSeconds(2.5f);
                }
            }

            if (result.TriggeredFreeSpins && !isInFreeSpins)
            {
                isInFreeSpins = true;
                remainingFreeSpins = result.RemainingFreeSpins;

                if (gridView != null)
                {
                    yield return StartCoroutine(gridView.PlayScatterCelebrationSequence());
                }

                if (mainView != null) mainView.SetFreeSpinsDisplay(true, remainingFreeSpins);
                if (uiManager != null) uiManager.ShowFreeSpinsIntro(result.FreeSpinsAwarded);
                yield return new WaitForSeconds(2.0f);
            }
            else if (isInFreeSpins)
            {
                remainingFreeSpins = result.RemainingFreeSpins;
                if (mainView != null) mainView.SetFreeSpinsDisplay(remainingFreeSpins > 0, remainingFreeSpins);
                if (remainingFreeSpins <= 0)
                {
                    isInFreeSpins = false;
                    if (uiManager != null) uiManager.ShowFreeSpinsOutro(result.FinalWinAmount);
                    yield return new WaitForSeconds(2.0f);
                }
            }

            currentState = GameState.Idle;
            UpdateHUD(false);

            if (isInFreeSpins && remainingFreeSpins > 0)
            {
                yield return new WaitForSeconds(0.6f);
                if (uiManager != null) uiManager.CloseCurrentPanel();
                SendSpinRequest(currentBetAmount, false, false);
            }
            else if (isAutoplaying)
            {
                remainingAutoplaySpins--;
                if (remainingAutoplaySpins > 0)
                {
                    if (uiManager != null) uiManager.SetAutoplayState(true, remainingAutoplaySpins);
                    yield return new WaitForSeconds(0.5f);
                    HandleSpinClicked();
                }
                else
                {
                    HandleAutoplayStopped();
                }
            }
        }

        private void HandleScatterAnticipation(int targetColumn)
        {
            SetStatusMessage("🍭 3 SCATTERS! LOOKING FOR 4TH... 🍭");
        }

        private void SetStatusMessage(string message)
        {
            if (uiManager != null) uiManager.SetStatusMessage(message);
            else if (mainView != null) mainView.SetStatusMessage(message);
        }

        private void HandleErrorOccurred(string error)
        {
            currentState = GameState.Idle;
            UpdateHUD(false);
            SetStatusMessage(error);
        }

        #region System Settings Handlers
        private void HandleQuickSpinChanged(bool isOn)
        {
            if (gridView != null) gridView.SetQuickSpinMode(isOn);
        }

        private void HandleBatterySaverChanged(bool isOn)
        {
            Application.targetFrameRate = isOn ? 30 : 60;
            QualitySettings.vSyncCount = isOn ? 0 : 1;
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

        private void OnDestroy()
        {
            if (backendService != null)
            {
                backendService.OnInitDataReceived -= HandleInitDataReceived;
                backendService.OnRoundResultReceived -= HandleRoundResultReceived;
                backendService.OnErrorOccurred -= HandleErrorOccurred;
            }

            if (uiManager != null)
            {
                uiManager.OnSpinClicked -= HandleSpinClicked;
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
            }
            else if (mainView != null)
            {
                mainView.OnSpinClicked -= HandleSpinClicked;
                mainView.OnBetIncreaseClicked -= HandleBetIncreaseClicked;
                mainView.OnBetDecreaseClicked -= HandleBetDecreaseClicked;
                mainView.OnAnteBetToggled -= HandleAnteBetToggled;
                mainView.OnBuyBonusClicked -= HandleBuyBonusConfirmed;
            }

            if (gridView != null)
            {
                gridView.OnScatterAnticipationTriggered -= HandleScatterAnticipation;
            }
        }

#if UNITY_EDITOR
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.B) && currentState == GameState.Idle)
            {
                Debug.Log("[DEV TEST] Key B pressed -> Triggering Free Spins Bonus Buy...");
                HandleBuyBonusConfirmed();
            }
        }
#endif
    }
}
