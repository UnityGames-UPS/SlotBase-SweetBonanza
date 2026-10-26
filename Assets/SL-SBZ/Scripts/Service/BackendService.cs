using System;
using UnityEngine;
using SweetBonanza.Model;

namespace SweetBonanza.Service
{
    public abstract class BackendService : MonoBehaviour
    {
        internal event Action<InitPayload> OnInitDataReceived;
        internal event Action<RoundResult> OnRoundResultReceived;
        internal event Action<string> OnErrorOccurred;

        internal abstract void Initialize();
        internal abstract void RequestSpin(double betAmount, bool isAnteBet, bool isBonusBuy);

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
    }
}
