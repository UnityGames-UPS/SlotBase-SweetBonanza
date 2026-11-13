using System;

namespace SweetBonanza.Model
{
    /// <summary>
    /// Single owner of session state and bet math.
    /// Written only by GameManager (Controller). Holds no UI references.
    /// </summary>
    internal class GameSession
    {
        internal GameState State = GameState.Initializing;

        internal double Balance;
        internal float BetAmount;
        internal int BetIndex;
        internal float[] AvailableBets;
        internal float AnteBetMultiplier;
        internal float BuyFeatureCostMultiplier;
        internal float MaxWinMultiplier;

        internal bool IsAnteBetActive;
        internal bool IsInFreeSpins;
        internal int RemainingFreeSpins;
        internal int TotalFreeSpinsAwarded = 10;
        internal double AccumulatedFreeSpinsWin;
        internal bool IsAutoplaying;
        internal int RemainingAutoplaySpins;

        internal bool IsIdle => State == GameState.Idle;
        internal bool IsRoundStoppable => State == GameState.Spinning || State == GameState.DroppingInitial || State == GameState.TumblingDown;
        internal bool HasBets => AvailableBets != null && AvailableBets.Length > 0;
        internal float MinBet => AvailableBets[0];
        internal float MaxBet => AvailableBets[AvailableBets.Length - 1];

        /// <summary>Bet actually charged per spin (base bet, or base bet x ante multiplier).</summary>
        internal double EffectiveBet => IsAnteBetActive ? BetAmount * AnteBetMultiplier : BetAmount;
        internal double BuyFeatureCost => BetAmount * BuyFeatureCostMultiplier;

        internal double GetSpinCost(double betAmount, bool isAnteBet, bool isBonusBuy)
        {
            return isBonusBuy ? betAmount * BuyFeatureCostMultiplier : (isAnteBet ? betAmount * AnteBetMultiplier : betAmount);
        }

        internal void SelectBetIndex(int index)
        {
            BetIndex = Math.Max(0, Math.Min(index, AvailableBets.Length - 1));
            BetAmount = AvailableBets[BetIndex];
        }

        /// <summary>Moves one step up (+1) or down (-1) the bet list. Returns false when already at the end.</summary>
        internal bool StepBet(int delta)
        {
            int next = BetIndex + delta;
            if (next < 0 || next > AvailableBets.Length - 1) return false;
            SelectBetIndex(next);
            return true;
        }

        internal void SelectMaxBet()
        {
            SelectBetIndex(AvailableBets.Length - 1);
        }

        /// <summary>Applies a free-form bet amount and snaps the index to the closest listed bet.</summary>
        internal void SelectCustomBet(double newBet)
        {
            BetAmount = (float)newBet;
            if (!HasBets) return;

            int closestIdx = 0;
            float closestDiff = float.MaxValue;
            for (int i = 0; i < AvailableBets.Length; i++)
            {
                float diff = Math.Abs(AvailableBets[i] - BetAmount);
                if (diff < closestDiff)
                {
                    closestDiff = diff;
                    closestIdx = i;
                }
            }
            BetIndex = closestIdx;
        }
    }
}
