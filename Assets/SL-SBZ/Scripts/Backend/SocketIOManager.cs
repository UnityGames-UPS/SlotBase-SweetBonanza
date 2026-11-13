using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Best.SocketIO;
using Best.SocketIO.Events;
using Newtonsoft.Json;

internal class SocketIOManager : MonoBehaviour
{
    internal static SocketIOManager Instance { get; private set; }

    [Header("Network Configuration")]
    [SerializeField] private string testToken = "e0b9273284d34388bcefa1356074617b";
    [SerializeField] protected string testSocketURL = "https://devrealtime.dingdinghouse.com/";
    [SerializeField] protected string nameSpace = "playground";

    [Header("References")]
    [SerializeField] internal JSFunctCalls JSManager;
    [SerializeField] private PopupManager popupManager;

    [Header("Debug Settings")]
    [SerializeField] private bool enablePingDebug = false;

    // Events for decoupled listeners (GameManager, UIManager, etc.)
    internal event Action<string> OnInitDataReceived;
    internal event Action<string> OnSpinResultReceived;
    internal event Action<double> OnBalanceSyncReceived;
    internal event Action<int> OnPingLatencyUpdated;
    internal event Action OnSocketConnectedEvent;
    internal event Action OnSocketDisconnectedEvent;
    internal event Action<string> OnSocketErrorEvent;
    internal event Action OnAnotherDeviceLogin;

    private SocketManager socketManager;
    private Socket gameSocket;

    private string authToken;
    private string socketURL;

    internal bool isConnected;
    internal bool isInitialized;
    internal bool isExiting;
    private bool isDestroyed;
    private bool socketSetupStarted;

    private Coroutine pingCoroutine;
    private float pingSendTime;
    private const float PING_INTERVAL = 2f;
    private static readonly WaitForSeconds WaitPing = new WaitForSeconds(PING_INTERVAL);

    #region Initialization

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        isInitialized = false;
        isConnected = false;
        isExiting = false;
        isDestroyed = false;
        socketSetupStarted = false;
    }

    private void Start()
    {
        RequestAuthToken();
    }

    private void RequestAuthToken()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (JSManager != null)
        {
            JSManager.RegisterVisibilityListener(gameObject.name);
            JSManager.SendCustomMessage("authToken");
        }
#else
        authToken = testToken;
        socketURL = testSocketURL;
        InitializeSocket();
#endif
    }

    private void ReceiveAuthToken(string jsonData)
    {
        Debug.Log("[SocketIO] Auth received");

        try
        {
            if (string.IsNullOrEmpty(jsonData)) return;

            var authData = JsonUtility.FromJson<AuthTokenData>(jsonData);
            string incomingToken = authData.cookie;
            string incomingSocketURL = authData.socketURL;
            string incomingNameSpace = !string.IsNullOrEmpty(authData.nameSpace) ? authData.nameSpace : nameSpace;

            if (socketSetupStarted && authToken == incomingToken && socketURL == incomingSocketURL)
            {
                Debug.LogWarning("[SocketIO] Matching auth token received, bypassing re-initialization.");
                return;
            }

            Debug.Log("[SocketIO] Connecting with received auth credentials.");
            authToken = incomingToken;
            socketURL = incomingSocketURL;
            nameSpace = incomingNameSpace;

            InitializeSocket();
        }
        catch (Exception e)
        {
            Debug.LogError($"[SocketIO] Auth parse failed: {e.Message}");
        }
    }

    private void InitializeSocket()
    {
        socketSetupStarted = true;
        StopPingRoutine();

        if (socketManager != null)
        {
            try { socketManager.Close(); } catch { }
            socketManager = null;
        }

        isInitialized = false;
        isConnected = false;
        isExiting = false;

        SocketOptions options = new SocketOptions
        {
            AutoConnect = false,
            Reconnection = false,
            Timeout = TimeSpan.FromSeconds(10),
            ConnectWith = Best.SocketIO.Transports.TransportTypes.WebSocket
        };

        options.Auth = (SocketManager manager, Socket socket) => new { token = authToken };

#if UNITY_EDITOR
        try
        {
            Uri uri = new Uri(testSocketURL);
            var hostSettings = Best.HTTP.Shared.HTTPManager.PerHostSettings.Get(uri.Host);
            hostSettings.TLSSettings.TLSHandler = Best.HTTP.Hosts.Settings.TLSHandlers.Framework;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[SocketIO] Could not configure Framework TLS: {ex.Message}");
        }
        socketManager = new SocketManager(new Uri(testSocketURL), options);
#else
        socketManager = new SocketManager(new Uri(socketURL), options);
#endif

        string ns = nameSpace;
        if (!string.IsNullOrEmpty(ns) && !ns.StartsWith("/"))
        {
            ns = "/" + ns;
        }

        gameSocket = string.IsNullOrEmpty(ns)
            ? socketManager.Socket
            : socketManager.GetSocket(ns);

        gameSocket.On<ConnectResponse>(SocketIOEventTypes.Connect, OnSocketConnected);
        gameSocket.On(SocketIOEventTypes.Disconnect, OnSocketDisconnected);
        gameSocket.On<Error>(SocketIOEventTypes.Error, OnSocketError);

        if (socketManager.Socket != gameSocket)
        {
            socketManager.Socket.On<Error>(SocketIOEventTypes.Error, OnSocketError);
        }

        gameSocket.On<string>("game:init", OnInitReceived);
        gameSocket.On<string>("result", OnResultReceived);
        gameSocket.On<string>("pong", OnPongReceived);
        gameSocket.On("pong", OnPongReceivedNoArgs);
        gameSocket.On<string>("AnotherDevice", OnAnotherDevice);
        gameSocket.On<string>("balance:sync", OnBalanceSyncReceivedInternal);

        socketManager.Open();
    }

    #endregion

    #region Socket Events

    private void OnSocketConnected(ConnectResponse resp)
    {
        Debug.Log("[SocketIO] Connected successfully");

        isConnected = true;
        pingSendTime = Time.realtimeSinceStartup;

        if (popupManager != null)
        {
            popupManager.CloseReconnectionPopup();
        }

        StartPingRoutine();
        SendPing();

        OnSocketConnectedEvent?.Invoke();
    }

    private void OnSocketDisconnected()
    {
        Debug.Log("[SocketIO] Disconnected");

        isConnected = false;
        StopPingRoutine();

        if (isDestroyed) return;

        if (isExiting)
        {
            if (popupManager != null)
            {
                popupManager.ShowLoadingPopup(0f);
            }
        }
        else
        {
            if (popupManager != null)
            {
                popupManager.ShowDisconnectionPopup("Disconnected from server. Please check your network connection.");
            }
        }

        OnSocketDisconnectedEvent?.Invoke();
    }

    private void OnSocketError(Error err)
    {
        Debug.LogError($"[SocketIO] Error: {err.message}");

        if (!string.IsNullOrEmpty(err.message) && err.message.Contains("Session expired"))
        {
            Debug.LogWarning("[SocketIO] Session expired detected");
            OnSocketDisconnected();
            if (popupManager != null)
            {
                popupManager.ShowErrorPopup("Session Expired", "Your session has expired. Please reload the game.", true);
            }
#if UNITY_WEBGL && !UNITY_EDITOR
            JSManager?.SendCustomMessage("session_expired");
#endif
        }
        else
        {
            // Do NOT send "error" message to JSManager/platform, as that triggers the fatal
            // platform black screen ("Launch Error / Unity Error: Unknown error") on the React host.
            // Instead, gracefully display the in-game disconnection or error modal so Unity stays alive.
            if (!isConnected)
            {
                if (popupManager != null)
                {
                    popupManager.ShowDisconnectionPopup("Disconnected from server. Please check your network connection.");
                }
            }
            else
            {
                if (popupManager != null)
                {
                    popupManager.ShowServerError(string.IsNullOrEmpty(err.message) ? "A network error occurred." : err.message);
                }
            }

            OnSocketErrorEvent?.Invoke(err.message);
        }
    }

    private void OnInitReceived(string jsonData)
    {
        Debug.Log($"[SocketIO] Init received: {jsonData}");

        try
        {
            isInitialized = true;
            OnInitDataReceived?.Invoke(jsonData);

#if UNITY_WEBGL && !UNITY_EDITOR
            JSManager?.SendCustomMessage("OnEnter");
#endif
        }
        catch (Exception e)
        {
            Debug.LogError($"[SocketIO] Init parse failed: {e.Message}");
            if (popupManager != null)
            {
                popupManager.ShowServerError("Failed to parse game initialization data.");
            }
            OnSocketErrorEvent?.Invoke("Failed to parse game initialization data.");
        }
    }

    private void OnResultReceived(string jsonData)
    {
        if (!jsonData.Contains("\"id\":\"spinResult\"") && !jsonData.Contains("\"id\":\"ResultData\""))
        {
            return;
        }

        Debug.Log($"[SocketIO] Result received: {jsonData}");
        OnSpinResultReceived?.Invoke(jsonData);
    }

    private void OnAnotherDevice(string data)
    {
        Debug.Log("[SocketIO] Another device login");
        if (popupManager != null)
        {
            popupManager.ShowAnotherDeviceError();
        }
        OnAnotherDeviceLogin?.Invoke();
    }

    private void OnBalanceSyncReceivedInternal(string jsonData)
    {
        Debug.Log($"[SocketIO] Balance Sync received: {jsonData}");

        try
        {
            var syncData = JsonConvert.DeserializeObject<BalanceSyncData>(jsonData);
            if (syncData != null)
            {
                OnBalanceSyncReceived?.Invoke(syncData.balance);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[SocketIO] Balance Sync parse failed: {e.Message}");
        }
    }

    #endregion

    #region Spin Requests

    internal void SendSpinRequest(int betIndex, double betAmount = 0, bool isAnteBet = false, bool isBonusBuy = false, bool isFreeSpin = false)
    {
        Debug.Log($"[SocketIO] Spin request: betIndex={betIndex}, betAmount={betAmount}, isAnte={isAnteBet}, isBonusBuy={isBonusBuy}, isFreeSpin={isFreeSpin}");

        var request = new SpinRequest
        {
            type = "SPIN",
            payload = new SpinPayload
            {
                betIndex = betIndex,
                betAmount = betAmount,
                isFreeSpin = isFreeSpin,
                isAnteBet = isAnteBet,
                isBonusBuy = isBonusBuy
            }
        };

        string json = JsonUtility.ToJson(request);
        if (gameSocket != null && isConnected)
        {
            gameSocket.Emit("request", json);
        }
        else
        {
            Debug.LogWarning("[SocketIO] Cannot send spin request: Socket is not connected.");
        }
    }

    #endregion

    #region Ping / Pong Latency Check

    private void StartPingRoutine()
    {
        if (pingCoroutine != null)
            StopCoroutine(pingCoroutine);

        pingCoroutine = StartCoroutine(PingRoutine());
    }

    private void StopPingRoutine()
    {
        if (pingCoroutine != null)
        {
            StopCoroutine(pingCoroutine);
            pingCoroutine = null;
        }
    }

    private IEnumerator PingRoutine()
    {
        while (isConnected)
        {
            yield return WaitPing;
            SendPing();
        }
    }

    private void SendPing()
    {
        if (gameSocket != null && isConnected)
        {
            pingSendTime = Time.realtimeSinceStartup;
            gameSocket.Emit("ping");

            if (enablePingDebug)
            {
                Debug.Log($"[SocketIO] Ping sent at {pingSendTime:F3}s");
            }
        }
    }

    private void OnPongReceivedNoArgs()
    {
        OnPongReceived(string.Empty);
    }

    private void OnPongReceived(string data)
    {
        if (pingSendTime > 0f)
        {
            float rtt = Time.realtimeSinceStartup - pingSendTime;
            int pingMs = Mathf.Max(1, Mathf.RoundToInt(rtt * 1000f));
            OnPingLatencyUpdated?.Invoke(pingMs);

            if (enablePingDebug)
            {
                Debug.Log($"[SocketIO] Pong received | Latency: {pingMs} ms");
            }
        }
    }

    #endregion

    #region Cleanup & Teardown

    internal void CloseSocket()
    {
        if (isDestroyed) return;

        isExiting = true;

        if (popupManager != null && !popupManager.IsLoadingPopupActive())
        {
            popupManager.ShowLoadingPopup(0f);
        }

        StopPingRoutine();

        if (socketManager != null)
        {
            try { socketManager.Close(); } catch { }
            socketManager = null;
        }

        isConnected = false;
        socketSetupStarted = false;

#if UNITY_WEBGL && !UNITY_EDITOR
        JSManager?.SendCustomMessage("OnExit");
#endif
    }

    private void OnDisable()
    {
        StopPingRoutine();
    }

    private void OnApplicationQuit()
    {
        isDestroyed = true;
    }

    private void OnDestroy()
    {
        isDestroyed = true;
        StopPingRoutine();

        if (socketManager != null)
        {
            try { socketManager.Close(); } catch { }
            socketManager = null;
        }
        isConnected = false;

        if (Instance == this)
        {
            Instance = null;
        }
    }

    #endregion
}

[Serializable]
internal class AuthTokenData
{
    [SerializeField] internal string cookie;
    [SerializeField] internal string socketURL;
    [SerializeField] internal string nameSpace;
}

#pragma warning disable 0649 // filled by JSON deserialization
[Serializable]
internal class BalanceSyncData
{
    [JsonProperty] internal string userId;
    [JsonProperty] internal string gameId;
    [JsonProperty] internal double balance;
}
#pragma warning restore 0649

[Serializable]
internal class SpinRequest
{
    [SerializeField] internal string type = "SPIN";
    [SerializeField] internal SpinPayload payload;
}

[Serializable]
internal class SpinPayload
{
    [SerializeField] internal int betIndex;
    [SerializeField] internal double betAmount;
    [SerializeField] internal bool isFreeSpin;
    [SerializeField] internal bool isAnteBet;
    [SerializeField] internal bool isBonusBuy;
}