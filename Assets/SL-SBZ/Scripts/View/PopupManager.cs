using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class PopupManager : MonoBehaviour
{
    [Header("Popup Backdrop Overlay")]
    [SerializeField] private GameObject popupParent;

    [Header("1. Disconnection Popup")]
    [SerializeField] private GameObject disconnectionPopup;
   
    [SerializeField] private Button disconnectionOkButton;

    [Header("2. Error Popup")]
    [SerializeField] private GameObject errorPopup;
    
    [SerializeField] private Button errorOkButton;

    [Header("3. Reconnection Popup")]
    [SerializeField] private GameObject reconnectionPopup;
  

    [Header("4. Loading Popup")]
    [SerializeField] private GameObject loadingPopup;
  

    [Header("5. Exit Game Popup")]
    [SerializeField] private GameObject exitGamePopup;
   
    [SerializeField] private Button exitGameYesButton;
    [SerializeField] private Button exitGameNoButton;

    [Header("Animation Settings")]
    [SerializeField] private float popupScaleDuration = 0.25f;

    internal static PopupManager Instance { get; private set; }

    private GameObject currentActivePopup;
    private bool isErrorCritical;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        SetupButtons();
        HideAllPopups();
    }

    private void SetupButtons()
    {
        if (disconnectionOkButton != null)
        {
            disconnectionOkButton.onClick.RemoveAllListeners();
            disconnectionOkButton.onClick.AddListener(() =>
            {
                AudioManager.Instance?.PlayButton();
                ClosePopup(disconnectionPopup, ExitGame);
            });
        }

        if (errorOkButton != null)
        {
            errorOkButton.onClick.RemoveAllListeners();
            errorOkButton.onClick.AddListener(() =>
            {
                AudioManager.Instance?.PlayButton();
                ClosePopup(errorPopup, () =>
                {
                    if (isErrorCritical) ExitGame();
                });
            });
        }

        if (exitGameYesButton != null)
        {
            exitGameYesButton.onClick.RemoveAllListeners();
            exitGameYesButton.onClick.AddListener(() =>
            {
                AudioManager.Instance?.PlayButton();
                ClosePopup(exitGamePopup, ExitGame);
            });
        }

        if (exitGameNoButton != null)
        {
            exitGameNoButton.onClick.RemoveAllListeners();
            exitGameNoButton.onClick.AddListener(() =>
            {
                AudioManager.Instance?.PlayButton();
                ClosePopup(exitGamePopup);
            });
        }
    }

    #region Popup Core (Open & Close)

    internal void OpenPopup(GameObject popup)
    {
        if (popup == null) return;

        if (currentActivePopup != null && currentActivePopup != popup)
        {
            currentActivePopup.SetActive(false);
        }

        currentActivePopup = popup;
        if (popupParent != null) popupParent.SetActive(true);

        popup.SetActive(true);
        AudioManager.Instance?.PlayPopupOpen();
        popup.transform.DOKill();
        popup.transform.localScale = Vector3.zero;
        popup.transform.DOScale(1f, popupScaleDuration).SetEase(Ease.OutBack);
    }

    internal void ClosePopup(GameObject popup, Action onComplete = null)
    {
        if (popup == null || !popup.activeSelf)
        {
            onComplete?.Invoke();
            return;
        }

        AudioManager.Instance?.PlayPopupClose();
        popup.transform.DOKill();
        popup.transform.DOScale(0f, popupScaleDuration * 0.8f).SetEase(Ease.InBack).OnComplete(() =>
        {
            popup.transform.localScale = Vector3.one;
            popup.SetActive(false);
            if (currentActivePopup == popup) currentActivePopup = null;
            UpdateBackdrop();
            onComplete?.Invoke();
        });
    }

    private void UpdateBackdrop()
    {
        if (popupParent != null)
        {
            bool anyActive = (disconnectionPopup != null && disconnectionPopup.activeSelf) ||
                             (errorPopup != null && errorPopup.activeSelf) ||
                             (reconnectionPopup != null && reconnectionPopup.activeSelf) ||
                             (loadingPopup != null && loadingPopup.activeSelf) ||
                             (exitGamePopup != null && exitGamePopup.activeSelf);
            popupParent.SetActive(anyActive);
        }
    }

    private void HideAllPopups()
    {
        if (disconnectionPopup != null) disconnectionPopup.SetActive(false);
        if (errorPopup != null) errorPopup.SetActive(false);
        if (reconnectionPopup != null) reconnectionPopup.SetActive(false);
        if (loadingPopup != null) loadingPopup.SetActive(false);
        if (exitGamePopup != null) exitGamePopup.SetActive(false);
        if (popupParent != null) popupParent.SetActive(false);
        currentActivePopup = null;
    }

    #endregion

    #region 5 Popups APIs

    // 1. Disconnection
    internal void ShowDisconnectionPopup(string message = null, string heading = null)
    {
      
        OpenPopup(disconnectionPopup);
    }

    // 2. Error
    internal void ShowErrorPopup(string heading, string message, bool isCritical = false)
    {
        isErrorCritical = isCritical;
     

        if (errorOkButton != null)
        {
            var btnText = errorOkButton.GetComponentInChildren<TMP_Text>(true);
            if (btnText != null) btnText.text = isCritical ? "Exit Game" : "OKAY";
        }

        OpenPopup(errorPopup);
    }

    internal void ShowServerError(string message = "A server error occurred.") => ShowErrorPopup("Server Error", message, true);
    internal void ShowAnotherDeviceError() => ShowErrorPopup("Warning", "Logged in from another device.", true);
    internal void ShowInsufficientFundsError() => ShowErrorPopup("Information", "Insufficient balance.", false);

    // 3. Reconnection
    internal void ShowReconnectionPopup(int currentTry, int maxTries)
    {
        OpenPopup(reconnectionPopup);
    }

    internal void CloseReconnectionPopup() => ClosePopup(reconnectionPopup);

    // 4. Loading
    internal void ShowLoadingPopup(float duration = 0f)
    {
        OpenPopup(loadingPopup);
        if (duration > 0f)
        {
            DOVirtual.DelayedCall(duration, () => CloseLoadingPopup());
        }
    }

    internal void CloseLoadingPopup(Action onComplete = null) => ClosePopup(loadingPopup, onComplete);
    internal bool IsLoadingPopupActive() => loadingPopup != null && loadingPopup.activeSelf;

    // 5. Exit Game
    internal void ShowExitGamePopup() => OpenPopup(exitGamePopup);

    internal void ExitGame()
    {
        var socketMgr = UnityEngine.Object.FindFirstObjectByType<SocketIOManager>();
        if (socketMgr != null) socketMgr.CloseSocket();

        var jsCall = UnityEngine.Object.FindFirstObjectByType<JSFunctCalls>();
        if (jsCall != null) jsCall.SendCustomMessage("OnExit");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    #endregion

    private void OnDestroy()
    {
        DOTween.Kill(this);
    }
}
