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
        [SerializeField] private int matrixColumns = 6;
        [SerializeField] private int matrixRows = 5;
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

        internal double CurrentBetAmount => isAnteBetActive ? currentBetAmount * anteBetMultiplier : currentBetAmount;

        private void Start()
        {
            InitializeServices();
            HookViewEvents();
            UpdateHUD(false);
            currentState = GameState.Idle;
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
                uiManager.OnAnteBetToggled += HandleAnteBetToggled;
                uiManager.OnBuyFeatureRequested += HandleBuyBonusRequested;
                uiManager.OnBuyFeatureConfirmed += HandleBuyBonusConfirmed;
                uiManager.OnAutoplayStarted += HandleAutoplayStarted;
                uiManager.OnAutoplayStopped += HandleAutoplayStopped;
            }
            else if (mainView != null)
            {
                mainView.OnSpinClicked += HandleSpinClicked;
                mainView.OnBetIncreaseClicked += HandleBetIncreaseClicked;
                mainView.OnBetDecreaseClicked += HandleBetDecreaseClicked;
                mainView.OnAnteBetToggled += HandleAnteBetToggled;
                mainView.OnBuyBonusClicked += HandleBuyBonusConfirmed;
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
            UpdateHUD(true);
            backendService.RequestSpin(currentBetAmount, isAnteBetActive, false);
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
            UpdateHUD(true);
            backendService.RequestSpin(currentBetAmount, false, true);
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
            StartCoroutine(PlayRoundSequence(result));
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
                if (mainView != null) mainView.SetFreeSpinsDisplay(true, remainingFreeSpins);
                if (uiManager != null) uiManager.ShowFreeSpinsIntro(result.FreeSpinsAwarded);
                yield return new WaitForSeconds(1.2f);
            }
            else if (isInFreeSpins)
            {
                remainingFreeSpins = result.RemainingFreeSpins;
                if (mainView != null) mainView.SetFreeSpinsDisplay(remainingFreeSpins > 0, remainingFreeSpins);
                if (remainingFreeSpins <= 0)
                {
                    isInFreeSpins = false;
                    if (uiManager != null) uiManager.ShowFreeSpinsOutro(result.FinalWinAmount);
                    yield return new WaitForSeconds(1.0f);
                }
            }

            currentState = GameState.Idle;
            UpdateHUD(false);

            if (isInFreeSpins && remainingFreeSpins > 0)
            {
                yield return new WaitForSeconds(0.6f);
                backendService.RequestSpin(currentBetAmount, false, false);
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
                uiManager.OnAnteBetToggled -= HandleAnteBetToggled;
                uiManager.OnBuyFeatureRequested -= HandleBuyBonusRequested;
                uiManager.OnBuyFeatureConfirmed -= HandleBuyBonusConfirmed;
                uiManager.OnAutoplayStarted -= HandleAutoplayStarted;
                uiManager.OnAutoplayStopped -= HandleAutoplayStopped;
            }
            else if (mainView != null)
            {
                mainView.OnSpinClicked -= HandleSpinClicked;
                mainView.OnBetIncreaseClicked -= HandleBetIncreaseClicked;
                mainView.OnBetDecreaseClicked -= HandleBetDecreaseClicked;
                mainView.OnAnteBetToggled -= HandleAnteBetToggled;
                mainView.OnBuyBonusClicked -= HandleBuyBonusConfirmed;
            }
        }
    }
}
