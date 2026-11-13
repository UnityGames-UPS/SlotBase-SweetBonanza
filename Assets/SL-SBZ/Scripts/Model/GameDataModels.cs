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
    internal class ServerPaytableItem
    {
        [JsonProperty] internal int id;
        [JsonProperty] internal string name;
        [JsonProperty] internal string group;
        [JsonProperty] internal Dictionary<string, float> payouts;
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

    [Serializable]
    internal class ServerGridPosition
    {
        [JsonProperty] internal int row;
        [JsonProperty] internal int col;
    }

    [Serializable]
    internal class ServerWinningGroup
    {
        [JsonProperty] internal int symbolId;
        [JsonProperty] internal string symbolName;
        [JsonProperty] internal int count;
        [JsonProperty] internal double multiplier;
        [JsonProperty] internal double winAmount;
        [JsonProperty] internal List<ServerGridPosition> positions;
    }

    [Serializable]
    internal class ServerBomb
    {
        [JsonProperty] internal int row;
        [JsonProperty] internal int col;
        [JsonProperty] internal int multiplier;
    }

    [Serializable]
    internal class ServerSymbolFill
    {
        [JsonProperty] internal int row;
        [JsonProperty] internal int col;
        [JsonProperty] internal int symbolId;
    }

    [Serializable]
    internal class ServerTumbleStep
    {
        [JsonProperty] internal int stepIndex;
        [JsonProperty] internal List<ServerWinningGroup> winningGroups;
        [JsonProperty] internal double stepWin;
        [JsonProperty] internal List<ServerGridPosition> symbolsRemoved;
        [JsonProperty] internal List<ServerSymbolFill> symbolsToFill;
        [JsonProperty] internal List<List<string>> resultingMatrix;
        [JsonProperty] internal List<ServerBomb> bombsAdded;
    }

    [Serializable]
    internal class ServerFreeSpinsData
    {
        [JsonProperty] internal bool triggered;
        [JsonProperty] internal int retriggers;
        [JsonProperty] internal bool inFreeSpins;
        [JsonProperty] internal int remaining;
        [JsonProperty] internal int total;
        [JsonProperty] internal int played;
        [JsonProperty] internal double accumulatedWin;
    }

    [Serializable]
    internal class ServerSpinPayload
    {
        [JsonProperty] internal double winAmount;
        [JsonProperty] internal double betAmount;
        [JsonProperty] internal double baseBet;
        [JsonProperty] internal bool isAnteBet;
        [JsonProperty] internal bool isBuyFeature;
        [JsonProperty] internal List<ServerTumbleStep> tumbles;
        [JsonProperty] internal List<ServerBomb> multiplierBombs;
        [JsonProperty] internal int totalMultiplier;
        [JsonProperty] internal double tumbleWinSum;
        [JsonProperty] internal double totalWin;
        [JsonProperty] internal double scatterWin;
        [JsonProperty] internal double grandTotalWin;
        [JsonProperty] internal bool isMaxWinCapped;
        [JsonProperty] internal ServerFreeSpinsData freeSpins;
    }

    [Serializable]
    internal class ServerSpinResponse
    {
        [JsonProperty] internal bool success;
        [JsonProperty] internal string id;
        [JsonProperty] internal List<List<string>> matrix;
        [JsonProperty] internal ServerSpinPayload payload;
        [JsonProperty] internal ServerPlayer player;
    }

#pragma warning restore 0649

    [Serializable]
    internal class InitPayload
    {
        internal double Balance;
        internal int DefaultBetIndex;
        internal float[] AvailableBets;
        internal float AnteBetMultiplier;
        internal float BuyFeatureCostMultiplier;
        internal float MaxWinMultiplier;
        internal Dictionary<SymbolType, float[]> Payouts;
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

        internal static readonly Dictionary<SymbolType, float[]> DemoPayouts = new Dictionary<SymbolType, float[]>
        {
            { SymbolType.RedHeart, new float[] { 20f, 50f, 100f } },
            { SymbolType.PurpleSquare, new float[] { 5f, 20f, 50f } },
            { SymbolType.GreenPentagon, new float[] { 4f, 10f, 30f } },
            { SymbolType.BlueOval, new float[] { 3f, 4f, 24f } },
            { SymbolType.Apple, new float[] { 2f, 3f, 20f } },
            { SymbolType.Plum, new float[] { 1.6f, 2.4f, 16f } },
            { SymbolType.Watermelon, new float[] { 1f, 2f, 10f } },
            { SymbolType.Grapes, new float[] { 0.8f, 1.8f, 8f } },
            { SymbolType.Banana, new float[] { 0.5f, 1.5f, 4f } },
            { SymbolType.Lollipop, new float[] { 6f, 10f, 200f } }
        };

        internal static double GetPayoutMultiplier(SymbolType symbol, int count)
        {
            if (!DemoPayouts.TryGetValue(symbol, out var tiers) || tiers == null || tiers.Length < 3)
            {
                return 0.0;
            }

            if (symbol == SymbolType.Lollipop)
            {
                if (count >= 6) return tiers[2];
                if (count == 5) return tiers[1];
                if (count == 4) return tiers[0];
                return 0.0;
            }

            if (count >= 12) return tiers[2];
            if (count >= 10) return tiers[1];
            if (count >= 8) return tiers[0];
            return 0.0;
        }
    }
}
