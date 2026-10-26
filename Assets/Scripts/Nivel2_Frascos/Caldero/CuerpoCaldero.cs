using UnityEngine;
using UnityEngine.UI;

namespace FluxLab.Nivel2
{
    /// <summary>
    /// Recorta el caldero del dibujo del fondo (FondoN2) y lo pone encima, exactamente en su lugar,
    /// para poder moverlo: respira, rebota al recibir un frasco, se sacude al hervir,
    /// se tambalea con un error y baila al acertar.
    /// Solo se estira o ensancha (nunca se encoge) para que no asome el caldero pintado detrás.
    /// </summary>
    public class CuerpoCaldero : MonoBehaviour
    {
        // Las medidas del caldero pintado están en FondoNivel2 (si cambia el fondo, se cambian allá).
        static float AnchoDibujo => FondoNivel2.Ancho;
        static float AltoDibujo => FondoNivel2.Alto;
        static RectInt Zona => FondoNivel2.ZonaCaldero;
        static float CentroX => FondoNivel2.CentroCalderoX;
        static float PiesY => FondoNivel2.PiesCalderoY;
        static Vector4[] Silueta => FondoNivel2.SiluetaCaldero;

        RectTransform rect;
        float resorte;
        float velocidadResorte;
        float tambaleo;
        float festejo;

        /// <summary>De 0 a 2, lo pone CalderoVivo según cuánto hierve.</summary>
        public float Hervor { get; set; }

        public Vector2 Escala { get; private set; } = Vector2.one;
        public float Giro { get; private set; }

        public static CuerpoCaldero Crear(Transform padre, Texture2D fondo)
        {
            if (fondo == null)
                return null;

            Texture2D recorte = Recortar(fondo);
            var go = new GameObject("CalderoAnimado", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            go.transform.SetParent(padre, false);
            var imagen = go.GetComponent<RawImage>();
            imagen.texture = recorte;
            imagen.raycastTarget = false;

            // Anclado en proporción al fondo estirado: queda encima del dibujo en cualquier pantalla.
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(Zona.x / AnchoDibujo, 1f - (Zona.y + Zona.height) / AltoDibujo);
            rect.anchorMax = new Vector2((Zona.x + Zona.width) / AnchoDibujo, 1f - Zona.y / AltoDibujo);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2((CentroX - Zona.x) / Zona.width, (Zona.y + Zona.height - PiesY) / Zona.height);

            var cuerpo = go.AddComponent<CuerpoCaldero>();
            cuerpo.rect = rect;
            return cuerpo;
        }

        public void Rebotar(float fuerza) => velocidadResorte += fuerza * 6f;

        public void Tambalear() => tambaleo = 1f;

        public void Festejar()
        {
            festejo = 1f;
            Rebotar(1.8f);
        }

        /// <summary>
        /// Lleva un punto del canvas (por ejemplo la boca) a donde queda con el caldero estirado y girado.
        /// </summary>
        public Vector2 Mover(Vector2 punto)
        {
            Vector2 pies = rect.localPosition;
            Vector2 v = punto - pies;
            v = new Vector2(v.x * Escala.x, v.y * Escala.y);
            float angulo = Giro * Mathf.Deg2Rad;
            v = new Vector2(v.x * Mathf.Cos(angulo) - v.y * Mathf.Sin(angulo), v.x * Mathf.Sin(angulo) + v.y * Mathf.Cos(angulo));
            return pies + v;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            float t = Time.time;

            // Resorte: cada golpe lo estira y vuelve rebotando.
            float aceleracion = -70f * resorte - 7f * velocidadResorte;
            velocidadResorte += aceleracion * dt;
            resorte += velocidadResorte * dt;
            tambaleo = Mathf.MoveTowards(tambaleo, 0f, dt * 0.9f);
            festejo = Mathf.MoveTowards(festejo, 0f, dt * 0.8f);

            float respirar = (1f + Mathf.Sin(t * 2.4f)) * 0.5f;
            float burbujeo = Mathf.Abs(Mathf.Sin(t * 17f)) * Hervor;
            float alto = 1.02f + respirar * 0.018f + Mathf.Max(0f, resorte) * 0.11f + burbujeo * 0.022f;
            float ancho = 1.02f + (1f - respirar) * 0.012f + Mathf.Max(0f, -resorte) * 0.11f + burbujeo * 0.012f;

            Giro = Mathf.Sin(t * 13f) * 2.4f * festejo
                 + Mathf.Sin(t * 31f) * 2.6f * tambaleo
                 + Mathf.Sin(t * 23f) * 0.5f * Hervor;

            Escala = new Vector2(ancho, alto);
            rect.localScale = new Vector3(ancho, alto, 1f);
            rect.localRotation = Quaternion.Euler(0f, 0f, Giro);
        }

        static Texture2D Recortar(Texture2D fondo)
        {
            float fx = fondo.width / AnchoDibujo;
            float fy = fondo.height / AltoDibujo;
            int x0 = Mathf.RoundToInt(Zona.x * fx);
            int ancho = Mathf.RoundToInt(Zona.width * fx);
            int alto = Mathf.RoundToInt(Zona.height * fy);
            int y0 = fondo.height - Mathf.RoundToInt((Zona.y + Zona.height) * fy); // Unity cuenta desde abajo

            Color32[] pixeles = ImagenesFrascos.LeerPixeles(fondo);
            var recorte = new Color32[ancho * alto];
            for (int y = 0; y < alto; y++)
            {
                for (int x = 0; x < ancho; x++)
                {
                    Color32 p = pixeles[(y0 + y) * fondo.width + x0 + x];
                    float dibujoX = (x0 + x + 0.5f) / fx;
                    float dibujoY = AltoDibujo - (y0 + y + 0.5f) / fy;
                    p.a = (byte)(255f * Alfa(dibujoX, dibujoY));
                    recorte[y * ancho + x] = p;
                }
            }

            var textura = new Texture2D(ancho, alto, TextureFormat.RGBA32, false);
            textura.SetPixels32(recorte);
            textura.Apply();
            textura.wrapMode = TextureWrapMode.Clamp;
            textura.filterMode = FilterMode.Bilinear;
            return textura;
        }

        // 1 dentro de la silueta, 0 fuera, con un borde suave de unos 5 píxeles.
        static float Alfa(float x, float y)
        {
            float mejor = 0f;
            foreach (Vector4 e in Silueta)
            {
                float dx = (x - e.x) / e.z;
                float dy = (y - e.y) / e.w;
                float distancia = Mathf.Sqrt(dx * dx + dy * dy);
                float borde = 5f / Mathf.Min(e.z, e.w);
                mejor = Mathf.Max(mejor, Mathf.Clamp01((1f + borde - distancia) / borde));
            }

            return mejor;
        }
    }
}
