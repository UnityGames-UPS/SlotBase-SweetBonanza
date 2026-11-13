using System;
using UnityEngine;
using SweetBonanza.Model;

namespace SweetBonanza.Service
{
    internal abstract class BackendService : MonoBehaviour
    {
        internal event Action<InitPayload> OnInitDataReceived;
        internal event Action<RoundResult> OnRoundResultReceived;
        internal event Action<string> OnErrorOccurred;
        internal event Action<double> OnBalanceSyncReceived;

        internal abstract void Initialize();
        internal abstract void RequestSpin(int betIndex, double betAmount, bool isAnteBet, bool isBonusBuy, bool isFreeSpin);

        protected void RaiseInitDataReceived(InitPayload payload)
        {
            OnInitDataReceived?.Invoke(payload);
        }

        protected void RaiseRoundResultReceived(RoundResult result)
        {
            OnRoundResultReceived?.Invoke(result);
        }

        protected void RaiseErrorOccurred(string error)
        {
            OnErrorOccurred?.Invoke(error);
        }

        protected void RaiseBalanceSyncReceived(double balance)
        {
            OnBalanceSyncReceived?.Invoke(balance);
        }
    }
}
