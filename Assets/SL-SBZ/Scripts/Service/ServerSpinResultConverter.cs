using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using SweetBonanza.Model;
using UnityEngine;

namespace SweetBonanza.Service
{
    internal static class ServerSpinResultConverter
    {
        internal static bool TryConvert(string json, out RoundResult result, out string error)
        {
            result = null;
            error = null;

            ServerSpinResponse response;
            try
            {
                response = JsonConvert.DeserializeObject<ServerSpinResponse>(json);
            }
            catch (Exception ex)
            {
                error = $"JSON parse error: {ex.Message}";
                Debug.LogError($"[ServerSpinResultConverter] {error}");
                return false;
            }

            if (response == null || !response.success || response.payload == null || response.matrix == null)
            {
                error = "Server spin result unsuccessful or payload missing.";
                Debug.LogError($"[ServerSpinResultConverter] {error}");
                return false;
            }

            var payload = response.payload;
            result = new RoundResult();

            // 1. Initial matrix conversion
            // Server matrix is 5 rows (0 = top, 4 = bottom) x 6 columns (0 = left, 5 = right).
            // Client matrix is [col, row] where row 0 is bottom and row 4 is top.
            // Formula: clientCol = serverCol, clientRow = GameConfig.ROWS - 1 - serverRow.
            result.InitialMatrix = new int[GameConfig.COLUMNS, GameConfig.ROWS];
            int scatterCount = 0;

            for (int c = 0; c < GameConfig.COLUMNS; c++)
            {
                for (int r = 0; r < GameConfig.ROWS; r++)
                {
                    int serverRow = GameConfig.ROWS - 1 - r;
                    int.TryParse(response.matrix[serverRow][c], out int symId);
                    SymbolType sym = MapServerSymbol(symId);
                    result.InitialMatrix[c, r] = (int)sym;
                    if (sym == SymbolType.Lollipop) scatterCount++;
                }
            }
            result.ScatterCount = scatterCount;

            // 2. Tumble steps conversion
            int[,] currentMatrix = (int[,])result.InitialMatrix.Clone();
            if (payload.tumbles != null)
            {
                for (int t = 0; t < payload.tumbles.Count; t++)
                {
                    var tumble = payload.tumbles[t];
                    var step = new TumbleStep
                    {
                        MatrixBefore = (int[,])currentMatrix.Clone(),
                        StepWinAmount = tumble.stepWin
                    };

                    if (tumble.winningGroups != null)
                    {
                        for (int w = 0; w < tumble.winningGroups.Count; w++)
                        {
                            var wg = tumble.winningGroups[w];
                            var win = new SymbolWin
                            {
                                Symbol = MapServerSymbol(wg.symbolId),
                                Count = wg.count,
                                Multiplier = wg.multiplier,
                                WinAmount = wg.winAmount,
                                Positions = new List<GridPosition>()
                            };
                            if (wg.positions != null)
                            {
                                for (int p = 0; p < wg.positions.Count; p++)
                                {
                                    var pos = wg.positions[p];
                                    win.Positions.Add(new GridPosition(pos.col, GameConfig.ROWS - 1 - pos.row));
                                }
                            }
                            step.Wins.Add(win);
                        }
                    }

                    var explodedSet = new HashSet<GridPosition>();
                    if (tumble.symbolsRemoved != null)
                    {
                        for (int p = 0; p < tumble.symbolsRemoved.Count; p++)
                        {
                            var rem = tumble.symbolsRemoved[p];
                            var pos = new GridPosition(rem.col, GameConfig.ROWS - 1 - rem.row);
                            explodedSet.Add(pos);
                            step.ExplodedPositions.Add(pos);
                        }
                    }

                    int[,] nextMatrix = new int[GameConfig.COLUMNS, GameConfig.ROWS];
                    for (int c = 0; c < GameConfig.COLUMNS; c++)
                    {
                        int writeRow = 0;
                        for (int r = 0; r < GameConfig.ROWS; r++)
                        {
                            var pos = new GridPosition(c, r);
                            if (!explodedSet.Contains(pos))
                            {
                                if (writeRow != r)
                                {
                                    step.FallingSymbols.Add(new SymbolFallInfo(c, r, writeRow, (SymbolType)currentMatrix[c, r]));
                                }
                                nextMatrix[c, writeRow] = currentMatrix[c, r];
                                writeRow++;
                            }
                        }

                        for (int r = writeRow; r < GameConfig.ROWS; r++)
                        {
                            int serverRow = GameConfig.ROWS - 1 - r;
                            int.TryParse(tumble.resultingMatrix[serverRow][c], out int symId);
                            SymbolType sym = MapServerSymbol(symId);
                            nextMatrix[c, r] = (int)sym;
                            step.NewSpawns.Add(new NewSpawnInfo(c, r, sym));
                        }
                    }

                    step.MatrixAfter = nextMatrix;
                    currentMatrix = (int[,])nextMatrix.Clone();
                    result.TumbleSteps.Add(step);
                }
            }

            // 3. Multiplier bombs
            if (payload.multiplierBombs != null)
            {
                for (int b = 0; b < payload.multiplierBombs.Count; b++)
                {
                    var bomb = payload.multiplierBombs[b];
                    result.MultiplierBombs.Add(new BombMultiplierInfo(
                        new GridPosition(bomb.col, GameConfig.ROWS - 1 - bomb.row),
                        bomb.multiplier));
                }
            }
            result.TotalMultiplier = payload.totalMultiplier > 0 ? payload.totalMultiplier : 1;

            // 4. Totals and free spins
            result.TotalBaseWin = payload.tumbleWinSum + payload.scatterWin;
            result.FinalWinAmount = payload.grandTotalWin;
            result.PlayerBalance = response.player != null ? response.player.balance : 0;

            bool hasFs = payload.freeSpins != null;
            result.TriggeredFreeSpins = hasFs && (payload.freeSpins.triggered || payload.freeSpins.retriggers > 0);
            result.FreeSpinsAwarded = result.TriggeredFreeSpins
                ? (payload.freeSpins.triggered ? (payload.freeSpins.total > 0 ? payload.freeSpins.total : 10) : payload.freeSpins.retriggers * 5)
                : 0;
            result.IsFreeSpin = hasFs && (payload.freeSpins.inFreeSpins || payload.freeSpins.played > 0) && !payload.freeSpins.triggered;
            result.RemainingFreeSpins = hasFs ? payload.freeSpins.remaining : 0;

            return true;
        }

        internal static SymbolType MapServerSymbol(int id)
        {
            switch (id)
            {
                case 1: return SymbolType.RedHeart;
                case 2: return SymbolType.PurpleSquare;
                case 3: return SymbolType.GreenPentagon;
                case 4: return SymbolType.BlueOval;
                case 5: return SymbolType.Apple;
                case 6: return SymbolType.Plum;
                case 7: return SymbolType.Watermelon;
                case 8: return SymbolType.Grapes;
                case 9: return SymbolType.Banana;
                case 10: return SymbolType.Lollipop;
                case 11: return SymbolType.MultiplierBomb;
                default: return SymbolType.Banana;
            }
        }
    }
}
