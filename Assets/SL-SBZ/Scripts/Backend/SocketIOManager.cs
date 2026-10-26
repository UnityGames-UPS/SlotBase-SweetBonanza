using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Best.SocketIO;
using Best.SocketIO.Events;
using Newtonsoft.Json;
using UnityEngine.UI;

public class SocketIOManager : MonoBehaviour
{
    [Header("Network Configuration")]
    [SerializeField] private string testToken = "test-token";
    protected string testSocketURL = "https://devrealtime.dingdinghouse.com/";
    protected string nameSpace = "playground";

    [Header("References")]
    [SerializeField] internal JSFunctCalls JSManager;
    [SerializeField] private GameObject RaycastBlocker;
    [SerializeField] private List<Button> buttons;

    [Header("Debug Settings")]
    [SerializeField] private bool enablePingDebug = false;

    // Events for decoupled listeners (GameManager, UIManager, etc.)
    internal event Action<string> OnInitDataReceived;
    internal event Action<string> OnSpinResultReceived;
    internal event Action<double> OnBalanceSyncReceived;
    internal event Action<JackpotSyncData> OnJackpotSyncReceived;
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
    private bool JackpotOpen;

    private bool hasFocus = true;
    private float focusLostTime = 0f;
    private Coroutine focusCheckRoutine;
    private float maxBackgroundTime = 60f;

    private Coroutine pingCoroutine;
    private float pingSendTime;
    private bool waitingForPong;
    private int missedPongs;
    private const int MAX_MISSED_PONGS = 5;
    private const float PING_INTERVAL = 2f;
    private const float PONG_TIMEOUT = 5f;

    #region Initialization

    private void Awake()
    {
        isInitialized = false;
        isConnected = false;
        isExiting = false;
        isDestroyed = false;
        socketSetupStarted = false;

        for (int i = 0; i < buttons.Count; i++)
        {
            if (buttons[i] != null)
            {
                buttons[i].onClick.RemoveAllListeners();
                string buttonName = buttons[i].name;
                buttons[i].onClick.AddListener(() => SendJackpotOpen(buttonName));
            }
        }
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
            var authData = JsonUtility.FromJson<AuthTokenData>(jsonData);
            string incomingToken = authData.cookie;
            string incomingSocketURL = authData.socketURL;
            string incomingNameSpace = !string.IsNullOrEmpty(authData.nameSpace) ? authData.nameSpace : nameSpace;

            if (socketSetupStarted && authToken == incomingToken && socketURL == incomingSocketURL)
            {
                Debug.LogWarning("[SocketIO] Matching auth token received, bypassing re-initialization.");
                return;
            }

            Debug.Log("[SocketIO] New or updated auth token received. Cleaning up old socket and re-initializing.");
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

        if (RaycastBlocker != null) RaycastBlocker.SetActive(true);

        SocketOptions options = new SocketOptions
        {
            AutoConnect = false,
            Reconnection = false,
            Timeout = TimeSpan.FromSeconds(3),
            ConnectWith = Best.SocketIO.Transports.TransportTypes.WebSocket
        };

        options.Auth = (SocketManager manager, Socket socket) => new { token = authToken };

#if UNITY_EDITOR
        socketManager = new SocketManager(new Uri(testSocketURL), options);
#else
        socketManager = new SocketManager(new Uri(socketURL), options);
#endif

        gameSocket = string.IsNullOrEmpty(nameSpace)
            ? socketManager.Socket
            : socketManager.GetSocket("/" + nameSpace);

        gameSocket.On<ConnectResponse>(SocketIOEventTypes.Connect, OnSocketConnected);
        gameSocket.On(SocketIOEventTypes.Disconnect, OnSocketDisconnected);
        gameSocket.On<Error>(SocketIOEventTypes.Error, OnSocketError);

        gameSocket.On<string>("game:init", OnInitReceived);
        gameSocket.On<string>("result", OnResultReceived);
        gameSocket.On<string>("pong", OnPongReceived);
        gameSocket.On("pong", OnPongReceivedNoArgs);
        gameSocket.On<string>("AnotherDevice", OnAnotherDevice);
        gameSocket.On<string>("balance:sync", OnBalanceSyncReceivedInternal);
        gameSocket.On<string>("jackpot:sync", OnJackpotSyncReceivedInternal);

        socketManager.Open();
    }

    #endregion

    #region Socket Events

    private void OnSocketConnected(ConnectResponse resp)
    {
        Debug.Log("[SocketIO] Connected");

        isConnected = true;
        waitingForPong = false;
        missedPongs = 0;
        pingSendTime = Time.realtimeSinceStartup;

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

        OnSocketDisconnectedEvent?.Invoke();
    }

    private void OnSocketError(Error err)
    {
        Debug.LogError($"[SocketIO] Error: {err.message}");

        if (!string.IsNullOrEmpty(err.message) && err.message.Contains("Session expired"))
        {
            Debug.LogWarning("Session expired detected");
            OnSocketDisconnected();
#if UNITY_WEBGL && !UNITY_EDITOR
            JSManager?.SendCustomMessage("session_expired");
#endif
        }
        else
        {
            OnSocketErrorEvent?.Invoke(err.message);
#if UNITY_WEBGL && !UNITY_EDITOR
            JSManager?.SendCustomMessage("error");
#endif
        }
    }

    private void OnInitReceived(string jsonData)
    {
        Debug.Log($"[SocketIO] Init received: {jsonData}");

        try
        {
            isInitialized = true;
            if (RaycastBlocker != null) RaycastBlocker.SetActive(false);

            OnInitDataReceived?.Invoke(jsonData);

#if UNITY_WEBGL && !UNITY_EDITOR
            JSManager?.SendCustomMessage("OnEnter");
#endif
        }
        catch (Exception e)
        {
            Debug.LogError($"[SocketIO] Init parse failed: {e.Message}");
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

    private void OnJackpotSyncReceivedInternal(string jsonData)
    {
        Debug.Log($"[SocketIO] Jackpot Sync received: {jsonData}");

        try
        {
            var syncData = JsonConvert.DeserializeObject<JackpotSyncData>(jsonData);
            if (syncData != null)
            {
                OnJackpotSyncReceived?.Invoke(syncData);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[SocketIO] Jackpot Sync parse failed: {e.Message}");
        }
    }

    #endregion

    #region Jackpot & Spin Requests

    private void SendJackpotOpen(string JackpotType)
    {
        if (JackpotOpen) return;

        JackpotOpen = true;

        var request = new JackpotOpenRequest
        {
            type = "JACKPOT_OPEN",
            payload = new JackpotOpenPayload
            {
                tier = JackpotType
            }
        };

        string json = JsonConvert.SerializeObject(request);
        Debug.Log($"[SocketIO] Jackpot Open request: {json}");
        if (gameSocket != null)
        {
            gameSocket.Emit("request", json);
        }

        Invoke(nameof(ResetJackpotOpen), 1f);
    }

    private void ResetJackpotOpen()
    {
        JackpotOpen = false;
    }

    internal void SendSpinRequest(int betIndex)
    {
        Debug.Log($"[SocketIO] Spin request: betIndex={betIndex}");

        var request = new SpinRequest
        {
            type = "SPIN",
            payload = new SpinPayload
            {
                betIndex = betIndex,
                isFreeSpin = false
            }
        };

        string json = JsonUtility.ToJson(request);
        if (gameSocket != null)
        {
            gameSocket.Emit("request", json);
        }
    }

    #endregion

    #region Focus & Background Timeout

    internal void SetRaycastBlocker(bool active)
    {
        if (RaycastBlocker != null) RaycastBlocker.SetActive(active);
    }

    internal void HandleFocusChange(bool focus)
    {
        hasFocus = focus;

        if (!focus)
        {
            focusLostTime = Time.time;
            if (focusCheckRoutine == null && !isExiting && !isDestroyed)
                focusCheckRoutine = StartCoroutine(FocusTimeoutCheck());
        }
        else
        {
            if (focusCheckRoutine != null)
            {
                StopCoroutine(focusCheckRoutine);
                focusCheckRoutine = null;
            }
        }
    }

    private IEnumerator FocusTimeoutCheck()
    {
        while (!hasFocus && !isExiting && !isDestroyed)
        {
            if (Time.time - focusLostTime >= maxBackgroundTime)
            {
                Debug.LogWarning("[SOCKET] Background timeout — closing connection");
                isConnected = false;
                StopPingRoutine();

                if (socketManager != null)
                {
                    try { socketManager.Close(); }
                    catch (Exception e) { Debug.LogWarning($"[SOCKET] Focus close error: {e.Message}"); }
                }

                OnSocketDisconnectedEvent?.Invoke();

                focusCheckRoutine = null;
                yield break;
            }

            yield return new WaitForSecondsRealtime(1f);
        }

        focusCheckRoutine = null;
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
            yield return new WaitForSeconds(PING_INTERVAL);

            if (waitingForPong && isInitialized)
            {
                missedPongs++;

                if (missedPongs >= MAX_MISSED_PONGS)
                {
                    Debug.LogWarning("[SocketIO] Max pongs missed - disconnecting");
                    OnSocketDisconnected();
                    yield break;
                }
            }

            SendPing();
        }
    }

    private void SendPing()
    {
        if (gameSocket != null && isConnected)
        {
            pingSendTime = Time.realtimeSinceStartup;
            waitingForPong = true;
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
        waitingForPong = false;

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

        if (missedPongs > 0)
        {
            missedPongs = 0;
        }
    }

    #endregion

    #region Cleanup & Teardown

    internal void CloseSocket()
    {
        if (isDestroyed) return;

        isExiting = true;

        if (RaycastBlocker != null) RaycastBlocker.SetActive(true);

        StopPingRoutine();

        if (socketManager != null)
        {
            socketManager.Close();
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
            socketManager.Close();
            socketManager = null;
        }
        isConnected = false;
    }

    #endregion
}

[Serializable]
public class AuthTokenData
{
    public string cookie;
    public string socketURL;
    public string nameSpace;
}

[Serializable]
public class BalanceSyncData
{
    public string userId;
    public string gameId;
    public double balance;
}

[Serializable]
public class JackpotSyncData
{
    public List<double> values;
}

[Serializable]
public class JackpotOpenRequest
{
    public string type = "JACKPOT_OPEN";
    public JackpotOpenPayload payload = new JackpotOpenPayload();
}

[Serializable]
public class JackpotOpenPayload
{
    public string tier;
}

[Serializable]
public class SpinRequest
{
    public string type = "SPIN";
    public SpinPayload payload;
}

[Serializable]
public class SpinPayload
{
    public int betIndex;
    public bool isFreeSpin;
}
