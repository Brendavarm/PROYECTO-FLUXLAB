using UnityEngine;
using UnityEngine.UI;

namespace FluxLab
{
    /// <summary>
    /// La media luna del cielo: se mece despacito, su halo respira, las estrellitas titilan
    /// y de vez en cuando cruza una estrella fugaz.
    /// </summary>
    public class Nivel3Luna : MonoBehaviour
    {
        const int CantidadEstrellas = 7;

        static Sprite creciente;

        Transform padre;
        RectTransform luna;
        Image halo;
        Vector2 posicion;
        float tamano;
        float proximaFugaz = 4f;

        readonly Image[] estrellas = new Image[CantidadEstrellas];
        readonly float[] ritmo = new float[CantidadEstrellas];
        readonly float[] desfase = new float[CantidadEstrellas];
        readonly float[] lado = new float[CantidadEstrellas];

        public static Nivel3Luna Crear(Transform padre, Vector2 posicion, float tamano)
        {
            var raiz = Nivel3UI.Vacio(padre, "Luna", posicion, Vector2.zero);
            var vivo = raiz.gameObject.AddComponent<Nivel3Luna>();
            vivo.padre = padre;
            vivo.posicion = posicion;
            vivo.tamano = tamano;

            // El halo queda detrás de la luna
            vivo.halo = Nivel3UI.Imagen(raiz, "Halo", Nivel3UI.LuzSuave(), Vector2.zero, Vector2.one * tamano * 3.4f);
            vivo.halo.color = new Color(0.7f, 0.85f, 1f, 0.3f);

            vivo.luna = Nivel3UI.Imagen(raiz, "Creciente", Creciente(), Vector2.zero, Vector2.one * tamano).rectTransform;

            // Estrellitas alrededor
            Sprite estrella = FrascoBrillante.Estrella();
            for (int i = 0; i < CantidadEstrellas; i++)
            {
                float angulo = (i / (float)CantidadEstrellas) * Mathf.PI * 2f + Random.Range(-0.3f, 0.3f);
                float radio = tamano * Random.Range(0.85f, 1.6f);
                vivo.lado[i] = Random.Range(0.14f, 0.28f) * tamano;
                var img = Nivel3UI.Imagen(raiz, "Estrella" + i, estrella,
                    new Vector2(Mathf.Cos(angulo) * radio, Mathf.Sin(angulo) * radio * 0.8f), Vector2.one * vivo.lado[i]);
                img.color = new Color(1f, 0.98f, 0.85f, 0f);
                vivo.estrellas[i] = img;
                vivo.ritmo[i] = Random.Range(1.2f, 2.6f);
                vivo.desfase[i] = Random.Range(0f, 6.28f);
            }

            return vivo;
        }

        void Update()
        {
            float t = Time.time;

            // La luna se mece y sube y baja apenas
            luna.anchoredPosition = new Vector2(0f, Mathf.Sin(t * 0.8f) * 7f);
            luna.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 0.55f) * 6f);

            // El halo respira
            float respira = 0.5f + 0.5f * Mathf.Sin(t * 1.1f);
            halo.color = new Color(0.7f, 0.85f, 1f, 0.2f + 0.2f * respira);
            halo.rectTransform.localScale = Vector3.one * (0.95f + 0.1f * respira);

            for (int i = 0; i < CantidadEstrellas; i++)
            {
                float onda = Mathf.Max(0f, Mathf.Sin(t * ritmo[i] + desfase[i]));
                onda *= onda;
                estrellas[i].color = new Color(1f, 0.98f, 0.85f, onda);
                estrellas[i].rectTransform.localScale = Vector3.one * (0.5f + 0.7f * onda);
                estrellas[i].rectTransform.localRotation = Quaternion.Euler(0f, 0f, t * 20f);
            }

            proximaFugaz -= Time.deltaTime;
            if (proximaFugaz <= 0f)
            {
                proximaFugaz = Random.Range(8f, 14f);
                EstrellaFugaz();
            }
        }

        void EstrellaFugaz()
        {
            // Cruza el cielo de derecha a izquierda, bajando un poco
            Vector2 desde = posicion + new Vector2(Random.Range(120f, 320f), Random.Range(-10f, 70f));
            Particula.Crear(padre, FrascoBrillante.Estrella(), desde, Vector2.one * 38f, new Color(1f, 1f, 0.9f, 1f),
                new Vector2(-620f, -180f), 0f, 1.1f, 0f, 0.3f);
            for (int i = 1; i <= 4; i++)
            {
                Particula.Crear(padre, FrascoBrillante.Estrella(), desde + new Vector2(i * 26f, i * 7f), Vector2.one * (30f - i * 5f),
                    new Color(1f, 1f, 0.9f, 0.8f - i * 0.15f), new Vector2(-620f, -180f), 0f, 1.1f, 0f, 0.3f);
            }
        }

        /// <summary>Dibuja la media luna: un círculo al que se le resta otro corrido.</summary>
        static Sprite Creciente()
        {
            if (creciente != null)
                return creciente;

            const int lado = 160;
            var textura = new Texture2D(lado, lado, TextureFormat.RGBA32, false);
            var centroA = new Vector2(78f, 80f);
            var centroB = new Vector2(104f, 94f);
            const float radioA = 62f;
            const float radioB = 54f;

            for (int y = 0; y < lado; y++)
            {
                for (int x = 0; x < lado; x++)
                {
                    var p = new Vector2(x, y);
                    float dentroA = Mathf.Clamp01((radioA - Vector2.Distance(p, centroA)) / 2.5f);
                    float fueraB = Mathf.Clamp01((Vector2.Distance(p, centroB) - radioB) / 2.5f);
                    float alfa = dentroA * fueraB;

                    // Más clara al centro y un poco cálida en el borde
                    float calidez = Mathf.Clamp01(Vector2.Distance(p, centroA) / radioA);
                    Color color = Color.Lerp(new Color(1f, 0.99f, 0.88f), new Color(1f, 0.9f, 0.6f), calidez * calidez);
                    textura.SetPixel(x, y, new Color(color.r, color.g, color.b, alfa));
                }
            }

            textura.Apply();
            creciente = Sprite.Create(textura, new Rect(0f, 0f, lado, lado), new Vector2(0.5f, 0.5f), 100f);
            return creciente;
        }
    }
}
