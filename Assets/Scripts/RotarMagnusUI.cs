using UnityEngine;
using UnityEngine.EventSystems;

namespace FluxLab
{
    /// <summary>
    /// Permite rotar al modelo 3D en 360 grados haciendo clic y arrastrando sobre la UI.
    /// </summary>
    public class RotarMagnusUI : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
    {
        [Header("Configuración de Giro")]
        public Transform modelo3D;
        public float sensibilidad = 0.35f;
        public float amortiguacionInercia = 5f;

        private float velocidadGiro = 0f;
        private bool estaArrastrando = false;
        private Quaternion rotacionInicial;
        private float ultimoClick = 0f;

        void Awake()
        {
            // Busca automáticamente a Magnus si no está asignado
            if (modelo3D == null)
            {
                GameObject magnus = GameObject.Find("Magnus_Base");
                if (magnus != null)
                    modelo3D = magnus.transform;
            }

            if (modelo3D != null)
                rotacionInicial = modelo3D.localRotation;
        }

        void Update()
        {
            if (modelo3D == null) return;

            // Inercia fluida al soltar el ratón para un giro natural
            if (!estaArrastrando && Mathf.Abs(velocidadGiro) > 0.05f)
            {
                modelo3D.Rotate(Vector3.up, velocidadGiro * Time.deltaTime, Space.World);
                velocidadGiro = Mathf.Lerp(velocidadGiro, 0f, Time.deltaTime * amortiguacionInercia);
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            estaArrastrando = true;
            velocidadGiro = 0f;

            // Doble clic rápido para volverlo a poner de frente
            if (Time.time - ultimoClick < 0.3f)
            {
                RestablecerRotacion();
            }
            ultimoClick = Time.time;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (modelo3D == null) return;

            // Invertido respecto al arrastre horizontal para que siga el cursor naturalmente
            float delta = -eventData.delta.x * sensibilidad;
            modelo3D.Rotate(Vector3.up, delta, Space.World);

            // Guardar impulso para la inercia
            velocidadGiro = (delta / Mathf.Max(Time.deltaTime, 0.001f)) * 0.15f;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            estaArrastrando = false;
        }

        public void RestablecerRotacion()
        {
            if (modelo3D != null)
                modelo3D.localRotation = rotacionInicial;
            velocidadGiro = 0f;
        }
    }
}
