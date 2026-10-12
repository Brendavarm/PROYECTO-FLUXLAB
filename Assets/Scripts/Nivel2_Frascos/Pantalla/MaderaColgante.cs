using UnityEngine;
using UnityEngine.EventSystems;

namespace FluxLab.Nivel2
{
    /// <summary>
    /// Movimiento de los letreros y botones de madera del HUD: caen rebotando al empezar,
    /// se balancean suave, crecen con el mouse y se aplastan al hacer clic.
    /// </summary>
    public class MaderaColgante : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
    {
        public float retraso;
        public float balanceo = 1.5f;
        public float velocidad = 1.3f;
        public bool reaccionaAlMouse = true;

        /// <summary>Si está activo, espera arriba hasta que alguien llame a Soltar().</summary>
        public bool esperarSenal = true;

        RectTransform rect;
        Vector2 sitio;
        float reloj;
        float fase;
        float encima;
        bool mouseEncima;
        float apretado;
        bool soltada;

        void Start()
        {
            rect = GetComponent<RectTransform>();
            sitio = rect.anchoredPosition;
            fase = Random.Range(0f, 6f);
            reloj = esperarSenal && !soltada ? -1000f : -retraso;
            rect.anchoredPosition = sitio + new Vector2(0f, 320f);
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;   // también se mueve en pausa
            reloj += dt;

            float caida = Mathf.Clamp01(reloj / 0.8f);
            rect.anchoredPosition = sitio + new Vector2(0f, Mathf.LerpUnclamped(320f, 0f, Elastico(caida)));

            float objetivo = reaccionaAlMouse && mouseEncima ? 1f : 0f;
            encima = Mathf.MoveTowards(encima, objetivo, dt * 6f);
            apretado = Mathf.MoveTowards(apretado, 0f, dt * 5f);

            float t = Time.unscaledTime;
            float giro = Mathf.Sin(t * velocidad + fase) * balanceo * (1f - encima * 0.7f);
            rect.localRotation = Quaternion.Euler(0f, 0f, giro);
            float aplaste = Mathf.Sin(apretado * Mathf.PI) * 0.12f;
            float crecer = 1f + encima * 0.06f;
            rect.localScale = new Vector3(crecer * (1f + aplaste), crecer * (1f - aplaste), 1f);
        }

        /// <summary>Hace caer la madera (después de su retraso).</summary>
        public void Soltar()
        {
            if (soltada)
                return;
            soltada = true;
            reloj = -retraso;
        }

        public void OnPointerEnter(PointerEventData evento) => mouseEncima = true;

        public void OnPointerExit(PointerEventData evento) => mouseEncima = false;

        public void OnPointerDown(PointerEventData evento)
        {
            if (reaccionaAlMouse)
                apretado = 1f;
        }

        static float Elastico(float t)
        {
            if (t <= 0f || t >= 1f)
                return t;
            return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * (2f * Mathf.PI / 3f)) + 1f;
        }
    }
}
