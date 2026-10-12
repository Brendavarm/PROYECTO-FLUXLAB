using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FluxLab
{
    /// <summary>
    /// El nivel 3 (Jardín de Luna) completo, con su propio código: bienvenida, 3 rondas distintas,
    /// caldero que reacciona, pausa, Mishi y resultados. La mesa de Brenda solo lo activa cuando el nivel es el 3.
    /// </summary>
    public class Nivel3Juego : MonoBehaviour
    {
        public const int Nivel = 3;

        // ---------------- Medidas del escenario (fondo_nivel3.png, lienzo de 1920x1080) ----------------
        const float SueloMesa = -255f;      // donde pisan Magnus, el caldero y los frascos de la mesa
        const float AlturaRotura = -330f;   // dónde se rompe un frasco que cae de la mesa
        const float MesaIzquierda = -690f;
        const float MesaDerecha = 730f;

        // Dónde puede estar de pie Mishi
        static readonly MishiAnimado.Plataforma[] PlataformasMishi =
        {
            new MishiAnimado.Plataforma(480f, 600f, 116f),     // 0: repisa pequeña de la derecha
            new MishiAnimado.Plataforma(-230f, 620f, -255f),   // 1: la mesa
            new MishiAnimado.Plataforma(-390f, -340f, 164f),   // 2: repisa de la izquierda
            new MishiAnimado.Plataforma(780f, 800f, -55f)      // 3: estante de la derecha
        };

        // ---------------- Ingredientes ----------------
        struct Ing
        {
            public string sprite;
            public string nombre;
            public Color color;
        }

        static readonly IngredientKind[] Orden =
        {
            IngredientKind.Fuego, IngredientKind.Gota, IngredientKind.Polvo,
            IngredientKind.Hierba, IngredientKind.Cristal, IngredientKind.Lagrima
        };

        readonly Dictionary<IngredientKind, Ing> catalogo = new Dictionary<IngredientKind, Ing>();

        enum Resultado
        {
            Pendiente,
            Acierto,
            Fallo
        }

        // ---------------- Escena ----------------
        Transform raiz;
        RectTransform mundo;
        Nivel3Caldero caldero;
        Nivel3Magnus magnus;
        Nivel3Pergamino pergamino;
        MishiAnimado mishi;
        GrilloAnimado grillo;
        MariposaAnimada mariposa;
        readonly List<Nivel3Lampara> lamparas = new List<Nivel3Lampara>();
        RectTransform capa;           // lo que se mueve por encima del pergamino (Mishi, grillo, frascos)
        AudioSource musica;
        AudioSource burbujasSonido;
        GameObject rayaAudio;
        Image[] fondosHuecos;
        Image[] gemasHuecos;
        Text textoRonda;
        Text textoPuntos;
        Text textoToast;
        Image fogonazo;
        GameObject panelBienvenida;
        GameObject panelPausa;
        GameObject panelConfirmar;
        bool confirmando;
        GameObject botonPausa;
        AudioSource sfx;
        Coroutine toastCo;

        // ---------------- Partida ----------------
        int rondas;
        int largo;
        float segundos;
        readonly List<IngredientKind> receta = new List<IngredientKind>();
        int colocados;
        bool entradaAbierta;
        Resultado resultado;
        int puntos;
        int fallosTotales;
        int rondaActual;
        bool empezar;
        bool pausado;
        bool partidaEnCurso;
        bool memorizando;

        // =====================================================================================
        // Construcción
        // =====================================================================================

        void Awake()
        {
            Taller taller = NivelesTaller.De(Nivel);
            rondas = taller.rondas;
            largo = Mathf.Clamp(taller.receta, 1, 5);
            segundos = taller.segundos;

            LlenarCatalogo();
            Construir();
        }

        void Start()
        {
            StartCoroutine(Partida());
            StartCoroutine(Travesuras());
        }

        // ---- Los turnos de Mishi, el grillo y la mariposa ----
        // Casi todo el tiempo están descansando (en silencio). Cada tanto, uno de los tres hace su gracia
        // unos segundos y vuelve a descansar. Para cambiar el ritmo, se ajustan estos números.
        const float TurnoMinimo = 8f;       // segundos que dura la gracia de uno
        const float TurnoMaximo = 12f;
        const float CalmaMinima = 12f;      // segundos de calma entre una gracia y la siguiente
        const float CalmaMaxima = 20f;

        IEnumerator Travesuras()
        {
            Despertar(0, false);
            Despertar(1, false);
            Despertar(2, false);
            yield return new WaitForSeconds(Random.Range(6f, 10f));

            int anterior = -1;
            while (true)
            {
                // Solo mientras se juega: no en la bienvenida, ni memorizando (ahí mandan las distracciones de las rondas), ni en pausa
                yield return new WaitUntil(() => partidaEnCurso && !memorizando && !pausado);

                int quien;
                do { quien = Random.Range(0, 3); } while (quien == anterior);
                anterior = quien;

                Despertar(quien, true);
                float fin = Time.time + Random.Range(TurnoMinimo, TurnoMaximo);
                while (Time.time < fin && !memorizando)
                    yield return null;
                Despertar(quien, false);

                yield return new WaitForSeconds(Random.Range(CalmaMinima, CalmaMaxima));
            }
        }

        /// <summary>0 = Mishi, 1 = el grillo, 2 = la mariposa. activo = true les da su turno; false los manda a descansar.</summary>
        void Despertar(int quien, bool activo)
        {
            if (quien == 0 && mishi != null)
                mishi.Tranquila = !activo;
            else if (quien == 1 && grillo != null)
                grillo.tranquilo = !activo;
            else if (quien == 2 && mariposa != null)
                mariposa.Tranquila = !activo;
        }

        void LlenarCatalogo()
        {
            catalogo[IngredientKind.Fuego] = new Ing { sprite = "gem_red_circle", nombre = "Esencia", color = Nivel3UI.Hex("#e23b3b") };
            catalogo[IngredientKind.Gota] = new Ing { sprite = "gem_blue_triangle", nombre = "Gota", color = Nivel3UI.Hex("#3a78d8") };
            catalogo[IngredientKind.Polvo] = new Ing { sprite = "gem_yellow_star", nombre = "Polvo", color = Nivel3UI.Hex("#f0c43a") };
            catalogo[IngredientKind.Hierba] = new Ing { sprite = "gem_green_square", nombre = "Hierba", color = Nivel3UI.Hex("#3ea86a") };
            catalogo[IngredientKind.Cristal] = new Ing { sprite = "gem_purple_pentagon", nombre = "Cristal", color = Nivel3UI.Hex("#8a4ec4") };
            catalogo[IngredientKind.Lagrima] = new Ing { sprite = "gem_orange_crescent", nombre = "Lágrima", color = Nivel3UI.Hex("#f08a2a") };
        }

        void Construir()
        {
            if (FindAnyObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            var lienzo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = lienzo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var escalador = lienzo.GetComponent<CanvasScaler>();
            escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            escalador.referenceResolution = new Vector2(1920f, 1080f);
            escalador.matchWidthOrHeight = 0.5f;
            raiz = lienzo.transform;

            ConstruirFondo();

            // Todo lo del escenario va dentro de "Mundo", que se sacude cuando explota el caldero.
            mundo = Nivel3UI.Vacio(raiz, "Mundo", Vector2.zero, Vector2.zero);
            Nivel3UI.Estirar(mundo);

            Nivel3Luna.Crear(mundo, new Vector2(200f, 440f), 150f);
            ConstruirLamparas();
            magnus = Nivel3Magnus.Crear(mundo, -520f, SueloMesa);
            caldero = Nivel3Caldero.Crear(mundo, 0f, SueloMesa, 330f);
            ConstruirHuecosDelCaldero();

            pergamino = Nivel3Pergamino.Crear(raiz);

            // Esta capa va por encima del pergamino: así, cuando Mishi o el grillo cruzan, se les ve de verdad.
            capa = Nivel3UI.Vacio(raiz, "CapaDistracciones", Vector2.zero, Vector2.zero);
            Nivel3UI.Estirar(capa);
            PonerFrascosYMishi();

            // La mariposa vuela por el escenario (por encima del pergamino, para poder posarse en él) y se posa en estos sitios
            mariposa = MariposaAnimada.Crear(capa, new[]
            {
                new Vector2(caldero.Boca.x + 110f, caldero.Boca.y + 22f),   // borde del caldero
                new Vector2(590f, 116f),                                    // repisa pequeña de la derecha
                new Vector2(-300f, 164f),                                   // repisa de la izquierda
                new Vector2(220f, SueloMesa)                                // la mesa
            });

            ConstruirBandeja();
            ConstruirMarcadores();
            ConstruirMensajes();
            ConstruirPausa();
            ConstruirConfirmacion();
            ConstruirAudio();
            ConstruirBienvenida();
            ConstruirBotonAudio();
        }

        void ConstruirFondo()
        {
            // Primero el fondo del nivel; si falta, el de siempre sin gato.
            Texture2D textura = Nivel3UI.LeerTextura("Menu/fondo_nivel3.png");
            if (textura == null)
                textura = Nivel3UI.LeerTextura("Menu/fondo_sin_gato.png");

            var go = new GameObject("Fondo", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            go.transform.SetParent(raiz, false);
            var imagen = go.GetComponent<RawImage>();
            imagen.texture = textura;
            imagen.raycastTarget = false;
            Nivel3UI.Estirar(imagen.rectTransform);
        }

        void ConstruirLamparas()
        {
            // Las lámparas colgantes animadas se quitaron (la pantalla estaba muy cargada). Para volver a ponerlas:
            //   lamparas.Add(Nivel3Lampara.Crear(mundo, new Vector2(-242f, 446f), 1.05f));   // la de arriba
            //   lamparas.Add(Nivel3Lampara.Crear(mundo, new Vector2(757f, 204f), 1.05f));    // la de la derecha
            // Solo queda la luz suave que titila sobre la lámpara del suelo, que ya está pintada en el fondo.
            lamparas.Add(Nivel3Lampara.CrearSoloLuz(mundo, new Vector2(-630f, -139f), 190f));
        }

        /// <summary>Cada ronda tiene su color de luz: 0 dorada, 1 azul lunar, 2 violeta mágica.</summary>
        void CambiarColorLamparas(int modo)
        {
            foreach (var lampara in lamparas)
                lampara.CambiarColor(modo);
        }

        void ReaccionarLamparas(Color color, float fuerza)
        {
            foreach (var lampara in lamparas)
                lampara.Reaccionar(color, fuerza);
        }

        void ConstruirHuecosDelCaldero()
        {
            // Una fila de huecos sobre el caldero: cada ingrediente correcto se queda ahí.
            fondosHuecos = new Image[largo];
            gemasHuecos = new Image[largo];
            float y = caldero.Boca.y + 150f;
            float centro = (largo - 1) * 0.5f;
            for (int i = 0; i < largo; i++)
            {
                var x = (i - centro) * 108f;
                fondosHuecos[i] = Nivel3UI.Imagen(mundo, "HuecoCaldero" + i, FrascoQuebradizo.Disco(), new Vector2(x, y), new Vector2(92f, 92f));
                fondosHuecos[i].color = new Color(0.1f, 0.06f, 0.2f, 0.45f);
                gemasHuecos[i] = Nivel3UI.Imagen(fondosHuecos[i].transform, "Gema", Nivel3UI.Solido(), Vector2.zero, new Vector2(70f, 70f), true);
                gemasHuecos[i].enabled = false;
            }
        }

        void PonerFrascosYMishi()
        {
            var rojo = new Color(0.95f, 0.3f, 0.15f, 0.85f);
            var verde = new Color(0.4f, 0.9f, 0.25f, 0.85f);
            var azul = new Color(0.2f, 0.45f, 1f, 0.85f);
            var amarillo = new Color(1f, 0.8f, 0.2f, 0.85f);
            var morado = new Color(0.65f, 0.3f, 1f, 0.85f);
            var oscuro = new Color(0.8f, 0.25f, 0.9f, 0.85f);

            var frascos = FrascosMesa.Crear(capa, new[]
            {
                // en la mesa
                new FrascosMesa.Def(2, azul, -330f, SueloMesa, 140f, 0.037f),
                new FrascosMesa.Def(0, rojo, 300f, SueloMesa, 120f, 0.015f),
                new FrascosMesa.Def(1, verde, 420f, SueloMesa, 140f, 0.022f),
                new FrascosMesa.Def(3, amarillo, 545f, SueloMesa, 120f, 0.033f),
                // repisa pequeña de la derecha
                new FrascosMesa.Def(4, morado, 410f, 116f, 110f, 0.057f),
                // estante de la derecha
                new FrascosMesa.Def(7, oscuro, 905f, -55f, 105f, 0.010f)
            }, SueloMesa, AlturaRotura, MesaIzquierda, MesaDerecha);

            mishi = MishiAnimado.Crear(capa, PlataformasMishi);
            mishi.AlAterrizar = frascos.Aterrizo;   // si Mishi cae cerca de un frasco, se cae y ella maúlla

            // El grillo también se posa en el borde del caldero (su altura sale del dibujo del caldero)
            var plataformasGrillo = new[]
            {
                new MishiAnimado.Plataforma(-90f, 90f, caldero.Boca.y + 18f),   // 0: borde del caldero
                new MishiAnimado.Plataforma(-200f, 600f, SueloMesa),            // 1: la mesa
                new MishiAnimado.Plataforma(380f, 560f, 116f),                  // 2: repisa pequeña de la derecha
                new MishiAnimado.Plataforma(-520f, -340f, 164f),                // 3: repisa de la izquierda
                new MishiAnimado.Plataforma(760f, 880f, -55f)                   // 4: estante de la derecha
            };
            grillo = GrilloAnimado.Crear(capa, plataformasGrillo, mishi);
        }

        // ---- La bandeja de ingredientes ----
        // Con arte (bandeja.png) las 6 cartas ya vienen dibujadas dentro de la bandeja: se ponen encima botones
        // invisibles con la gema y el nombre. Estas medidas son de esa imagen (1760 x 291 píxeles).
        const float BandejaAncho = 1509f;
        const float BandejaAlto = 174f;
        const float CartaAncho = 206f;
        const float CartaAlto = 149f;
        const float CartaCentroY = 80.5f;      // desde el borde de arriba de la imagen
        static readonly float[] CartaCentroX = { 137f, 382f, 628f, 876f, 1123f, 1373f };

        // Sin arte: cartas lisas, cada una del color pastel de su ingrediente
        static readonly string[] PastelCarta = { "#f4a79c", "#9cc3f2", "#f6dc8c", "#a4e0a8", "#c8a6ee", "#f6c08a" };

        void ConstruirBandeja()
        {
            Sprite arteBandeja = Nivel3UI.SpriteUI("bandeja");
            float yBandeja = arteBandeja != null ? -440f : -470f;

            if (arteBandeja != null)
                ConstruirBandejaConCartas(arteBandeja, new Vector2(-90f, yBandeja));
            else
                ConstruirBandejaLisa();

            // Vaciar: botón ancho con una poción a la izquierda
            var vaciar = Nivel3UI.BotonConArte(raiz, "boton_ancho", "Vaciar", new Vector2(arteBandeja != null ? 790f : 745f, yBandeja),
                new Vector2(arteBandeja != null ? 252f : 270f, arteBandeja != null ? 95f : 105f), Vaciar, 34);
            vaciar.transform.GetChild(0).GetComponent<RectTransform>().anchoredPosition = new Vector2(26f, 0f);   // el texto, a la derecha del icono
            Nivel3UI.Icono(vaciar.transform, "pocion.png", new Vector2(-66f, 0f), 48f);
        }

        void ConstruirBandejaConCartas(Sprite arte, Vector2 centro)
        {
            const float ancho = 1352f;
            float k = ancho / BandejaAncho;   // 0,9: la bandeja se muestra casi a su tamaño
            Nivel3UI.Imagen(raiz, "Bandeja", arte, centro, new Vector2(ancho, BandejaAlto * k));

            for (int i = 0; i < Orden.Length; i++)
            {
                IngredientKind tipo = Orden[i];
                Ing info = catalogo[tipo];

                // Centro de esta carta dentro de la bandeja
                float cx = (CartaCentroX[i] - BandejaAncho * 0.5f) * k;
                float cy = (BandejaAlto * 0.5f - CartaCentroY) * k;
                Vector2 posicion = centro + new Vector2(cx, cy);

                // Un botón casi invisible del tamaño de la carta
                var carta = Nivel3UI.Imagen(raiz, "Carta_" + tipo, Nivel3UI.Solido(), posicion, new Vector2(CartaAncho * k, CartaAlto * k));
                carta.color = new Color(1f, 1f, 1f, 0.01f);
                carta.raycastTarget = true;
                var boton = carta.gameObject.AddComponent<Button>();
                boton.targetGraphic = carta;
                boton.transition = Selectable.Transition.None;
                boton.navigation = new Navigation { mode = Navigation.Mode.None };
                IngredientKind elegido = tipo;
                boton.onClick.AddListener(() => Elegir(elegido));
                carta.gameObject.AddComponent<HoverScale>().scale = 1.06f;

                // La gema en la parte de arriba, el nombre sobre el listón y el número en la esquina
                // La carta es lisa y de un solo color: la gema arriba y el nombre debajo, con letra clara y sombra suave
                Nivel3UI.Imagen(carta.transform, "Gema", Nivel3UI.Spr(info.sprite), new Vector2(0f, 16f), Vector2.one * 64f, true);
                var nombre = Nivel3UI.Texto(carta.transform, info.nombre, 25, Nivel3UI.Hex("#fff3da"), new Vector2(0f, -36f), new Vector2(170f, 34f));
                Nivel3UI.Contorno(nombre, new Color(0.1f, 0.05f, 0.03f, 0.75f), 1.6f);
                var numero = Nivel3UI.Texto(carta.transform, (i + 1).ToString(), 22, Nivel3UI.Hex("#fff3da"), new Vector2(-70f, 44f), new Vector2(34f, 28f));
                Nivel3UI.Contorno(numero, new Color(0.1f, 0.05f, 0.03f, 0.75f), 1.4f);
            }
        }

        void ConstruirBandejaLisa()
        {
            var bandeja = Nivel3UI.Imagen(raiz, "Bandeja", Nivel3UI.Spr("panel_beige"), new Vector2(-40f, -470f), new Vector2(1280f, 168f));
            bandeja.type = Image.Type.Sliced;
            bandeja.color = new Color(0.91f, 0.78f, 0.58f, 0.94f);

            const float anchoCarta = 176f;
            const float altoCarta = 142f;
            const float hueco = 14f;
            float total = Orden.Length * anchoCarta + (Orden.Length - 1) * hueco;
            float x = -total * 0.5f + anchoCarta * 0.5f - 70f;

            for (int i = 0; i < Orden.Length; i++)
            {
                IngredientKind tipo = Orden[i];
                Ing info = catalogo[tipo];

                var carta = Nivel3UI.Imagen(raiz, "Carta_" + tipo, Nivel3UI.Spr("panel_beige"), new Vector2(x, -468f), new Vector2(anchoCarta, altoCarta));
                carta.type = Image.Type.Sliced;
                carta.color = Color.Lerp(Color.white, Nivel3UI.Hex(PastelCarta[i]), 0.65f);
                carta.raycastTarget = true;
                var sombra = carta.gameObject.AddComponent<Shadow>();
                sombra.effectColor = new Color(0.28f, 0.16f, 0.08f, 0.28f);
                sombra.effectDistance = new Vector2(0f, -4f);

                var boton = carta.gameObject.AddComponent<Button>();
                boton.targetGraphic = carta;
                boton.navigation = new Navigation { mode = Navigation.Mode.None };
                IngredientKind elegido = tipo;
                boton.onClick.AddListener(() => Elegir(elegido));
                carta.gameObject.AddComponent<HoverScale>().scale = 1.05f;

                Nivel3UI.Imagen(carta.transform, "Gema", Nivel3UI.Spr(info.sprite), new Vector2(0f, 24f), Vector2.one * 64f, true);
                var banda = Nivel3UI.Imagen(carta.transform, "Banda", Nivel3UI.Solido(), new Vector2(0f, -48f), new Vector2(anchoCarta - 18f, 40f));
                banda.color = info.color;
                Nivel3UI.Texto(carta.transform, info.nombre, 26, Color.white, new Vector2(0f, -48f), new Vector2(anchoCarta - 16f, 38f), TextAnchor.MiddleCenter, true);
                Nivel3UI.Texto(carta.transform, (i + 1).ToString(), 26, Nivel3UI.Hex("#6a4b32"), new Vector2(-62f, 48f), new Vector2(40f, 32f));

                x += anchoCarta + hueco;
            }
        }

        void ConstruirMarcadores()
        {
            // Sobre madera oscura el texto va claro; sobre el panel beige de siempre, oscuro
            bool madera = Nivel3UI.SpriteUI("placa_titulo") != null;
            Color texto = madera ? Nivel3UI.Hex("#f3e4c4") : Nivel3UI.Hex("#5a3b24");
            Color textoPuntos2 = madera ? Nivel3UI.Hex("#e6c98b") : Nivel3UI.Hex("#8a5a20");
            Color sombraTexto = new Color(0.1f, 0.05f, 0.03f, 0.7f);

            // Arriba a la izquierda: el nombre del nivel y el botón del mapa
            var placaNivel = Nivel3UI.Placa(raiz, "placa_titulo", new Vector2(-690f, 398f), madera ? new Vector2(460f, 137f) : new Vector2(450f, 108f));
            var nombreNivel = Nivel3UI.Texto(placaNivel.transform, "Jardín de Luna", madera ? 40 : 44, texto, Vector2.zero, new Vector2(330f, 90f));
            if (madera)
                Nivel3UI.Contorno(nombreNivel, sombraTexto, 1.6f);

            var mapa = Nivel3UI.BotonConArte(raiz, "boton_ancho", "Mapa", new Vector2(-790f, 282f), madera ? new Vector2(210f, 79f) : new Vector2(200f, 70f), PedirConfirmacionMapa, 29);
            mapa.transform.GetChild(0).GetComponent<RectTransform>().anchoredPosition = new Vector2(22f, 0f);
            Nivel3UI.Icono(mapa.transform, "puerta.png", new Vector2(-52f, 0f), 36f);

            // Arriba a la derecha: ronda y puntos, y los botones de pausa y de sonido
            var placaRonda = Nivel3UI.Placa(raiz, "placa_ronda", new Vector2(640f, 398f), madera ? new Vector2(330f, 125f) : new Vector2(350f, 116f));
            textoRonda = Nivel3UI.Texto(placaRonda.transform, "Ronda 1 / " + rondas, madera ? 36 : 40, texto, new Vector2(0f, 17f), new Vector2(260f, 44f));
            textoPuntos = Nivel3UI.Texto(placaRonda.transform, "Puntos: 0", madera ? 28 : 30, textoPuntos2, new Vector2(0f, -22f), new Vector2(260f, 36f));
            if (madera)
            {
                Nivel3UI.Contorno(textoRonda, sombraTexto, 1.5f);
                Nivel3UI.Contorno(textoPuntos, sombraTexto, 1.4f);
            }

            var pausa = Nivel3UI.BotonConArte(raiz, "boton_cuadrado", null, new Vector2(866f, 398f), new Vector2(112f, 110f), AlternarPausa);
            Nivel3UI.Icono(pausa.transform, "engranaje.png", Vector2.zero, 56f);
            botonPausa = pausa.gameObject;
            botonPausa.SetActive(false); // aparece cuando empieza el juego
        }

        void ConstruirMensajes()
        {
            textoToast = Nivel3UI.Texto(raiz, "", 62, Color.white, new Vector2(0f, 300f), new Vector2(1500f, 200f), TextAnchor.MiddleCenter, true);
            textoToast.color = new Color(1f, 1f, 1f, 0f);

            fogonazo = Nivel3UI.Imagen(raiz, "Fogonazo", Nivel3UI.Solido(), Vector2.zero, Vector2.zero);
            Nivel3UI.Estirar(fogonazo.rectTransform);
            fogonazo.color = new Color(1f, 0.55f, 0.2f, 0f);
        }

        void ConstruirPausa()
        {
            panelPausa = new GameObject("Pausa", typeof(RectTransform));
            panelPausa.transform.SetParent(raiz, false);
            Nivel3UI.Estirar(panelPausa.GetComponent<RectTransform>());

            var oscuro = Nivel3UI.Imagen(panelPausa.transform, "Oscuro", Nivel3UI.Solido(), Vector2.zero, Vector2.zero);
            Nivel3UI.Estirar(oscuro.rectTransform);
            oscuro.color = new Color(0.05f, 0.02f, 0.12f, 0.7f);
            oscuro.raycastTarget = true; // para que no se pueda pulsar nada de atrás

            var panel = Nivel3UI.Panel(panelPausa.transform, "Panel", Vector2.zero, new Vector2(700f, 700f));
            Nivel3UI.Texto(panel.transform, Nivel3Textos.PausaTitulo, 80, Nivel3UI.Hex("#5a3b24"), new Vector2(0f, 250f), new Vector2(600f, 100f));
            Nivel3UI.Boton(panel.transform, "Reanudar", new Vector2(0f, 90f), new Vector2(470f, 108f), AlternarPausa, 44);
            Nivel3UI.Boton(panel.transform, "Reiniciar", new Vector2(0f, -50f), new Vector2(470f, 108f), Reiniciar, 44);
            Nivel3UI.Boton(panel.transform, "Salir", new Vector2(0f, -190f), new Vector2(470f, 108f), Salir, 44);
            panelPausa.SetActive(false);
        }

        void ConstruirConfirmacion()
        {
            panelConfirmar = new GameObject("ConfirmarMapa", typeof(RectTransform));
            panelConfirmar.transform.SetParent(raiz, false);
            Nivel3UI.Estirar(panelConfirmar.GetComponent<RectTransform>());

            var oscuro = Nivel3UI.Imagen(panelConfirmar.transform, "Oscuro", Nivel3UI.Solido(), Vector2.zero, Vector2.zero);
            Nivel3UI.Estirar(oscuro.rectTransform);
            oscuro.color = new Color(0.05f, 0.02f, 0.12f, 0.75f);
            oscuro.raycastTarget = true;

            var panel = Nivel3UI.Panel(panelConfirmar.transform, "Panel", Vector2.zero, new Vector2(860f, 520f));
            var tinta = Nivel3UI.Hex("#5a3b24");
            Nivel3UI.Texto(panel.transform, Nivel3Textos.ConfirmarTitulo, 64, tinta, new Vector2(0f, 150f), new Vector2(780f, 90f));
            Nivel3UI.Texto(panel.transform, Nivel3Textos.ConfirmarTexto, 36, Nivel3UI.Hex("#6a4b32"), new Vector2(0f, 40f), new Vector2(740f, 130f));
            Nivel3UI.Boton(panel.transform, Nivel3Textos.ConfirmarSeguir, new Vector2(-200f, -150f), new Vector2(360f, 100f), CerrarConfirmacion, 34);
            Nivel3UI.Boton(panel.transform, Nivel3Textos.ConfirmarSalir, new Vector2(200f, -150f), new Vector2(360f, 100f), Salir, 34);
            panelConfirmar.SetActive(false);
        }

        /// <summary>El botón Mapa no saca al jugador de golpe: primero le pregunta, con el juego en pausa.</summary>
        void PedirConfirmacionMapa()
        {
            if (!partidaEnCurso || pausado || confirmando)
                return;

            confirmando = true;
            pausado = true;
            panelConfirmar.SetActive(true);
            Time.timeScale = 0f;
            AudioListener.pause = true;
        }

        void CerrarConfirmacion()
        {
            if (!confirmando)
                return;

            confirmando = false;
            pausado = false;
            panelConfirmar.SetActive(false);
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }

        void ConstruirBienvenida()
        {
            panelBienvenida = new GameObject("Bienvenida", typeof(RectTransform));
            panelBienvenida.transform.SetParent(raiz, false);
            Nivel3UI.Estirar(panelBienvenida.GetComponent<RectTransform>());

            var oscuro = Nivel3UI.Imagen(panelBienvenida.transform, "Oscuro", Nivel3UI.Solido(), Vector2.zero, Vector2.zero);
            Nivel3UI.Estirar(oscuro.rectTransform);
            oscuro.color = new Color(0.05f, 0.03f, 0.16f, 0.82f);
            oscuro.raycastTarget = true;

            var tinta = Nivel3UI.Hex("#5a3b24");
            var panel = Nivel3UI.Panel(panelBienvenida.transform, "Tarjeta", new Vector2(0f, -10f), new Vector2(1240f, 880f));

            Nivel3UI.Texto(panel.transform, Nivel3Textos.BienvenidaTitulo, 58, tinta, new Vector2(0f, 340f), new Vector2(1100f, 80f));
            var nivel = Nivel3UI.Texto(panel.transform, Nivel3Textos.BienvenidaNivel, 110, Nivel3UI.Hex("#d98c1f"), new Vector2(0f, 230f), new Vector2(1100f, 130f), TextAnchor.MiddleCenter, true);
            nivel.color = Nivel3UI.Hex("#ffd86b");
            Nivel3UI.Texto(panel.transform, Nivel3Textos.BienvenidaFrase, 34, Nivel3UI.Hex("#6a4b32"), new Vector2(0f, 120f), new Vector2(1000f, 90f));

            float y = 10f;
            foreach (string linea in Nivel3Textos.Instrucciones)
            {
                Nivel3UI.Texto(panel.transform, linea, 31, tinta, new Vector2(0f, y), new Vector2(1060f, 76f), TextAnchor.MiddleLeft);
                y -= 76f;
            }

            Nivel3UI.Boton(panel.transform, Nivel3Textos.BotonComenzar, new Vector2(0f, -330f), new Vector2(620f, 124f), Comenzar, 48);

            StartCoroutine(AnimarBienvenida(panel.rectTransform, nivel.rectTransform));
        }

        void ConstruirAudio()
        {
            sfx = gameObject.AddComponent<AudioSource>();
            sfx.playOnAwake = false;
            musica = Bucle("ambiente");
            burbujasSonido = Bucle("burbujas");
            AplicarAudio();
        }

        AudioSource Bucle(string nombre)
        {
            var clip = Resources.Load<AudioClip>("FluxLab/Audio/" + nombre);
            if (clip == null)
                return null;
            var fuente = gameObject.AddComponent<AudioSource>();
            fuente.clip = clip;
            fuente.loop = true;
            fuente.playOnAwake = false;
            fuente.Play();
            return fuente;
        }

        /// <summary>Pone el volumen de la música y los efectos según lo que el jugador tenga activado.</summary>
        void AplicarAudio()
        {
            if (musica != null)
                musica.volume = AjustesJuego.MusicaActiva ? 0.28f : 0f;
            if (burbujasSonido != null)
                burbujasSonido.volume = AjustesJuego.EfectosActivos ? 0.12f : 0f;
            if (rayaAudio != null)
                rayaAudio.SetActive(!AjustesJuego.MusicaActiva && !AjustesJuego.EfectosActivos);
        }

        /// <summary>Botón de audio: apaga todo el sonido (música y efectos), o lo vuelve a encender si estaba apagado.</summary>
        void AlternarAudio()
        {
            bool algoSuena = AjustesJuego.MusicaActiva || AjustesJuego.EfectosActivos;
            AjustesJuego.MusicaActiva = !algoSuena;
            AjustesJuego.EfectosActivos = !algoSuena;
            AplicarAudio();
            Sonar("click", 0.6f);
        }

        void ConstruirBotonAudio()
        {
            // Debajo del engranaje; se construye al final para quedar por encima de la bienvenida y la pausa.
            var boton = Nivel3UI.BotonConArte(raiz, "boton_cuadrado", null, new Vector2(866f, 288f), new Vector2(112f, 110f), AlternarAudio);
            boton.gameObject.name = "BotonAudio";
            Nivel3UI.Icono(boton.transform, "sonido.png", Vector2.zero, 56f);

            // Una raya roja tachando el altavoz cuando todo está en silencio
            var raya = Nivel3UI.Imagen(boton.transform, "Raya", Nivel3UI.Solido(), Vector2.zero, new Vector2(84f, 9f));
            raya.color = new Color(0.9f, 0.2f, 0.2f, 1f);
            raya.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            rayaAudio = raya.gameObject;
            AplicarAudio();
        }

        void Sonar(string nombre, float volumen = 0.85f)
        {
            if (!AjustesJuego.EfectosActivos)
                return;
            var clip = Resources.Load<AudioClip>("FluxLab/Audio/" + nombre);
            if (clip != null)
                sfx.PlayOneShot(clip, volumen);
        }

        // =====================================================================================
        // Control de la partida
        // =====================================================================================

        void Update()
        {
            if (!partidaEnCurso)
                return;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (confirmando)
                    CerrarConfirmacion();
                else
                    AlternarPausa();
            }

            if (!entradaAbierta || pausado)
                return;

            for (int i = 0; i < Orden.Length; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i))
                    Elegir(Orden[i]);
            }

            if (Input.GetKeyDown(KeyCode.Backspace))
                Vaciar();
        }

        void OnDestroy()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }

        IEnumerator Partida()
        {
            yield return new WaitUntil(() => empezar);
            yield return DesvanecerBienvenida();

            partidaEnCurso = true;
            botonPausa.SetActive(true);

            for (int r = 1; r <= rondas; r++)
            {
                rondaActual = r;
                ActualizarMarcadores();
                yield return IntroRonda(r);
                yield return JugarRonda(r);
            }

            partidaEnCurso = false;
            botonPausa.SetActive(false);
            yield return Final();
        }

        IEnumerator IntroRonda(int ronda)
        {
            int i = ModoDe(ronda) - 1;
            Mensaje("<size=70>" + Nivel3Textos.TituloRonda[i] + "</size>\n<size=40>" + Nivel3Textos.SubtituloRonda[i] + "</size>",
                Nivel3UI.Hex("#ffd86b"), 2.6f);
            yield return new WaitForSeconds(2.7f);
        }

        /// <summary>Qué tipo de ronda toca: 1 completa, 2 uno por uno, 3 solo sombras.</summary>
        static int ModoDe(int ronda)
        {
            return ((ronda - 1) % 3) + 1;
        }

        IEnumerator JugarRonda(int ronda)
        {
            SortearReceta();
            CambiarColorLamparas(ModoDe(ronda) - 1);
            int fallosRonda = 0;

            while (true)
            {
                yield return MostrarReceta(ronda);

                resultado = Resultado.Pendiente;
                colocados = 0;
                LimpiarHuecos();
                entradaAbierta = true;
                Mensaje(Nivel3Textos.TuTurno, Color.white, 2.2f);

                while (resultado == Resultado.Pendiente)
                    yield return null;
                entradaAbierta = false;

                if (resultado == Resultado.Acierto)
                {
                    puntos += Mathf.Max(25, 100 - 25 * fallosRonda);
                    ActualizarMarcadores();
                    caldero.Triunfo();
                    ReaccionarLamparas(new Color(1f, 0.85f, 0.3f), 1f);
                    mariposa.Atraer(caldero.Boca + new Vector2(0f, 90f), true);   // de premio
                    magnus.Poner(Nivel3Magnus.Animo.Festeja, 0.72f);
                    mishi.Celebrar();
                    Sonar("acierto");
                    Sonar("hechizo", 0.5f);
                    Mensaje(Nivel3Textos.Elegir(Nivel3Textos.RondaCompleta), Nivel3UI.Hex("#9dff8a"), 2.2f);
                    yield return new WaitForSeconds(2.4f);
                    LimpiarHuecos();
                    yield break;
                }

                // Se equivocó: el caldero explota y la ronda empieza de nuevo
                fallosRonda++;
                fallosTotales++;
                caldero.Explotar();
                foreach (var lampara in lamparas)
                    lampara.Sacudir();
                StartCoroutine(Fogonazo());
                StartCoroutine(Sacudir(0.55f, 14f));
                magnus.Poner(Nivel3Magnus.Animo.SeEquivoca, 0.55f);
                mishi.Maullar();
                Sonar("error");
                Mensaje(Nivel3Textos.Elegir(Nivel3Textos.Error), Nivel3UI.Hex("#ff8a66"), 2.4f);
                LimpiarHuecos();
                yield return new WaitForSeconds(2.6f);
            }
        }

        IEnumerator MostrarReceta(int ronda)
        {
            int modo = ModoDe(ronda);
            pergamino.Preparar(largo);
            pergamino.PonerTitulo("La receta");
            pergamino.PonerIndicacion(Nivel3Textos.IndicacionPergamino[modo - 1]);

            // Rondas 1 y 3 enseñan todo de golpe (la 3, solo en sombras); la 2 los va enseñando de uno en uno
            if (modo != 2)
            {
                for (int i = 0; i < largo; i++)
                    pergamino.MostrarGema(i, Nivel3UI.Spr(catalogo[receta[i]].sprite), catalogo[receta[i]].nombre, modo == 3);
            }

            pergamino.Enrollar();
            magnus.Poner(Nivel3Magnus.Animo.Observa, 0f);
            memorizando = true;
            yield return pergamino.Abrir();

            // Mientras memorizas hay una distracción distinta en cada ronda:
            //   ronda 1: la mariposa se posa sobre el pergamino   ronda 2: el grillo salta de un lado a otro
            //   ronda 3: Mishi lanza varias bolas mágicas seguidas
            if (modo == 1)
                StartCoroutine(Distraccion(0.3f, 0));
            else if (modo == 2)
                StartCoroutine(Distraccion(0.4f, 1));
            else
                StartCoroutine(Distraccion(0.4f, 2));

            if (modo != 2)
            {
                float restante = segundos;
                while (restante > 0f)
                {
                    restante -= Time.deltaTime;
                    pergamino.PonerTemporizador("Recuerda: " + Mathf.Max(0f, restante).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + "s",
                        Mathf.Clamp01(restante / segundos));
                    yield return null;
                }
            }
            else
            {
                for (int i = 0; i < largo; i++)
                {
                    pergamino.PonerTemporizador("Ingrediente " + (i + 1) + " de " + largo, -1f);
                    pergamino.MostrarGema(i, Nivel3UI.Spr(catalogo[receta[i]].sprite), catalogo[receta[i]].nombre, false);
                    Sonar("click", 0.5f);
                    yield return new WaitForSeconds(1.1f);
                    pergamino.OcultarGema(i);
                    yield return new WaitForSeconds(0.35f);
                }
            }

            memorizando = false;
            pergamino.PonerTemporizador("", -1f);
            if (mariposa != null)
                mariposa.TerminarVisita();      // si estaba posada sobre el pergamino, se va antes de que se cierre
            yield return pergamino.Cerrar();
            pergamino.OcultarTodas();
            magnus.Poner(Nivel3Magnus.Animo.Quieto, 0f);
        }

        /// <param name="quien">0 = la mariposa, 1 = el grillo, 2 = Mishi</param>
        IEnumerator Distraccion(float espera, int quien)
        {
            yield return new WaitForSeconds(espera);
            if (!memorizando)
                yield break;

            if (quien == 0 && mariposa != null)
            {
                // Se posa sobre la receta, a la izquierda o a la derecha del centro, tapando parte
                float lado = Random.value < 0.5f ? -1f : 1f;
                mariposa.VisitarPergamino(new Vector2(20f + lado * 95f, 322f), 2.6f);
            }
            else if (quien == 1 && grillo != null)
            {
                grillo.Distraer(3);
            }
            else if (quien == 2 && mishi != null)
            {
                mishi.DistraerVarias(3);
            }
        }

        void SortearReceta()
        {
            var bolsa = new List<IngredientKind>(Orden);
            for (int i = 0; i < bolsa.Count; i++)
            {
                int otro = Random.Range(i, bolsa.Count);
                IngredientKind tmp = bolsa[i];
                bolsa[i] = bolsa[otro];
                bolsa[otro] = tmp;
            }

            receta.Clear();
            receta.AddRange(bolsa.GetRange(0, largo));
        }

        // =====================================================================================
        // Lo que hace el jugador
        // =====================================================================================

        void Elegir(IngredientKind tipo)
        {
            if (!entradaAbierta || pausado)
                return;

            Ing info = catalogo[tipo];

            if (tipo != receta[colocados])
            {
                // Ingrediente equivocado: se acabó la mezcla
                entradaAbierta = false;
                resultado = Resultado.Fallo;
                return;
            }

            // Ingrediente correcto: entra al caldero y lo ilumina
            gemasHuecos[colocados].sprite = Nivel3UI.Spr(info.sprite);
            gemasHuecos[colocados].color = Color.white;
            gemasHuecos[colocados].enabled = true;
            StartCoroutine(Aparecer(gemasHuecos[colocados].rectTransform));
            colocados++;

            caldero.Hechizo(info.color);
            ReaccionarLamparas(info.color, 0.55f);
            mariposa.Atraer(caldero.Boca + new Vector2(0f, 90f));
            magnus.Poner(Nivel3Magnus.Animo.Remueve, 0.46f);
            Sonar("click");
            Sonar("hechizo", 0.3f);

            if (colocados >= largo)
            {
                entradaAbierta = false;
                resultado = Resultado.Acierto;
            }
            else
            {
                Mensaje(Nivel3Textos.Elegir(Nivel3Textos.AciertoIngrediente), Nivel3UI.Hex("#9dff8a"), 1.1f);
            }
        }

        void Vaciar()
        {
            if (!entradaAbierta || pausado || colocados == 0)
                return;

            colocados = 0;
            LimpiarHuecos();
            Sonar("click");
            Mensaje(Nivel3Textos.CalderoLimpio, Color.white, 1.8f);
        }

        void LimpiarHuecos()
        {
            for (int i = 0; i < gemasHuecos.Length; i++)
                gemasHuecos[i].enabled = false;
        }

        // =====================================================================================
        // Pausa
        // =====================================================================================

        void AlternarPausa()
        {
            if (!partidaEnCurso || confirmando)
                return;

            pausado = !pausado;
            panelPausa.SetActive(pausado);
            Time.timeScale = pausado ? 0f : 1f;
            AudioListener.pause = pausado;
        }

        void Reiniciar()
        {
            SoltarTiempo();
            SceneManager.LoadScene("MesaDeAlquimia"); // vuelve a entrar al nivel 3
        }

        void Salir()
        {
            SoltarTiempo();
            SceneManager.LoadScene("MapaNiveles");
        }

        static void SoltarTiempo()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }

        // =====================================================================================
        // Bienvenida y final
        // =====================================================================================

        void Comenzar()
        {
            if (empezar)
                return;
            Sonar("click");
            Sonar("hechizo", 0.6f);
            empezar = true;
        }

        IEnumerator AnimarBienvenida(RectTransform tarjeta, RectTransform nombreNivel)
        {
            // La tarjeta aparece con un rebote suave
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / 0.5f;
                float k = Mathf.Clamp01(t);
                float rebote = 1f + 0.12f * Mathf.Sin(k * Mathf.PI) * (1f - k);
                tarjeta.localScale = Vector3.one * (Mathf.SmoothStep(0.6f, 1f, k) * rebote);
                yield return null;
            }

            tarjeta.localScale = Vector3.one;

            // Y mientras espera: el nombre del nivel respira y suben estrellitas mágicas
            float proxima = 0f;
            while (!empezar)
            {
                nombreNivel.localScale = Vector3.one * (1f + 0.04f * Mathf.Sin(Time.time * 2.4f));
                proxima -= Time.deltaTime;
                if (proxima <= 0f)
                {
                    proxima = 0.09f;
                    var estrella = Particula.Crear(panelBienvenida.transform, FrascoBrillante.Estrella(),
                        new Vector2(Random.Range(-900f, 900f), Random.Range(-560f, -380f)), Vector2.one * Random.Range(26f, 56f),
                        Color.Lerp(new Color(1f, 0.9f, 0.5f), new Color(0.7f, 0.9f, 1f), Random.value),
                        new Vector2(Random.Range(-30f, 30f), Random.Range(110f, 220f)), 0f, Random.Range(2.5f, 4f), Random.Range(-60f, 60f), 0.2f);
                    estrella.transform.SetSiblingIndex(1); // detrás de la tarjeta, delante del fondo oscuro
                }

                yield return null;
            }
        }

        IEnumerator DesvanecerBienvenida()
        {
            var grupo = panelBienvenida.AddComponent<CanvasGroup>();
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / 0.45f;
                grupo.alpha = 1f - Mathf.Clamp01(t);
                panelBienvenida.transform.localScale = Vector3.one * (1f + 0.08f * Mathf.Clamp01(t));
                yield return null;
            }

            Destroy(panelBienvenida);
        }

        IEnumerator Final()
        {
            int estrellas = fallosTotales == 0 ? 3 : fallosTotales <= 2 ? 2 : 1;
            AjustesJuego.RegistrarPuntaje(puntos, rondas);

            bool desbloqueo = false;
            if (AjustesJuego.NivelSeleccionado >= AjustesJuego.NivelAlcanzado && AjustesJuego.NivelAlcanzado < 5)
            {
                AjustesJuego.AvanzarNivel();
                desbloqueo = true;
            }

            yield return new WaitForSeconds(0.4f);
            yield return Resultados(estrellas, desbloqueo);
        }

        IEnumerator Resultados(int estrellas, bool desbloqueo)
        {
            var panelRaiz = new GameObject("Resultados", typeof(RectTransform));
            panelRaiz.transform.SetParent(raiz, false);
            Nivel3UI.Estirar(panelRaiz.GetComponent<RectTransform>());

            var oscuro = Nivel3UI.Imagen(panelRaiz.transform, "Oscuro", Nivel3UI.Solido(), Vector2.zero, Vector2.zero);
            Nivel3UI.Estirar(oscuro.rectTransform);
            oscuro.color = new Color(0.05f, 0.03f, 0.16f, 0.82f);
            oscuro.raycastTarget = true;

            var tinta = Nivel3UI.Hex("#5a3b24");
            var panel = Nivel3UI.Panel(panelRaiz.transform, "Tarjeta", Vector2.zero, new Vector2(1240f, 920f));

            var titulo = Nivel3UI.Texto(panel.transform, Nivel3Textos.ResultadoTitulo, 84, Nivel3UI.Hex("#ffd86b"), new Vector2(0f, 370f), new Vector2(1100f, 110f), TextAnchor.MiddleCenter, true);
            titulo.color = Nivel3UI.Hex("#ffd86b");

            // Estrellas
            var imagenesEstrella = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                imagenesEstrella[i] = Nivel3UI.Imagen(panel.transform, "Estrella" + i, Nivel3UI.Spr("gem_yellow_star"), new Vector2((i - 1) * 190f, 215f), new Vector2(160f, 160f), true);
                imagenesEstrella[i].color = new Color(0.4f, 0.35f, 0.32f, 0.55f);
            }

            Nivel3UI.Texto(panel.transform, "Puntos: " + puntos, 52, tinta, new Vector2(0f, 75f), new Vector2(900f, 70f));
            Nivel3UI.Texto(panel.transform, "Rondas superadas: " + rondas + " / " + rondas + "      Errores: " + fallosTotales, 38,
                Nivel3UI.Hex("#6a4b32"), new Vector2(0f, 10f), new Vector2(1000f, 56f));

            if (desbloqueo)
            {
                Nivel3UI.Texto(panel.transform, Nivel3Textos.ResultadoDesbloqueo, 44, Nivel3UI.Hex("#2f7a4a"), new Vector2(0f, -90f), new Vector2(1100f, 60f));
                var siguiente = Nivel3UI.Texto(panel.transform, NivelesTaller.De(Nivel + 1).nombre, 76, Nivel3UI.Hex("#d98c1f"), new Vector2(0f, -170f), new Vector2(1100f, 100f), TextAnchor.MiddleCenter, true);
                siguiente.color = Nivel3UI.Hex("#ffd86b");
            }
            else
            {
                Nivel3UI.Texto(panel.transform, "¡Otra poción perfecta para tu colección!", 44, Nivel3UI.Hex("#2f7a4a"), new Vector2(0f, -130f), new Vector2(1100f, 70f));
            }

            Nivel3UI.Boton(panel.transform, "Continuar", new Vector2(190f, -330f), new Vector2(500f, 124f), Salir, 52);
            Nivel3UI.Boton(panel.transform, "Repetir", new Vector2(-340f, -330f), new Vector2(340f, 104f), Reiniciar, 42);

            // Las estrellas se encienden una por una
            Sonar("hechizo", 0.6f);
            for (int i = 0; i < estrellas; i++)
            {
                yield return new WaitForSeconds(0.5f);
                imagenesEstrella[i].color = Color.white;
                StartCoroutine(Aparecer(imagenesEstrella[i].rectTransform));
                Sonar("acierto", 0.7f);
                for (int c = 0; c < 10; c++)
                {
                    Particula.Crear(panel.transform, FrascoBrillante.Estrella(), imagenesEstrella[i].rectTransform.anchoredPosition,
                        Vector2.one * Random.Range(24f, 46f), new Color(1f, 0.92f, 0.5f), new Vector2(Random.Range(-260f, 260f), Random.Range(80f, 360f)),
                        420f, Random.Range(0.6f, 1f), Random.Range(-300f, 300f), 0.1f);
                }
            }

            // Lluvia de estrellitas mientras decide
            while (panelRaiz != null)
            {
                Particula.Crear(panelRaiz.transform, FrascoBrillante.Estrella(), new Vector2(Random.Range(-950f, 950f), 600f),
                    Vector2.one * Random.Range(22f, 44f), Color.Lerp(new Color(1f, 0.9f, 0.5f), new Color(0.7f, 0.9f, 1f), Random.value),
                    new Vector2(Random.Range(-30f, 30f), Random.Range(-160f, -300f)), 0f, Random.Range(3f, 4.5f), Random.Range(-90f, 90f), 0.5f)
                    .transform.SetSiblingIndex(1);
                yield return new WaitForSeconds(0.14f);
            }
        }

        // =====================================================================================
        // Efectos de pantalla
        // =====================================================================================

        void ActualizarMarcadores()
        {
            textoRonda.text = "Ronda " + rondaActual + " / " + rondas;
            textoPuntos.text = "Puntos: " + puntos;
        }

        void Mensaje(string texto, Color color, float segundosVisible)
        {
            if (toastCo != null)
                StopCoroutine(toastCo);
            toastCo = StartCoroutine(MensajeCo(texto, color, segundosVisible));
        }

        IEnumerator MensajeCo(string texto, Color color, float segundosVisible)
        {
            textoToast.text = texto;
            var rect = textoToast.rectTransform;

            // Entra creciendo
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / 0.18f;
                float k = Mathf.Clamp01(t);
                rect.localScale = Vector3.one * Mathf.Lerp(0.6f, 1f, k);
                textoToast.color = new Color(color.r, color.g, color.b, k);
                yield return null;
            }

            rect.localScale = Vector3.one;
            textoToast.color = color;
            yield return new WaitForSeconds(Mathf.Max(0.1f, segundosVisible - 0.55f));

            // Y se desvanece
            t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / 0.35f;
                textoToast.color = new Color(color.r, color.g, color.b, 1f - Mathf.Clamp01(t));
                yield return null;
            }

            textoToast.color = new Color(color.r, color.g, color.b, 0f);
        }

        IEnumerator Fogonazo()
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / 0.45f;
                float resto = 1f - Mathf.Clamp01(t);
                fogonazo.color = new Color(1f, 0.55f, 0.2f, 0.5f * resto * resto);
                yield return null;
            }

            fogonazo.color = new Color(1f, 0.55f, 0.2f, 0f);
        }

        IEnumerator Sacudir(float duracion, float fuerza)
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / duracion;
                float resto = 1f - Mathf.Clamp01(t);
                Vector2 temblor = Random.insideUnitCircle * (fuerza * resto);
                mundo.anchoredPosition = temblor;
                capa.anchoredPosition = temblor;
                yield return null;
            }

            mundo.anchoredPosition = Vector2.zero;
            capa.anchoredPosition = Vector2.zero;
        }

        IEnumerator Aparecer(RectTransform rect)
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / 0.22f;
                rect.localScale = Vector3.one * Mathf.Lerp(0.4f, 1f, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t)));
                yield return null;
            }

            rect.localScale = Vector3.one;
        }
    }
}
