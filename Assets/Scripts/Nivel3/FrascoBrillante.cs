using UnityEngine;
using UnityEngine.UI;

namespace FluxLab
{
    /// <summary>
    /// La magia de un frasco: una luz suave que pulsa y unas estrellitas que titilan alrededor.
    /// Va como hijo del frasco, así que se mueve, gira y se cae junto con él.
    /// Todas las medidas son locales: (0,0) es el centro del frasco.
    /// </summary>
    public class FrascoBrillante : MonoBehaviour
    {
        const int CantidadEstrellas = 3;

        static Sprite luzSuave;
        static Sprite estrella;

        Image luz;
        Color color;
        float fase;
        float velocidadPulso;

        readonly Image[] estrellas = new Image[CantidadEstrellas];
        readonly float[] ritmo = new float[CantidadEstrellas];
        readonly float[] desfase = new float[CantidadEstrellas];
        readonly float[] giro = new float[CantidadEstrellas];
        readonly float[] tamano = new float[CantidadEstrellas];

        public static FrascoBrillante Crear(Transform frasco, float ancho, float alto, Color color)
        {
            var go = new GameObject("Vida", typeof(RectTransform));
            go.transform.SetParent(frasco, false);
            var r = go.GetComponent<RectTransform>();
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = Vector2.zero;
            r.sizeDelta = Vector2.zero;

            var vivo = go.AddComponent<FrascoBrillante>();
            vivo.Armar(ancho, alto, color);
            return vivo;
        }

        void Armar(float ancho, float alto, Color color)
        {
            this.color = color;

            // Cada frasco late a su propio ritmo, para que no parpadeen todos a la vez.
            fase = Random.Range(0f, 6.28f);
            velocidadPulso = Random.Range(1.2f, 2f);

            luz = Imagen("Luz", Vector2.zero, new Vector2(ancho * 1.25f, alto * 1.15f), LuzSuave());

            Color claro = Color.Lerp(color, Color.white, 0.65f);
            for (int i = 0; i < CantidadEstrellas; i++)
            {
                // Repartidas alrededor del frasco: arriba-izquierda, derecha y abajo-izquierda, con algo de azar.
                float angulo = (i * 120f + 40f + Random.Range(-25f, 25f)) * Mathf.Deg2Rad;
                var posicion = new Vector2(Mathf.Cos(angulo) * ancho * 0.5f, Mathf.Sin(angulo) * alto * 0.42f);
                tamano[i] = Random.Range(0.1f, 0.16f) * alto;
                estrellas[i] = Imagen("Estrella" + i, posicion, Vector2.one * tamano[i], Estrella());
                estrellas[i].color = new Color(claro.r, claro.g, claro.b, 0f);
                ritmo[i] = Random.Range(1.3f, 2.4f);
                desfase[i] = Random.Range(0f, 6.28f);
                giro[i] = Random.Range(-25f, 25f);
            }
        }

        Image Imagen(string nombre, Vector2 posicion, Vector2 tam, Sprite sprite)
        {
            var go = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(transform, false);
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            var r = img.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = posicion;
            r.sizeDelta = tam;
            return img;
        }

        void Update()
        {
            float t = Time.time;

            // Luz: sube y baja suavemente, apenas insinuada para no tapar el dibujo.
            float pulso = 0.5f + 0.5f * Mathf.Sin(t * velocidadPulso + fase);
            luz.color = new Color(color.r, color.g, color.b, 0.03f + 0.14f * pulso);

            // Estrellitas: aparecen, brillan y se apagan; cada una a su ritmo.
            Color claro = Color.Lerp(color, Color.white, 0.65f);
            for (int i = 0; i < CantidadEstrellas; i++)
            {
                float onda = Mathf.Max(0f, Mathf.Sin(t * ritmo[i] + desfase[i]));
                onda *= onda;
                estrellas[i].color = new Color(claro.r, claro.g, claro.b, onda);
                estrellas[i].rectTransform.localScale = Vector3.one * (0.5f + 0.7f * onda);
                estrellas[i].rectTransform.localRotation = Quaternion.Euler(0f, 0f, t * giro[i]);
            }
        }

        /// <summary>Un círculo que se va apagando hacia los bordes, como una luz.</summary>
        static Sprite LuzSuave()
        {
            if (luzSuave != null)
                return luzSuave;

            const int lado = 64;
            var textura = new Texture2D(lado, lado, TextureFormat.RGBA32, false);
            float radio = (lado - 1) * 0.5f;
            var c = new Vector2(radio, radio);
            for (int y = 0; y < lado; y++)
            {
                for (int x = 0; x < lado; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), c) / radio;
                    float a = Mathf.Clamp01(1f - d);
                    textura.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
                }
            }

            textura.Apply();
            luzSuave = Sprite.Create(textura, new Rect(0f, 0f, lado, lado), new Vector2(0.5f, 0.5f), 100f);
            return luzSuave;
        }

        /// <summary>Una estrella de cuatro puntas.</summary>
        public static Sprite Estrella()
        {
            if (estrella != null)
                return estrella;

            const int lado = 48;
            var textura = new Texture2D(lado, lado, TextureFormat.RGBA32, false);
            for (int y = 0; y < lado; y++)
            {
                for (int x = 0; x < lado; x++)
                {
                    float u = Mathf.Abs((x + 0.5f) / lado * 2f - 1f);
                    float v = Mathf.Abs((y + 0.5f) / lado * 2f - 1f);
                    // Las cuatro puntas salen de |u|^0.5 + |v|^0.5 = 1
                    float a = Mathf.Clamp01(1f - (Mathf.Sqrt(u) + Mathf.Sqrt(v)));
                    textura.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(a * 2.2f)));
                }
            }

            textura.Apply();
            estrella = Sprite.Create(textura, new Rect(0f, 0f, lado, lado), new Vector2(0.5f, 0.5f), 100f);
            return estrella;
        }
    }
}
