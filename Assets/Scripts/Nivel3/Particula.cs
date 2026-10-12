using UnityEngine;
using UnityEngine.UI;

namespace FluxLab
{
    /// <summary>
    /// Una partícula suelta (chispa, gota, burbuja, humo): se mueve, gira, cambia de tamaño,
    /// se desvanece y desaparece sola.
    /// </summary>
    public class Particula : MonoBehaviour
    {
        public Vector2 velocidad;
        public float gravedad;          // positivo cae, negativo sube
        public float vida = 1f;
        public float giro;              // grados por segundo
        public float escalaFinal = 0.2f; // 1 = no cambia, menor se encoge, mayor crece

        Image imagen;
        Vector2 tamanoInicial;
        float alfaInicial;
        float edad;

        public static Particula Crear(Transform padre, Sprite sprite, Vector2 posicion, Vector2 tamano, Color color,
            Vector2 velocidad, float gravedad, float vida, float giro = 0f, float escalaFinal = 0.2f)
        {
            var imagen = Nivel3UI.Imagen(padre, "Particula", sprite, posicion, tamano);
            imagen.color = color;

            var particula = imagen.gameObject.AddComponent<Particula>();
            particula.imagen = imagen;
            particula.velocidad = velocidad;
            particula.gravedad = gravedad;
            particula.vida = Mathf.Max(0.05f, vida);
            particula.giro = giro;
            particula.escalaFinal = escalaFinal;
            particula.tamanoInicial = tamano;
            particula.alfaInicial = color.a;
            return particula;
        }

        void Update()
        {
            edad += Time.deltaTime;
            float p = edad / vida;
            if (p >= 1f)
            {
                Destroy(gameObject);
                return;
            }

            velocidad.y -= gravedad * Time.deltaTime;
            var r = imagen.rectTransform;
            r.anchoredPosition += velocidad * Time.deltaTime;
            r.localRotation *= Quaternion.Euler(0f, 0f, giro * Time.deltaTime);
            r.sizeDelta = tamanoInicial * Mathf.Lerp(1f, escalaFinal, p);

            var color = imagen.color;
            color.a = alfaInicial * (1f - p * p);
            imagen.color = color;
        }
    }
}
