using UnityEngine;
using UnityEngine.EventSystems;

namespace FluxLab
{
    public class HoverScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public float scale = 1.06f;

        public void OnPointerEnter(PointerEventData eventData)
        {
            transform.localScale = Vector3.one * scale;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            transform.localScale = Vector3.one;
        }
    }
}
