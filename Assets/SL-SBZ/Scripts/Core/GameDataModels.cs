using System;
using System.Collections.Generic;

namespace SweetBonanza.Core
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

        public bool Equals(GridPosition other)
        {
            return Column == other.Column && Row == other.Row;
        }

        public override bool Equals(object obj)
        {
            return obj is GridPosition other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Column * 397) ^ Row;
            }
        }

        public override string ToString()
        {
            return $"({Column}, {Row})";
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
        internal int ScatterCount;
        internal bool TriggeredFreeSpins;
        internal int FreeSpinsAwarded;
        internal bool IsFreeSpin;
        internal int RemainingFreeSpins;
    }

    [Serializable]
    public class ServerMatrix
    {
        public int x;
        public int y;
    }

    [Serializable]
    public class ServerGameData
    {
        public ServerMatrix matrix;
        public List<double> bets;
        public double anteBetMultiplier;
        public double buyFeatureCostMultiplier;
        public double maxWinMultiplier;
    }

    [Serializable]
    public class ServerPayouts
    {
        public double tier8_9;
        public double tier10_11;
        public double tier12_plus;
        public double tier4;
        public double tier5;
        public double tier6_plus;
    }

    [Serializable]
    public class ServerPaytableItem
    {
        public int id;
        public string name;
        public string group;
        public ServerPayouts payouts;
    }

    [Serializable]
    public class ServerUIData
    {
        public List<ServerPaytableItem> paytable;
    }

    [Serializable]
    public class ServerPlayer
    {
        public double balance;
    }

    [Serializable]
    public class ServerInitData
    {
        public string id;
        public ServerGameData gameData;
        public ServerUIData uiData;
        public ServerPlayer player;
    }

    [Serializable]
    internal class InitPayload
    {
        internal double Balance;
        internal int DefaultBetIndex;
        internal double[] AvailableBets;
        internal int MatrixColumns = 6;
        internal int MatrixRows = 5;
        internal double AnteBetMultiplier = 1.25;
        internal double BuyFeatureCostMultiplier = 100.0;
        internal double MaxWinMultiplier = 21100.0;
        internal List<ServerPaytableItem> Paytable = new List<ServerPaytableItem>();
    }

    internal static class GameConfig
    {
        internal const int COLUMNS = 6;
        internal const int ROWS = 5;
        internal const int TOTAL_CELLS = COLUMNS * ROWS;

        internal static readonly double[] DefaultAvailableBets =
        {
            0.20, 0.40, 0.60, 0.80, 1.00, 1.20, 1.40, 1.60, 1.80, 2.00,
            2.40, 3.00, 4.00, 5.00, 10.00, 15.00, 20.00, 25.00, 50.00,
            75.00, 100.00, 125.00, 150.00, 200.00, 250.00, 300.00
        };

        internal static double GetPayoutMultiplier(SymbolType symbol, int count)
        {
            if (count < 8 && symbol != SymbolType.Scatter) return 0.0;

            switch (symbol)
            {
                case SymbolType.RedHeart:
                    if (count >= 12) return 50.0;
                    if (count >= 10) return 25.0;
                    return 10.0;

                case SymbolType.PurpleCandy:
                    if (count >= 12) return 25.0;
                    if (count >= 10) return 5.0;
                    return 2.5;

                case SymbolType.GreenCandy:
                    if (count >= 12) return 15.0;
                    if (count >= 10) return 5.0;
                    return 2.0;

                case SymbolType.BlueCandy:
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

                case SymbolType.Scatter:
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
