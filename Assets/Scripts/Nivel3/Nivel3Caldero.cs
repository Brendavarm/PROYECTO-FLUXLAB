using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace FluxLab
{
    /// <summary>
    /// El caldero del nivel 3: burbujea solo, se ilumina con cada ingrediente (Hechizo),
    /// celebra cuando completas la receta (Triunfo) y explota cuando te equivocas (Explotar).
    /// Si existe Resources/FluxLab/Nivel3/caldero.png usa ese dibujo; si no, el caldero.png de siempre.
    /// </summary>
    public class Nivel3Caldero : MonoBehaviour
    {
        // Dónde está la boca del caldero dentro de su dibujo (fracciones del alto y del ancho).
        // Se ajustan si el dibujo cambia.
        public float bocaY = 0.752f;
        public float bocaAncho = 0.50f;
        // El dibujo tiene un poco de aire bajo las patas (fracción del alto); se descuenta para que apoye en la mesa.
        public float margenInferior = 0.057f;

        static readonly Color VerdeBase = new Color(0.4f, 1f, 0.4f);

        RectTransform raiz;
        Image cuerpo;
        Image brillo;
        Image destello;
        Transform padre;
        Vector2 posicionBase;
        Vector2 sacudida;
        float ancho;
        float alto;
        float energia;
        Color colorMagia = VerdeBase;
        float proximaBurbuja;
        Sprite[] burbujas;

        /// <summary>Centro de la boca, en coordenadas del lienzo.</summary>
        public Vector2 Boca
        {
            get { return new Vector2(posicionBase.x, posicionBase.y + alto * bocaY); }
        }

        public float Alto { get { return alto; } }

        public static Nivel3Caldero Crear(Transform padre, float x, float ySuelo, float alto)
        {
            Sprite dibujo = Nivel3UI.SpriteOpcional("FluxLab/Nivel3/caldero");
            if (dibujo == null)
                dibujo = Nivel3UI.SpriteOpcional("FluxLab/caldero");

            float ancho = dibujo != null ? alto * dibujo.rect.width / dibujo.rect.height : alto * 1.2f;

            var go = new GameObject("Caldero", typeof(RectTransform));
            go.transform.SetParent(padre, false);
            var r = go.GetComponent<RectTransform>();
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.pivot = new Vector2(0.5f, 0f); // los pies del caldero están en la mesa
            r.sizeDelta = new Vector2(ancho, alto);
            var caldero = go.AddComponent<Nivel3Caldero>();
            ySuelo -= alto * caldero.margenInferior;
            r.anchoredPosition = new Vector2(x, ySuelo);
            caldero.raiz = r;
            caldero.padre = padre;
            caldero.posicionBase = new Vector2(x, ySuelo);
            caldero.ancho = ancho;
            caldero.alto = alto;
            caldero.burbujas = new[]
            {
                Resources.Load<Sprite>("FluxLab/burbuja_a"),
                Resources.Load<Sprite>("FluxLab/burbuja_b"),
                Resources.Load<Sprite>("FluxLab/burbuja_c")
            };

            // Todo lo de dentro se coloca respecto al centro del caldero.
            caldero.cuerpo = Nivel3UI.Imagen(go.transform, "Cuerpo", dibujo != null ? dibujo : Nivel3UI.Solido(), Vector2.zero, r.sizeDelta, true);
            float yBoca = alto * caldero.bocaY - alto * 0.5f;
            caldero.brillo = Nivel3UI.Imagen(go.transform, "Brillo", Nivel3UI.LuzSuave(), new Vector2(0f, yBoca), new Vector2(ancho * caldero.bocaAncho * 1.8f, alto * 0.25f));
            caldero.destello = Nivel3UI.Imagen(go.transform, "Destello", Nivel3UI.LuzSuave(), new Vector2(0f, yBoca), new Vector2(ancho * 1.7f, alto * 1.3f));
            caldero.destello.color = new Color(1f, 1f, 1f, 0f);
            return caldero;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            energia = Mathf.MoveTowards(energia, 0f, dt * 1.1f);

            // La luz del líquido late suave; con la magia se tiñe y crece.
            float latido = 0.5f + 0.5f * Mathf.Sin(Time.time * 2.2f);
            Color c = Color.Lerp(VerdeBase, colorMagia, Mathf.Clamp01(energia * 1.4f));
            brillo.color = new Color(c.r, c.g, c.b, Mathf.Clamp01(0.22f + 0.16f * latido + 0.55f * energia));
            brillo.rectTransform.localScale = Vector3.one * (1f + 0.5f * energia);

            raiz.anchoredPosition = posicionBase + sacudida;

            // Burbujas que suben desde la boca y se apagan
            proximaBurbuja -= dt;
            if (proximaBurbuja <= 0f)
            {
                proximaBurbuja = Random.Range(0.22f, 0.5f);
                SoltarBurbuja();
            }
        }

        void SoltarBurbuja()
        {
            Sprite sprite = burbujas[Random.Range(0, burbujas.Length)];
            if (sprite == null)
                return;

            float lado = Random.Range(24f, 46f);
            Vector2 desde = Boca + new Vector2(Random.Range(-1f, 1f) * ancho * bocaAncho * 0.4f, Random.Range(-8f, 8f));
            Particula.Crear(padre, sprite, desde, Vector2.one * lado, new Color(0.85f, 1f, 0.85f, 0.85f),
                new Vector2(Random.Range(-12f, 12f), Random.Range(55f, 95f)), 0f, Random.Range(1.2f, 1.8f), 0f, 0.6f);
        }

        // ---------------- Reacciones ----------------

        /// <summary>Un ingrediente cae en el caldero: se ilumina del color del ingrediente y salen chispas.</summary>
        public void Hechizo(Color color)
        {
            energia = 1f;
            colorMagia = color;
            Chispas(9, Color.Lerp(color, Color.white, 0.5f), 150f, 330f);
            Anillo(Color.Lerp(color, Color.white, 0.3f), 0.6f, 2.6f);
            StartCoroutine(Humo(color, 9, 0.7f));
            StartCoroutine(Pulso(1.07f, 0.22f));
        }

        /// <summary>Terminaste la receta: una explosión de luz dorada, bonita.</summary>
        public void Triunfo()
        {
            energia = 1f;
            colorMagia = new Color(1f, 0.85f, 0.3f);
            Chispas(30, new Color(1f, 0.92f, 0.5f), 200f, 520f);
            Anillo(new Color(1f, 0.9f, 0.4f), 0.9f, 4.5f);
            Anillo(new Color(1f, 1f, 0.8f), 1.3f, 6.5f);
            StartCoroutine(Humo(new Color(1f, 0.85f, 0.3f), 16, 1.1f));
            StartCoroutine(Pulso(1.14f, 0.35f));
        }

        public void Explotar()
        {
            StartCoroutine(ExplotarCo());
        }

        IEnumerator ExplotarCo()
        {
            energia = 1.6f;
            colorMagia = new Color(1f, 0.5f, 0.12f);
            Vector2 boca = Boca;
            Sprite disco = FrascoQuebradizo.Disco();

            // Chorros de líquido
            for (int i = 0; i < 22; i++)
            {
                Color color = i % 2 == 0 ? new Color(0.45f, 0.95f, 0.35f, 0.95f) : new Color(1f, 0.6f, 0.15f, 0.95f);
                float lado = Random.Range(18f, 42f);
                Particula.Crear(padre, disco, boca + new Vector2(Random.Range(-60f, 60f), 0f), Vector2.one * lado, color,
                    new Vector2(Random.Range(-380f, 380f), Random.Range(380f, 900f)), 1500f, Random.Range(0.9f, 1.3f), 0f, 0.6f);
            }

            // Chispas hacia todos lados
            Chispas(16, new Color(1f, 0.82f, 0.3f), 320f, 720f);

            // Humo que sube y se ensancha
            for (int i = 0; i < 8; i++)
            {
                float lado = Random.Range(60f, 110f);
                Particula.Crear(padre, disco, boca + new Vector2(Random.Range(-70f, 70f), Random.Range(0f, 30f)), Vector2.one * lado,
                    new Color(0.4f, 0.37f, 0.45f, 0.7f), new Vector2(Random.Range(-60f, 60f), Random.Range(90f, 170f)), -30f, Random.Range(1.3f, 1.8f), 0f, 2.3f);
            }

            Anillo(new Color(1f, 0.6f, 0.2f), 0.5f, 5f);
            StartCoroutine(Pulso(1.25f, 0.3f));

            // Fogonazo, temblor y un poco de hollín
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / 0.55f;
                float resto = 1f - Mathf.Clamp01(t);
                destello.color = new Color(1f, 0.95f, 0.8f, 0.95f * resto * resto);
                sacudida = Random.insideUnitCircle * (18f * resto);
                float hollin = Mathf.Lerp(0.5f, 1f, Mathf.Clamp01(t));
                cuerpo.color = new Color(hollin, hollin * 0.95f, hollin * 0.95f, 1f);
                yield return null;
            }

            sacudida = Vector2.zero;
            destello.color = new Color(1f, 1f, 1f, 0f);
            cuerpo.color = Color.white;
        }

        // ---------------- Efectos ----------------

        void Chispas(int cantidad, Color color, float velocidadMin, float velocidadMax)
        {
            Sprite estrella = FrascoBrillante.Estrella();
            Vector2 boca = Boca;
            for (int i = 0; i < cantidad; i++)
            {
                float angulo = Random.Range(0.15f, Mathf.PI - 0.15f);
                float rapidez = Random.Range(velocidadMin, velocidadMax);
                Color c = Color.Lerp(color, Color.white, Random.Range(0f, 0.4f));
                float lado = Random.Range(22f, 44f);
                Particula.Crear(padre, estrella, boca + new Vector2(Random.Range(-50f, 50f), 0f), Vector2.one * lado, c,
                    new Vector2(Mathf.Cos(angulo) * rapidez, Mathf.Sin(angulo) * rapidez), 380f, Random.Range(0.7f, 1.1f),
                    Random.Range(-300f, 300f), 0.1f);
            }
        }

        /// <summary>Una nube de humo del color del ingrediente: sube desde la boca del caldero, se ensancha y se desvanece.</summary>
        IEnumerator Humo(Color color, int bocanadas, float segundos)
        {
            Sprite suave = Nivel3UI.LuzSuave();
            Color humo = Color.Lerp(color, Color.white, 0.15f);
            humo.a = 0.6f;
            float espera = segundos / Mathf.Max(1, bocanadas);

            for (int i = 0; i < bocanadas; i++)
            {
                Vector2 desde = Boca + new Vector2(Random.Range(-1f, 1f) * ancho * bocaAncho * 0.35f, Random.Range(-6f, 10f));
                float lado = Random.Range(90f, 150f);
                Particula.Crear(padre, suave, desde, Vector2.one * lado, humo,
                    new Vector2(Random.Range(-28f, 28f), Random.Range(90f, 170f)), -18f, Random.Range(1.5f, 2.2f),
                    Random.Range(-20f, 20f), 2.4f);
                yield return new WaitForSeconds(espera);
            }
        }

        void Anillo(Color color, float duracion, float crecimiento)
        {
            color.a = 0.55f;
            Particula.Crear(padre, Nivel3UI.LuzSuave(), Boca, Vector2.one * 90f, color, Vector2.zero, 0f, duracion, 0f, crecimiento);
        }

        IEnumerator Pulso(float escala, float segundos)
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / segundos;
                float s = 1f + (escala - 1f) * Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);
                raiz.localScale = new Vector3(s, s, 1f);
                yield return null;
            }

            raiz.localScale = Vector3.one;
        }
    }
}
