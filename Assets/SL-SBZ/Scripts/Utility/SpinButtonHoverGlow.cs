using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class SpinButtonHoverGlow : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private RectTransform glowRect;
    [SerializeField] private RectTransform spinRect;

    private Coroutine shakeRoutine;
    private Vector2 initialAnchorPos;
    private Sequence wobbleSequence;

    private void Awake()
    {
        if (glowRect != null)
        {
            glowRect.gameObject.SetActive(false);
        }

        if (spinRect != null)
        {
            initialAnchorPos = spinRect.anchoredPosition;
        }
    }

    private void OnEnable()
    {
        // Start the idle wobble loop when active
        if (spinRect != null)
        {
            shakeRoutine = StartCoroutine(ShakeAnimationRoutine());
        }
    }

    private void OnDisable()
    {
        // Stop coroutine and reset position cleanly when disabled
        if (shakeRoutine != null)
        {
            StopCoroutine(shakeRoutine);
            shakeRoutine = null;
        }

        KillTweens();
        ResetSpinRect();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (glowRect != null)
        {
            glowRect.gameObject.SetActive(true);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (glowRect != null)
        {
            glowRect.gameObject.SetActive(false);
        }
    }

    /// <summary>Loops the attractive shake & rotation wiggle every 3 seconds.</summary>
    private IEnumerator ShakeAnimationRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(5f);

            if (spinRect == null) yield break;

            KillTweens();
            ResetSpinRect();

            // 1. Shake position slightly
            //spinRect.DOShakeAnchorPos(0.5f, 10f, 10, 90f, false, true);

            // 2. Play the -40 -> 25 -> 0 rotation wobble together
            wobbleSequence = DOTween.Sequence();
            wobbleSequence
                .Append(spinRect.DOLocalRotate(new Vector3(0, 0, -18f), 0.18f).SetEase(Ease.OutSine))
                .Append(spinRect.DOLocalRotate(new Vector3(0, 0, 12f), 0.22f).SetEase(Ease.InOutSine))
                .Append(spinRect.DOLocalRotate(Vector3.zero, 0.26f).SetEase(Ease.OutBack))
                .OnKill(() => ResetSpinRect());
        }
    }

    private void ResetSpinRect()
    {
        if (spinRect == null) return;
        spinRect.anchoredPosition = initialAnchorPos;
        spinRect.localRotation = Quaternion.identity;
    }

    private void KillTweens()
    {
        if (spinRect != null) spinRect.DOKill();
        if (wobbleSequence != null && wobbleSequence.IsActive()) wobbleSequence.Kill();
    }

    private void OnDestroy()
    {
        KillTweens();
    }
}