using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SweetBonanza.Model
{
    [Serializable]
    internal struct GridPosition : IEquatable<GridPosition>
    {
        internal int Column;
        internal int Row;

        internal GridPosition(int column, int row)
        {
            Column = column;
            Row = row;
        }

        internal bool Matches(GridPosition other)
        {
            return Column == other.Column && Row == other.Row;
        }

        bool IEquatable<GridPosition>.Equals(GridPosition other)
        {
            return Matches(other);
        }
    }

    [Serializable]
    internal class SymbolWin
    {
        internal SymbolType Symbol;
        internal int Count;
        internal double Multiplier;
        internal double WinAmount;
        internal List<GridPosition> Positions = new List<GridPosition>();
    }

    [Serializable]
    internal class BombMultiplierInfo
    {
        internal GridPosition Position;
        internal int MultiplierValue;

        internal BombMultiplierInfo(GridPosition position, int multiplierValue)
        {
            Position = position;
            MultiplierValue = multiplierValue;
        }
    }

    [Serializable]
    internal class SymbolFallInfo
    {
        internal int Column;
        internal int FromRow;
        internal int ToRow;
        internal SymbolType Symbol;

        internal SymbolFallInfo(int column, int fromRow, int toRow, SymbolType symbol)
        {
            Column = column;
            FromRow = fromRow;
            ToRow = toRow;
            Symbol = symbol;
        }
    }

    [Serializable]
    internal class NewSpawnInfo
    {
        internal int Column;
        internal int TargetRow;
        internal SymbolType Symbol;

        internal NewSpawnInfo(int column, int targetRow, SymbolType symbol)
        {
            Column = column;
            TargetRow = targetRow;
            Symbol = symbol;
        }
    }

    [Serializable]
    internal class TumbleStep
    {
        internal int[,] MatrixBefore;
        internal List<SymbolWin> Wins = new List<SymbolWin>();
        internal double StepWinAmount;
        internal List<GridPosition> ExplodedPositions = new List<GridPosition>();
        internal List<SymbolFallInfo> FallingSymbols = new List<SymbolFallInfo>();
        internal List<NewSpawnInfo> NewSpawns = new List<NewSpawnInfo>();
        internal int[,] MatrixAfter;
    }

    [Serializable]
    internal class RoundResult
    {
        internal int[,] InitialMatrix;
        internal List<TumbleStep> TumbleSteps = new List<TumbleStep>();
        internal double TotalBaseWin;
        internal List<BombMultiplierInfo> MultiplierBombs = new List<BombMultiplierInfo>();
        internal int TotalMultiplier = 1;
        internal double FinalWinAmount;
        internal double PlayerBalance;
        internal int ScatterCount;
        internal bool TriggeredFreeSpins;
        internal int FreeSpinsAwarded;
        internal bool IsFreeSpin;
        internal int RemainingFreeSpins;
    }

    #region Server JSON DTOs
#pragma warning disable 0649 // filled by JSON deserialization
    [Serializable]
    internal class ServerMatrix
    {
        [JsonProperty] internal int x;
        [JsonProperty] internal int y;
    }

    [Serializable]
    internal class ServerPayouts
    {
        [JsonProperty] internal float eightToNine;
        [JsonProperty] internal float tenToEleven;
        [JsonProperty] internal float twelvePlus;
    }

    [Serializable]
    internal class ServerPaytableItem
    {
        [JsonProperty] internal int id;
        [JsonProperty] internal string name;
        [JsonProperty] internal string group;
        [JsonProperty] internal ServerPayouts payouts;
    }

    [Serializable]
    internal class ServerUIData
    {
        [JsonProperty] internal List<ServerPaytableItem> paytable;
    }

    [Serializable]
    internal class ServerGameData
    {
        [JsonProperty] internal ServerMatrix matrix;
        [JsonProperty] internal List<float> bets;
        [JsonProperty] internal float anteBetMultiplier;
        [JsonProperty] internal float buyFeatureCostMultiplier;
        [JsonProperty] internal float maxWinMultiplier;
    }

    [Serializable]
    internal class ServerPlayer
    {
        [JsonProperty] internal double balance;
    }

    [Serializable]
    internal class ServerInitData
    {
        [JsonProperty] internal string id;
        [JsonProperty] internal ServerGameData gameData;
        [JsonProperty] internal ServerUIData uiData;
        [JsonProperty] internal ServerPlayer player;
    }

#pragma warning restore 0649

    [Serializable]
    internal class InitPayload
    {
        internal double Balance = 11645.27;
        internal int DefaultBetIndex = 9;
        internal float[] AvailableBets = GameConfig.DefaultAvailableBets;
        internal float AnteBetMultiplier = 1.25f;
        internal float BuyFeatureCostMultiplier = 100f;
        internal float MaxWinMultiplier = 21100f;
    }
    #endregion

    internal static class GameConfig
    {
        internal const int COLUMNS = 6;
        internal const int ROWS = 5;
        internal const int TOTAL_CELLS = COLUMNS * ROWS;

        internal static readonly float[] DefaultAvailableBets = new float[]
        {
            0.2f, 0.4f, 0.6f, 0.8f, 1.0f, 1.2f, 1.4f, 1.6f, 1.8f, 2.0f,
            2.4f, 3.0f, 4.0f, 5.0f, 10.0f, 15.0f, 20.0f, 25.0f, 50.0f,
            75.0f, 100.0f, 125.0f, 150.0f, 200.0f, 250.0f, 300.0f
        };

        internal static double GetPayoutMultiplier(SymbolType symbol, int count)
        {
            if (count < 8 && symbol != SymbolType.Lollipop) return 0.0;

            switch (symbol)
            {
                case SymbolType.RedHeart:
                    if (count >= 12) return 50.0;
                    if (count >= 10) return 25.0;
                    return 10.0;

                case SymbolType.PurpleSquare:
                    if (count >= 12) return 25.0;
                    if (count >= 10) return 10.0;
                    return 2.5;

                case SymbolType.GreenPentagon:
                    if (count >= 12) return 15.0;
                    if (count >= 10) return 5.0;
                    return 2.0;

                case SymbolType.BlueOval:
                    if (count >= 12) return 12.0;
                    if (count >= 10) return 2.0;
                    return 1.5;

                case SymbolType.Apple:
                    if (count >= 12) return 10.0;
                    if (count >= 10) return 1.5;
                    return 1.0;

                case SymbolType.Plum:
                    if (count >= 12) return 8.0;
                    if (count >= 10) return 1.2;
                    return 0.8;

                case SymbolType.Watermelon:
                    if (count >= 12) return 5.0;
                    if (count >= 10) return 1.0;
                    return 0.5;

                case SymbolType.Grapes:
                    if (count >= 12) return 4.0;
                    if (count >= 10) return 0.9;
                    return 0.4;

                case SymbolType.Banana:
                    if (count >= 12) return 2.0;
                    if (count >= 10) return 0.75;
                    return 0.25;

                case SymbolType.Lollipop:
                    if (count >= 6) return 100.0;
                    if (count == 5) return 5.0;
                    if (count == 4) return 3.0;
                    return 0.0;

                default:
                    return 0.0;
            }
        }
    }
}
