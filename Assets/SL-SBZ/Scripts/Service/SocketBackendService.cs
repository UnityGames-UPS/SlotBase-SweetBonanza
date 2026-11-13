using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using SweetBonanza.Model;

namespace SweetBonanza.Service
{
    internal class SocketBackendService : BackendService
    {
        [SerializeField] private SocketIOManager socketManager;
        [SerializeField] private float spinTimeoutSeconds = 15f;

        private bool isSpinPending;
        private Coroutine timeoutCoroutine;

        private void OnEnable()
        {
            if (socketManager != null)
            {
                socketManager.OnInitDataReceived += HandleSocketInitData;
                socketManager.OnSpinResultReceived += HandleSocketSpinResult;
                socketManager.OnBalanceSyncReceived += HandleSocketBalanceSync;
                socketManager.OnSocketDisconnectedEvent += HandleSocketDisconnected;
                socketManager.OnSocketErrorEvent += HandleSocketError;
            }
        }

        private void OnDisable()
        {
            StopSpinTimeout();
            isSpinPending = false;

            if (socketManager != null)
            {
                socketManager.OnInitDataReceived -= HandleSocketInitData;
                socketManager.OnSpinResultReceived -= HandleSocketSpinResult;
                socketManager.OnBalanceSyncReceived -= HandleSocketBalanceSync;
                socketManager.OnSocketDisconnectedEvent -= HandleSocketDisconnected;
                socketManager.OnSocketErrorEvent -= HandleSocketError;
            }
        }

        internal override void Initialize()
        {
            Debug.Log("[SocketBackendService] Initializing: Waiting for init data from socket server...");
        }

        internal override void RequestSpin(int betIndex, double betAmount, bool isAnteBet, bool isBonusBuy, bool isFreeSpin)
        {
            if (isSpinPending)
            {
                RaiseErrorOccurred("Spin already in progress.");
                return;
            }

            if (socketManager == null || !socketManager.isConnected || !socketManager.isInitialized)
            {
                RaiseErrorOccurred("Not connected to server.");
                return;
            }

            isSpinPending = true;
            StartSpinTimeout();
            socketManager.SendSpinRequest(betIndex, betAmount, isAnteBet, isBonusBuy, isFreeSpin);
        }

        private void HandleSocketInitData(string json)
        {
            try
            {
                var initData = JsonConvert.DeserializeObject<ServerInitData>(json);
                if (initData == null || initData.gameData == null || initData.player == null ||
                    initData.uiData == null || initData.uiData.paytable == null || initData.uiData.paytable.Count == 0)
                {
                    Debug.LogError("[SocketBackendService] Failed to read game data from server: invalid payload structure.");
                    RaiseErrorOccurred("Failed to read game data from server.");
                    return;
                }

                var gd = initData.gameData;
                if (gd.bets == null || gd.bets.Count == 0 ||
                    gd.anteBetMultiplier <= 0f ||
                    gd.buyFeatureCostMultiplier <= 0f ||
                    gd.maxWinMultiplier <= 0f)
                {
                    Debug.LogError("[SocketBackendService] Failed to read game data from server: invalid bets or multipliers.");
                    RaiseErrorOccurred("Failed to read game data from server.");
                    return;
                }

                var payouts = new Dictionary<SymbolType, float[]>();
                for (int i = 0; i < initData.uiData.paytable.Count; i++)
                {
                    var item = initData.uiData.paytable[i];
                    if (item.payouts == null) continue;

                    SymbolType sym = ServerSpinResultConverter.MapServerSymbol(item.id);
                    bool isScatter = item.group == "scatter";
                    string kLow = isScatter ? "4" : "8-9";
                    string kMid = isScatter ? "5" : "10-11";
                    string kHigh = isScatter ? "6+" : "12+";

                    item.payouts.TryGetValue(kLow, out float low);
                    item.payouts.TryGetValue(kMid, out float mid);
                    item.payouts.TryGetValue(kHigh, out float high);

                    payouts[sym] = new float[] { low, mid, high };
                }

                var payload = new InitPayload
                {
                    Balance = initData.player.balance,
                    DefaultBetIndex = 0,
                    AvailableBets = gd.bets.ToArray(),
                    AnteBetMultiplier = gd.anteBetMultiplier,
                    BuyFeatureCostMultiplier = gd.buyFeatureCostMultiplier,
                    MaxWinMultiplier = gd.maxWinMultiplier,
                    Payouts = payouts
                };

                RaiseInitDataReceived(payload);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SocketBackendService] Failed to parse init data: {ex.Message}");
                RaiseErrorOccurred("Failed to read game data from server.");
            }
        }

        private void HandleSocketSpinResult(string json)
        {
            if (!isSpinPending)
            {
                Debug.LogWarning("[SocketBackendService] Received spin result but no spin was pending. Ignoring.");
                return;
            }

            isSpinPending = false;
            StopSpinTimeout();

            if (ServerSpinResultConverter.TryConvert(json, out RoundResult result, out string error))
            {
                RaiseRoundResultReceived(result);
            }
            else
            {
                Debug.LogError($"[SocketBackendService] Spin result conversion failed: {error}");
                RaiseErrorOccurred(error);
            }
        }

        private void HandleSocketBalanceSync(double balance)
        {
            RaiseBalanceSyncReceived(balance);
        }

        private void HandleSocketDisconnected()
        {
            if (isSpinPending)
            {
                isSpinPending = false;
                StopSpinTimeout();
                RaiseErrorOccurred("Connection lost during spin.");
            }
        }

        private void HandleSocketError(string error)
        {
            if (isSpinPending)
            {
                isSpinPending = false;
                StopSpinTimeout();
                RaiseErrorOccurred("Connection lost during spin.");
            }
        }

        private void StartSpinTimeout()
        {
            StopSpinTimeout();
            timeoutCoroutine = StartCoroutine(SpinTimeoutRoutine());
        }

        private void StopSpinTimeout()
        {
            if (timeoutCoroutine != null)
            {
                StopCoroutine(timeoutCoroutine);
                timeoutCoroutine = null;
            }
        }

        private IEnumerator SpinTimeoutRoutine()
        {
            yield return new WaitForSecondsRealtime(spinTimeoutSeconds);
            if (isSpinPending)
            {
                isSpinPending = false;
                timeoutCoroutine = null;
                RaiseErrorOccurred("No response from server.");
            }
        }
    }
}

