using UnityEngine;
using UnityEngine.UI;

namespace SweetBonanza.View
{
    [RequireComponent(typeof(Toggle))]
    public class SwitchToggleVisual : MonoBehaviour
    {
        [Header("Visual Elements")]
        [SerializeField] private Image trackImage;
        [SerializeField] private RectTransform knobRect;
        [SerializeField] private Image knobImage;

        [Header("Sprites")]
        [SerializeField] private Sprite trackOnSprite;
        [SerializeField] private Sprite trackOffSprite;
        [SerializeField] private Sprite knobOnSprite;
        [SerializeField] private Sprite knobOffSprite;

        [Header("Knob Positions")]
        [SerializeField] private float offX = -11f;
        [SerializeField] private float onX = 11f;

        private Toggle toggle;

        internal void Setup(Image track, RectTransform knob, Image knobImg, Sprite trkOn, Sprite trkOff, Sprite knbOn, Sprite knbOff, float offPos = -11f, float onPos = 11f)
        {
            trackImage = track;
            knobRect = knob;
            knobImage = knobImg;
            trackOnSprite = trkOn;
            trackOffSprite = trkOff;
            knobOnSprite = knbOn;
            knobOffSprite = knbOff;
            offX = offPos;
            onX = onPos;
        }

        private void Awake()
        {
            Init();
        }

        private void OnEnable()
        {
            Init();
        }

        private void Init()
        {
            if (toggle == null) toggle = GetComponent<Toggle>();
            if (toggle != null)
            {
                toggle.onValueChanged.RemoveListener(UpdateVisual);
                toggle.onValueChanged.AddListener(UpdateVisual);
                UpdateVisual(toggle.isOn);
            }
        }

        internal void UpdateVisual(bool isOn)
        {
            if (trackImage != null)
            {
                if (isOn && trackOnSprite != null) trackImage.sprite = trackOnSprite;
                else if (!isOn && trackOffSprite != null) trackImage.sprite = trackOffSprite;
            }

            if (knobImage != null)
            {
                if (isOn && knobOnSprite != null) knobImage.sprite = knobOnSprite;
                else if (!isOn && knobOffSprite != null) knobImage.sprite = knobOffSprite;
            }

            if (knobRect != null)
            {
                knobRect.anchoredPosition = new Vector2(isOn ? onX : offX, 0f);
            }
        }
    }
}
