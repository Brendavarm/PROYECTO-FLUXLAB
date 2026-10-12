using UnityEngine;
using UnityEngine.UI;

namespace FluxLab
{
    /// <summary>
    /// Un frasco sobre la mesa. Si Mishi cae cerca, se tambalea, se cae de la mesa
    /// y se rompe en pedacitos. Unos segundos después vuelve a aparecer en su lugar.
    /// </summary>
    public class FrascoQuebradizo : MonoBehaviour
    {
        enum Fase
        {
            Quieto,
            Tambalea,
            Cae,
            Roto,
            Reaparece
        }

        const float Gravedad = 1500f;
        const float SegundosParaVolver = 8f;

        static Sprite disco;
        static Sprite cuadro;

        RectTransform rect;
        Image imagen;
        FrascosMesa mesa;
        Color liquido;
        Vector2 casa;     // centro del frasco cuando está en su lugar
        float yBase;      // altura de la superficie donde está parado
        float rotura;     // altura a la que se romperá esta vez
        GameObject vida;  // brillo y burbujas del líquido
        Fase fase = Fase.Quieto;
        float t;
        float dir;
        Vector2 velocidad;
        float giroVel;

        public bool Disponible => fase == Fase.Quieto;
        public float X => casa.x;
        public float YBase => yBase;

        /// <param name="alturaPx">Alto del frasco en el lienzo.</param>
        /// <param name="espacioAbajo">Fracción vacía bajo el cuerpo del dibujo (para que apoye bien en la superficie).</param>
        public static FrascoQuebradizo Crear(FrascosMesa mesa, Sprite dibujo, Color liquido, float x, float ySuperficie, float alturaPx, float espacioAbajo)
        {
            var go = new GameObject("Frasco", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(mesa.transform, false);

            var imagen = go.GetComponent<Image>();
            imagen.sprite = dibujo;
            imagen.preserveAspect = true;
            imagen.raycastTarget = false;

            var r = go.GetComponent<RectTransform>();
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.pivot = new Vector2(0.5f, 0.5f); // gira alrededor de su centro al caer
            r.sizeDelta = dibujo.rect.size * (alturaPx / dibujo.rect.height);

            var frasco = go.AddComponent<FrascoQuebradizo>();
            frasco.mesa = mesa;
            frasco.liquido = liquido;
            frasco.yBase = ySuperficie;
            frasco.casa = new Vector2(x, ySuperficie + (0.5f - espacioAbajo) * r.sizeDelta.y);
            r.anchoredPosition = frasco.casa;

            // Luz y estrellitas que titilan alrededor del frasco.
            var brillo = FrascoBrillante.Crear(go.transform, r.sizeDelta.x, r.sizeDelta.y, liquido);
            frasco.vida = brillo.gameObject;
            return frasco;
        }

        void Awake()
        {
            rect = GetComponent<RectTransform>();
            imagen = GetComponent<Image>();
        }

        /// <summary>Mishi cayó cerca: el frasco se inclina hacia el lado contrario a ella.</summary>
        public void Golpear(float xDelGato)
        {
            if (fase != Fase.Quieto)
                return;

            dir = casa.x >= xDelGato ? 1f : -1f;
            // Desde una repisa y encima de la mesa: choca con la mesa. Desde la mesa: cae al frente.
            bool sobreMesa = yBase > mesa.alturaMesa + 50f && casa.x > mesa.mesaIzquierda && casa.x < mesa.mesaDerecha;
            rotura = sobreMesa ? mesa.alturaMesa + rect.sizeDelta.y * 0.5f : mesa.alturaRotura;
            fase = Fase.Tambalea;
            t = 0f;
            mesa.Sonar(mesa.sonidoGolpe, 0.8f);
        }

        void Update()
        {
            switch (fase)
            {
                case Fase.Tambalea:
                    t += Time.deltaTime / 0.35f;
                    float inclina = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
                    rect.localRotation = Quaternion.Euler(0f, 0f, -dir * 40f * inclina);
                    rect.anchoredPosition = casa + new Vector2(dir * 26f * inclina, 0f);
                    if (t >= 1f)
                    {
                        fase = Fase.Cae;
                        velocidad = new Vector2(dir * 150f, 120f);   // sale rodando del borde
                        giroVel = -dir * 260f;
                    }
                    break;

                case Fase.Cae:
                    velocidad.y -= Gravedad * Time.deltaTime;
                    rect.anchoredPosition += velocidad * Time.deltaTime;
                    rect.localRotation *= Quaternion.Euler(0f, 0f, giroVel * Time.deltaTime);
                    if (rect.anchoredPosition.y <= rotura)
                        Romper(rect.anchoredPosition);
                    break;

                case Fase.Roto:
                    t += Time.deltaTime;
                    if (t >= SegundosParaVolver)
                    {
                        fase = Fase.Reaparece;
                        t = 0f;
                        rect.localRotation = Quaternion.identity;
                        rect.anchoredPosition = casa;
                        imagen.enabled = true;
                        vida.SetActive(true);
                    }
                    break;

                case Fase.Reaparece:
                    t += Time.deltaTime / 0.4f;
                    rect.localScale = Vector3.one * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
                    if (t >= 1f)
                    {
                        rect.localScale = Vector3.one;
                        fase = Fase.Quieto;
                    }
                    break;
            }
        }

        void Romper(Vector2 punto)
        {
            fase = Fase.Roto;
            t = 0f;
            imagen.enabled = false;
            vida.SetActive(false);
            mesa.Sonar(mesa.sonidoRotura, 0.9f);

            Color vidrio = new Color(0.8f, 0.95f, 1f, 0.9f);

            // Pedacitos de vidrio y de líquido que saltan hacia los lados
            for (int i = 0; i < 14; i++)
            {
                Color color = i % 2 == 0 ? vidrio : liquido;
                float lado = Random.Range(8f, 20f);
                var pieza = Pieza(punto + Random.insideUnitCircle * 14f, Cuadro(), new Vector2(lado, lado * Random.Range(0.4f, 1f)), color);
                pieza.velocidad = new Vector2(Random.Range(-320f, 320f), Random.Range(160f, 480f));
                pieza.gravedad = Gravedad;
                pieza.giro = Random.Range(-540f, 540f);
                pieza.vida = Random.Range(0.7f, 1.1f);
            }

            // Salpicadura que se expande y un charquito que se seca despacio
            var salpica = Pieza(punto, Disco(), new Vector2(40f, 40f), new Color(liquido.r, liquido.g, liquido.b, 0.7f));
            salpica.crece = 4f;
            salpica.vida = 0.45f;

            var charco = Pieza(new Vector2(punto.x, rotura - rect.sizeDelta.y * 0.5f + 6f), Disco(), new Vector2(210f, 34f), new Color(liquido.r, liquido.g, liquido.b, 0.55f));
            charco.vida = 4.5f;
        }

        PiezaVolante Pieza(Vector2 posicion, Sprite sprite, Vector2 tamano, Color color)
        {
            var go = new GameObject("Pieza", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(mesa.transform, false);
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            var r = img.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = posicion;
            r.sizeDelta = tamano;
            var pieza = go.AddComponent<PiezaVolante>();
            pieza.imagen = img;
            return pieza;
        }

        public static Sprite Disco()
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
                    textura.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01((1f - d) * 3f)));
                }
            }

            textura.Apply();
            disco = Sprite.Create(textura, new Rect(0f, 0f, lado, lado), new Vector2(0.5f, 0.5f), 100f);
            return disco;
        }

        static Sprite Cuadro()
        {
            if (cuadro != null)
                return cuadro;

            var textura = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            for (int y = 0; y < 4; y++)
                for (int x = 0; x < 4; x++)
                    textura.SetPixel(x, y, Color.white);
            textura.Apply();
            cuadro = Sprite.Create(textura, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 100f);
            return cuadro;
        }

        /// <summary>Un trocito que vuela, gira, crece o se encoge y se desvanece hasta desaparecer.</summary>
        class PiezaVolante : MonoBehaviour
        {
            public Image imagen;
            public Vector2 velocidad;
            public float gravedad;
            public float giro;
            public float crece;     // cuántas veces se agranda mientras vive
            public float vida = 1f;

            float edad;
            Vector2 tamanoInicial;
            float alfaInicial;

            void Start()
            {
                tamanoInicial = imagen.rectTransform.sizeDelta;
                alfaInicial = imagen.color.a;
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
                if (crece > 0f)
                    r.sizeDelta = tamanoInicial * (1f + crece * p);

                var color = imagen.color;
                color.a = alfaInicial * (1f - p * p);
                imagen.color = color;
            }
        }
    }
}
