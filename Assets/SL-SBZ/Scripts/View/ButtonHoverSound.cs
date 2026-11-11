using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace SweetBonanza.View
{
    /// <summary>Plays an audio clip when the pointer hovers over an interactable UI control (Button, Slider, Toggle, etc.).</summary>
    public class ButtonHoverSound : MonoBehaviour, IPointerEnterHandler
    {
        [SerializeField] private Selectable selectable;
        [SerializeField] private AudioClip hoverClip;

        private void Awake()
        {
            if (selectable == null) selectable = GetComponent<Selectable>();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (selectable != null && !selectable.interactable) return;

            if (hoverClip != null)
            {
                AudioManager.Instance?.PlayOneShot(hoverClip);
            }
            else
            {
                AudioManager.Instance?.PlayButton();
            }
        }
    }
}
