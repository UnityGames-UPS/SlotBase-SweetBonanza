using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SweetBonanza.Core;

namespace SweetBonanza.Service
{
    internal class LocalDemoService : BackendService
    {
        [Header("Starting Balance")]
        [SerializeField] private double startingBalance = 1000.00;

        [Header("Network Simulation")]
        [SerializeField] private float networkSimulationDelay = 0.15f;

        private double currentBalance;
        private int currentBetIndex = 2;

        private bool isInFreeSpins;
        private int remainingFreeSpins;
        private double totalFreeSpinsWin;
        private double activeFreeSpinBet;

        private static readonly int[] MultiplierValues = { 2, 3, 4, 5, 6, 8, 10, 12, 15, 20, 25, 50, 100 };

        private void Awake()
        {
            currentBalance = startingBalance;
        }

        internal override void Initialize()
        {
            StartCoroutine(SimulateInit());
        }

        private IEnumerator SimulateInit()
        {
            yield return new WaitForSeconds(networkSimulationDelay);

            var initPayload = new InitPayload
            {
                Balance = currentBalance,
                DefaultBetIndex = currentBetIndex,
                AvailableBets = GameConfig.DefaultAvailableBets
            };

            RaiseInitDataReceived(initPayload);
        }

        internal override void RequestSpin(double betAmount, bool isAnteBet, bool isBonusBuy)
        {
            StartCoroutine(SimulateSpinRoutine(betAmount, isAnteBet, isBonusBuy));
        }

        private IEnumerator SimulateSpinRoutine(double betAmount, bool isAnteBet, bool isBonusBuy)
        {
            yield return new WaitForSeconds(networkSimulationDelay);

            double effectiveCost = 0;
            if (!isInFreeSpins)
            {
                if (isBonusBuy)
                {
                    effectiveCost = betAmount * 100.0;
                }
                else if (isAnteBet)
                {
                    effectiveCost = betAmount * 1.25;
                }
                else
                {
                    effectiveCost = betAmount;
                }

                if (currentBalance < effectiveCost)
                {
                    RaiseErrorOccurred("Insufficient balance for spin.");
                    yield break;
                }

                currentBalance -= effectiveCost;
            }

            var result = GenerateRoundResult(betAmount, isAnteBet, isBonusBuy);

            currentBalance += result.FinalWinAmount;

            if (isInFreeSpins)
            {
                totalFreeSpinsWin += result.FinalWinAmount;
                remainingFreeSpins--;
                result.RemainingFreeSpins = remainingFreeSpins;

                if (remainingFreeSpins <= 0)
                {
                    isInFreeSpins = false;
                }
            }
            else if (result.TriggeredFreeSpins)
            {
                isInFreeSpins = true;
                remainingFreeSpins = result.FreeSpinsAwarded;
                totalFreeSpinsWin = 0;
                activeFreeSpinBet = betAmount;
                result.RemainingFreeSpins = remainingFreeSpins;
            }

            RaiseRoundResultReceived(result);
        }

        private RoundResult GenerateRoundResult(double betAmount, bool isAnteBet, bool isBonusBuy)
        {
            var result = new RoundResult();
            result.IsFreeSpin = isInFreeSpins;

            int[,] currentMatrix = new int[GameConfig.COLUMNS, GameConfig.ROWS];

            FillRandomMatrix(currentMatrix, isAnteBet, isBonusBuy, isInFreeSpins);
            result.InitialMatrix = (int[,])currentMatrix.Clone();

            int scatterCount = 0;
            for (int c = 0; c < GameConfig.COLUMNS; c++)
            {
                for (int r = 0; r < GameConfig.ROWS; r++)
                {
                    if (currentMatrix[c, r] == (int)SymbolType.Scatter)
                    {
                        scatterCount++;
                    }
                }
            }
            result.ScatterCount = scatterCount;

            double scatterWin = 0;
            if (scatterCount >= 4)
            {
                scatterWin = betAmount * GameConfig.GetPayoutMultiplier(SymbolType.Scatter, scatterCount);
                result.TriggeredFreeSpins = true;
                result.FreeSpinsAwarded = 10;
            }
            else if (isInFreeSpins && scatterCount >= 3)
            {
                result.TriggeredFreeSpins = true;
                result.FreeSpinsAwarded = 5;
            }

            double accumulatedTumbleWin = 0;
            List<BombMultiplierInfo> bombsInRound = new List<BombMultiplierInfo>();

            CollectMultiplierBombs(currentMatrix, bombsInRound);

            bool hasWins = true;
            int maxSafetyIterations = 20;
            int iteration = 0;

            while (hasWins && iteration < maxSafetyIterations)
            {
                iteration++;
                var step = new TumbleStep();
                step.MatrixBefore = (int[,])currentMatrix.Clone();

                var wins = FindStepWins(currentMatrix, betAmount);
                if (wins.Count == 0)
                {
                    hasWins = false;
                    break;
                }

                double stepWin = 0;
                var explodedPositionsSet = new HashSet<GridPosition>();

                foreach (var win in wins)
                {
                    step.Wins.Add(win);
                    stepWin += win.WinAmount;
                    foreach (var pos in win.Positions)
                    {
                        explodedPositionsSet.Add(pos);
                    }
                }

                step.StepWinAmount = stepWin;
                accumulatedTumbleWin += stepWin;
                step.ExplodedPositions = new List<GridPosition>(explodedPositionsSet);

                ApplyGravityAndSpawn(currentMatrix, explodedPositionsSet, step, isInFreeSpins);
                step.MatrixAfter = (int[,])currentMatrix.Clone();

                CollectMultiplierBombs(currentMatrix, bombsInRound);
                result.TumbleSteps.Add(step);
            }

            result.TotalBaseWin = accumulatedTumbleWin + scatterWin;

            result.MultiplierBombs = bombsInRound;
            int combinedMultiplier = 1;
            if (isInFreeSpins && bombsInRound.Count > 0)
            {
                combinedMultiplier = 0;
                foreach (var bomb in bombsInRound)
                {
                    combinedMultiplier += bomb.MultiplierValue;
                }
            }
            result.TotalMultiplier = combinedMultiplier;

            if (isInFreeSpins && bombsInRound.Count > 0 && accumulatedTumbleWin > 0)
            {
                result.FinalWinAmount = (accumulatedTumbleWin * combinedMultiplier) + scatterWin;
            }
            else
            {
                result.FinalWinAmount = result.TotalBaseWin;
            }

            return result;
        }

        private void FillRandomMatrix(int[,] matrix, bool isAnteBet, bool isBonusBuy, bool isFreeSpins)
        {
            int guaranteedScatters = isBonusBuy ? UnityEngine.Random.Range(4, 6) : 0;
            var placedScatterIndices = new HashSet<int>();

            while (placedScatterIndices.Count < guaranteedScatters)
            {
                int idx = UnityEngine.Random.Range(0, GameConfig.TOTAL_CELLS);
                placedScatterIndices.Add(idx);
            }

            for (int c = 0; c < GameConfig.COLUMNS; c++)
            {
                for (int r = 0; r < GameConfig.ROWS; r++)
                {
                    int flatIndex = c * GameConfig.ROWS + r;
                    if (placedScatterIndices.Contains(flatIndex))
                    {
                        matrix[c, r] = (int)SymbolType.Scatter;
                    }
                    else
                    {
                        matrix[c, r] = (int)GetRandomSymbol(isAnteBet, isFreeSpins);
                    }
                }
            }
        }

        private SymbolType GetRandomSymbol(bool isAnteBet, bool isFreeSpins)
        {
            int scatterWeight = isAnteBet ? 4 : 2;
            int bombWeight = isFreeSpins ? 5 : 0;

            int roll = UnityEngine.Random.Range(0, 100 + scatterWeight + bombWeight);

            if (roll < scatterWeight) return SymbolType.Scatter;
            roll -= scatterWeight;

            if (isFreeSpins && roll < bombWeight) return SymbolType.MultiplierBomb;
            if (isFreeSpins) roll -= bombWeight;

            if (roll < 16) return SymbolType.Banana;
            if (roll < 31) return SymbolType.Grapes;
            if (roll < 44) return SymbolType.Watermelon;
            if (roll < 55) return SymbolType.Plum;
            if (roll < 65) return SymbolType.Apple;
            if (roll < 76) return SymbolType.BlueCandy;
            if (roll < 85) return SymbolType.GreenCandy;
            if (roll < 93) return SymbolType.PurpleCandy;
            return SymbolType.RedHeart;
        }

        private List<SymbolWin> FindStepWins(int[,] matrix, double betAmount)
        {
            var symbolCounts = new Dictionary<SymbolType, List<GridPosition>>();

            for (int c = 0; c < GameConfig.COLUMNS; c++)
            {
                for (int r = 0; r < GameConfig.ROWS; r++)
                {
                    SymbolType symbol = (SymbolType)matrix[c, r];
                    if (symbol == SymbolType.Scatter || symbol == SymbolType.MultiplierBomb)
                    {
                        continue;
                    }

                    if (!symbolCounts.ContainsKey(symbol))
                    {
                        symbolCounts[symbol] = new List<GridPosition>();
                    }
                    symbolCounts[symbol].Add(new GridPosition(c, r));
                }
            }

            var wins = new List<SymbolWin>();
            foreach (var kvp in symbolCounts)
            {
                if (kvp.Value.Count >= 8)
                {
                    double multiplier = GameConfig.GetPayoutMultiplier(kvp.Key, kvp.Value.Count);
                    double winAmount = betAmount * multiplier;

                    var win = new SymbolWin
                    {
                        Symbol = kvp.Key,
                        Count = kvp.Value.Count,
                        Multiplier = multiplier,
                        WinAmount = winAmount,
                        Positions = kvp.Value
                    };
                    wins.Add(win);
                }
            }

            return wins;
        }

        private void ApplyGravityAndSpawn(int[,] matrix, HashSet<GridPosition> exploded, TumbleStep step, bool isFreeSpins)
        {
            for (int c = 0; c < GameConfig.COLUMNS; c++)
            {
                int writeRow = 0;
                for (int r = 0; r < GameConfig.ROWS; r++)
                {
                    var pos = new GridPosition(c, r);
                    if (!exploded.Contains(pos))
                    {
                        if (writeRow != r)
                        {
                            step.FallingSymbols.Add(new SymbolFallInfo(c, r, writeRow, (SymbolType)matrix[c, r]));
                            matrix[c, writeRow] = matrix[c, r];
                        }
                        writeRow++;
                    }
                }

                for (int r = writeRow; r < GameConfig.ROWS; r++)
                {
                    SymbolType newSym = GetRandomSymbol(false, isFreeSpins);
                    matrix[c, r] = (int)newSym;
                    step.NewSpawns.Add(new NewSpawnInfo(c, r, newSym));
                }
            }
        }

        private void CollectMultiplierBombs(int[,] matrix, List<BombMultiplierInfo> bombs)
        {
            for (int c = 0; c < GameConfig.COLUMNS; c++)
            {
                for (int r = 0; r < GameConfig.ROWS; r++)
                {
                    if (matrix[c, r] == (int)SymbolType.MultiplierBomb)
                    {
                        var pos = new GridPosition(c, r);
                        bool alreadyRecorded = false;
                        foreach (var b in bombs)
                        {
                            if (b.Position.Equals(pos))
                            {
                                alreadyRecorded = true;
                                break;
                            }
                        }

                        if (!alreadyRecorded)
                        {
                            int val = MultiplierValues[UnityEngine.Random.Range(0, MultiplierValues.Length)];
                            bombs.Add(new BombMultiplierInfo(pos, val));
                        }
                    }
                }
            }
        }
    }
}
