using UnityEngine;
using UnityEngine.UI;

namespace FluxLab
{
    /// <summary>
    /// Bola de luz que lanza Mishi. No usa ningún dibujo: la pinta el propio código
    /// (un halo celeste y un centro blanco) y se destruye sola al terminar.
    /// </summary>
    public class BolaMagica : MonoBehaviour
    {
        /// <summary>Tamaño de la bola: 1 = normal, 2 = el doble de grande, 3 = enorme.</summary>
        public const float Tamano = 1.6f;

        static Sprite disco;

        Vector2 inicio;
        Vector2 velocidad;
        float vida;
        float duracion = 2.2f;
        float proximaChispa;
        RectTransform rect;
        Image halo;
        Image anillo;
        Image centro;

        /// <param name="direccion">1 = hacia la derecha, -1 = hacia la izquierda.</param>
        public static void Lanzar(Transform lienzo, Vector2 posicion, float direccion, float impulsoVertical = 120f)
        {
            var go = new GameObject("BolaMagica", typeof(RectTransform));
            go.transform.SetParent(lienzo, false);
            var bola = go.AddComponent<BolaMagica>();
            bola.inicio = posicion;
            bola.velocidad = new Vector2(direccion * 470f, impulsoVertical);
            bola.Armar();
        }

        void Armar()
        {
            rect = GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = inicio;
            rect.sizeDelta = Vector2.zero;

            halo = Capa("Halo", 340f, new Color(0.2f, 0.75f, 1f, 0.55f));
            anillo = Capa("Anillo", 210f, new Color(0.45f, 0.9f, 1f, 0.7f));
            centro = Capa("Centro", 120f, new Color(1f, 1f, 1f, 0.98f));
        }

        Image Capa(string nombre, float lado, Color color)
        {
            var go = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(transform, false);
            var imagen = go.GetComponent<Image>();
            imagen.sprite = Disco();
            imagen.color = color;
            imagen.raycastTarget = false;
            imagen.rectTransform.sizeDelta = new Vector2(lado, lado) * Tamano;
            return imagen;
        }

        void Update()
        {
            vida += Time.deltaTime;
            float avance = vida / duracion;
            if (avance >= 1f)
            {
                Destroy(gameObject);
                return;
            }

            // Trayectoria en arco: velocidad horizontal constante, vertical con "gravedad".
            Vector2 posicion = inicio + new Vector2(velocidad.x * vida, velocidad.y * vida - 250f * vida * vida);
            rect.anchoredPosition = posicion;

            float pulso = 1f + Mathf.Sin(vida * 14f) * 0.1f;
            float aparece = Mathf.Lerp(0.35f, 1f, Mathf.SmoothStep(0f, 1f, vida / 0.4f)); // nace pequeña y crece
            float desvanece = Mathf.Clamp01((1f - avance) / 0.2f);   // se apaga al final
            float estalla = 1f + Mathf.Clamp01((avance - 0.85f) / 0.15f) * 0.8f; // y se expande al final
            rect.localScale = Vector3.one * (pulso * aparece * estalla);
            anillo.rectTransform.localRotation = Quaternion.Euler(0f, 0f, vida * 90f);
            halo.color = new Color(0.2f, 0.75f, 1f, 0.55f * desvanece);
            anillo.color = new Color(0.45f, 0.9f, 1f, 0.7f * desvanece);
            centro.color = new Color(1f, 1f, 1f, 0.98f * desvanece);

            proximaChispa -= Time.deltaTime;
            if (proximaChispa <= 0f)
            {
                proximaChispa = 0.03f;
                Chispa(posicion);
            }
        }

        void Chispa(Vector2 posicion)
        {
            var go = new GameObject("Chispa", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(transform.parent, false);
            go.transform.SetSiblingIndex(transform.GetSiblingIndex());
            var imagen = go.GetComponent<Image>();
            imagen.sprite = Disco();
            imagen.color = new Color(0.6f, 0.95f, 1f, 0.8f);
            imagen.raycastTarget = false;
            var r = imagen.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = posicion + Random.insideUnitCircle * 55f * Tamano;
            r.sizeDelta = Vector2.one * Random.Range(22f, 52f) * Tamano;
            go.AddComponent<ChispaFugaz>();
        }

        static Sprite Disco()
        {
            if (disco != null)
                return disco;

            const int lado = 64;
            var textura = new Texture2D(lado, lado, TextureFormat.RGBA32, false);
            float radio = (lado - 1) * 0.5f;
            var c = new Vector2(radio, radio);
            for (int y = 0; y < lado; y++)
            {
                for (int x = 0; x < lado; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), c) / radio;
                    float alpha = Mathf.Clamp01(1f - d);
                    textura.SetPixel(x, y, new Color(1f, 1f, 1f, alpha * alpha));
                }
            }

            textura.Apply();
            disco = Sprite.Create(textura, new Rect(0f, 0f, lado, lado), new Vector2(0.5f, 0.5f), 100f);
            return disco;
        }

        /// <summary>Una chispita que se encoge y desaparece en medio segundo.</summary>
        class ChispaFugaz : MonoBehaviour
        {
            float t;
            Image imagen;
            Vector2 tamano;

            void Start()
            {
                imagen = GetComponent<Image>();
                tamano = imagen.rectTransform.sizeDelta;
            }

            void Update()
            {
                t += Time.deltaTime / 0.5f;
                if (t >= 1f)
                {
                    Destroy(gameObject);
                    return;
                }

                imagen.rectTransform.sizeDelta = tamano * (1f - t);
                var color = imagen.color;
                color.a = 0.8f * (1f - t);
                imagen.color = color;
            }
        }
    }
}
