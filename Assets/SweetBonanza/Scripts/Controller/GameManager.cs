using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SweetBonanza.Core;
using SweetBonanza.Service;
using SweetBonanza.View;

namespace SweetBonanza.Controller
{
    public class GameManager : MonoBehaviour
    {
        [Header("Service & View References")]
        [SerializeField] private BackendService backendService;
        [SerializeField] private GridView gridView;
        [SerializeField] private MainView mainView;
        [SerializeField] private UIManager uiManager;

        private GameState currentState = GameState.Initializing;
        private double currentBalance;
        private int currentBetIndex = 2;
        private double[] availableBets;
        private bool isAnteBetActive;
        private bool isInFreeSpins;
        private int remainingFreeSpins;

        private bool isAutoplaying;
        private int remainingAutoplaySpins;

        private double CurrentBetAmount => (availableBets != null && currentBetIndex < availableBets.Length)
            ? availableBets[currentBetIndex]
            : 1.00;

        private void Start()
        {
            if (backendService != null)
            {
                backendService.OnInitDataReceived += HandleInitDataReceived;
                backendService.OnRoundResultReceived += HandleRoundResultReceived;
                backendService.OnErrorOccurred += HandleErrorOccurred;
            }

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
                uiManager.OnQuickSpinToggled += isQuick => gridView?.SetQuickSpinMode(isQuick);
            }
            else if (mainView != null)
            {
                mainView.OnSpinClicked += HandleSpinClicked;
                mainView.OnBetIncreaseClicked += HandleBetIncreaseClicked;
                mainView.OnBetDecreaseClicked += HandleBetDecreaseClicked;
                mainView.OnAnteBetToggled += HandleAnteBetToggled;
                mainView.OnBuyBonusClicked += HandleBuyBonusConfirmed;
            }

            if (backendService != null)
            {
                backendService.Initialize();
            }
        }

        private void HandleInitDataReceived(InitPayload payload)
        {
            currentBalance = payload.Balance;
            currentBetIndex = payload.DefaultBetIndex;
            availableBets = payload.AvailableBets;

            UpdateHUD(false);

            if (mainView != null)
            {
                mainView.SetAnteBetState(isAnteBetActive);
                mainView.SetFreeSpinsDisplay(false, 0);
                mainView.SetStatusMessage("PRESS SPIN TO PLAY");
            }

            currentState = GameState.Idle;
        }

        private void UpdateHUD(bool isSpinning)
        {
            if (uiManager != null)
            {
                uiManager.UpdateHUD(currentBalance, CurrentBetAmount, 0, isAnteBetActive, isSpinning);
            }
            else if (mainView != null)
            {
                mainView.SetBalance(currentBalance);
                mainView.SetBet(CurrentBetAmount, isAnteBetActive);
                mainView.SetSpinButtonInteractable(!isSpinning);
            }
        }

        private void HandleSpinClicked()
        {
            if (currentState != GameState.Idle) return;

            currentState = GameState.Spinning;
            UpdateHUD(true);
            SetStatusMessage("GOOD LUCK!");

            backendService.RequestSpin(CurrentBetAmount, isAnteBetActive, false);
        }

        private void HandleBuyBonusRequested()
        {
            if (currentState != GameState.Idle || isAnteBetActive) return;

            if (uiManager != null)
            {
                uiManager.OpenBuyFeatureModal(CurrentBetAmount);
            }
            else
            {
                HandleBuyBonusConfirmed();
            }
        }

        private void HandleBuyBonusConfirmed()
        {
            if (currentState != GameState.Idle || isAnteBetActive) return;

            currentState = GameState.Spinning;
            UpdateHUD(true);
            SetStatusMessage("BUYING FREE SPINS FEATURE...");

            backendService.RequestSpin(CurrentBetAmount, isAnteBetActive, true);
        }

        private void HandleBetIncreaseClicked()
        {
            if (currentState != GameState.Idle || availableBets == null || availableBets.Length == 0) return;

            currentBetIndex++;
            if (currentBetIndex >= availableBets.Length) currentBetIndex = 0;

            UpdateHUD(false);
        }

        private void HandleBetDecreaseClicked()
        {
            if (currentState != GameState.Idle || availableBets == null || availableBets.Length == 0) return;

            currentBetIndex--;
            if (currentBetIndex < 0) currentBetIndex = availableBets.Length - 1;

            UpdateHUD(false);
        }

        private void HandleBetMaxClicked()
        {
            if (currentState != GameState.Idle || availableBets == null || availableBets.Length == 0) return;

            currentBetIndex = availableBets.Length - 1;
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
            yield return gridView.PlayInitialDropSequence(result.InitialMatrix);

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
                yield return gridView.PlayTumbleStepSequence(step);
                if (mainView != null) mainView.SetWin(rollingTumbleWin, true);
            }

            if (result.MultiplierBombs != null && result.MultiplierBombs.Count > 0 && rollingTumbleWin > 0)
            {
                currentState = GameState.EvaluatingMultipliers;
                SetStatusMessage($"💣 MULTIPLIERS ACTIVATED: {result.TotalMultiplier}X!");
                yield return gridView.PlayMultiplierBombRevealSequence(result.MultiplierBombs);
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

                if (uiManager != null)
                {
                    uiManager.ShowFreeSpinsIntro(result.FreeSpinsAwarded);
                }
                SetStatusMessage($"🎉 CONGRATULATIONS! {result.FreeSpinsAwarded} FREE SPINS WON! 🎉");
                yield return new WaitForSeconds(1.2f);
            }
            else if (isInFreeSpins)
            {
                remainingFreeSpins = result.RemainingFreeSpins;
                if (mainView != null) mainView.SetFreeSpinsDisplay(remainingFreeSpins > 0, remainingFreeSpins);

                if (remainingFreeSpins <= 0)
                {
                    isInFreeSpins = false;
                    if (uiManager != null)
                    {
                        uiManager.ShowFreeSpinsOutro(result.FinalWinAmount);
                    }
                    SetStatusMessage($"FREE SPINS FINISHED! TOTAL WIN: ${result.FinalWinAmount:F2}");
                    yield return new WaitForSeconds(1.0f);
                }
            }
            else
            {
                if (result.FinalWinAmount > 0)
                {
                    SetStatusMessage($"WIN: ${result.FinalWinAmount:F2}");
                }
                else
                {
                    SetStatusMessage("TRY AGAIN");
                }
            }

            currentState = GameState.Idle;
            UpdateHUD(false);

            if (isInFreeSpins && remainingFreeSpins > 0)
            {
                yield return new WaitForSeconds(0.6f);
                SetStatusMessage($"FREE SPIN REMAINING: {remainingFreeSpins}");
                backendService.RequestSpin(CurrentBetAmount, false, false);
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
