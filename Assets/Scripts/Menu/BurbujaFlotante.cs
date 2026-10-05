using UnityEngine;

namespace FluxLab
{
    public class BurbujaFlotante : MonoBehaviour
    {
        public float velocidad = 36f;
        public float amplitud = 16f;

        RectTransform rect;
        float fase;
        float baseX;

        void Start()
        {
            rect = GetComponent<RectTransform>();
            fase = Random.Range(0f, Mathf.PI * 2f);
            baseX = rect.anchoredPosition.x;
        }

        void Update()
        {
            if (rect == null)
                return;

            Vector2 posicion = rect.anchoredPosition;
            posicion.y += velocidad * Time.deltaTime;
            posicion.x = baseX + Mathf.Sin(Time.time * 1.15f + fase) * amplitud;
            if (posicion.y > 640f)
            {
                posicion.y = -640f;
                baseX = Random.Range(-880f, 880f);
                posicion.x = baseX;
            }

            rect.anchoredPosition = posicion;
        }
    }
}
