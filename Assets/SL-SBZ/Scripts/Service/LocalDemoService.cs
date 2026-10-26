using System;
using System.Collections.Generic;
using UnityEngine;
using SweetBonanza.Model;

namespace SweetBonanza.Service
{
    public class LocalDemoService : BackendService
    {
        private static readonly int[] MultiplierValues = { 2, 3, 5, 8, 10, 15, 20, 25, 50, 100 };

        private bool isInFreeSpins = false;
        private int remainingFreeSpins = 0;
        private double playerBalance = 11645.27;

        internal override void Initialize()
        {
            var payload = new InitPayload
            {
                Balance = playerBalance,
                DefaultBetIndex = 9,
                AvailableBets = GameConfig.DefaultAvailableBets,
                AnteBetMultiplier = 1.25f,
                BuyFeatureCostMultiplier = 100f,
                MaxWinMultiplier = 21100f
            };
            RaiseInitDataReceived(payload);
        }

        internal override void RequestSpin(double betAmount, bool isAnteBet, bool isBonusBuy)
        {
            double cost = isBonusBuy ? betAmount * 100.0 : (isAnteBet ? betAmount * 1.25 : betAmount);
            if (!isInFreeSpins)
            {
                playerBalance -= cost;
            }

            var result = SimulateSweetBonanzaRound(betAmount, isAnteBet, isBonusBuy);
            playerBalance += result.FinalWinAmount;

            RaiseRoundResultReceived(result);
        }

        private RoundResult SimulateSweetBonanzaRound(double betAmount, bool isAnteBet, bool isBonusBuy)
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
                    if (currentMatrix[c, r] == (int)SymbolType.Lollipop) scatterCount++;
                }
            }
            result.ScatterCount = scatterCount;

            double scatterWin = 0;
            if (scatterCount >= 4)
            {
                scatterWin = betAmount * GameConfig.GetPayoutMultiplier(SymbolType.Lollipop, scatterCount);
                if (!isInFreeSpins)
                {
                    result.TriggeredFreeSpins = true;
                    result.FreeSpinsAwarded = 10;
                    isInFreeSpins = true;
                    remainingFreeSpins = 10;
                }
                else
                {
                    remainingFreeSpins += 5;
                    result.FreeSpinsAwarded = 5;
                }
            }

            if (isInFreeSpins && !result.TriggeredFreeSpins)
            {
                remainingFreeSpins--;
                if (remainingFreeSpins <= 0) isInFreeSpins = false;
            }
            result.RemainingFreeSpins = remainingFreeSpins;

            var bombsInRound = new List<BombMultiplierInfo>();
            CollectMultiplierBombs(currentMatrix, bombsInRound);

            double accumulatedTumbleWin = 0;
            bool hasWins = true;
            int iteration = 0;

            while (hasWins && iteration < 20)
            {
                iteration++;
                var step = new TumbleStep();
                step.MatrixBefore = (int[,])currentMatrix.Clone();

                var wins = FindStepWins(currentMatrix, betAmount);
                if (wins.Count == 0) break;

                double stepWin = 0;
                var explodedPositionsSet = new HashSet<GridPosition>();

                foreach (var win in wins)
                {
                    step.Wins.Add(win);
                    stepWin += win.WinAmount;
                    foreach (var pos in win.Positions) explodedPositionsSet.Add(pos);
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
                foreach (var bomb in bombsInRound) combinedMultiplier += bomb.MultiplierValue;
            }
            result.TotalMultiplier = combinedMultiplier > 0 ? combinedMultiplier : 1;

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
                placedScatterIndices.Add(UnityEngine.Random.Range(0, GameConfig.TOTAL_CELLS));
            }

            for (int c = 0; c < GameConfig.COLUMNS; c++)
            {
                for (int r = 0; r < GameConfig.ROWS; r++)
                {
                    int flatIndex = c * GameConfig.ROWS + r;
                    if (placedScatterIndices.Contains(flatIndex)) matrix[c, r] = (int)SymbolType.Lollipop;
                    else matrix[c, r] = (int)GetRandomSymbol(isAnteBet, isFreeSpins);
                }
            }
        }

        private SymbolType GetRandomSymbol(bool isAnteBet, bool isFreeSpins)
        {
            int scatterWeight = isAnteBet ? 4 : 2;
            int bombWeight = isFreeSpins ? 5 : 0;
            int roll = UnityEngine.Random.Range(0, 100 + scatterWeight + bombWeight);

            if (roll < scatterWeight) return SymbolType.Lollipop;
            roll -= scatterWeight;

            if (isFreeSpins && roll < bombWeight) return SymbolType.MultiplierBomb;
            if (isFreeSpins) roll -= bombWeight;

            if (roll < 16) return SymbolType.Banana;
            if (roll < 31) return SymbolType.Grapes;
            if (roll < 44) return SymbolType.Watermelon;
            if (roll < 55) return SymbolType.Plum;
            if (roll < 65) return SymbolType.Apple;
            if (roll < 76) return SymbolType.BlueOval;
            if (roll < 85) return SymbolType.GreenPentagon;
            if (roll < 93) return SymbolType.PurpleSquare;
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
                    if (symbol == SymbolType.Lollipop || symbol == SymbolType.MultiplierBomb) continue;
                    if (!symbolCounts.ContainsKey(symbol)) symbolCounts[symbol] = new List<GridPosition>();
                    symbolCounts[symbol].Add(new GridPosition(c, r));
                }
            }

            var wins = new List<SymbolWin>();
            foreach (var kvp in symbolCounts)
            {
                if (kvp.Value.Count >= 8)
                {
                    double multiplier = GameConfig.GetPayoutMultiplier(kvp.Key, kvp.Value.Count);
                    wins.Add(new SymbolWin
                    {
                        Symbol = kvp.Key,
                        Count = kvp.Value.Count,
                        Multiplier = multiplier,
                        WinAmount = betAmount * multiplier,
                        Positions = kvp.Value
                    });
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
                        bool exists = false;
                        foreach (var b in bombs)
                        {
                            if (b.Position.Equals(pos)) { exists = true; break; }
                        }
                        if (!exists)
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
