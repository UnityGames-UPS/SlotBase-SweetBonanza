using System;
using System.Collections.Generic;

namespace SweetBonanza.Model
{
    [Serializable]
    public struct GridPosition : IEquatable<GridPosition>
    {
        public int Column;
        public int Row;

        public GridPosition(int column, int row)
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
    public class SymbolWin
    {
        public SymbolType Symbol;
        public int Count;
        public double Multiplier;
        public double WinAmount;
        public List<GridPosition> Positions = new List<GridPosition>();
    }

    [Serializable]
    public class BombMultiplierInfo
    {
        public GridPosition Position;
        public int MultiplierValue;

        public BombMultiplierInfo(GridPosition position, int multiplierValue)
        {
            Position = position;
            MultiplierValue = multiplierValue;
        }
    }

    [Serializable]
    public class SymbolFallInfo
    {
        public int Column;
        public int FromRow;
        public int ToRow;
        public SymbolType Symbol;

        public SymbolFallInfo(int column, int fromRow, int toRow, SymbolType symbol)
        {
            Column = column;
            FromRow = fromRow;
            ToRow = toRow;
            Symbol = symbol;
        }
    }

    [Serializable]
    public class NewSpawnInfo
    {
        public int Column;
        public int TargetRow;
        public SymbolType Symbol;

        public NewSpawnInfo(int column, int targetRow, SymbolType symbol)
        {
            Column = column;
            TargetRow = targetRow;
            Symbol = symbol;
        }
    }

    [Serializable]
    public class TumbleStep
    {
        public int[,] MatrixBefore;
        public List<SymbolWin> Wins = new List<SymbolWin>();
        public double StepWinAmount;
        public List<GridPosition> ExplodedPositions = new List<GridPosition>();
        public List<SymbolFallInfo> FallingSymbols = new List<SymbolFallInfo>();
        public List<NewSpawnInfo> NewSpawns = new List<NewSpawnInfo>();
        public int[,] MatrixAfter;
    }

    [Serializable]
    public class RoundResult
    {
        public int[,] InitialMatrix;
        public List<TumbleStep> TumbleSteps = new List<TumbleStep>();
        public double TotalBaseWin;
        public List<BombMultiplierInfo> MultiplierBombs = new List<BombMultiplierInfo>();
        public int TotalMultiplier = 1;
        public double FinalWinAmount;
        public int ScatterCount;
        public bool TriggeredFreeSpins;
        public int FreeSpinsAwarded;
        public bool IsFreeSpin;
        public int RemainingFreeSpins;
    }

    #region Server JSON DTOs
    [Serializable]
    public class ServerMatrix
    {
        public int x;
        public int y;
    }

    [Serializable]
    public class ServerPayouts
    {
        public float eightToNine;
        public float tenToEleven;
        public float twelvePlus;
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
    public class ServerGameData
    {
        public ServerMatrix matrix;
        public List<float> bets;
        public float anteBetMultiplier;
        public float buyFeatureCostMultiplier;
        public float maxWinMultiplier;
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
    public class InitPayload
    {
        public double Balance = 11645.27;
        public int DefaultBetIndex = 9;
        public float[] AvailableBets = new float[]
        {
            0.2f, 0.4f, 0.6f, 0.8f, 1.0f, 1.2f, 1.4f, 1.6f, 1.8f, 2.0f,
            2.4f, 3.0f, 4.0f, 5.0f, 10.0f, 15.0f, 20.0f, 25.0f, 50.0f,
            75.0f, 100.0f, 125.0f, 150.0f, 200.0f, 250.0f, 300.0f
        };
        public float AnteBetMultiplier = 1.25f;
        public float BuyFeatureCostMultiplier = 100f;
        public float MaxWinMultiplier = 21100f;
    }
    #endregion

    public static class GameConfig
    {
        public const int COLUMNS = 6;
        public const int ROWS = 5;
        public const int TOTAL_CELLS = COLUMNS * ROWS;

        public static readonly float[] DefaultAvailableBets = new float[]
        {
            0.2f, 0.4f, 0.6f, 0.8f, 1.0f, 1.2f, 1.4f, 1.6f, 1.8f, 2.0f,
            2.4f, 3.0f, 4.0f, 5.0f, 10.0f, 15.0f, 20.0f, 25.0f, 50.0f,
            75.0f, 100.0f, 125.0f, 150.0f, 200.0f, 250.0f, 300.0f
        };

        public static double GetPayoutMultiplier(SymbolType symbol, int count)
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
