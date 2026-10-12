using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FluxLab.Nivel2
{
    /// <summary>
    /// Frascos pintados del fondo que reaccionan a cómo juegas. Cada frasco de FondoNivel2.FrascosPintados
    /// se recorta del dibujo (con orilla suave) y se pone exactamente encima, para poder moverlo:
    ///  - Mientras se juega: solo se mecen sutil, a veces uno tintinea y sube alguna burbujita.
    ///  - RomperUno() (al fallar): uno de las repisas se vuelca, cae y se rompe; luego reaparece por magia.
    ///  - Festejar() (al acertar una receta): ¡locura! todos saltan, algunos giran o vuelan en loop y brillan.
    /// Para que no se vea el frasco pintado mientras está fuera de su lugar, debajo hay un "parche"
    /// que rellena ese lugar mezclando los colores de los costados.
    /// Va debajo de la viñeta, para que tenga la misma luz que el resto del dibujo.
    /// </summary>
    public class FrascosDelFondo : MonoBehaviour
    {
        enum Estado { Quieto, Cayendo, Roto, Volviendo, Fiesta }

        class Frasco
        {
            public RectTransform rect;
            public RawImage imagen;
            public RawImage parche;
            public Image brillo;
            public Color colorLiquido;
            public float fase;
            public float amplitud;
            public float tintineo;          // 1 = acaba de tintinear, baja hasta 0
            public Vector2 boca;            // punta del cuello (coordenadas del canvas)
            public Vector2 centro;
            public float suelo;             // y del canvas donde se rompe si cae
            public bool puedeCaer;          // solo los de las repisas (los de la mesa no)
            public Estado estado;
            public float reloj;
            public Vector2 velocidad;
            public float giro;
            public float lado;              // hacia dónde se vuelca o vuela (-1 o 1)
            public Vector2 brilloBase;
            // Fiesta
            public float retraso;
            public float altura;
            public int vueltas;             // 0 = solo salta, 1 = da una vuelta en el aire
            public bool vuela;              // hace un loop en el aire
        }

        const float DuracionFiesta = 1.7f;

        /// <summary>Se avisa cuando un frasco se rompe (para que Magnus reaccione).</summary>
        public event System.Action AlRomperse;

        readonly List<Frasco> frascos = new List<Frasco>();
        EscenaFrascos escena;
        ConfigFrascos config;
        ParticulasUI chispas;
        float relojTintineo = 3f;
        float relojBurbujita = 2f;

        public void Construir(EscenaFrascos escena, ConfigFrascos config, Texture2D fondo)
        {
            this.escena = escena;
            this.config = config;
            var raiz = GetComponent<RectTransform>();
            raiz.anchorMin = Vector2.zero;
            raiz.anchorMax = Vector2.one;
            raiz.offsetMin = Vector2.zero;
            raiz.offsetMax = Vector2.zero;
            if (fondo == null)
                return;

            Color32[] pixeles = ImagenesFrascos.LeerPixeles(fondo);
            float fx = fondo.width / FondoNivel2.Ancho;
            float fy = fondo.height / FondoNivel2.Alto;

            foreach (Vector4 e in FondoNivel2.FrascosPintados)
            {
                var frasco = new Frasco
                {
                    fase = Random.Range(0f, 6.3f),
                    amplitud = Random.Range(0.6f, 1.2f),   // sutil mientras se juega
                    colorLiquido = ColorDelCentro(pixeles, fondo.width, fondo.height, e, fx, fy),
                    boca = FondoNivel2.ACanvas(e.x, e.y - e.w * 0.9f),
                    centro = FondoNivel2.ACanvas(e.x, e.y),
                    suelo = FondoNivel2.ACanvas(e.x, 650f).y,
                    puedeCaer = e.y + e.w < 600f
                };

                // Primero el parche (debajo), luego el frasco, luego su brillo (encima).
                frasco.parche = Recortar(pixeles, fondo.width, fondo.height, e, fx, fy, true);
                frasco.parche.color = new Color(1f, 1f, 1f, 0f);

                frasco.imagen = Recortar(pixeles, fondo.width, fondo.height, e, fx, fy, false);
                frasco.rect = frasco.imagen.rectTransform;

                Vector2 luz = FondoNivel2.ACanvas(e.x, e.y + e.w * 0.25f);
                frasco.brillo = escena.Imagen(transform, "BrilloFrasco", FormasUI.Humo, luz, Vector2.one * e.z * FondoNivel2.Escala * 3.2f, false);
                frasco.brillo.color = new Color(frasco.colorLiquido.r, frasco.colorLiquido.g, frasco.colorLiquido.b, 0f);
                frasco.brilloBase = luz;
                frascos.Add(frasco);
            }

            var capa = new GameObject("Chispas", typeof(RectTransform));
            capa.transform.SetParent(transform, false);
            chispas = capa.AddComponent<ParticulasUI>();
        }

        void Update()
        {
            if (config == null || !config.fondoVivo || frascos.Count == 0)
                return;
            float dt = Time.deltaTime;
            float t = Time.time;

            foreach (Frasco f in frascos)
            {
                switch (f.estado)
                {
                    case Estado.Quieto: Mecer(f, t, dt); break;
                    case Estado.Cayendo: Caer(f, dt); break;
                    case Estado.Roto: Esperar(f, dt); break;
                    case Estado.Volviendo: Volver(f, dt); break;
                    case Estado.Fiesta: Fiesta(f, t, dt); break;
                }

                // El brillo acompaña al frasco cuando se mueve.
                f.brillo.rectTransform.anchoredPosition = f.brilloBase + f.rect.anchoredPosition;
            }

            // Seguido un frasco tintinea.
            relojTintineo -= dt;
            if (relojTintineo <= 0f)
            {
                relojTintineo = Random.Range(3.5f, 7f);
                Frasco elegido = Elegir(false);
                if (elegido != null)
                {
                    elegido.tintineo = 0.6f;   // tintineo suave mientras se juega
                    chispas.Una(FormasUI.Destello, elegido.boca, Vector2.one * Random.Range(18f, 28f), Color.white, Vector2.zero, 0.5f, 0f, 120f, 0.5f);
                }
            }

            // A veces sube una burbujita por el cuello de algún frasco.
            relojBurbujita -= dt;
            if (relojBurbujita <= 0f)
            {
                relojBurbujita = Random.Range(1.6f, 3f);
                Frasco elegido = Elegir(false);
                if (elegido != null)
                {
                    Color color = Color.Lerp(elegido.colorLiquido, Color.white, 0.5f);
                    color.a = 0.85f;
                    chispas.Una(FormasUI.Punto, elegido.boca, Vector2.one * Random.Range(7f, 12f), color,
                        new Vector2(Random.Range(-6f, 6f), Random.Range(25f, 40f)), Random.Range(1.2f, 1.8f), 0f, 0f, 0.3f);
                }
            }

        }

        // ───────────────────────── Reacciones al juego ─────────────────────────

        /// <summary>Al fallar: varios frascos de las repisas se vuelcan, caen y se rompen, uno tras otro.</summary>
        public void RomperVarios(int cantidad = 3)
        {
            if (config == null || !config.fondoVivo || !config.frascosReaccionan)
                return;
            for (int i = 0; i < cantidad; i++)
            {
                Frasco elegido = Elegir(true);
                if (elegido == null)
                    return;
                elegido.estado = Estado.Cayendo;
                elegido.reloj = -i * Random.Range(0.18f, 0.32f);   // en cadena: cada uno empieza un poco después
                elegido.lado = Random.value < 0.5f ? -1f : 1f;
                elegido.velocidad = Vector2.zero;
                elegido.giro = 0f;
            }
        }

        /// <summary>Al acertar una receta: ¡todos los frascos festejan!</summary>
        public void Festejar()
        {
            if (config == null || !config.fondoVivo || !config.frascosReaccionan)
                return;
            foreach (Frasco f in frascos)
            {
                if (f.estado != Estado.Quieto)
                    continue;
                f.estado = Estado.Fiesta;
                f.reloj = 0f;
                f.retraso = Random.Range(0f, 0.35f);
                f.altura = Random.Range(40f, 110f) * (f.puedeCaer ? 1f : 0.7f);
                f.vueltas = Random.value < 0.35f ? 1 : 0;
                f.vuela = f.puedeCaer && Random.value < 0.3f;
                f.lado = Random.value < 0.5f ? -1f : 1f;
            }
            escena.Sonar("Nivel2/cristal", 0.3f);
        }

        // ───────────────────────── Estados ─────────────────────────

        // Mientras se juega: movimiento sutil.
        void Mecer(Frasco f, float t, float dt)
        {
            f.tintineo = Mathf.MoveTowards(f.tintineo, 0f, dt * 1.2f);
            float temblor = Mathf.Sin((1f - f.tintineo) * 26f) * 4f * f.tintineo;
            float saltito = Mathf.Sin((1f - f.tintineo) * Mathf.PI) * 3f * f.tintineo;
            f.rect.anchoredPosition = new Vector2(0f, saltito);
            f.rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 0.8f + f.fase) * f.amplitud + temblor);
            f.rect.localScale = new Vector3(1f, 1f + Mathf.Sin(t * 1.2f + f.fase) * 0.006f, 1f);

            Color c = f.brillo.color;
            c.a = 0.1f + (Mathf.Sin(t * 0.9f + f.fase) + 1f) * 0.06f + f.tintineo * 0.3f;
            f.brillo.color = c;
        }

        // ¡Locura! Saltos con aplaste al caer, alguna vuelta en el aire o un loop volando.
        void Fiesta(Frasco f, float t, float dt)
        {
            f.reloj += dt;
            float r = f.reloj - f.retraso;
            if (r < 0f)
            {
                Mecer(f, t, dt);
                return;
            }

            f.parche.color = Color.white;   // el frasco sale de su lugar: tapa el pintado
            float u = Mathf.Clamp01(r / DuracionFiesta);
            Vector2 mover;
            float giro;
            float aplaste = 0f;

            if (f.vuela)
            {
                // Un loop en el aire y de vuelta a su lugar.
                float a = u * Mathf.PI * 2f;
                float radio = f.altura * 0.8f;
                mover = new Vector2(Mathf.Sin(a) * radio * f.lado, (1f - Mathf.Cos(a)) * radio * 0.9f);
                giro = -f.lado * u * 360f;
            }
            else
            {
                // Dos saltos (el segundo más bajo), con una vuelta en el primero si le toca.
                float salto = u < 0.55f ? Mathf.Sin(u / 0.55f * Mathf.PI) : Mathf.Sin((u - 0.55f) / 0.45f * Mathf.PI) * 0.45f;
                mover = new Vector2(Mathf.Sin(u * Mathf.PI * 4f) * 6f, Mathf.Max(0f, salto) * f.altura);
                giro = (u < 0.55f ? f.vueltas * 360f * (u / 0.55f) : 0f) + Mathf.Sin(u * Mathf.PI * 6f) * 8f * (1f - u);
                float alSuelo = Mathf.Min(Mathf.Abs(u - 0.55f), Mathf.Abs(u - 1f));
                aplaste = Mathf.Clamp01(1f - alSuelo / 0.06f) * 0.15f;
            }

            f.rect.anchoredPosition = mover;
            f.rect.localRotation = Quaternion.Euler(0f, 0f, giro);
            f.rect.localScale = new Vector3(1f + aplaste, 1f - aplaste, 1f);
            Color c = f.brillo.color;
            c.a = 0.55f + Mathf.Sin(r * 12f) * 0.15f;
            f.brillo.color = c;

            // Destellos y gotitas de su color mientras festeja.
            if (Random.value < dt * 6f)
            {
                Vector2 punto = f.centro + mover;
                chispas.Una(Random.value < 0.6f ? FormasUI.Destello : FormasUI.Punto, punto + new Vector2(Random.Range(-20f, 20f), Random.Range(-10f, 30f)),
                    Vector2.one * Random.Range(12f, 24f), Random.value < 0.5f ? Color.white : Color.Lerp(f.colorLiquido, Color.white, 0.3f),
                    new Vector2(Random.Range(-60f, 60f), Random.Range(40f, 140f)), Random.Range(0.6f, 1f), 180f, Random.Range(-200f, 200f), 0f);
            }

            if (u >= 1f)
            {
                f.estado = Estado.Quieto;
                f.rect.anchoredPosition = Vector2.zero;
                f.rect.localRotation = Quaternion.identity;
                f.rect.localScale = Vector3.one;
                f.parche.color = new Color(1f, 1f, 1f, 0f);
            }
        }

        // Se tambalea cada vez más fuerte, se vuelca y cae girando hasta el suelo.
        void Caer(Frasco f, float dt)
        {
            f.reloj += dt;
            if (f.reloj < 0f)
                return;   // todavía le falta su turno en la cadena
            if (f.reloj < 0.7f)
            {
                float k = f.reloj / 0.7f;
                f.rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(f.reloj * 30f) * (4f + k * 10f) + f.lado * k * 18f);
                f.rect.anchoredPosition = new Vector2(f.lado * k * 6f, Mathf.Abs(Mathf.Sin(f.reloj * 30f)) * 3f);
                if (f.parche != null && f.reloj > 0.4f)
                    f.parche.color = Color.white;   // desde aquí el frasco pintado ya no debe verse
                return;
            }

            if (f.parche != null)
                f.parche.color = Color.white;
            f.velocidad += new Vector2(0f, -1400f) * dt;
            if (f.velocidad.x == 0f)
                f.velocidad.x = f.lado * 70f;
            f.giro += f.lado * 360f * dt;
            f.rect.anchoredPosition += f.velocidad * dt;
            f.rect.localRotation = Quaternion.Euler(0f, 0f, f.lado * 18f + f.giro);
            f.brillo.color = new Color(f.colorLiquido.r, f.colorLiquido.g, f.colorLiquido.b, 0f);

            float y = f.centro.y + f.rect.anchoredPosition.y;
            if (y <= f.suelo)
                Romper(f, new Vector2(f.centro.x + f.rect.anchoredPosition.x, f.suelo));
        }

        void Romper(Frasco f, Vector2 donde)
        {
            f.estado = Estado.Roto;
            f.reloj = 0f;
            f.imagen.color = new Color(1f, 1f, 1f, 0f);
            escena.Sonar("Nivel2/romper", 0.45f);

            // Trocitos de vidrio, salpicadura del líquido y un charquito.
            chispas.Emitir(new ParticulasUI.Rafaga
            {
                forma = FormasUI.Destello, cantidad = 14, centro = donde, dispersion = new Vector2(16f, 4f),
                color = new Color(0.85f, 0.95f, 1f, 1f), angulo = new Vector2(20f, 160f), rapidez = new Vector2(180f, 380f),
                vida = new Vector2(0.5f, 0.9f), tamano = new Vector2(10f, 20f), gravedad = 900f, giro = 600f
            });
            chispas.Emitir(new ParticulasUI.Rafaga
            {
                forma = FormasUI.Punto, cantidad = 16, centro = donde, dispersion = new Vector2(20f, 4f),
                color = f.colorLiquido, angulo = new Vector2(30f, 150f), rapidez = new Vector2(120f, 300f),
                vida = new Vector2(0.5f, 0.9f), tamano = new Vector2(8f, 16f), gravedad = 800f
            });
            chispas.Una(FormasUI.Elipse, donde, new Vector2(80f, 18f), new Color(f.colorLiquido.r, f.colorLiquido.g, f.colorLiquido.b, 0.7f),
                Vector2.zero, 2.4f, 0f, 0f, 0.8f);
            chispas.Una(FormasUI.Humo, donde + new Vector2(0f, 20f), new Vector2(110f, 110f), new Color(1f, 1f, 1f, 0.5f), Vector2.zero, 0.4f, 0f, 0f, 0.8f);

            AlRomperse?.Invoke();
        }

        // Unos segundos sin el frasco y luego reaparece por magia.
        void Esperar(Frasco f, float dt)
        {
            f.reloj += dt;
            if (f.reloj < 3f)
                return;
            f.estado = Estado.Volviendo;
            f.reloj = 0f;
            f.rect.anchoredPosition = Vector2.zero;
            f.rect.localRotation = Quaternion.identity;
            chispas.Emitir(new ParticulasUI.Rafaga
            {
                forma = FormasUI.Destello, cantidad = 12, centro = f.centro, dispersion = new Vector2(20f, 30f),
                color = new Color(1f, 0.95f, 0.7f, 1f), angulo = new Vector2(0f, 360f), rapidez = new Vector2(40f, 140f),
                vida = new Vector2(0.6f, 1f), tamano = new Vector2(14f, 26f), giro = 200f, crece = 0.3f
            });
            escena.Sonar("Nivel2/estrella", 0.25f);
        }

        void Volver(Frasco f, float dt)
        {
            f.reloj += dt;
            float k = Mathf.Clamp01(f.reloj / 0.7f);
            f.imagen.color = new Color(1f, 1f, 1f, k);
            f.rect.localScale = Vector3.one * Mathf.Lerp(0.6f, 1f, 1f - (1f - k) * (1f - k));
            if (f.parche != null)
                f.parche.color = new Color(1f, 1f, 1f, 1f - k);
            if (k >= 1f)
            {
                f.estado = Estado.Quieto;
                f.tintineo = 1f;
            }
        }

        // ───────────────────────── Ayudas ─────────────────────────

        Frasco Elegir(bool soloLosQueCaen)
        {
            var candidatos = new List<Frasco>();
            foreach (Frasco f in frascos)
            {
                if (f.estado == Estado.Quieto && (!soloLosQueCaen || f.puedeCaer))
                    candidatos.Add(f);
            }

            return candidatos.Count == 0 ? null : candidatos[Random.Range(0, candidatos.Count)];
        }

        /// <summary>
        /// Recorta el frasco del dibujo con una orilla suave y lo pone encima, girando desde la base.
        /// Con "parche" en true, en vez del frasco rellena ese lugar mezclando los colores de los costados.
        /// </summary>
        RawImage Recortar(Color32[] pixeles, int anchoTextura, int altoTextura, Vector4 e, float fx, float fy, bool parche)
        {
            float margen = 1.15f;
            int x0 = Mathf.Max(0, Mathf.FloorToInt((e.x - e.z * margen) * fx));
            int x1 = Mathf.Min(anchoTextura - 1, Mathf.CeilToInt((e.x + e.z * margen) * fx));
            // En la textura la fila 0 es la de abajo.
            int yArriba = Mathf.Min(altoTextura - 1, altoTextura - 1 - Mathf.FloorToInt((e.y - e.w * margen) * fy));
            int yAbajo = Mathf.Max(0, altoTextura - 1 - Mathf.CeilToInt((e.y + e.w * margen) * fy));
            int ancho = x1 - x0 + 1;
            int alto = yArriba - yAbajo + 1;

            var recorte = new Color32[ancho * alto];
            for (int y = 0; y < alto; y++)
            {
                Color32 izquierda = pixeles[(yAbajo + y) * anchoTextura + x0];
                Color32 derecha = pixeles[(yAbajo + y) * anchoTextura + x1];
                for (int x = 0; x < ancho; x++)
                {
                    float dibujoX = (x0 + x + 0.5f) / fx;
                    float dibujoY = FondoNivel2.Alto - (yAbajo + y + 0.5f) / fy;
                    float dx = (dibujoX - e.x) / e.z;
                    float dy = (dibujoY - e.y) / e.w;
                    float distancia = Mathf.Sqrt(dx * dx + dy * dy);
                    Color32 p = parche
                        ? Color32.Lerp(izquierda, derecha, x / (float)Mathf.Max(1, ancho - 1))
                        : pixeles[(yAbajo + y) * anchoTextura + x0 + x];
                    // Orilla muy suave: nada de bordes marcados al moverse.
                    p.a = (byte)(255f * Mathf.Clamp01((1.1f - distancia) / 0.3f));
                    recorte[y * ancho + x] = p;
                }
            }

            var textura = new Texture2D(ancho, alto, TextureFormat.RGBA32, false);
            textura.SetPixels32(recorte);
            textura.Apply();
            textura.wrapMode = TextureWrapMode.Clamp;
            textura.filterMode = FilterMode.Bilinear;

            var go = new GameObject(parche ? "ParcheFrasco" : "FrascoPintado", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            go.transform.SetParent(transform, false);
            var imagen = go.GetComponent<RawImage>();
            imagen.texture = textura;
            imagen.raycastTarget = false;

            // Anclado en proporción al fondo estirado (igual que el caldero): calza en cualquier pantalla.
            var rect = go.GetComponent<RectTransform>();
            float izquierdaN = x0 / (float)anchoTextura;
            float derechaN = (x1 + 1) / (float)anchoTextura;
            float abajo = yAbajo / (float)altoTextura;
            float arriba = (yArriba + 1) / (float)altoTextura;
            rect.anchorMin = new Vector2(izquierdaN, abajo);
            rect.anchorMax = new Vector2(derechaN, arriba);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            // Gira desde la base del frasco.
            float baseFrasco = 1f - (e.y + e.w) / FondoNivel2.Alto;
            rect.pivot = new Vector2(0.5f, Mathf.Clamp01((baseFrasco - abajo) / (arriba - abajo)));
            return imagen;
        }

        static Color ColorDelCentro(Color32[] pixeles, int ancho, int alto, Vector4 e, float fx, float fy)
        {
            // Promedio de un cuadradito en la parte baja del frasco (donde está el líquido).
            int cx = Mathf.RoundToInt(e.x * fx);
            int cy = alto - 1 - Mathf.RoundToInt((e.y + e.w * 0.35f) * fy);
            float r = 0f, g = 0f, b = 0f;
            int n = 0;
            for (int y = -3; y <= 3; y++)
            {
                for (int x = -3; x <= 3; x++)
                {
                    int px = Mathf.Clamp(cx + x, 0, ancho - 1);
                    int py = Mathf.Clamp(cy + y, 0, alto - 1);
                    Color32 p = pixeles[py * ancho + px];
                    r += p.r; g += p.g; b += p.b; n++;
                }
            }

            return Color.Lerp(new Color(r / n / 255f, g / n / 255f, b / n / 255f, 1f), Color.white, 0.15f);
        }
    }
}
