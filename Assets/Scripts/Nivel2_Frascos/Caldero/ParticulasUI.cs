using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FluxLab.Nivel2
{
    /// <summary>
    /// Partículas simples para la UI: gotas, chispas, humo, destellos.
    /// Cada una nace, se mueve, gira, crece o se encoge y se desvanece.
    /// </summary>
    public class ParticulasUI : MonoBehaviour
    {
        class Particula
        {
            public RectTransform rect;
            public Image imagen;
            public Vector2 velocidad;
            public float vida;
            public float total;
            public float gravedad;
            public float giro;
            public float crece;
            public float alfa;
        }

        /// <summary>Cómo sale una ráfaga. Los rangos se eligen al azar para cada partícula.</summary>
        public struct Rafaga
        {
            public Sprite forma;
            public int cantidad;
            public Vector2 centro;
            public Vector2 dispersion;
            public Color color;
            public Vector2 angulo;
            public Vector2 rapidez;
            public Vector2 vida;
            public Vector2 tamano;
            public float gravedad;
            public float crece;
            public float giro;
        }

        readonly List<Particula> vivas = new List<Particula>();

        public void Emitir(Rafaga r)
        {
            for (int i = 0; i < r.cantidad; i++)
            {
                float angulo = Random.Range(r.angulo.x, r.angulo.y) * Mathf.Deg2Rad;
                float rapidez = Random.Range(r.rapidez.x, r.rapidez.y);
                Vector2 posicion = r.centro + new Vector2(Random.Range(-r.dispersion.x, r.dispersion.x), Random.Range(-r.dispersion.y, r.dispersion.y));
                float lado = Random.Range(r.tamano.x, r.tamano.y);
                Una(r.forma, posicion, new Vector2(lado, lado), r.color,
                    new Vector2(Mathf.Cos(angulo), Mathf.Sin(angulo)) * rapidez,
                    Random.Range(r.vida.x, r.vida.y), r.gravedad, Random.Range(-r.giro, r.giro), r.crece);
            }
        }

        public RectTransform Una(Sprite forma, Vector2 posicion, Vector2 tamano, Color color, Vector2 velocidad, float vida, float gravedad, float giro, float crece)
        {
            var go = new GameObject("Particula", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(transform, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = posicion;
            rect.sizeDelta = tamano;
            // Solo las que giran empiezan torcidas; los anillos y elipses quedan derechos.
            rect.localRotation = Quaternion.Euler(0f, 0f, giro != 0f ? Random.Range(0f, 360f) : 0f);
            var imagen = go.GetComponent<Image>();
            imagen.sprite = forma;
            imagen.color = color;
            imagen.raycastTarget = false;
            imagen.preserveAspect = false;

            vivas.Add(new Particula
            {
                rect = rect,
                imagen = imagen,
                velocidad = velocidad,
                vida = Mathf.Max(0.05f, vida),
                total = Mathf.Max(0.05f, vida),
                gravedad = gravedad,
                giro = giro,
                crece = crece,
                alfa = color.a
            });
            return rect;
        }

        public void Limpiar()
        {
            for (int i = 0; i < vivas.Count; i++)
            {
                if (vivas[i].rect != null)
                    Destroy(vivas[i].rect.gameObject);
            }

            vivas.Clear();
        }

        void Update()
        {
            float dt = Time.deltaTime;
            for (int i = vivas.Count - 1; i >= 0; i--)
            {
                Particula p = vivas[i];
                p.vida -= dt;
                if (p.vida <= 0f || p.rect == null)
                {
                    if (p.rect != null)
                        Destroy(p.rect.gameObject);
                    vivas.RemoveAt(i);
                    continue;
                }

                float t = 1f - p.vida / p.total;
                p.velocidad.y -= p.gravedad * dt;
                p.rect.anchoredPosition += p.velocidad * dt;
                p.rect.localRotation = Quaternion.Euler(0f, 0f, p.rect.localEulerAngles.z + p.giro * dt);
                p.rect.localScale = Vector3.one * Mathf.Max(0.05f, 1f + p.crece * t);
                Color color = p.imagen.color;
                color.a = p.alfa * (t < 0.15f ? t / 0.15f : 1f - Mathf.Pow((t - 0.15f) / 0.85f, 2f));
                p.imagen.color = color;
            }
        }
    }

    /// <summary>
    /// Formas dibujadas por código para las partículas y el líquido.
    /// </summary>
    public static class FormasUI
    {
        static Sprite punto;
        static Sprite humo;
        static Sprite destello;
        static Sprite anillo;
        static Sprite elipse;
        static Sprite hoja;

        public static Sprite Punto => punto != null ? punto : punto = Crear(64, (x, y) => Borde(1f - Mathf.Sqrt(x * x + y * y), 0.08f));
        public static Sprite Humo => humo != null ? humo : humo = Crear(96, (x, y) => Mathf.Pow(Mathf.Clamp01(1f - Mathf.Sqrt(x * x + y * y)), 1.6f));
        public static Sprite Anillo => anillo != null ? anillo : anillo = Crear(128, (x, y) => Borde(1f - Mathf.Abs(Mathf.Sqrt(x * x + y * y) - 0.82f) / 0.12f, 0.5f));
        public static Sprite Elipse => elipse != null ? elipse : elipse = Crear(128, (x, y) => Borde(1f - Mathf.Sqrt(x * x + y * y), 0.18f));

        public static Sprite Destello => destello != null ? destello : destello = Crear(64, (x, y) =>
        {
            float ax = Mathf.Abs(x);
            float ay = Mathf.Abs(y);
            float cruz = Mathf.Max(Mathf.Clamp01(1f - ax / 0.12f) * Mathf.Clamp01(1f - ay), Mathf.Clamp01(1f - ay / 0.12f) * Mathf.Clamp01(1f - ax));
            float centro = Mathf.Clamp01(1f - Mathf.Sqrt(x * x + y * y) / 0.35f);
            return Mathf.Clamp01(cruz + centro * centro);
        });

        static Sprite corazon;

        public static Sprite Corazon => corazon != null ? corazon : corazon = Crear(64, (x, y) =>
        {
            float cx = x * 1.25f;
            float cy = y * 1.25f + 0.2f;
            float a = cx * cx + cy * cy - 1f;
            float f = a * a * a - cx * cx * cy * cy * cy;
            return Mathf.Clamp01(-f * 18f);
        });

        static Sprite vineta;

        /// <summary>Oscurece las orillas y deja el centro limpio (se estira a toda la pantalla).</summary>
        public static Sprite Vineta => vineta != null ? vineta : vineta = Crear(128, (x, y) =>
        {
            float r = Mathf.Sqrt(x * x + y * y);
            float k = Mathf.Clamp01((r - 0.6f) / 0.55f);
            return k * k * (3f - 2f * k);
        });

        static Sprite reloj;

        /// <summary>Reloj: aro, dos agujas y un punto al centro.</summary>
        public static Sprite Reloj => reloj != null ? reloj : reloj = Crear(64, (x, y) =>
        {
            float r = Mathf.Sqrt(x * x + y * y);
            float aro = Borde(1f - Mathf.Abs(r - 0.8f) / 0.13f, 0.4f);
            float minutero = Mathf.Abs(x) < 0.07f && y > 0f && y < 0.58f ? 1f : 0f;
            float horario = Mathf.Abs(y) < 0.07f && x > 0f && x < 0.42f ? 1f : 0f;
            float centro = r < 0.12f ? 1f : 0f;
            return Mathf.Max(Mathf.Max(aro, centro), Mathf.Max(minutero, horario));
        });

        static Sprite nota;

        /// <summary>Nota musical (corchea): cabeza ovalada, palito y banderita.</summary>
        public static Sprite Nota => nota != null ? nota : nota = Crear(64, (x, y) =>
        {
            float hx = (x + 0.28f) / 0.3f;
            float hy = (y + 0.55f) / 0.2f;
            float cabeza = Borde(1f - Mathf.Sqrt(hx * hx + hy * hy), 0.25f);
            float palito = x > -0.04f && x < 0.06f && y > -0.55f && y < 0.75f ? 1f : 0f;
            float bandera = x > 0f && x < 0.45f && y > 0.75f - x * 0.9f - 0.22f && y < 0.75f - x * 0.9f ? 1f : 0f;
            return Mathf.Max(cabeza, Mathf.Max(palito, bandera));
        });

        static Sprite remolino;

        /// <summary>Tres brazos en espiral, para el líquido que se mezcla.</summary>
        public static Sprite Remolino => remolino != null ? remolino : remolino = Crear(256, (x, y) =>
        {
            float r = Mathf.Sqrt(x * x + y * y);
            if (r > 1f)
                return 0f;
            float brazo = Mathf.Sin(3f * Mathf.Atan2(y, x) + r * 9f);
            return Mathf.Clamp01((brazo - 0.15f) / 0.5f) * Mathf.Clamp01((1f - r) / 0.2f) * Mathf.Clamp01(r / 0.12f);
        });

        public static Sprite Hoja =>hoja != null ? hoja : hoja = Crear(64, (x, y) =>
        {
            float ancho = 0.5f * (1f - y * y);
            return Borde(1f - Mathf.Abs(x) / Mathf.Max(0.01f, ancho), 0.2f) * (Mathf.Abs(y) < 0.95f ? 1f : 0f);
        });

        static float Borde(float valor, float suavidad) => Mathf.Clamp01(valor / suavidad);

        static Sprite Crear(int lado, System.Func<float, float, float> alfa)
        {
            var textura = new Texture2D(lado, lado, TextureFormat.RGBA32, false);
            var pixeles = new Color32[lado * lado];
            for (int y = 0; y < lado; y++)
            {
                for (int x = 0; x < lado; x++)
                {
                    float u = (x + 0.5f) / lado * 2f - 1f;
                    float v = (y + 0.5f) / lado * 2f - 1f;
                    pixeles[y * lado + x] = new Color32(255, 255, 255, (byte)(255f * Mathf.Clamp01(alfa(u, v))));
                }
            }

            textura.SetPixels32(pixeles);
            textura.Apply();
            textura.wrapMode = TextureWrapMode.Clamp;
            return Sprite.Create(textura, new Rect(0f, 0f, lado, lado), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
