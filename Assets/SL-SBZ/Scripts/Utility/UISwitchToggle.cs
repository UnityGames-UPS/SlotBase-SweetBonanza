using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace SweetBonanza.View
{
    [RequireComponent(typeof(Toggle))]
    internal class UISwitchToggle : MonoBehaviour
    {
        [Header("References (Auto-detected if unassigned)")]
        [SerializeField] private Toggle toggle;
        [SerializeField] private Image trackImage;
        [SerializeField] private RectTransform knobRect;
        [SerializeField] private Image knobImage;

        [Header("Sprites")]
        [SerializeField] private Sprite trackOffSprite;
        [SerializeField] private Sprite trackOnSprite;
        [SerializeField] private Sprite knobOffSprite;
        [SerializeField] private Sprite knobOnSprite;

        [Header("Positions")]
        [SerializeField] private float offPositionX = -11f;
        [SerializeField] private float onPositionX = 11f;
        [SerializeField] private float animationDuration = 0.15f;

        private void Reset()
        {
            AutoAssignReferences();
        }

        private void Awake()
        {
            // Clear graphic so Unity's built-in Toggle does not hide the Knob when isOn == false
            toggle.graphic = null;
            toggle.onValueChanged.AddListener(OnToggleValueChanged);
            UpdateVisuals(toggle.isOn, animate: false);
        }

        private void OnEnable()
        {
            UpdateVisuals(toggle.isOn, animate: false);
        }

        private void AutoAssignReferences()
        {
            if (toggle == null) toggle = GetComponent<Toggle>();
            if (trackImage == null) trackImage = GetComponent<Image>();
            if (knobRect == null)
            {
                Transform knobTransform = transform.Find("Knob");
                if (knobTransform != null) knobRect = knobTransform as RectTransform;
            }
            if (knobImage == null && knobRect != null)
            {
                knobImage = knobRect.GetComponent<Image>();
            }
        }

        private void OnToggleValueChanged(bool isOn)
        {
            UpdateVisuals(isOn, animate: true);
        }

        internal void UpdateVisuals(bool isOn, bool animate = true)
        {
            Sprite targetTrack = isOn ? trackOnSprite : trackOffSprite;
            trackImage.sprite = targetTrack;

            if (!knobImage.gameObject.activeSelf)
                knobImage.gameObject.SetActive(true);

            if (knobImage.canvasRenderer != null)
                knobImage.canvasRenderer.SetAlpha(1f);

            Sprite targetKnob = isOn ? knobOnSprite : knobOffSprite;
            knobImage.sprite = targetKnob;

            float targetX = isOn ? onPositionX : offPositionX;
            knobRect.DOKill();

            if (animate && gameObject.activeInHierarchy)
            {
                knobRect.DOAnchorPosX(targetX, animationDuration).SetEase(Ease.OutQuad);
            }
            else
            {
                Vector2 pos = knobRect.anchoredPosition;
                pos.x = targetX;
                knobRect.anchoredPosition = pos;
            }
        }

        private void OnDestroy()
        {
            if (knobRect != null) knobRect.DOKill();
            if (toggle != null) toggle.onValueChanged.RemoveListener(OnToggleValueChanged);
        }
    }
}

